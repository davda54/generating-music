import sys
sys.path.append("../")

import argparse
from utils import *


def generate_volumes(model, primer, cuda, priming_length=50, n_primes=1, events_for_regression=None):
    model = torch.load(model)
    loader = Loader(primer)

    event_tensor, volume_tensor = loader.create_volume_tensor()
    if events_for_regression == None: events_for_regression = event_tensor
    primer_size = len(event_tensor)
    prediction_size = len(events_for_regression)

    result = []

    # wrapping scalars into tensors, so they can be put into the network
    input = Variable(torch.LongTensor(1, 1))

    if cuda:
        input = input.cuda()
        model.cuda()
    else:
        model.cpu()

    # set state of the model to evaluation and initialize hidden states
    model.eval()
    model.init_hidden(1)

    # feed forward the whole network with primer and then generate new music of maximal length args.max_length
    for i in range(n_primes*primer_size + prediction_size):
        model.repackage_hidden()

        # don't generate anything, just feed the network to set its hidden states
        if i < n_primes*primer_size + priming_length:
            input[0,0] = event_tensor[i % primer_size]
            _ = model(input)
            if i < n_primes * primer_size: continue

            output = volume_tensor[i % primer_size]

        # else generate new volumes
        else:
            input[0,0] = events_for_regression[i - n_primes*primer_size]

            output  = model(input)
            output = output.data[0,0,0]

        result.append(output)

    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Generative Model -- Volume Predictor Generating')
    parser.add_argument('--model', type=str, default='volume-model.loss_0.02557.pt', help='path to trained model')
    parser.add_argument('--seed', type=int, default=42, help='random seed (default: 42)')
    parser.add_argument('--cuda', type=bool, default=False, help='use CUDA (default: False)')
    parser.add_argument('--primer', type=str, default='', help='path to priming song')
    parser.add_argument('--priming_length', type=int, default=100, help='number of items primed from the input')
    parser.add_argument('--n_primes', type=int, default=1, help='number of loops over the primer')
    args = parser.parse_args()

    # Set the random seed manually for reproducibility.
    if torch.cuda.is_available():
        if not args.cuda:
            print("WARNING: You have a CUDA device, so you should probably run with --cuda True")
        else:
            torch.cuda.manual_seed(args.seed)

    output = generate_volumes(args.model, args.primer, args.cuda, args.priming_length, args.n_primes)
    print(output)

