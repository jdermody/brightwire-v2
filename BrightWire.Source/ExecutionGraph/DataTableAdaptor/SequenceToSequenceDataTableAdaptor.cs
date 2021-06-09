﻿using BrightWire.ExecutionGraph.Engine.Helper;
using BrightWire.ExecutionGraph.Helper;
using BrightWire.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrightWire.ExecutionGraph.DataTableAdaptor
{
    /// <summary>
    /// Executes a preliminary graph and uses its output as the input for the main graph
    /// </summary>
    class SequenceToSequenceDataTableAdaptor : AdaptiveDataTableAdaptorBase
    {
        int[] _rowDepth;
        int _inputSize, _outputSize;

        public SequenceToSequenceDataTableAdaptor(ILinearAlgebraProvider lap, ILearningContext learningContext, GraphFactory factory, IDataTable dataTable, Action<WireBuilder> dataConversionBuilder)
            : base(lap, learningContext, dataTable)
        {
            _Initialise(dataTable);

            var wireBuilder = factory.Connect(_inputSize, _input);
            dataConversionBuilder(wireBuilder);

            // execute the graph to find the input size (which is the size of the adaptive graph's output)
            using (var executionContext = new ExecutionContext(lap)) {
                var output = _Encode(executionContext, new[] { 0 });
                _inputSize = output.Item1.ColumnCount;
                _learningContext.Clear();
            }
        }

        public SequenceToSequenceDataTableAdaptor(ILinearAlgebraProvider lap, ILearningContext learningContext, IDataTable dataTable, INode input, DataSourceModel dataSource)
            : base(lap, learningContext, dataTable)
        {
            _Initialise(dataTable);
            _input = input;
            _inputSize = dataSource.InputSize;
            _outputSize = dataSource.OutputSize;
        }

        void _Initialise(IDataTable dataTable)
        {
            _rowDepth = new int[dataTable.RowCount];
            FloatMatrix inputMatrix = null, outputMatrix = null;
            dataTable.ForEach((row, i) => {
                inputMatrix = row.GetField<FloatMatrix>(0);
                outputMatrix = row.GetField<FloatMatrix>(1);
                _rowDepth[i] = outputMatrix.RowCount;
            });

            _inputSize = inputMatrix.ColumnCount;
            _outputSize = outputMatrix.ColumnCount;
        }

        private SequenceToSequenceDataTableAdaptor(ILinearAlgebraProvider lap, ILearningContext learningContext, IDataTable dataTable, INode input, int inputSize, int outputSize)
            : base(lap, learningContext, dataTable)
        {
            _Initialise(dataTable);
            _input = input;
            _inputSize = inputSize;
            _outputSize = outputSize;
        }

        public override IDataSource CloneWith(IDataTable dataTable)
        {
            return new SequenceToSequenceDataTableAdaptor(_lap, _learningContext, dataTable, _input, _inputSize, _outputSize);
        }

        public override bool IsSequential => true;
        public override int InputSize => _inputSize;
        public override int OutputSize => _outputSize;

        public override IReadOnlyList<IReadOnlyList<int>> GetBuckets()
        {
            return _rowDepth
                .Select((r, i) => (r, i))
                .GroupBy(t => t.Item1)
                .Select(g => g.Select(d => d.Item2).ToList())
                .ToList()
            ;
        }

        (IMatrix, IReadOnlyList<IRow>) _Encode(IExecutionContext executionContext, IReadOnlyList<int> rows)
        {
            var data = _GetRows(rows);

            // create the input batch
            var inputData = new List<(FloatMatrix Input, FloatMatrix Output)>();
            foreach (var row in data)
                inputData.Add(((FloatMatrix)row.Data[0], null));
            var encoderInput = _GetSequentialMiniBatch(rows, inputData);

            // execute the encoder
            IMiniBatchSequence sequence;
            IMatrix encoderOutput = null;
            while ((sequence = encoderInput.GetNextSequence()) != null) {
                using (var context = _Process(executionContext, sequence)) {
                    if (sequence.Type == MiniBatchSequenceType.SequenceEnd)
                        encoderOutput = context.Data.GetMatrix();
                }
            }
            return (encoderOutput, data);
        }

        public override IMiniBatch Get(IExecutionContext executionContext, IReadOnlyList<int> rows)
        {
            (var encoderOutput, var data) = _Encode(executionContext, rows);

            // create the decoder input
            var outputData = new Dictionary<int, List<FloatVector>>();
            foreach (var item in data) {
                var output = item.GetField<FloatMatrix>(1);
                for (int i = 0, len = output.RowCount; i < len; i++) {
                    if (!outputData.TryGetValue(i, out List<FloatVector> temp))
                        outputData.Add(i, temp = new List<FloatVector>());
                    temp.Add(output.Row[i]);
                }
            }
            var miniBatch = new MiniBatch(rows, this);
            var curr = encoderOutput;
            foreach (var item in outputData.OrderBy(kv => kv.Key)) {
                var output = _lap.CreateMatrixFromRows(item.Value);
                var type = (item.Key == 0)
                    ? MiniBatchSequenceType.SequenceStart
                    : item.Key == (outputData.Count - 1)
                        ? MiniBatchSequenceType.SequenceEnd
                        : MiniBatchSequenceType.Standard
                ;
                var inputList = new List<IGraphData> {
                    new MatrixGraphData(curr)
                };
                miniBatch.Add(type, inputList, new MatrixGraphData(output));
                curr = output;
            }
            return miniBatch;
        }

        public override void OnBatchProcessed(IContext context)
        {
            var batch = context.BatchSequence;
            if(context.IsTraining && batch.Type == MiniBatchSequenceType.SequenceStart) {
                context.LearningContext.DeferBackpropagation(null, signal => {
                    _learningContext.BackpropagateThroughTime(signal);
                });
            }
        }
    }
}
