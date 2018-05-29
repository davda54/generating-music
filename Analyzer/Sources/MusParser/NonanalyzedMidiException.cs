using System;
using System.Runtime.Serialization;

namespace MusParser
{ 
    public class NonanalyzedMidiException : Exception
    {
        public NonanalyzedMidiException()
        {
        }

        public NonanalyzedMidiException(string message) : base(message)
        {
        }

        public NonanalyzedMidiException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NonanalyzedMidiException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
