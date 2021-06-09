﻿using BrightWire.ExecutionGraph.Helper;
using System;

namespace BrightWire.ExecutionGraph.Node.Gate
{
    /// <summary>
    /// Base class for nodes that accept two input signals and output one signal
    /// </summary>
    public abstract class BinaryGateBase : NodeBase
    {
        IMatrix _primary = null, _secondary = null;
        INode _primarySource, _secondarySource = null;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="name"></param>
        public BinaryGateBase(string name) : base(name) { }

        /// <summary>
        /// Executes on the primary channel
        /// </summary>
        /// <param name="context">The graph context</param>
        public override void ExecuteForward(IContext context)
        {
            _primarySource = context.Source;
            _primary = context.Data.GetMatrix();
            _TryComplete(context);
        }

        /// <summary>
        /// Executes on a secondary channel
        /// </summary>
        /// <param name="context">The graph context</param>
        /// <param name="channel">The channel</param>
        protected override void _ExecuteForward(IContext context, int channel)
        {
            if (channel == 1) {
                _secondarySource = context.Source;
                _secondary = context.Data.GetMatrix();
                _TryComplete(context);
            }
        }

        void _TryComplete(IContext context)
        {
            if (_primary != null && _secondary != null) {
                _Activate(context, _primary, _secondary);
                _primary = _secondary = null;
                _primarySource = _secondarySource = null;
            }
        }

        /// <summary>
        /// When both the primary and secondary inputs have arrived
        /// </summary>
        /// <param name="context">Graph context</param>
        /// <param name="primary">Primary signal</param>
        /// <param name="secondary">Secondary signal</param>
        protected abstract void _Activate(IContext context, IMatrix primary, IMatrix secondary);

        /// <summary>
        /// Records the network activity
        /// </summary>
        /// <param name="context">Graph context</param>
        /// <param name="output">The output signal</param>
        /// <param name="backpropagation">Backpropagation creator (optional)</param>
        protected void _AddHistory(IContext context, IMatrix output, Func<IBackpropagation> backpropagation)
        {
            context.AddForward(new TrainingAction(this, new MatrixGraphData(output), new[] { _primarySource, _secondarySource }), backpropagation);
        }
    }
}
