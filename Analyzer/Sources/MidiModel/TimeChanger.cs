using System;
using System.Collections.Generic;
using System.Linq;

namespace MidiModel
{
    public static class TimeChanger
    {
        public static int[] GetNoteDifferences(List<NoteOn> notes, int maxDifference, int minDifference)
        {
            var histogram = new int[maxDifference];

            var noteStarts = notes.Select(n => n.AbsoluteRealTime.TotalMilliseconds).ToArray();

            for (int i = 1; i < noteStarts.Length; i++)
            {
                var diff = (int)Math.Round((noteStarts[i] - noteStarts[i - 1]) * 10);
                if (diff < minDifference || diff >= histogram.Length) continue;

                histogram[diff]++;

            }

            return histogram;
        }

        public static int[] BoxBlur(int[] array, int size)
        {
            var output = new int[array.Length];

            for (int i = 0; i < array.Length; i++)
            {
                int sum = 0;
                for (int j = i - size; j <= i + size; j++)
                {
                    if (j < 0 || j >= array.Length) continue;
                    sum += array[j];
                }
                output[i] = sum;
            }

            return output;
        }

        public static float GetMostCommonDifference(List<NoteOn> notes, int frameRateMillis)
        {
            var histogram = GetNoteDifferences(notes, frameRateMillis * 100, 1);

            histogram = BoxBlur(histogram, 10);
            histogram = BoxBlur(histogram, 10);
            histogram = BoxBlur(histogram, 10);
            
            (int value, int index) max = (0, 0);
            for (int i = 0; i < histogram.Length; i++)
            {
                if (max.value < histogram[i]) max = (histogram[i], i);
            }         

            return max.index / 10f;
        }

        public static float GetSmallestImportantDifference(Model midi, int frameRateMillis)
        {
            var notes = midi.EventsOfType<NoteOn>().Where(n => n.Volume > 0).OrderBy(note => note.AbsoluteRealTime).ToList();
            return GetSmallestImportantDifference(notes, frameRateMillis);
        }

        public static float GetSmallestImportantDifference(List<NoteOn> notes, int frameRateMillis)
        {
            var histogram = GetNoteDifferences(notes, frameRateMillis * 100, frameRateMillis * 10);

            histogram = BoxBlur(histogram, 10);
            histogram = BoxBlur(histogram, 10);
            histogram = BoxBlur(histogram, 10);

            var median = histogram.Sum() / 2;
            if (median == 0) return 0;

            //var average = histogram.Sum() / histogram.Where(v => v > 0).Count();

            (int value, int index) max = (0, 0);
            var sum = 0;
            for (int i = 0; i < histogram.Length; i++)
            {
                sum += histogram[i];

                if (max.value < histogram[i]) max = (histogram[i], i);
                else if (max.value > histogram[i] && sum > median)
                    break;
            }

            var div = 1;

            if (histogram[(int)((max.index + 0.5) / 8)] * 4 > max.value) div *= 8;
            else if (histogram[(int)((max.index + 0.5) / 4)] * 4 > max.value) div *= 4;
            else if (histogram[(int)((max.index + 0.5) / 2)] * 4 > max.value) div *= 2;

            if (histogram[(int)((max.index + 0.5) / 9)] * 4 > max.value) div *= 9;
            else if (histogram[(int)((max.index + 0.5) / 3)] * 4 > max.value) div *= 3;

            if (histogram[(int)((max.index + 0.5) / 5)] * 4 > max.value) div *= 5;
            if (histogram[(int)((max.index + 0.5) / 7)] * 4 > max.value) div *= 7;

            return max.index / 10f / div;
        }

        public static float GetRelativeTimeChange(Model midi, int frameRateMillis)
        {
            var notes = midi.EventsOfType<NoteOn>().Where(n => n.Volume > 0).OrderBy(note => note.AbsoluteRealTime).ToList();
            var rate = frameRateMillis;

            var diff = GetSmallestImportantDifference(notes, frameRateMillis);
            Console.WriteLine(diff);
            if (diff == 0) return 1;

            var low = ((int)diff / rate) * rate;
            var high = ((int)diff / rate + 1) * rate;

            int closer = low == 0 || high - diff < diff - low ? high : low;

            return closer / diff;
        }
    }
}
