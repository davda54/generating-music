using System;
using System.Windows;
using PianoRoll.MidiInterface;
using PianoRoll.SkiaRenderer;

namespace PianoRoll
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    partial class Settings
    {
        private readonly INoteRenderer _noteRenderer;
        private readonly MidiPlayer _player;
        private readonly MainWindow _mainWindow;

        public Settings(MainWindow mainWindow, INoteRenderer noteRenderer, MidiPlayer player)
        {
            _noteRenderer = noteRenderer;
            _player = player;
            _mainWindow = mainWindow;

            InitializeComponent();

            SettingsShowLines.IsChecked = Properties.Settings.Default.ShowLinesOnBackground;
            SettingsShowNotes.IsChecked = Properties.Settings.Default.ShowNoteNames;
            SettingsNormalize.IsChecked = Properties.Settings.Default.NormalizeTempo;
            SettingsShowBeats.IsChecked = Properties.Settings.Default.ShowBeats;
            SettingsProlongNotes.IsChecked = Properties.Settings.Default.ProlongSustainedNotes;
            SettingsShowKey.IsChecked = Properties.Settings.Default.ShowKey;
            SettingsShowChords.IsChecked = Properties.Settings.Default.ShowChords;
            SettingsPlayChords.IsChecked = Properties.Settings.Default.PlayChords;
            SettingsDiscretizeBends.IsChecked = Properties.Settings.Default.DiscretizeBends;
            SettingsTransposeToC.IsChecked = Properties.Settings.Default.TransposeToC;
            SettingsRandomize.IsChecked = Properties.Settings.Default.Randomize;

            PpsTextbox.Text = Properties.Settings.Default.PixelsPerSecond.ToString();
            LatencyTextbox.Text = Properties.Settings.Default.Latency.ToString();
            FrameLengthTextbox.Text = Properties.Settings.Default.FrameLength.ToString();
            TreesTextbox.Text = Properties.Settings.Default.KeyDetectorTrees.ToString();

            if (SettingsPlayChords.IsChecked == true)
            {
                SettingsShowChords.IsChecked = true;
                SettingsChords_Click(null, null);
                SettingsShowChords.IsEnabled = false;
            }
            if (SettingsShowChords.IsChecked == true || SettingsTransposeToC.IsChecked == true)
            {
                SettingsShowKey.IsChecked = true;
                SettingsKey_Click(null, null);
                SettingsShowKey.IsEnabled = false;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _mainWindow.SettingsButton.IsEnabled = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _mainWindow.SettingsButton.IsEnabled = false;
        }

        private void SettingsShowLines_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsShowLines.IsChecked != null)
            {
                Properties.Settings.Default.ShowLinesOnBackground = (bool) SettingsShowLines?.IsChecked;
                _noteRenderer.IsBackgrounedLined = (bool) SettingsShowLines?.IsChecked;

            }
        }

        private void SettingsShowNotes_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsShowNotes.IsChecked != null)
            {
                Properties.Settings.Default.ShowNoteNames = (bool) SettingsShowNotes?.IsChecked;
                _noteRenderer.AreNoteNamesShown = (bool) SettingsShowNotes?.IsChecked;
            }
        }

        private void SettingsNormalize_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsNormalize.IsChecked != null)
            {
                var isChecked = (bool) SettingsNormalize.IsChecked;

                Properties.Settings.Default.NormalizeTempo = isChecked;
                _player.IsTempoNormalized = isChecked;
            }
        }

        private void SettingsShowBeats_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsShowBeats.IsChecked != null)
            {
                var isChecked = (bool) SettingsShowBeats.IsChecked;

                Properties.Settings.Default.ShowBeats = isChecked;
                _player.ShowBeats = isChecked;
            }
        }

        private void SettingsProlong_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsProlongNotes.IsChecked != null)
            {
                Properties.Settings.Default.ProlongSustainedNotes = (bool) SettingsProlongNotes?.IsChecked;
                _player.ProlongSustainedNotes = (bool) SettingsProlongNotes?.IsChecked;
            }
        }
        
        private void SettingsKey_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsShowKey.IsChecked != null)
            {
                var isChecked = (bool)SettingsShowKey.IsChecked;

                Properties.Settings.Default.ShowKey = isChecked;
                _player.ShowKey = isChecked;
            }
        }

        private void SettingsChords_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsShowChords.IsChecked != null)
            {
                var isChecked = (bool)SettingsShowChords.IsChecked;

                Properties.Settings.Default.ShowChords = isChecked;
                _player.ShowChords = isChecked;

                if(isChecked)
                {
                    SettingsShowKey.IsChecked = true;
                    SettingsKey_Click(null, null);
                    SettingsShowKey.IsEnabled = false;
                }
                else
                {
                    SettingsShowKey.IsEnabled = true;
                }
            }
        }

        private void SettingsPlayChords_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsPlayChords.IsChecked != null)
            {
                var isChecked = (bool)SettingsPlayChords.IsChecked;

                Properties.Settings.Default.PlayChords = isChecked;
                _player.PlayChords = isChecked;

                if (isChecked)
                {
                    SettingsShowChords.IsChecked = true;
                    SettingsChords_Click(null, null);
                    SettingsShowChords.IsEnabled = false;
                    SettingsShowKey.IsChecked = true;
                    SettingsKey_Click(null, null);
                    SettingsShowKey.IsEnabled = false;
                }
                else
                {
                    SettingsShowChords.IsEnabled = true;
                    SettingsShowKey.IsEnabled = true;
                }
            }
        }

        private void SettingsDiscretizeBends_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsDiscretizeBends.IsChecked != null)
            {
                var isChecked = (bool)SettingsDiscretizeBends.IsChecked;

                Properties.Settings.Default.DiscretizeBends = isChecked;
                _player.DiscretizeBends = isChecked;
            }
        }

        private void SettingsTransposeToC_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsTransposeToC.IsChecked != null)
            {
                var isChecked = (bool)SettingsTransposeToC.IsChecked;

                Properties.Settings.Default.TransposeToC = isChecked;
                _player.TransposeToC = isChecked;

                if (isChecked)
                {
                    SettingsShowKey.IsChecked = true;
                    SettingsKey_Click(null, null);
                    SettingsShowKey.IsEnabled = false;
                }
                else
                {
                    SettingsShowKey.IsEnabled = true;
                }
            }
        }

        private void SettingsRandomize_Click(object sender, RoutedEventArgs e)
        {
            if (SettingsRandomize.IsChecked != null)
            {
                var isChecked = (bool)SettingsRandomize.IsChecked;

                Properties.Settings.Default.Randomize = isChecked;
                _player.RandomizeMus = isChecked;
            }
        }

        private void PpsTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(PpsTextbox.Text, out int value) && value > 0)
            {
                Properties.Settings.Default.PixelsPerSecond = value;
                _noteRenderer.PixelsPerSecond = value;
            }
        }

        private void LatencyTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(LatencyTextbox.Text, out int value) && value > 0)
            {
                Properties.Settings.Default.Latency = value;
                _player.Latency = TimeSpan.FromMilliseconds(value);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.Save();
        }

        private void FrameLengthTextbox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(FrameLengthTextbox.Text, out int value) && value > 0)
            {
                Properties.Settings.Default.FrameLength = value;
                _player.MusFrameLength = value;
            }
        }

        private void TreesTextbox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(TreesTextbox.Text, out int value) && value > 0)
            {
                value = Math.Min(value, 200);
                Properties.Settings.Default.KeyDetectorTrees = value;
                _player.KeyDetectorTrees = value;
            }
        }
    }
}