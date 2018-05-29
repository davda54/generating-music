namespace MidiModel
{
    public static class ChannelPlayabilityChecker
    {
        public static void Check(Model midi)
        {
            midi.IsChannelPlayable = new bool[Model.NumberOfChannels];

            foreach (var track in midi.Tracks)
            {
                for (var i = 0; i < Model.NumberOfChannels; i++)
                {
                    if (track.Channels[i].IsPlayable) midi.IsChannelPlayable[i] = true;
                }
            }
        }
    }
}