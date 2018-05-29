import sys
sys.path.append("../")

import argparse
import time
import datetime
import torch.optim as optim

from Chord_Predictor.lstm_model import *
from utils import *


parser = argparse.ArgumentParser(description='Generative Model -- Chord Predictor Training')

parser.add_argument('--train_file', type=str, default='', help='name of the file with training data in data folder')
parser.add_argument('--val_file', type=str, default='', help='name of the file with validation data in data folder')
parser.add_argument('--batch_size', type=int, default=128, help='batch size (default: 128)')
parser.add_argument('--cuda', type=bool, default=True, help='use CUDA (default: True)')
parser.add_argument('--dropout', type=float, default=0.0,  help='dropout applied to the embedding layers (default: 0.0)')
parser.add_argument('--clip', type=float, default=0.25, help='gradient clip, -1 means no clip (default: 0.25)')
parser.add_argument('--epochs', type=int, default=100, help='upper epoch limit (default: 100)')
parser.add_argument('--layers', type=int, default=2, help='# of layers (default: 2)')
parser.add_argument('--log-interval', type=int, default=1000, help='report interval -- how many steps it takes before logging training progress (default: 1000)')
parser.add_argument('--val_interval', type=int, default=5000, help='validation interval -- how many steps it takes before evaluation on the validation set (default: 5000)')
parser.add_argument('--lr', type=float, default=0.006, help='initial learning rate (default: 0.006)')
parser.add_argument('--lr_decay', type=float, default=2, help='initial learning rate (default: 2)')
parser.add_argument('--emsize', type=int, default=16, help='dimension of chord embeddings (default: 16)')
parser.add_argument('--optim', type=str, default='Adam', help='optimizer to use (default: Adam)')
parser.add_argument('--nhid', type=int, default=32, help='number of hidden units per layer (default: 32)')
parser.add_argument('--seq_len', type=int, default=100, help='total sequence length; how many time steps are unrolled (default: 100)')
parser.add_argument('--tie', type=bool, default=False, help='tie the encoder-decoder weights (default: False)')
parser.add_argument('--cell', type=str, default='bnlstm', help='type of rnn cell, supported values are "bnlstm" for LSTM with batch norm and "lstm" for standard LSTM cell (default: bnlstm)')
parser.add_argument('--seed', type=int, default=42, help='random seed (default: 42)')
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
train_loader = Loader('../data/' + args.train_file)
val_loader = Loader('../data/' + args.val_file)

n_chords = Loader.number_of_chords()

# create tensors from the datasets as the direct I/O of the networks
train_data = train_loader.create_chord_tensor()
val_data = val_loader.create_chord_tensor()

# divide the tensors into batches
train_data = batchify(train_data, args.batch_size)
val_data = batchify(val_data, args.batch_size)


# initialize the network graph
model = lstm_model(args.emsize, args.nhid, args.layers, n_chords, args.dropout, args.tie, args.cell, args.seq_len)

if args.cuda:
    model.cuda()


# define the loss function
criterion = nn.CrossEntropyLoss()

# initialize the optimizer
lr = args.lr
optimizer = getattr(optim, args.optim)(model.parameters(), lr=lr)


def evaluate(event_source):
    model.eval()
    model.init_hidden(args.batch_size)
    total_loss = 0

    for i in range(0, event_source.size(0) - 1, args.seq_len):
        input, targets = get_batch(event_source, i, args, evaluation=True)
        output = model(input)

        output_flat = output.view(-1, n_chords)
        total_loss += len(input) * criterion(output_flat, targets).data
        model.repackage_hidden()
    return total_loss[0] / len(event_source)


def train(epoch, train_log, test_log):
    model.train()
    model.init_hidden(args.batch_size)
    total_loss = 0
    start_time = time.time()

    #for each batch
    for batch, i in enumerate(range(0, train_data.size(0) - 1, args.seq_len)):
        input, targets = get_batch(train_data, i, args, evaluation=False)

        # repackage hidden states to not backpropagate into the old ones
        model.repackage_hidden()
        optimizer.zero_grad()

        # forward and backward pass
        output = model(input)
        loss = criterion(output.view(-1, n_chords), targets)
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
                epoch, batch, train_data.size(0) // args.seq_len, lr,
                elapsed * 1000 / args.log_interval, cur_loss))

            total_loss = 0
            start_time = time.time()

            print(cur_loss, file=train_log, flush=True)

        # evaluate the progress on validation set if we are at the right step
        if batch % args.val_interval == 0 and batch > 0:
            val_loss = evaluate(val_data)
            print(val_loss, file=test_log, flush=True)

            model.train()
            model.init_hidden(args.batch_size)


# at any point you can hit Ctrl + C to break out of training early.
if __name__ == "__main__":
    best_val_loss = None

    with open("chord_train_log_{}".format(datetime.datetime.now().strftime("%Y-%m-%d_%H.%M.%S")), "w") as train_log:
        with open("chord_test_log_{}".format(datetime.datetime.now().strftime("%Y-%m-%d_%H.%M.%S")), "w") as test_log:

            try:
                for epoch in range(1, args.epochs+1):
                    epoch_start_time = time.time()
                    train(epoch, train_log, test_log)
                    val_loss = evaluate(val_data)

                    print('-' * 89)
                    print('| end of epoch {:3d} | time: {:5.2f}s | valid loss {:5.2f}'.format(epoch, (time.time() - epoch_start_time), val_loss))
                    print('-' * 89)

                    # save the model if the validation loss is the best we've seen so far.
                    if not best_val_loss or val_loss < best_val_loss:
                        save(model, 'chord', val_loss, args)
                        best_val_loss = val_loss

                    # or if the validation got worse, decay the learning rate
                    else:
                        lr /= args.lr_decay
                        for param_group in optimizer.param_groups:
                            param_group['lr'] = lr


            except KeyboardInterrupt:
                print('-' * 89)
                print('Exiting from training early')