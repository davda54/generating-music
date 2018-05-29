using System.IO;

namespace MidiParser
{
    /// <summary>
    /// Binary reader that reads data in Big Endian order
    /// </summary>
    public class BigEndianReader : BinaryReader
    {
        public BigEndianReader(Stream input) : base(input)
        {
        }

        public override ushort ReadUInt16()
        {
            byte[] b = ReadBytes(2);
            return (ushort) (b[1] | (b[0] << 8));
        }

        public uint ReadUInt24()
        {
            byte[] b = ReadBytes(3);
            return (uint) (b[2] | (b[1] << 8) | (b[0] << 16));
        }

        public override uint ReadUInt32()
        {
            byte[] b = ReadBytes(4);
            return (uint) (b[3] | (b[2] << 8) | (b[1] << 16) | (b[0] << 24));
        }

        public override ulong ReadUInt64()
        {
            byte[] b = ReadBytes(8);
            return (ulong) (b[7] | (b[6] << 8) | (b[5] << 16) | (b[4] << 24) | (b[3] << 32) | (b[2] << 40) | (b[1] << 48) | (b[0] << 56));
        }
    }
}