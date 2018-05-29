import sys
sys.path.append("../")

import argparse
from utils import *


def generate_chords(model, primer, cuda, priming_length, max_length=1000, temperature=1.0, n_primes=1):
    model = torch.load(model)
    loader = Loader("../Primers/" + primer)

    input_tensor = loader.create_chord_tensor()
    input_size = len(input_tensor)

    output = input_tensor[0]
    result = [output]

    # wrapping scalars into tensors, so they can be put into the network
    input = Variable(torch.LongTensor(1,1))
    input[0,0] = output

    if cuda:
        input = input.cuda()
        model.cuda()
    else:
        model.cpu()

    # set state of the model to evaluation and initialize hidden states
    model.eval()
    model.init_hidden(1)

    # feed forward the whole network with primer and then generate new music of maximal length args.max_length
    for i in range(n_primes*input_size + max_length):
        model.repackage_hidden()

        # don't generate anything, just feed the network to set its hidden states
        if i < n_primes*input_size + priming_length:
            _ = model(input)
            output = input_tensor[(i+1) % input_size]

        # else generate new events
        else:
            # generate probability distribution over all chords
            output = model(input)
            output = torch.squeeze(output.data.double().div_(temperature).exp_())

            # select a random event from the distribution
            output = output.div_(torch.sum(output))
            output = torch.multinomial(output, 1)[0]

        # last chord is the new input
        input[0,0] = output

        # if we are still priming, just continue the loop
        if i < n_primes*input_size: continue
        # if we encounter the "stop" chord, end
        if output == 24: break
        # else append the generated event to result
        result.append(output)

    result.append(24)
    return result


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description='Generative Model -- Chord Predictor Generating')
    parser.add_argument('--model', type=str, default='chord-model.loss_0.54380.pt', help='path to trained model')
    parser.add_argument('--seed', type=int, default=42, help='random seed (default: 42)')
    parser.add_argument('--cuda', type=bool, default=False, help='use CUDA (default: False)')
    parser.add_argument('--primer', type=str, default='', help='path to priming song')
    parser.add_argument('--priming_length', type=int, default=20, help='number of items primed from the input')
    parser.add_argument('--n_primes', type=int, default=1, help="how many times do we feed forward the whole primer (default: 1)")
    parser.add_argument('--length', type=int, default=500, help='max length of the generated sequence')
    parser.add_argument('--temperature', type=float, default=1.0, help='certainty of the prediction')
    args = parser.parse_args()

    # Set the random seed manually for reproducibility.
    if torch.cuda.is_available():
        if not args.cuda:
            print("WARNING: You have a CUDA device, so you should probably run with --cuda True")
        else:
            torch.cuda.manual_seed(args.seed)

    output = generate_chords(args.model, args.primer, args.cuda, args.priming_length, args.length, args.temperature, args.n_primes)
    print(output)

