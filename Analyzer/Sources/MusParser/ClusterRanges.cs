using System.Collections.Generic;
using MidiModel;

namespace MusParser
{
    public static class ClusterRanges
    {
        private static readonly Dictionary<InstrumentCluster, (byte min, byte max)> Ranges = new Dictionary<InstrumentCluster, (byte min, byte max)>()
        {
            {InstrumentCluster.Piano, (36,84) }, //49
            {InstrumentCluster.AcusticGuitar, (43,76) }, //34
            {InstrumentCluster.Orchestra, (43,84) }, //42
            {InstrumentCluster.Bass, (24,50) }, //27
            {InstrumentCluster.Brass, (43,84) }, //42
            {InstrumentCluster.Warm, (55,81) }, //27
            {InstrumentCluster.ElectricGuitar, (36,76) }, //41
            {InstrumentCluster.Organ, (48,84) }, //37
            {InstrumentCluster.ClappingBox, (43,84) }, //42
            {InstrumentCluster.Percussion, (Percussion.MinNoteNumber, Percussion.MaxNoteNumber) }, //48
            {InstrumentCluster.Violin, (43,84)} //42
        };

        public static byte Min(InstrumentCluster cluster) => Ranges[cluster].min;
        public static byte Max(InstrumentCluster cluster) => Ranges[cluster].max;
        public static int Size(InstrumentCluster cluster) => Ranges[cluster].max - Ranges[cluster].min;
    }
}
