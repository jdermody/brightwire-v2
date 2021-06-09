﻿using System.Collections.Generic;

namespace BrightWire.ExecutionGraph.Helper
{
	/// <inheritdoc />
	public class TrainingAction : IExecutionHistory
    {
		/// <summary>
		/// Creates a training action history from a single optional parent
		/// </summary>
		/// <param name="source">The node that executed</param>
		/// <param name="data">The output of the node</param>
		/// <param name="parent">The single parent that contributed to the output (optional)</param>
	    public TrainingAction(INode source, IGraphData data, INode parent = null)
        {
            if (parent != null)
                Parents = new[] { parent };
            else
                Parents = new List<INode>();

            Source = source;
            Data = data;
        }

		/// <summary>
		/// Creates a training action history from multiple parents
		/// </summary>
		/// <param name="source">The node that executed</param>
		/// <param name="data">The output of the node</param>
		/// <param name="parents">The parent nodes that contributed to the output</param>
        public TrainingAction(INode source, IGraphData data, IReadOnlyList<INode> parents)
        {
            Parents = parents;
            Source = source;
            Data = data;
        }

	    /// <inheritdoc />
        public INode Source { get; }
	    /// <inheritdoc />
        public IGraphData Data { get; }
	    /// <inheritdoc />
        public IBackpropagation Backpropagation { get; set; }
	    /// <inheritdoc />
        public IReadOnlyList<INode> Parents { get; }
    }
}
