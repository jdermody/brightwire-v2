using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using ProtoBuf;

namespace BrightWire.Models
{
    static class ModelSerialisation
    {
        static void WriteTo(this String str, BinaryWriter writer) => writer.Write(str ?? "");
        static void WriteTo(this int val, BinaryWriter writer) => writer.Write(val);
        static void WriteTo(this IReadOnlyList<IWriteToBinaryWriter> list, BinaryWriter writer)
        {
            writer.Write(list?.Count ?? 0);
            if (list?.Count > 0)
            {
                foreach (var item in list)
                    item.WriteTo(writer);
            }
        }

        public static T Create<T>(BinaryReader reader)
            where T : IReadFromBinaryReader
        {
            var ret = (T)FormatterServices.GetUninitializedObject(typeof(T));
            ret.ReadFrom(reader);
            return ret;
        }

        static T[] CreateArray<T>(BinaryReader reader)
            where T : IReadFromBinaryReader
        {
            var len = reader.ReadInt32();
            var ret = new T[len];
            for (var i = 0; i < len; i++)
                ret[i] = Create<T>(reader);
            return ret;
        }

        public static void WriteTo(GraphModel model, BinaryWriter writer)
        {
            model.Version.WriteTo(writer);
            model.Name.WriteTo(writer);
            model.Graph.WriteTo(writer);
            writer.Write(model.DataSource != null);
            model.DataSource?.WriteTo(writer);
        }

        public static void ReadFrom(BinaryReader reader, GraphModel model)
        {
            model.Version = reader.ReadString();
            model.Name = reader.ReadString();
            model.Graph = Create<ExecutionGraph>(reader);
            if (reader.ReadBoolean())
                model.DataSource = Create<DataSourceModel>(reader);
        }

        public static void WriteTo(DataSourceModel model, BinaryWriter writer)
        {
            model.Version.WriteTo(writer);
            model.Name.WriteTo(writer);
            model.InputSize.WriteTo(writer);
            model.OutputSize.WriteTo(writer);
            model.Graph.WriteTo(writer);
        }

        public static void ReadFrom(BinaryReader reader, DataSourceModel model)
        {
            model.Version = reader.ReadString();
            model.Name = reader.ReadString();
            model.InputSize = reader.ReadInt32();
            model.OutputSize = reader.ReadInt32();
            model.Graph = Create<ExecutionGraph>(reader);
        }

        public static void WriteTo(ExecutionGraph model, BinaryWriter writer)
        {
            model.Version.WriteTo(writer);
            model.Name.WriteTo(writer);
            model.InputNode.WriteTo(writer);
            model.OtherNodes.WriteTo(writer);
            model.Wires.WriteTo(writer);
        }

        public static void ReadFrom(BinaryReader reader, ExecutionGraph model)
        {
            model.Version = reader.ReadString();
            model.Name = reader.ReadString();
            model.InputNode = Create<ExecutionGraph.Node>(reader);
            model.OtherNodes = CreateArray<ExecutionGraph.Node>(reader);
            model.Wires = CreateArray<ExecutionGraph.Wire>(reader);
        }

        static void CopyProtobufSerialisedNode(BinaryReader reader, BinaryWriter writer)
        {
            using (var buffer = new MemoryStream(reader.ReadBytes(reader.ReadInt32())))
            {
                var model = Serializer.Deserialize<Models.ExecutionGraph.Node>(buffer);
                model.WriteTo(writer);
            }
        }

        public static void WriteTo(ExecutionGraph.Node model, BinaryWriter writer)
        {
            // special case handling for simple recurrent and elman jordan that use protobuf internally
            if (model.TypeName == "BrightWire.ExecutionGraph.Node.Layer.SimpleRecurrent")
            {
                using (var newOutputBuffer = new MemoryStream())
                using (var newOutputWriter = new BinaryWriter(newOutputBuffer, Encoding.UTF8, true))
                using (var buffer = new MemoryStream(model.Data))
                {
                    var reader = new BinaryReader(buffer);
                    var inputSize = reader.ReadInt32();
                    var memoryId = reader.ReadString();
                    var memory = FloatVector.ReadFrom(reader);

                    newOutputWriter.Write(inputSize);
                    newOutputWriter.Write(memoryId);
                    memory.WriteTo(newOutputWriter);

                    CopyProtobufSerialisedNode(reader, newOutputWriter);
                    newOutputWriter.Flush();

                    // copy the rest of the buffer
                    buffer.CopyTo(newOutputBuffer);
                    model.Data = newOutputBuffer.ToArray();
                }
            }
            else if (model.TypeName == "BrightWire.ExecutionGraph.Node.Layer.ElmanJordan")
            {
                using (var newOutputBuffer = new MemoryStream())
                using (var newOutputWriter = new BinaryWriter(newOutputBuffer, Encoding.UTF8, true))
                using (var buffer = new MemoryStream(model.Data))
                {
                    var reader = new BinaryReader(buffer);
                    var isElman = reader.ReadBoolean();
                    var inputSize = reader.ReadInt32();
                    var memoryId = reader.ReadString();
                    var memory = FloatVector.ReadFrom(reader);

                    writer.Write(isElman);
                    writer.Write(inputSize);
                    writer.Write(memoryId);
                    memory.WriteTo(writer);

                    CopyProtobufSerialisedNode(reader, newOutputWriter);
                    CopyProtobufSerialisedNode(reader, newOutputWriter);

                    // copy the rest of the buffer
                    newOutputWriter.Flush();
                    buffer.CopyTo(newOutputBuffer);
                    model.Data = newOutputBuffer.ToArray();
                }
            }
            model.TypeName.WriteTo(writer);
            model.Id.WriteTo(writer);
            model.Name.WriteTo(writer);
            model.Description.WriteTo(writer);
            writer.Write(model.Data?.Length ?? 0);
            if (model.Data?.Length > 0)
                writer.Write(model.Data);
        }

        public static void ReadFrom(BinaryReader reader, ExecutionGraph.Node model)
        {
            model.TypeName = reader.ReadString();
            model.Id = reader.ReadString();
            model.Name = reader.ReadString();
            model.Description = reader.ReadString();
            var len = reader.ReadInt32();
            model.Data = reader.ReadBytes(len);
        }

        public static void WriteTo(ExecutionGraph.Wire model, BinaryWriter writer)
        {
            model.FromId.WriteTo(writer);
            model.ToId.WriteTo(writer);
            model.InputChannel.WriteTo(writer);
        }

        public static void ReadFrom(BinaryReader reader, ExecutionGraph.Wire model)
        {
            model.FromId = reader.ReadString();
            model.ToId = reader.ReadString();
            model.InputChannel = reader.ReadInt32();
        }
    }
}
