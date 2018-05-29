import sys
sys.path.append("../")

from torch import nn
from torch.autograd import Variable
import bnlstm as bn


# definition of the network graph
class lstm_model(nn.Module):

    def __init__(self, emsize, hidden_size, layers, event_size, dropout, cell, seq_len, tie_weights):
        super(lstm_model, self).__init__()

        # dropout applied to embedding layers
        self.drop = nn.Dropout(dropout)

        # embedding layers
        self.forward_encoder = nn.Embedding(event_size, emsize)

        self.hidden_size = hidden_size
        self.n_layers = layers
        self.seq_len = seq_len
        self.cell = cell

        # recurrent layers
        if cell == 'lstm':
            self.forward_lstm = nn.LSTM(input_size=emsize, hidden_size=hidden_size, num_layers=layers, dropout=dropout)
        elif cell == 'bnlstm':
            self.forward_lstm = bn.LSTM(input_size=emsize, hidden_size=hidden_size, num_layers=layers, max_length=seq_len)
        else:
            raise Exception("unknown cell type, please see help for supported cell types")

        # output fully-connected layer
        self.volume_decoder = nn.Linear(hidden_size, 1)

        self.init_weights()


    # initializes hidden states with uniform noise
    def init_hidden(self, batch_size):
        weight = next(self.parameters()).data

        self.hidden = (Variable(nn.init.xavier_uniform(weight.new(self.n_layers, batch_size, self.hidden_size))),
                       Variable(nn.init.xavier_uniform(weight.new(self.n_layers, batch_size, self.hidden_size))))


    # initializes weights by normal distribution
    def init_weights(self):
        self.forward_encoder.weight.data.normal_(0, 0.01)
        self.volume_decoder.bias.data.fill_(0)
        self.volume_decoder.weight.data.normal_(0, 0.01)


    # repackage hidden states to not backpropagate into the old ones
    def __repackage_hidden(self, h):
        """Wraps hidden states in new Variables, to detach them from their history."""
        if type(h) == Variable:
            h.detach_()
        else:
            return tuple(self.__repackage_hidden(v) for v in h)

    def repackage_hidden(self):
        self.__repackage_hidden(self.hidden)


    # forward pass
    def forward(self, input):
        emb = self.drop(self.forward_encoder(input))

        if self.cell == 'lstm':
            lstm_out, self.hidden = self.forward_lstm(emb, self.hidden)
            lstm_out = self.drop(lstm_out)
        else:
            lstm_out, self.hidden = self.forward_lstm(emb, hx=self.hidden)

        lstm_out_reshaped = lstm_out.view(lstm_out.size(0) * lstm_out.size(1), lstm_out.size(2))

        volume_out = self.volume_decoder(lstm_out_reshaped)
        volume_out = volume_out.view(lstm_out.size(0), lstm_out.size(1), volume_out.size(1))
        return volume_out