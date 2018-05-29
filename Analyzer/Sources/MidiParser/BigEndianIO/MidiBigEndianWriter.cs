using System.IO;

namespace MidiParser.BigEndian
{
    public class MidiBigEndianWriter: BigEndianWriter
    {
        public MidiBigEndianWriter(Stream output) : base(output)
        {
        }

        /// <summary>
        /// Writes a value in the variable-length format, that has constant size of 4 bytes
        /// </summary>
        public void WriteSemiVariableLengthValue(uint value)
        {
            byte[] b = { (byte) ((value >> 21) | 0x80), (byte) ((value >> 14) | 0x80), (byte) ((value >> 7) | 0x80),(byte) (value & 0x7F) };
            Write(b);
        }

        public byte WriteVariableLengthValue(uint value)
        {
            byte[] b =  { (byte)(value >> 21), (byte)(value >> 14), (byte)(value >> 7), (byte)value };

            if (b[0] != 0)
            {
                Write((byte) (b[0] | 0x80));
                Write((byte) (b[1] | 0x80));
                Write((byte) (b[2] | 0x80));
                Write((byte) (b[3] & 0x7F));

                return 4;
            }
            else if (b[1] != 0)
            {
                Write((byte)(b[1] | 0x80));
                Write((byte)(b[2] | 0x80));
                Write((byte)(b[3] & 0x7F));

                return 3;
            }
            else if (b[2] != 0)
            {
                Write((byte)(b[2] | 0x80));
                Write((byte)(b[3] & 0x7F));

                return 2;
            }
            else
            {
                Write((byte)(b[3] & 0x7F));

                return 1;
            }
        }
    }
}
