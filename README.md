# Artificial Composition of Multi-Instrumental Polyphonic Music

## Abstract

We propose a generative model for artificial composition of both classical and popular music with the goal of producing music as well as humans do. The problem is that music is based on a highly sophisticated hierarchical structureanditishardtomeasureitsqualityautomatically. Contrarytoother’swork, we try to generate a symbolic representation of music with multiple different instruments playing simultaneously to cover a broader musical space. We train three modules based on LSTM networks to generate the music; a lot of effort is put into reducing high complexity of multi-instrumental music representation by a thorough musical analysis. Our work serves mainly as a proof-of-concept for music composition. We believe that the proposed preprocessing techniques and symbolic representation constitute a useful resource for future research in this field. 

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
