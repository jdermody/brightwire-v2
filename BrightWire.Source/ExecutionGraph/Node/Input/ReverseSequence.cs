﻿using BrightWire.ExecutionGraph.Helper;
using System.IO;

namespace BrightWire.ExecutionGraph.Node.Input
{
    /// <summary>
    /// Inputs the opposite sequential item from the input index (for bidirectional recurrent neural networks)
    /// https://en.wikipedia.org/wiki/Bidirectional_recurrent_neural_networks
    /// </summary>
    class ReverseSequence : NodeBase
    {
        int _inputIndex;

        public ReverseSequence(int inputIndex = 0, string name = null) : base(name)
        {
            _inputIndex = inputIndex;
        }

        public override void ExecuteForward(IContext context)
        {
            var curr = context.BatchSequence;
            var batch = curr.MiniBatch;
            var reversed = batch.GetSequenceAtIndex(batch.SequenceCount - curr.SequenceIndex - 1).Input;

            context.AddForward(new TrainingAction(this, reversed[_inputIndex], context.Source), null);
        }

        protected override (string Description, byte[] Data) _GetInfo()
        {
            return ("RS", _WriteData(WriteTo));
        }

        public override void WriteTo(BinaryWriter writer)
        {
            writer.Write(_inputIndex);
        }

        public override void ReadFrom(GraphFactory factory, BinaryReader reader)
        {
            _inputIndex = reader.ReadInt32();
        }
    }
}
