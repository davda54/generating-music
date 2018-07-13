# Artificial Composition of Multi-Instrumental Polyphonic Music

We have tried to create a **generative model capable of composing polyphonic music**. Contrary to other work in the same field, our goal was to generate music with **multiple instruments** playing simultaneously to cover a broader musical space. 

We train three modules based on **LSTM neural networks** as the generative model; a lot of effort is put into reducing high complexity of multi- instrumental music representation by a thorough musical analysis.

## Musical Analysis

We need to **reduce the complexity of input MIDI data** without significant compromises on the possible musical expression. Training a neural network without this reduction would be **impossible** for current state-of-the-art neural architectures because the space of raw MIDI files is too large. The analysis consists of three main procesures:

### Meter detection
Capturing the rhythmic structure correctly is difficult, as the rhythm is dependent on time which is inherently continuous. To discretize the time with as low loss of information as possible, we have to **find the underlying metrical structure and normalize the tempo according to it**.

### Key detection
When considering the most usual major and minor modes, there are 24 different keys in total. A single pitch has a different role in each of these keys, which adds a lot of complexity that a prediction model should learn. Hence, we **classify the keys** (using a random forest model) **and then transpose them** to a unified key.

### Chord sequences detection
We use the harmonic structure as a representation of music at a higher abstract level (than simple notes) to **prolong the scope of the LSTM generative model**. Therefore we try to estimate the chord progressions with a help of the detected meter and key.


## Overview

The repository is divided into four different folders that are briefly described below. More details can be found in readme files contained in each of them.


## Analyzer

GUI application used for analysis, conversion and visualization of .mid and internal .mus files. Please note that the GUI front-end is a side project, not an explicit part of the bachelor thesis, but it contains all algorithms described in Chapter 5 about music analysis. We use it for an easier and more intuitive evaluation of the algorithms.


## Generative Model

Source files for the generative model implemented in PyTorch (deep learning library for python). It also contains already trained models that can be used to generate new music.


## Samples

Sample files outputed from the models contained in the Generative Model folder. The folder contains both good and bad sounding examples that should illustrate the overall behaviour of the generative model.


## Survey

Files related to the online questionaire. Contains the 104 audio files used in the questionaire and also a table with all answers from 293 users.
