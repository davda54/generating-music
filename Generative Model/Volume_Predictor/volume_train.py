import sys
sys.path.append("../")

import argparse
import torch.optim as optim
import time
import datetime

from Volume_Predictor.lstm_model import *
from utils import *

parser = argparse.ArgumentParser(description='Generative Model -- Volume Predictor Training')

parser.add_argument('--train_file', type=str, default='', help='name of the file with training data in data folder')
parser.add_argument('--val_file', type=str, default='', help='name of the file with validation data in data folder')
parser.add_argument('--batch_size', type=int, default=128, help='batch size (default: 128)')
parser.add_argument('--cuda', type=bool, default=True, help='use CUDA (default: True)')
parser.add_argument('--dropout', type=float, default=0.1,  help='dropout applied to layers (default: 0.1)')
parser.add_argument('--clip', type=float, default=0.25, help='gradient clip, -1 means no clip (default: 0.25)')
parser.add_argument('--epochs', type=int, default=100, help='upper epoch limit (default: 100)')
parser.add_argument('--layers', type=int, default=2, help='# of layers (default: 2)')
parser.add_argument('--log-interval', type=int, default=1000, help='report interval -- how many steps it takes before logging training progress (default: 1000)')
parser.add_argument('--val_interval', type=int, default=5000, help='validation interval -- how many steps it takes before evaluation on the validation set (default: 5000)')
parser.add_argument('--lr', type=float, default=0.003, help='initial learning rate (default: 0.003)')
parser.add_argument('--lr_decay', type=float, default=4, help='learning rate decay after each epoch (default: 4)')
parser.add_argument('--emsize', type=int, default=128, help='dimension of event embeddings (default: 256)')
parser.add_argument('--optim', type=str, default='Adam', help='optimizer to use (default: Adam)')
parser.add_argument('--nhid', type=int, default=64, help='number of hidden units per layer (default: 64)')
parser.add_argument('--seq_len', type=int, default=60, help='total sequence length; how many time steps are unrolled (default: 80)')
parser.add_argument('--seed', type=int, default=42, help='random seed (default: 42)')
parser.add_argument('--tie', type=bool, default=True, help='tie weights of the encoder and decoder')
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
train_loader = Loader('../data/' + args.train_file)
val_loader = Loader('../data/' + args.val_file)

# create tensors from the datasets as the direct I/O of the networks
train_input, train_output = train_loader.create_volume_tensor()
val_input, val_output = val_loader.create_volume_tensor()

# divide the tensors into batches
train_input = batchify(train_input, args.batch_size)
train_output = batchify(train_output, args.batch_size)
val_input = batchify(val_input, args.batch_size)
val_output = batchify(val_output, args.batch_size)

n_events = Loader.number_of_events()

# initialize the network graph
model = lstm_model(args.emsize, args.nhid, args.layers, n_events, args.dropout, 'bnlstm', args.seq_len, args.tie)

if args.cuda:
    model.cuda()

# define the loss function
volume_criterion = nn.MSELoss(reduce=False)

# initialize the optimizer
lr = args.lr
optimizer = getattr(optim, args.optim)(model.parameters(), lr=lr)


def evaluate(event_source, volume_source):
    model.eval()
    model.init_hidden(args.batch_size)
    total_loss = 0
    count = 0

    for i in range(0, event_source.size(0) - 1, args.seq_len):
        input = get_batch_without_target(event_source, i, args, evaluation=True)
        targets = get_target_float_batch(volume_source, i, args)
        volume_output = model(input)

        volume_loss = volume_criterion(volume_output.view(-1), targets).data
        mask = (targets.data != -1).float()
        total_loss += (volume_loss*mask).sum()
        count += mask.sum()

        model.repackage_hidden()

    return total_loss / count


def train(epoch, train_log, test_log):
    model.train()
    model.init_hidden(args.batch_size)
    total_loss = 0
    start_time = time.time()

    #for each batch
    for batch, i in enumerate(range(0, train_input.size(0) - 1, args.seq_len)):
        input, event_targets = get_batch(train_input, i, args, evaluation=False)
        volume_targets = get_target_float_batch(train_output, i, args)

        # repackage hidden states to not backpropagate into the old ones
        model.repackage_hidden()
        optimizer.zero_grad()

        # forward and backward pass
        volume_out = model(input)
        loss = volume_criterion(volume_out.view(-1), volume_targets)

        mask = (volume_targets != -1).float()
        loss = loss*mask
        loss = loss.sum()/mask.sum()
        loss.backward()

        # clip gradient and update weights according to it
        torch.nn.utils.clip_grad_norm(model.parameters(), args.clip)
        optimizer.step()

        total_loss += loss.data

        # log train progress if we are at the right step
        if batch % args.log_interval == 0 and batch > 0:
            cur_loss = total_loss[0] / args.log_interval
            elapsed = time.time() - start_time
            print('| epoch {:3d} | {:5d}/{:5d} batches | lr {:02.5f} | ms/batch {:5.5f} | loss {:5.5f}'.format(
                epoch, batch, train_input.size(0) // args.seq_len, lr,
                elapsed * 1000 / args.log_interval, cur_loss))

            total_loss = 0
            start_time = time.time()

            print(cur_loss, file=train_log, flush=True)

        # evaluate the progress on validation set if we are at the right step
        if batch % args.val_interval == 0 and batch > 0:
            val_loss = evaluate(val_input, val_output)
            print(val_loss, file=test_log, flush=True)
            save(model, 'volume', val_loss, args)

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
                    val_loss = evaluate(val_input, val_output)

                    print('-' * 89)
                    print('| end of epoch {:3d} | time: {:5.2f}s | valid loss {:5.5f}'.format(epoch, (time.time() - epoch_start_time), val_loss))
                    print('-' * 89)

                    # save the model if the validation loss is the best we've seen so far.
                    if not best_val_loss or val_loss < best_val_loss:
                        save(model, 'volume', val_loss, args)
                        best_val_loss = val_loss

                    # or if the validation got worse, decay the learning rate
                    else:
                        lr /= args.lr_decay
                        for param_group in optimizer.param_groups:
                            param_group['lr'] = lr


            except KeyboardInterrupt:
                print('-' * 89)
                print('Exiting from training early')