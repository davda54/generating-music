import sys
sys.path.append("../")

import argparse
import datetime
import time
import torch.optim as optim

from utils import *
from Note_Predictor.lstm_model import *

parser = argparse.ArgumentParser(description='Generative Model -- Note Predictor Training')

parser.add_argument('--train_file', type=str, default='', help='name of the file with training data in data folder')
parser.add_argument('--val_file', type=str, default='', help='name of the file with validation data in data folder')
parser.add_argument('--batch_size', type=int, default=256, help='batch size (default: 256)')
parser.add_argument('--cuda', type=bool, default=True, help='use CUDA (default: True)')
parser.add_argument('--dropout', type=float, default=0.1, help='dropout applied to embedding layers (default: 0.1)')
parser.add_argument('--clip', type=float, default=0.25, help='gradient clip, -1 means no clip (default: 0.25)')
parser.add_argument('--epochs', type=int, default=5, help='upper epoch limit (default: 5)')
parser.add_argument('--event_emsize', type=int, default=800, help='size of event embeddings (default: 800)')
parser.add_argument('--chord_emsize', type=int, default=12, help='size of chord embeddings (default: 12)')
parser.add_argument('--layers', type=int, default=3, help='# of layers (default: 3)')
parser.add_argument('--log_interval', type=int, default=100, help='report interval -- how many steps it takes before logging training progress (default: 100)')
parser.add_argument('--val_interval', type=int, default=5000, help='validation interval -- how many steps it takes before evaluation on the validation set (default: 5000)')
parser.add_argument('--lr', type=float, default=0.003, help='initial learning rate (default: 0.003)')
parser.add_argument('--lr_decay', type=float, default=4, help='learning rate decay after each epoch (default: 4)')
parser.add_argument('--nhid', type=int, default=800, help='number of hidden units per layer (default: 800)')
parser.add_argument('--seed', type=int, default=42, help='random seed (default: 42)')
parser.add_argument('--tied', type=bool, default=True, help='tie the encoder-decoder weights (default: True)')
parser.add_argument('--optim', type=str, default='Adam', help='optimizer type (default: Adam)')
parser.add_argument('--seq_len', type=int, default=120, help='total sequence length; how many time steps are unrolled (default: 120)')
parser.add_argument('--cell', type=str, default='bnlstm', help='type of rnn cell, supported values are "bnlstm" for LSTM with batch norm and "lstm" for standard LSTM cell (default: bnlstm)')
args = parser.parse_args()


# Set the random seed manually for reproducibility.
torch.manual_seed(args.seed)
if torch.cuda.is_available():
    if not args.cuda:
        print("WARNING: You have a CUDA device, so you should probably run with --cuda True")
    else:
        torch.cuda.manual_seed(args.seed)

print(args)

# load training and validation datasets
train_loader = Loader('../Data/' + args.train_file)
val_loader = Loader('../Data/' + args.val_file)

# create tensors from the datasets as the direct I/O of the networks
train_event_data, train_chord_data = train_loader.create_event_tensor()
val_event_data, val_chord_data = val_loader.create_event_tensor()

# divide the tensors into batches
train_event_data = batchify(train_event_data, args.batch_size)
train_chord_data = batchify(train_chord_data, args.batch_size)
val_event_data = batchify(val_event_data, args.batch_size)
val_chord_data = batchify(val_chord_data, args.batch_size)

n_event = Loader.number_of_events()
n_chords = Loader.number_of_chords()

# initialize the network graph
model = lstm_model(args.event_emsize, args.chord_emsize, n_event, args.nhid, args.layers, n_chords, args.dropout, args.tied, args.cell, args.seq_len)

if args.cuda:
    model.cuda()

# define the loss function
criterion = nn.CrossEntropyLoss()

# initialize the optimizer
lr = args.lr
optimizer = getattr(optim, args.optim)(model.parameters(), lr=lr)


def evaluate(event_source, chord_source):
    model.eval()
    model.init_hidden(args.batch_size)
    total_loss = 0

    for i in range(0, event_source.size(0) - 1, args.seq_len):
        event_data, targets = get_batch(event_source, i, args, evaluation=True)
        chord_data = get_batch_without_target(chord_source, i, args, evaluation=True)
        output = model(event_data, chord_data)

        output_flat = output.view(-1, n_event)
        total_loss += len(event_data) * criterion(output_flat, targets).data
        model.repackage_hidden()
    return total_loss[0] / len(event_source)


def train(train_log, test_log):
    model.train()
    model.init_hidden(args.batch_size)
    total_loss = 0
    start_time = time.time()

    #for each batch
    for batch, i in enumerate(range(0, train_event_data.size(0) - 1, args.seq_len)):
        event_data, targets = get_batch(train_event_data, i, args, evaluation=False)
        chord_data = get_batch_without_target(train_chord_data, i, args, evaluation=False)

        # repackage hidden states to not backpropagate into the old ones
        model.repackage_hidden()
        optimizer.zero_grad()

        # forward and backward pass
        output = model(event_data, chord_data)
        loss = criterion(output.view(-1, n_event), targets)
        loss.backward()

        # clip gradient and update weights according to it
        torch.nn.utils.clip_grad_norm(model.parameters(), args.clip)
        optimizer.step()

        total_loss += loss.data

        # log train progress if we are at the right step
        if batch % args.log_interval == 0 and batch > 0:
            cur_loss = total_loss[0] / args.log_interval
            elapsed = time.time() - start_time
            print('| epoch {:3d} | {:5d}/{:5d} batches | lr {:02.5f} | ms/batch {:5.5f} | loss {:5.2f}'.format(
                epoch, batch, train_event_data.size(0) // args.seq_len, lr,
                elapsed * 1000 / args.log_interval, cur_loss))

            total_loss = 0
            start_time = time.time()

            print(cur_loss, file=train_log, flush=True)

        # evaluate the progress on validation set if we are at the right step
        if batch % args.val_interval == 0 and batch > 0:
            val_loss = evaluate(val_event_data, val_chord_data)
            print(val_loss, file=test_log, flush=True)
            save(model, 'music', val_loss, args)

            model.train()
            model.init_hidden(args.batch_size)


# at any point you can hit Ctrl + C to break out of training early.
if __name__ == "__main__":
    best_val_loss = None

    with open("train_log_{}".format(datetime.datetime.now().strftime("%Y-%m-%d_%H.%M.%S")), "w") as train_log:
        with open("test_log_{}".format(datetime.datetime.now().strftime("%Y-%m-%d_%H.%M.%S")), "w") as test_log:

            try:
                for epoch in range(1, args.epochs+1):
                    epoch_start_time = time.time()
                    train(train_log, test_log)
                    val_loss = evaluate(val_event_data, val_chord_data)

                    print('-' * 89)
                    print('| end of epoch {:3d} | time: {:5.2f}s | valid loss {:5.2f}'.format(epoch, (time.time() - epoch_start_time), val_loss))
                    print('-' * 89)

                    save(model, 'music', val_loss, args)

                    # decay learning rate
                    lr /= args.lr_decay
                    for param_group in optimizer.param_groups:
                        param_group['lr'] = lr


            except KeyboardInterrupt:
                print('-' * 89)
                print('Exiting from training early')
