﻿using System.IO;
using ProtoBuf;

namespace BrightWire.Models
{
    /// <summary>
    /// A serialised execution graph
    /// </summary>
    [ProtoContract]
    public class ExecutionGraph : ISerialisable
    {
        /// <summary>
        /// A node within the graph
        /// </summary>
        [ProtoContract]
        public class Node : ISerialisable
        {
            /// <summary>
            /// The .NET type name of the node type
            /// </summary>
            [ProtoMember(1)]
            public string TypeName { get; set; }

            /// <summary>
            /// The unique id within the graph
            /// </summary>
            [ProtoMember(2)]
            public string Id { get; set; }

            /// <summary>
            /// Node friendly name
            /// </summary>
            [ProtoMember(3)]
            public string Name { get; set; }

            /// <summary>
            /// A short description of the node
            /// </summary>
            [ProtoMember(4)]
            public string Description { get; set; }

            /// <summary>
            /// The node's parameters
            /// </summary>
            [ProtoMember(5)]
            public byte[] Data { get; set; }

            /// <inheritdoc />
            public void ReadFrom(BinaryReader reader) => ModelSerialisation.ReadFrom(reader, this);

            /// <inheritdoc />
            public void WriteTo(BinaryWriter writer) => ModelSerialisation.WriteTo(this, writer);
        }

        /// <summary>
        /// Wires connect nodes (aka edges)
        /// </summary>
        [ProtoContract]
        public class Wire : ISerialisable
        {
            /// <summary>
            /// The source node id
            /// </summary>
            [ProtoMember(1)]
            public string FromId { get; set; }

            /// <summary>
            /// The target node id
            /// </summary>
            [ProtoMember(2)]
            public string ToId { get; set; }

            /// <summary>
            /// The channel on the target node to send the source node's output
            /// </summary>
            [ProtoMember(3)]
            public int InputChannel { get; set; }

            /// <inheritdoc />
            public override string ToString()
            {
                return $"{FromId} -> {ToId} on channel {InputChannel}";
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return (FromId + ToId + InputChannel).GetHashCode();
            }

            /// <inheritdoc />
            public void WriteTo(BinaryWriter writer) => ModelSerialisation.WriteTo(this, writer);

            /// <inheritdoc />
            public void ReadFrom(BinaryReader reader) => ModelSerialisation.ReadFrom(reader, this);

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                if (obj is Wire other)
                    return FromId == other.FromId && ToId == other.ToId && InputChannel == other.InputChannel;
                return false;
            }
        }

        /// <summary>
        /// Data contract version number
        /// </summary>
        [ProtoMember(1)]
        public string Version { get; set; } = "2.0";

        /// <summary>
        /// The name of the graph
        /// </summary>
        [ProtoMember(2)]
        public string Name { get; set; }

        /// <summary>
        /// The primary input node
        /// </summary>
        [ProtoMember(3)]
        public Node InputNode { get; set; }

        /// <summary>
        /// Other connected nodes
        /// </summary>
        [ProtoMember(4)]
        public Node[] OtherNodes { get; set; }

        /// <summary>
        /// A list of the wires that connect the nodes in the graph
        /// </summary>
        [ProtoMember(5)]
        public Wire[] Wires { get; set; }

        /// <inheritdoc />
        public void ReadFrom(BinaryReader reader) => ModelSerialisation.ReadFrom(reader, this);

        /// <inheritdoc />
        public void WriteTo(BinaryWriter writer) => ModelSerialisation.WriteTo(this, writer);
    }
}
