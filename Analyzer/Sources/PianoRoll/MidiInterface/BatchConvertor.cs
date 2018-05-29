using MidiModel;
using MusParser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace PianoRoll.MidiInterface
{
    public class BatchConvertor
    {
        private string _pathToFolder;
        private string _filename;
        private MidiPlayer _player;

        public BatchConvertor(string pathToFolder, string filename, MidiPlayer player)
        {
            _pathToFolder = pathToFolder;
            _filename = filename;
            _player = player;
        }

        public void DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BackgroundWorker;

            var files = GetAllMidiFiles(_pathToFolder);
            using (var writer = new BinaryWriter(File.Open(_filename, FileMode.Create)))
            {
                FlushFiles(files, writer, worker);
            }
        }

        private void FlushFiles(IEnumerable<string> files, BinaryWriter writer, BackgroundWorker worker)
        {
            var random = new Random();
            int counter = 0;
            float totalCount = files.Count();

            files = files.OrderBy(f => random.Next());

            var badCount = 0;

            foreach (var file in files)
            {
                if (worker.CancellationPending == true)
                {
                    return;
                }

                counter++;
                int progress = (int)(100 * counter++ / totalCount + 0.5);
                worker.ReportProgress(progress);

                Model midi;
                try
                {                    
                    midi = _player.Parse(file);
                }
                catch
                {
                    badCount++;
                    continue;
                }

                ModelToMusicEvents.Parse(midi, writer, 50, 0, 0);
            }
        }
               

        private static IEnumerable<string> GetAllMidiFiles(string directory)
        {
            return Directory.EnumerateFiles(directory, "*.mid", SearchOption.AllDirectories);
        }
    }
}
