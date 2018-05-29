import os
import torch
from torch.autograd import Variable
import itertools
from pathlib import Path


# divide the data into batches
def batchify(data, batch_size):
    # Work out how cleanly we can divide the dataset into batch_size parts (i.e. continuous seqs).
    nbatch = data.size(0) // batch_size
    # Trim off any extra elements that wouldn't cleanly fit (remainders).
    data = data.narrow(0, 0, nbatch * batch_size)
    # Evenly divide the data across the batch_size batches.
    data = data.view(batch_size, -1).t().contiguous()

    return data


# get the ith batch (both input and output) and transform it into backpropagatable Variable
def get_batch(source, i, args, evaluation=False):
    seq_len = min(args.seq_len, len(source) - 1 - i)

    data = source[i:i+seq_len].long()

    # get the output, it has to ofsetted by 1
    target = source[i+1:i+1+seq_len].view(-1).long()

    # transform into Variable and maybe push into GPU
    data = Variable(data.cuda() if args.cuda else data, volatile=evaluation)
    target = Variable(target.cuda() if args.cuda else target)

    return data, target


# get the ith batch (only continuous output) and transform it into backpropagatable Variable
def get_target_float_batch(source, i, args):
    seq_len = min(args.seq_len, len(source) - 1 - i)
    target = source[i+1:i+1+seq_len].view(-1)
    target = Variable(target.cuda() if args.cuda else target)
    return target


# get the ith batch (only input) and transform it into backpropagatable Variable
def get_batch_without_target(source, i, args, evaluation=False):
    seq_len = min(args.seq_len, len(source) - 1 - i)
    data = source[i:i+seq_len].long()
    data = Variable(data.cuda() if args.cuda else data, volatile=evaluation)
    return data


# serialize and save the model
def save(model, typ, loss, args):
    save_filename = '{:}-model.loss_{:.5f}.pt'.format(typ, loss)
    torch.save(model, save_filename)
    print('Saved as %s' % save_filename)


# class used for loading the dataset and transforming it into tensors
class Loader:
    num_clusters = 11 # num of intrument clusters
    cluster_range = [49, 34, 42, 27, 42, 27, 41, 37, 42, 48, 42] # pitch ranges of each cluster
    base_index = [] # base index of each event group (aka "piano note-ons" or "guitar note-offs")

    def __init__(self, filename):
        self.filename = filename

        file = Path(self.filename)
        if not file.is_file(): raise FileExistsError(self.filename + " does not exist or is not a file, please add a valid training and validation file, or priming song to the parameters")

        if len(Loader.base_index) == 0: Loader.compute_base_indices()

    def create_event_tensor(self):
        size = self.total_event_inputs()
        event_tensor = torch.ShortTensor(size)
        chord_tensor = torch.ByteTensor(size)

        for i, event in enumerate(self.iterate_events()):
            event_tensor[i] = event[0]
            chord_tensor[i] = event[1]

        return event_tensor, chord_tensor

    def create_chord_tensor(self):
        size = self.total_chord_inputs()
        tensor = torch.ByteTensor(size)

        for i, chord in enumerate(self.iterate_chords()):
            tensor[i] = chord

        return tensor

    def create_volume_tensor(self):
        size = self.total_event_inputs()
        event_tensor = torch.ShortTensor(size)
        volume_tensor = torch.FloatTensor(size)

        for i, event in enumerate(self.iterate_volumes()):
            event_tensor[i] = event[0]
            volume_tensor[i] = event[1]

        return event_tensor, volume_tensor

    def iterate_chords(self):
        time = 0
        chord = 0
        for bytes in self.iterate_quadruples():
            if chord != None and time % 12 == 0: yield chord

            chord, offset = self.get_chord(bytes)
            time += offset

        if chord != 24: yield 24  # force ending with the special ending "chord"

    def iterate_events(self):
        chord = 0
        for bytes in self.iterate_quadruples():
            event, chord = self.get_input(bytes, chord)
            yield event, chord

    def iterate_volumes(self):
        for bytes in self.iterate_quadruples():
            event, _ = self.get_input(bytes, 0)
            volume = self.get_volume(bytes)
            yield event, volume


    def iterate_quadruples(self):
        a, b, c, d = itertools.tee(self.iterate_bytes_from_file(), 4)

        a = itertools.islice(a, 0, None, 4)
        b = itertools.islice(b, 1, None, 4)
        c = itertools.islice(c, 2, None, 4)
        d = itertools.islice(d, 3, None, 4)

        return zip(a, b, c, d)

    def iterate_bytes_from_file(self, chunksize=32768):
        with open(self.filename, "rb") as f:
            while True:
                chunk = f.read(chunksize)
                if chunk:
                    yield from chunk
                else:
                    break



    def total_chord_inputs(self):
        counter = 0
        for _ in self.iterate_chords():
            counter += 1
        return counter

    def total_event_inputs(self):
        return os.path.getsize(self.filename) // 4

    @staticmethod
    def number_of_chords(): return 25

    @staticmethod
    def number_of_events(): return Loader.base_index_space()+3

    @staticmethod
    def compute_base_indices():
        Loader.base_index = [0]
        for i in range(1, Loader.num_clusters + 1):
            Loader.base_index.append(Loader.base_index[i - 1] + Loader.cluster_range[i - 1])

        for i in range(1, Loader.num_clusters + 1):
            Loader.base_index.append(Loader.base_index[Loader.num_clusters + i - 1] + Loader.cluster_range[i - 1])

    @staticmethod
    def base_index_on(cluster):
        return Loader.base_index[cluster]

    @staticmethod
    def base_index_off(cluster):
        return Loader.base_index[Loader.num_clusters + cluster]

    @staticmethod
    def base_index_space():
        return Loader.base_index[2*Loader.num_clusters]

    # we have read four bytes from the input, what event id do they represent? (see documentation of .mus format for more info)
    @staticmethod
    def get_input(bytes, last_chord):
        event_type = bytes[0] & 0x0f
        chord = last_chord

        if event_type == 0:
            channel = bytes[0] >> 4
            y_value = Loader.base_index_on(channel) + bytes[1]

        elif event_type == 1:
            channel = bytes[2]
            y_value = Loader.base_index_off(channel) + bytes[1]

        elif event_type == 2:
            channel = 9
            y_value = Loader.base_index_on(channel) + bytes[1]

        elif(event_type == 3):
            channel = 9
            y_value = Loader.base_index_off(channel) + bytes[1]

        elif(event_type == 4):
            y_value = Loader.base_index_space() + 0
            chord = bytes[3]

        elif(event_type == 5):
            y_value = Loader.base_index_space() + 1
            chord = bytes[3]

        elif(event_type == 6):
            y_value = Loader.base_index_space() + 2

        else:
            raise("unexpected event type " + str(event_type))

        return y_value, chord

    # we have read four bytes from the input, what volume do they have? (see documentation of .mus format for more info)
    @staticmethod
    def get_volume(bytes):
        event_type = bytes[0] & 0x0f

        if (event_type == 0) or (event_type == 2):
            return bytes[2] / 255.0

        return -1

    # we have read four bytes from the input, what chord do they represent? (see documentation of .mus format for more info)
    @staticmethod
    def get_chord(bytes):
        event_type = bytes[0] & 0x0f

        if (event_type == 4):
            return bytes[3], 1
        elif (event_type == 5):
            return bytes[3], 6
        else:
            return None, 0

    # we have an event, chord and its volume, what four bytes in .mus format do represent them?
    @staticmethod
    def output_to_bytes(output, chord, volume):

        # if note-ons
        if output < Loader.base_index_off(0):
            for i in range(1, Loader.num_clusters+1):
                if output < Loader.base_index_on(i):
                    cluster = i - 1
                    break

            pitch = output - Loader.base_index_on(cluster)

            if cluster == 9:
                byte_1 = 2
            else:
                byte_1 = cluster << 4

            byte_2 = pitch
            byte_3 = int(volume*255.99999999)
            byte_4 = 0

        # if note-offs
        elif output < Loader.base_index_space():
            for i in range(1, Loader.num_clusters+1):
                if output < Loader.base_index_off(i):
                    cluster = i - 1
                    break

            pitch = output - Loader.base_index_off(cluster)

            if cluster == 9:
                byte_1 = 3
                byte_3 = 0
            else:
                byte_1 = 1
                byte_3 = cluster

            byte_2 = pitch
            byte_4 = 0

        # if other events
        else:
            if output == Loader.base_index_space():
                byte_1 = 4
                byte_4 = chord
            elif output == Loader.base_index_space() + 1:
                byte_1 = 5
                byte_4 = chord
            elif output == Loader.base_index_space() + 2:
                byte_1 = 6
                byte_4 = 0
            else:
                raise("unknown output" + str(output))

            byte_2 = 0
            byte_3 = 0

        return byte_1, byte_2, byte_3, byte_4