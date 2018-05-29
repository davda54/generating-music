using System.IO;

namespace MidiParser
{
    public class BigEndianWriter: BinaryWriter
    {
        public BigEndianWriter(Stream output): base(output) { }

        public override void Write(ushort value)
        {
            byte[] b = { (byte)(value >> 8), (byte) value };
            Write(b);
        }

        public void WriteUint24(uint value)
        {
            byte[] b = { (byte)(value >> 16), (byte)(value >> 8), (byte)value };
            Write(b);
        }

        public override void Write(uint value)
        {
            byte[] b = { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };
            Write(b);
        }
    }
}
