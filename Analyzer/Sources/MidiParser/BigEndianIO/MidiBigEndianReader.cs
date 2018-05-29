using System;
using System.IO;
using System.Text;

namespace MidiParser
{
    /// <summary>
    /// Extension of MidiParser.Parser.BigEndianReader for reading midi-specific data
    /// </summary>
    public class MidiBigEndianReader : BigEndianReader
    {

        public MidiBigEndianReader(Stream input) : base(input)
        {
        }

        /// <summary>
        /// Reads a string from the input
        /// </summary>
        /// <param name="length">The number of characters to read</param>
        public string ReadText(uint length)
        {
            var output = new StringBuilder((int) length);

            for (uint i = 0; i < length; i++)
            {
                output.Append((char) ReadByte());
            }

            return output.ToString();
        }

        /// <summary>
        /// Reads a value in variable-length format, see midi specification for more details
        /// </summary>
        /// <param name="length">Output parameter that gives the final length of the returned value in bytes</param>
        public uint ReadVariableLengthValue(out byte length)
        {
            length = 0;
            uint value = 0;

            while (true)
            {
                var c = ReadByte();
                length++;
                value = (value << 7) | (uint) (c & 0x7F);

                if ((c & 0x80) == 0) break;
                if (length >= 4) throw new FormatException("Variable length value shouldn't be longer than 4 bytes");
            }

            return value;
        }
    }
}