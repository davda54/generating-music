namespace MidiModel
{
    /// <summary>
    /// Model for time division; is is determines how to transform the internal midi unit of 
    /// measurement - clock ticks - into real time values
    /// </summary>
    public abstract class AbstractTimeDivision
    {
    }

    public class TicksPerBeatTimeDivision : AbstractTimeDivision
    {
        public ushort NumberOfClockTicks;
    }

    public class FramesPerSecondTimeDivision : AbstractTimeDivision
    {
        public byte NumberOfFrames;
        public byte TicksPerFrame;
    }
}
