﻿using System;
using BrightWire.Source.Helper;

namespace BrightWire.ExecutionGraph.Action
{
    /// <summary>
    /// Backpropagates the graph against the error metric
    /// </summary>
    class Backpropagate : IAction
    {
        IErrorMetric _errorMetric;

        public Backpropagate(IErrorMetric errorMetric)
        {
            _errorMetric = errorMetric;
        }

        public void Initialise(string data)
        {
            _errorMetric = (IErrorMetric)Activator.CreateInstance(TypeLoader.LoadType(data));
        }

        public string Serialise()
        {
            return _errorMetric.GetType().AssemblyQualifiedName;
        }

        public IGraphData Execute(IGraphData input, IContext context)
        {
            var output = input.GetMatrix();
            if (context.IsTraining) {
				if(context.LearningContext.ErrorMetric == null)
					context.LearningContext.ErrorMetric = _errorMetric;

	            var gradient = _errorMetric.CalculateGradient(context, output, context.BatchSequence.Target.GetMatrix());
                context.Backpropagate(input.ReplaceWith(gradient));
            }
            return input;
        }
    }
}
