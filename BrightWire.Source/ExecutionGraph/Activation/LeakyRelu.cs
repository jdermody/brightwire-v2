﻿using BrightWire.ExecutionGraph.Node;
using System.Collections.Generic;

namespace BrightWire.ExecutionGraph.Activation
{
    /// <summary>
    /// Leaky RELU activation
    /// https://en.wikipedia.org/wiki/Rectifier_(neural_networks)
    /// </summary>
    class LeakyRelu : NodeBase
    {
        class Backpropagation : SingleBackpropagationBase<LeakyRelu>
        {
            readonly IMatrix _input;

            public Backpropagation(LeakyRelu source, IMatrix matrix) : base(source)
            {
                _input = matrix;
            }

            protected override IGraphData _Backpropagate(INode fromNode, IGraphData errorSignal, IContext context, IReadOnlyList<INode> parents)
            {
                using (var od = _input.LeakyReluDerivative()) {
                    var delta = errorSignal.GetMatrix().PointwiseMultiply(od);
                    return errorSignal.ReplaceWith(delta);
                }
            }
        }

        public LeakyRelu(string name = null) : base(name) { }

        public override void ExecuteForward(IContext context)
        {
            var input = context.Data.GetMatrix();
            var output = context.Data.ReplaceWith(input.LeakyReluActivation());
            _AddNextGraphAction(context, output, () => new Backpropagation(this, input));
        }
    }
}
