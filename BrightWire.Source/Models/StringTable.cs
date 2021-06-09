using System.IO;
using ProtoBuf;

namespace BrightWire.Models
{
    /// <summary>
    /// An array of indexed strings
    /// </summary>
    [ProtoContract]
    public class StringTable : ISerialisable
    {
        /// <summary>
        /// The array of indexed strings
        /// </summary>
        [ProtoMember(1)]
        public string[] Data { get; set; }

        /// <inheritdoc />
        public void ReadFrom(BinaryReader reader)
        {
            var len = reader.ReadInt32();
            Data = new string[len];
            for (var i = 0; i < len; i++)
                Data[i] = reader.ReadString();
        }

        /// <inheritdoc />
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(Data?.Length ?? 0);
            if (Data?.Length > 0)
            {
                foreach (var item in Data)
                    writer.Write(item);
            }
        }
    }
}
