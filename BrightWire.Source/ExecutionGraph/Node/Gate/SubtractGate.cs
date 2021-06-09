﻿using System.Collections.Generic;
using System.Linq;

namespace BrightWire.ExecutionGraph.Node.Gate
{
    /// <summary>
    /// Subtracts the second input from the first input
    /// </summary>
    class SubtractGate : BinaryGateBase
    {
        class Backpropagation : BackpropagationBase<SubtractGate>
        {
            public Backpropagation(SubtractGate source) : base(source)
            {
            }

            public override void _Backward(INode fromNode, IGraphData errorSignal, IContext context, IReadOnlyList<INode> parents)
            {
                var es = errorSignal.GetMatrix();
                var negative = es.Clone();
                negative.Multiply(-1f);

                context.AddBackward(errorSignal, parents.First(), _source);
                context.AddBackward(errorSignal.ReplaceWith(negative), parents.Last(), _source);
            }
        }
        public SubtractGate(string name = null) : base(name) { }

        protected override void _Activate(IContext context, IMatrix primary, IMatrix secondary)
        {
            var output = primary.Subtract(secondary);
            _AddHistory(context, output, () => new Backpropagation(this));
        }
    }
}
