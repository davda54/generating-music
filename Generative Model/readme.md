## Generative Model

This folder contains source files for the generative model implemented in PyTorch (deep learning library for python). It also contains already trained models that can be used to generate new music.

Please make sure you have installed Numpy and PyTorch libraries before running the scripts. If you'd like to use GPU for more effective training/generating, please install CUDA with cudnn.

The folder is divided into a number of folders
 - Data folder should contain training datasets in .mus format
 - Primers folder should contain song used as primers (songs out of which new songs are improvized) in .mus format
 - Samples folder contains newly geenrated songs
 - Note_Predictor folder contains scripts for training and sampling from Note Predictor
 - Chord_Predictor folder contains scripts for training and sampling from Chord Predictor
 - Volume_Predictor folder contains scripts for training and sampling from Volume Predictor


### User Documentation

This section will cover sampling of new songs from the already trained model, for more detailed information about the implementation, please see the next chapter.

The sampling procedure uses another song as the base and generates an improvization based on it. Any such song should be converted to .mus format and put into the Primers folder. You can use our Analyzer to perform the conversion from a MIDI file.

The generator can be executed by calling the script music_generate.py [-h] [--primer PRIMER] [--note_model NOTE_MODEL]
                                                             [--priming_length PRIMING_LENGTH]
                                                             [--chord_priming_length CHORD_PRIMING_LENGTH]
                                                             [--cuda CUDA] [--max_length MAX_LENGTH]
                                                             [--temperature TEMPERATURE]
                                                             [--chord_temperature CHORD_TEMPERATURE]
                                                             [--chord_model CHORD_MODEL]
                                                             [--volume_model VOLUME_MODEL] [--n_primes N_PRIMES]
                                                             [--single_instrument SINGLE_INSTRUMENT]
                                                             [--output_folder OUTPUT_FOLDER]

The most important parameter to be set is --primer, which represents the name of the priming song in Primers folder. Usage of other parameters is explained by calling: python music_generate.py --help


### Programmer Documentation

Each predictor is located in its own folder with three files:
 - lstm_model.py defines the neural network graph used for sampling
 - XXX_train.py is a script for training a new network from provided dataset, please call the script with --help parameter to see its options
 - XXX_generate.py is a script for sampling new songs. The scripts chord_generate.py and volume_generate.py can generate only events from the respective generator, music_generate.py substitues the whole generative model as it is able to used the chord and volume generator as subroutines and thus create the whole songs.

 Other two files are located in the root folder: bnlstm.py is a corrected version of an implementation of recurrent batch normalization for LSTM by Jihun Choi (https://github.com/jihunchoi/recurrent-batch-normalization-pytorch). The script utils.py contains helper procedures for loading and dividing the datasets.

 Please see the comments inside the scripts to see how is each file implemented.