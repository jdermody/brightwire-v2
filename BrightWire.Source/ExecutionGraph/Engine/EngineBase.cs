﻿using BrightWire.ExecutionGraph.Engine.Helper;
using BrightWire.ExecutionGraph.Helper;
using BrightWire.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrightWire.ExecutionGraph.Engine
{
    abstract class EngineBase
    {
        protected readonly ILinearAlgebraProvider _lap;
        protected IDataSource _dataSource = null;

        protected EngineBase(ILinearAlgebraProvider lap) { _lap = lap; }

        protected abstract void _ClearContextList();
        protected abstract void _Execute(IExecutionContext context, IMiniBatch miniBatch);
        protected abstract IReadOnlyList<ExecutionResult> _GetResults();

        protected bool _Continue(IMiniBatch batch, IExecutionContext executionContext, Func<IMiniBatchSequence, IContext> lookupContext)
        {
            var ret = false;

	        while (executionContext.HasContinuations) {
                batch.Reset();
	            IMiniBatchSequence curr = null;
	            while ((curr = batch.GetNextSequence()) != null) {
                    var context = lookupContext(curr);
                    executionContext.Continue(context);
                    while (context.HasNext)
                        context.ExecuteNext();
                }
                ret = true;
            }
            return ret;
        }

        public ExecutionResult Execute(float[] input)
        {
            _lap.PushLayer();
            ExecutionResult ret = null;
            _dataSource = new SingleRowDataSource(input, false, MiniBatchSequenceType.Standard, 0);
            var provider = new MiniBatchProvider(_dataSource, false);
            using (var executionContext = new ExecutionContext(_lap)) {
                executionContext.Add(provider.GetMiniBatches(1, mb => _Execute(executionContext, mb)));

                IGraphOperation operation;
                while ((operation = executionContext.GetNextOperation()) != null) {
                    _lap.PushLayer();
                    operation.Execute(executionContext);
                    ret = _GetResults().Single();
                    _ClearContextList();
                    _lap.PopLayer();
                }
            }
            _lap.PopLayer();
            _dataSource = null;
            return ret;
        }

        protected ExecutionResult _Execute(float[] input)
        {
            _lap.PushLayer();
            ExecutionResult ret = null;
            _dataSource = new SingleRowDataSource(input, false, MiniBatchSequenceType.Standard, 0);
            var provider = new MiniBatchProvider(_dataSource, _lap.IsStochastic);
            using (var executionContext = new ExecutionContext(_lap)) {
                executionContext.Add(provider.GetMiniBatches(1, mb => _Execute(executionContext, mb)));

                IGraphOperation operation;
                while ((operation = executionContext.GetNextOperation()) != null) {
                    operation.Execute(executionContext);
                    _ClearContextList();
                }

                ret = _GetResults().Single();
            }
            _lap.PopLayer();
            _dataSource = null;
            return ret;
        }

        public IReadOnlyList<ExecutionResult> ExecuteSequential(IReadOnlyList<float[]> input)
        {
            _lap.PushLayer();
            var ret = new List<ExecutionResult>();
            _dataSource = new SequentialRowDataSource(input);
            var provider = new MiniBatchProvider(_dataSource, false);
            using (var executionContext = new ExecutionContext(_lap)) {
                executionContext.Add(provider.GetMiniBatches(1, mb => _Execute(executionContext, mb)));

                IGraphOperation operation;
                while ((operation = executionContext.GetNextOperation()) != null) {
                    _lap.PushLayer();
                    operation.Execute(executionContext);
                    ret.AddRange(_GetResults());
                    _ClearContextList();
                    _lap.PopLayer();
                }
            }
            _lap.PopLayer();
            _dataSource = null;
            return ret;
        }

        public ExecutionResult ExecuteSequential(int sequenceIndex, float[] input, IExecutionContext executionContext, MiniBatchSequenceType sequenceType)
        {
            _lap.PushLayer();
            _dataSource = new SingleRowDataSource(input, true, sequenceType, sequenceIndex);
            var provider = new MiniBatchProvider(_dataSource, _lap.IsStochastic);
            executionContext.Add(provider.GetMiniBatches(1, mb => _Execute(executionContext, mb)));

            IGraphOperation operation;
            while ((operation = executionContext.GetNextOperation()) != null) {
                operation.Execute(executionContext);
                _ClearContextList();
            }

            var ret = _GetResults().Single();
            _lap.PopLayer();
            _dataSource = null;
            return ret;
        }
    }
}
