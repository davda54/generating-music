# Analyzer

GUI application used for analysis, conversion and visualization of .mid and internal .mus files. Please note that the GUI front-end is a side project, not an explicit part of the bachelor thesis, but it contains all algorithms described in Chapter 5 about music analysis. We use it for an easier and more intuitive evaluation of the algorithms.


## User Documentation

The program Piano Roll can be installed by clicking on Setup.exe.

### User Interface

The user interface should be straightforward. All control button are located on the left side, the majority of the window is devoted to visualization of currently played notes as a piano roll.

The program functions are described when hovering over control items.

The program can be controlled by the following buttons:
- **Open** -- *opens a new MIDI a .mus file and applies all modifications selected in Settings window*
- **Save** -- *saves an opened song to MIDI or .mus file (can be used for conversion)*
- **Convert Batch** -- *convert a folder with MIDI files into one continuous .mus file (used as a dataset in the generative model)
- **Play**
- **Pause**
- **Settings** -- *opens Settings window*

## Programmer Documentation

This section is divided into Directory Structure, .mus Format Description and External Libraries. More detailed information is provided in the source codes.

The program is written for .NET Framework 4.7.1 in C# 7.1.

The algorithms described in the thesis are located in folders ChordDetector, KeyDetector and MetreNormalizer.


### Directory Structure

- **MathExtension (library)** -- *implements some support mathematical routines used throughout the whole project*
    - Blur.cs -- *implements fast gaussian and box blur*
    - Calc.cs -- *difines some simple mathematical operations*
    - LinqExtension.cs -- *Linq extension that enable calculation of index with max/min value*
    - Statistics.cs -- *implements some basic statistics like standard deviation, correlation or chi-squared measure*
- **MidiModel (library)** -- *defines internal object representation*
    - **Simplifiers** -- *procedures for simple editing of the object representation (like transposition or volume calculation)*
        - Clipper.cs -- *clips a n-second frame from MidiModel*
        - ChannelPlayabilityChecker.cs
        - InstrumentChangeCollector.cs
        - PitchBendCalculator.cs
        - Sustainer.cs -- *extend note lengths according to their sustain*
        - TimeCalculator.cs -- *converts inner MIDI time units to real time*
        - Transposer.cs
        - VolumeChangeCollector.cs
    - Event.cs -- *object models of MIDI event messages*
    - Channel.cs -- *object model of MIDI channels containing events*
    - Instrument.cs -- *music instruments*
    - LinqToMidi.cs -- *extends the MidiModel to be treated as LINQ-ready collection*
    - MidiModel.cs -- *main class representing the whole song*
    - Scale.cs -- *object model of scales*
    - TimeDivision.cs
    - TimeChanger.cs
    - Track.cs -- *object model of tracks containing* 
- **MidiParser (library)** -- *convertor between MIDI format and MidiModel*
    - **BigEndianIO** -- *I/O in big endian because of MIDI format*
        - BigEndianReader.cs
        - BigEndianWriter.cs
        - MidiBigEndianReader.cs
        - MidiBigEndianWriter.cs
    - **ChordDetector** -- *detects chords and the can play them on piano*
        - ChordAnalyzer.cs -- *the main class coordiniting the computation*
        - ChordSegment.cs -- *one beat with a chord assigned*
        - NotesInSegment.cs -- *notes in a chord segment and their scores*
    - **KeyDetector** -- *detects keys*
        - KeyFinder.cs -- *original Krumhansl's algorithm for key detection*
        - MLKeyFinder.cs -- *Random forest model for key detection*
    - **MetreNormalizer** -- *detects meter and then can normalize the tempo or play percussion sounds on beat pulses*
        - BeatSegment.cs -- *segment of notes for detection of strong beats*
        - BeatStrengthAnalyzer.cs -- *selects strong beats*
        - Globals.cs -- *setting of global parameters*
        - Normalizer.cs -- *main class of MetreNormalizer, normalizes and renders the tempo*
        - NoteWrapper.cs
        - Pip.cs -- *one segment/time unit after the time is quantized, contains notes occuring at the time and computes base scores*
        - Tactus.cs -- *main class of meter detector, computes the total scores*
    - keySignatureDataset.csv -- *dateset of keys for key detector; contains pairs of pitch profiles and keys*
    - MidiToModelParser.cs -- *converts MIDI file to MidiModel*
    - ModelToMidiParser.cs -- *converts MidiModel to MIDI file*
- **MusParser (library)** -- *convertor between .mus format and MidiModel*
    - ClusterRanges.cs -- *pitch ranges of intrument clusters*
    - EventType.cs -- *events encoded in .mus files*
    - ModelToMusicEvents.cs -- *converts MidiModel to .mus format*
    - MusicEventsToModel.cs -- *converts .mus format to MidiModel*
    - NonanalyzedMidiException.cs -- *exception thrown when the MidiModel is not analyzed enought to be converted to .mus*
    - NoteCluster.cs -- *helper structure containing all event occuring at the same time*
- **PianoRoll (application)** -- *GUI application for visualizing MIDI/.mus files, their conversion and analysis*
    - **Extensions** -- *excension method of external classes*
        - ColorExtensions.cs -- basic operations with structures Color and SKColor
        - SKPointExtensions.cs -- basic arithmetics with structure SKPoint
    - **Gui** -- *classes for graphical user interface*
        - **SkiaRenderer** -- *dynamic visualization of notes using SkiaSharp*
            - BackgroundRenderer.cs -- *renders background on the piano roll screen*
            - FpsRenderer.cs -- *renders FPS in debug mode*
            - ImageRenderer.cs -- *class responsible for rendering of the whole skia bitmap. It renders moving notes, semitone names and elapsed time*
            - IRenderer.cs
            - SkiaBitmap.cs -- *interface between IUpdatableBitmap and SKCanvas*
        - ChannelButton.cs -- *implementation of a channel button, that has a unique color, can mute the corresponding channel and changes appearence when a note from the corresponding channel is being played*
        - MainWindow.xaml -- *GUI for the main window*
        - ProgressBar.xaml -- *GUI for progress bar*
        - Settings.xaml -- *GUI for the setings window*
    - **MidiInterface** -- *classes for interacting with MIDI synthetizer*
        - BatchConvertor.cs -- *converts multiple MIDI files into one .mus file in a background thread*
        - ClockScheduler.cs -- *schedules all the events to Midi.Clock so the file can be played*
        - ChannelState.cs -- *represents current state of a midi channel: current instrument, volume and whether the channel is just playing or is being muted*
        - MidiPlayer.cs -- *manages all events connected to the playback of midi file*
        - NoteStream.cs -- *container for the NoteOn midi events, tt is used to gather all the notes that should appear on the screen*
    - **Resources** -- *definitions of graphical styles for graphical interface*
        - Colours.xaml
        - DarkStyle.xaml

### .mus Format Definition

The Music Events (.mus) binary format is used as an interface between the object MidiModel format and event format for LSTM models. Therefore it consists of a sequence of messeges corresponding to the note-on, note-off and time-shift events of Note Predictor, which also contain information about chord progressions (Chord Predictor) and volumes (Volume Predictor) -- see the chapter about input and output representation in the thesis for more information.

There are 7 difference kinds of messages: NoteOn, NoteOff, PercussionOn, PercussionOf, ShortTimeShift, LargeTimeShift and SongEnd; each of them is encoded in exactly four bytes in the little-endian format. They are defined as follows:

- **NoteOn** -- *start of a note of a non-percussion instrument*
    - byte 1: *upper four bits encode instrument cluster (0-10), lowe two bits are set to 0*
    - byte 2: *pitch -- decoded as the number of semitones from the minimal pitch of the instrument cluster*
    - byte 3: *volume -- shrinked into 256 discrete values*
    - byte 4: *currently unused, can be used to encode the exact intrument that should play the note*

- **NoteOff** -- *end of a note of a non-percussion instrument*
    - byte 1: *1*
    - byte 2: *pitch -- decoded as the number of semitones from the minimal pitch of the instrument cluster*
    - byte 3: *instrument cluster (0-10)*
    - byte 4: *0*
    
- **PercussionOn** -- start of a note of a percussion instrument
    - byte 1: *2* 
    - byte 2: *NoteNumber - 35 (minimal NoteNumber of percussion events)*
    - byte 3: *volume -- shrinked into 256 discrete values*
    - byte 4: *0*

- **PercussionOff** -- end of a note of a percussion instrument
    - byte 1: *3*
    - byte 2: *NoteNumber - 35 (minimal NoteNumber of percussion events)*
    - byte 3: *0*
    - byte 4: *0*

- **ShortTimeShift** -- end of a note of a non-percussion instrument
    - byte 1: *4*
    - byte 2: *currently unused -- can be used to store the original tempo before tempo normalization*
    - byte 3: *currently unused -- can be used to store the original tempo before tempo normalization*
    - byte 4: *chord (from 0 to 23)*

- **LargeTimeShift** -- end of a note of a non-percussion instrument
    - byte 1: *5*
    - byte 2: *currently unused -- can be used to store the original tempo before tempo normalization*
    - byte 3: *currently unused -- can be used to store the original tempo before tempo normalization*
    - byte 4: *chord (from 0 to 23)*

- **SongEnd** -- end of a note of a non-percussion instrument
    - byte 1: *6*
    - byte 2: *0*
    - byte 3: *0*
    - byte 4: *0*
    
### External Libraries

- SkiaSharp (https://github.com/mono/SkiaSharp) --*2D graphics API for fast drawing of rolling notes*
- Windows-API-Code-Pack-1.1 (https://github.com/contre/Windows-API-Code-Pack-1.1) -- *better "Open Folder" dialog than in standard WPF*
- midi-dot-net (https://code.google.com/archive/p/midi-dot-net/) -- *used for playing the MIDI files*
- SharpLearning (https://github.com/mdabros/SharpLearning) -- *used to train a random forest in the key detection*
- JonSkeet.MiscUtil -- *implements dome low-levels utils that are missing in standard .NET*