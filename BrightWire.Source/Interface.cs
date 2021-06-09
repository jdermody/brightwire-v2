using System.IO;

namespace BrightWire
{
    /// <summary>
    /// Legacy serialisation interface
    /// </summary>
    public interface ICanSerialiseToStream
    {
        /// <summary>
        /// Writes the current object state to the stream
        /// </summary>
        /// <param name="stream">Stream to write to</param>
        void SerialiseTo(Stream stream);

        /// <summary>
        /// Reads the current object state from the stream
        /// </summary>
        /// <param name="stream">Stream to read from</param>
        /// <param name="clear">True to clear the existing state</param>
        void DeserialiseFrom(Stream stream, bool clear);
    }

    /// <summary>
    /// Standard serialisation interface
    /// </summary>
    public interface IWriteToBinaryWriter
    {
        /// <summary>
        /// Saves data to the binary writer
        /// </summary>
        /// <param name="writer"></param>
        void WriteTo(BinaryWriter writer);
    }

    /// <summary>
    /// Standard serialisation interface
    /// </summary>
    public interface IReadFromBinaryReader
    {
        /// <summary>
        /// Loads data from the binary reader
        /// </summary>
        /// <param name="reader"></param>
        void ReadFrom(BinaryReader reader);
    }

    /// <summary>
    /// Standard serialisation interface
    /// </summary>
    public interface ISerialisable : IReadFromBinaryReader, IWriteToBinaryWriter
    {
    }

    // other declarations in nested files...
}
