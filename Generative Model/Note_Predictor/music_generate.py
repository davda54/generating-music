import sys
sys.path.append("../")

from Chord_Predictor.chord_generate import *
from Volume_Predictor.volume_generate import *
from utils import *



parser = argparse.ArgumentParser(description='Generative Model -- Note Predictor Generating')
parser.add_argument('--note_model', type=str, default='music-model.loss_0.880.pt', help='path to trained model')
parser.add_argument('--primer', type=str, default="Nirvana - Lithium.mus", help='name of the priming song')
parser.add_argument('--priming_length', type=int, default=400, help='number of events primed from the input (default: 400)')
parser.add_argument('--chord_priming_length', type=int, default=20, help='number of events primed from the input for Chord Predictor (default: 20)')
parser.add_argument('--cuda', type=bool, default=False, help='use CUDA (default: False)')
parser.add_argument('--max_length', type=int, default=10000, help='maximal length of the generated sequence (default: 10000)')
parser.add_argument('--temperature', type=float, default=0.95, help='temperature -- certainty of the prediction (default: 0.95)')
parser.add_argument('--chord_temperature', type=float, default=1.00, help='temperature -- certainty of the prediction for Chord Predictor (default: 1.00)')
parser.add_argument('--chord_model', type=str, default='../Chord_Predictor/chord-model.loss_0.54380.pt', help='path to the chord model, when left empty, chords in the original song are used')
parser.add_argument('--volume_model', type=str, default='../Volume_Predictor/volume-model.loss_0.02557.pt', help='path to the volume model, when left empty, no volume dynamics is used')
parser.add_argument('--n_primes', type=int, default=2, help="how many times do we feed forward the whole primer (default: 1)")
parser.add_argument('--single_instrument', type=bool, default=False, help="filter output to generate only single-instrumental music? (default: False)")
parser.add_argument('--output_folder', type=str, default="../Samples/")
parser.add_argument('--seed', type=int, default=42, help='random seed (default: 42)')
args = parser.parse_args()


# Set the random seed manually for reproducibility.
if torch.cuda.is_available():
    if not args.cuda:
        print("WARNING: You have a CUDA device, so you should probably run with --cuda True")
    else:
        torch.cuda.manual_seed(args.seed)


def generate_music(args, primer):
    model = torch.load(args.note_model)

    loader = Loader(primer)
    event_tensor, _ = loader.create_event_tensor()
    chord_tensor = loader.create_chord_tensor()

    # use chord predictor to generate chords if specified
    if args.chord_model != '':
        print("Generating chords")
        generated_chords = generate_chords(args.chord_model, primer, args.cuda, priming_length=args.chord_priming_length, n_primes=args.n_primes, temperature=args.chord_temperature)

    # original chords used for priming
    chords = chord_tensor
    input_size = len(event_tensor)

    print("Generating notes")

    output = event_tensor[0]
    # contains [event, chord, volume]
    result = [(output, 0, 0.5)]

    # wrapping scalars into tensors, so they can be put into the network
    input_event = Variable(torch.LongTensor(1, 1))
    input_chord = Variable(torch.LongTensor(1, 1))
    input_event[0, 0] = output
    input_chord[0, 0] = 0

    if args.cuda:
        input_event = input_event.cuda()
        input_chord = input_chord.cuda()
        model.cuda()
    else:
        model.cpu()

    # set state of the model to evaluation and initialize hidden states
    model.eval()
    model.init_hidden(1)

    time = 0

    # capture the right instrument when generating single-instrument music
    instrument_cluster = None

    # feed forward the whole network with primer and then generate new music of maximal length args.max_length
    for i in range(args.n_primes*input_size + args.max_length):
        model.repackage_hidden()

        # when we get the first note, assign its instrument to the instrument_cluster variable
        if instrument_cluster == None and input_event.data[0,0] < Loader.base_index_off(0):
            for cluster in range(Loader.num_clusters):
                if input_event.data[0,0] < Loader.base_index_on(cluster + 1):
                    instrument_cluster = cluster
                    break

        # don't generate anything, just feed the network to set its hidden states
        if i < args.n_primes*input_size + args.priming_length:
            _ = model(input_event, input_chord)
            output = event_tensor[(i + 1) % input_size]

        # else generate new events
        else:
            # generate probability distribution over all events
            output = model(input_event, input_chord)
            output = torch.squeeze(output.data.double().div_(args.temperature).exp_())

            # mask the output if we want to generate single-instrumental music
            if args.single_instrument:
                for j in range(len(output)):
                    if j < Loader.base_index_on(instrument_cluster) or (j >= Loader.base_index_on(instrument_cluster + 1) and j < Loader.base_index_off(0)):
                        output[j] = 0

            # select a random event from the distribution
            output = output.div_(torch.sum(output))
            output = torch.multinomial(output, 1)[0]

        # if we are at the start of the song
        if i > 0 and (i % input_size) == 0 and i <= args.n_primes*input_size:
            time = 0

            # if we want to generate chords and the priming hac just ended, use the generated chords
            if args.chord_model != '' and i == args.n_primes*input_size:
                chords = generated_chords

        # shift the time if time-shift event was generated
        if output == Loader.base_index_space(): time += 1
        elif output == Loader.base_index_space() + 1: time += 6

        # for safety, end the generating if we don't have any remaining chords
        if (time + 11) // 12 > len(chords) - 1:
            break
        # else, choose the right chor occuring in the next beat
        else:
            input_chord[0, 0] = chords[(time + 11) // 12]

        # last event is the new input
        input_event[0, 0] = output

        # if we are still priming, just continue the loop
        if i < args.n_primes*input_size: continue

        # if we encounter the "stop" chord, end
        if input_chord.data[0, 0] == 24: break

        # else append the generated event to result
        result.append((output, input_chord.data[0,0], 0.5))

    # assign volumes to each event
    if args.volume_model != '':
        print("Generating volumes")

        notes = [event[0] for event in result]
        volumes = generate_volumes(args.volume_model, primer, args.cuda, args.priming_length, args.n_primes, notes)
        return [(result[i][0], result[i][1], volumes[i]) for i in range(len(volumes))]


    return result


if __name__ == "__main__":

    primer = "../Primers/{}".format(args.primer)
    output = generate_music(args, primer)

    filename = args.output_folder + args.primer
    with open(filename, 'wb') as f:
        for pair in output:
            a,b,c,d = Loader.output_to_bytes(pair[0], pair[1], pair[2])
            f.write(bytes([a,b,c,d]))
    print('saved as ' + filename)