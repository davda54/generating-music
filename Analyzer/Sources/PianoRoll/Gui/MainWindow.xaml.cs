using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MidiModel;
using PianoRoll.Gui;
using PianoRoll.MidiInterface;
using PianoRoll.SkiaRenderer;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.ComponentModel;
using MusParser;

namespace PianoRoll
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly MidiPlayer _player;
        private readonly ImageRenderer _noteRenderer;
        private IUpdatableBitmap _skiaBitmap;
        private ChannelButton[] _channelButtons;
        private string _openedFilename;

        public MainWindow()
        {
            InitializeComponent();
            LoadChannelButtons();

            RenderOptions.SetBitmapScalingMode(PianoRollImage, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(PianoRollImage, EdgeMode.Aliased);

            _player = new MidiPlayer(from button in _channelButtons select button.ChannelState);
            _noteRenderer = new ImageRenderer();
        }

        public MainWindow(string filename) : this()
        {
            Dispatcher.BeginInvoke(new Action(() => OpenFile(filename)), DispatcherPriority.ContextIdle, null);
        }
        
        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg =
                new Microsoft.Win32.OpenFileDialog { DefaultExt = ".mid", Filter = "MIDI Files (*.mid)|*.mid|RNN output (*.mus)|*.mus"};

            // display OpenFileDialog by calling ShowDialog method 
            bool? result = dlg.ShowDialog();

            // get the selected file name
            if (result == true)
            {
                // Open document 
                _openedFilename = dlg.FileName;
                OpenFile(_openedFilename);
            }
        }

        private void OpenFile(string filename)
        {
            ProgressBar progress = new ProgressBar();
            progress.Progress.IsIndeterminate = true;
            progress.Title = "Training a random forest, please wait";

            this.IsEnabled = false;
            progress.Show();
            StopButton_Click(null, null);

            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.DoWork += new DoWorkEventHandler((e, ev) => 
            {
                _player.Open(this, filename);
            });
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((s, e) => 
            {
                progress.Close();
                this.IsEnabled = true;

                if (e.Error != null)
                {
                    ShowOpenFileError(filename);
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(new Action(() => {
                        OpenedFilePathLabel.Content = $"{filename}";

                        _noteRenderer.Player = _player;

                        PlayButton.IsEnabled = true;
                        StopButton.IsEnabled = true;
                        PlayButton.Content = "Pause";
                        _skiaBitmap.ContinueRendering();

                        EnableChannelButtons();
                        SaveFileButton.IsEnabled = true;
                    }));                    
                }
            });

            backgroundWorker.RunWorkerAsync();
        }

        private void ShowNonanalyzedError()
        {
            MessageBox.Show($"Files without normalized tempo can't be converted to .mus, please eneble it in the Settings window before opening a file.", "Can't convert file");
        }

        private void ShowOpenFileError(string filename)
        {
            MessageBox.Show($"The format of {filename} in not supported, it cannot be opened.", "Can't open file");
        }

        private void ShowSaveFileError(string filename)
        {
            MessageBox.Show($"The format of {filename} in not supported, it cannot be saved.", "Can't save file");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _player.Stop();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (_player.IsPlaying)
            {
                PlayButton.Content = "Play";
                StopButton.IsEnabled = false;
                _skiaBitmap.PauseRendering();

                _player.Pause();
            }
            else
            {
                PlayButton.Content = "Pause";
                StopButton.IsEnabled = true;
                _skiaBitmap.ContinueRendering();

                _player.Play();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopButton.IsEnabled = false;
            PlayButton.Content = "Play";
            
            _player.Stop();

            _skiaBitmap.UpdateBitmap(_noteRenderer.Render);
            _skiaBitmap.PauseRendering();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // initialize bitmap after the layout has been measured

            var size = SizeOfBitmap();
            
            _skiaBitmap = new SkiaBitmap(size.width, size.height);
            PianoRollImage.Source = _skiaBitmap.Bitmap;

            CompositionTarget.Rendering += (o, ee) => _skiaBitmap.UpdateBitmap(_noteRenderer.Render);
            _skiaBitmap.UpdateBitmap(_noteRenderer.Render);
            _skiaBitmap.PauseRendering();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new Settings(this, _noteRenderer, _player) { Owner = this };
            settings.Show();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (_skiaBitmap == null) return;

            var size = SizeOfBitmap();

            _skiaBitmap.Resize(size.width, size.height);
            PianoRollImage.Source = _skiaBitmap.Bitmap;
        }

        private void EnableChannelButtons()
        {
            for (int i = 0; i < _channelButtons.Length; i++)
            {
                _channelButtons[i].IsEnabled = _player.IsChannelPlayable(i);
            }
        }

        private void LoadChannelButtons()
        {
            _channelButtons = new ChannelButton[Model.NumberOfChannels];

            for (int i = 0; i < _channelButtons.Length; i++)
            {
                _channelButtons[i] = new ChannelButton(i) { IsEnabled = false };

                ChannelButtonsGrid.Children.Add(_channelButtons[i]);
                _channelButtons[i].SetValue(Grid.ColumnProperty, i);
            }
        }

        private (int width, int height) SizeOfBitmap()
        {
            return ((int) ImageGrid.ActualWidth, (int) ImageGrid.RowDefinitions[0].ActualHeight);
        }

        private void SaveFileButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = Path.ChangeExtension(_openedFilename, "mus"),
                DefaultExt = ".mus",
                Filter = "Music events (.mus)|*.mus|Midi format (.mid)|*.mid"
            };
            // Show save file dialog box
            var result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                var filename = dlg.FileName;

                try
                {
                    _player.Save(filename);
                }
                catch (NonanalyzedMidiException)
                {
                    ShowNonanalyzedError();
                }
                catch (ApplicationException)
                {
                    ShowSaveFileError(filename);
                }
            }
        }

        private void BatchConvertButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.EnsurePathExists = true;
            dialog.Title = "Select folder containing MIDI files to be converted into .mus dataset";

            var result = dialog.ShowDialog();
            if (result != CommonFileDialogResult.Ok) return;
            var path = dialog.FileName;


            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = Path.ChangeExtension(_openedFilename, "mus"),
                DefaultExt = ".mus",
                Filter = "Music events (.mus)|*.mus"
            };

            if (dlg.ShowDialog() != true) return;
            var filename = dlg.FileName;

            var convertor = new BatchConvertor(path, filename, _player);

            ProgressBar progress = new ProgressBar();
            this.IsEnabled = false;
            progress.Show();

            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += new DoWorkEventHandler(convertor.DoWork);
            backgroundWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler((s, ev) =>
            {
                progress.Close();
                this.IsEnabled = true;

                if (ev.Error != null)
                {
                    ShowNonanalyzedError();
                }
            });

            backgroundWorker.ProgressChanged += new ProgressChangedEventHandler((s, ev) => progress.SetValue(ev.ProgressPercentage));
            backgroundWorker.RunWorkerAsync();
        }
    }
}
