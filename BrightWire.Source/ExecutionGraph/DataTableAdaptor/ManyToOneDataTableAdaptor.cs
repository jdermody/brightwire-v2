﻿using BrightWire.ExecutionGraph.Helper;
using BrightWire.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BrightWire.ExecutionGraph.DataTableAdaptor
{
    /// <summary>
    /// Adapts data tables that classify a sequence into a single classification
    /// </summary>
    class ManyToOneDataTableAdaptor : RowBasedDataTableAdaptorBase
    {
        readonly int[] _rowDepth;

	    public ManyToOneDataTableAdaptor(ILinearAlgebraProvider lap, IDataTable dataTable) 
            : base(lap, dataTable)
        {
            if (_dataColumnIndex.Length > 1)
                throw new NotImplementedException("Sequential datasets not supported with more than one input data column");

            _rowDepth = new int[dataTable.RowCount];
            FloatMatrix inputMatrix = null;
            FloatVector outputVector = null;
            dataTable.ForEach((row, i) => {
                inputMatrix = row.GetField<FloatMatrix>(_dataColumnIndex[0]);
                outputVector = row.GetField<FloatVector>(_dataTargetIndex);
                _rowDepth[i] = inputMatrix.RowCount;
                if (inputMatrix.ColumnCount != outputVector.Size)
                    throw new ArgumentException("Rows between input and output data tables do not match");
            });

            InputSize = inputMatrix.ColumnCount;
            OutputSize = outputVector.Size;
        }

        public override IDataSource CloneWith(IDataTable dataTable)
        {
            return new ManyToOneDataTableAdaptor(_lap, dataTable);
        }

        public override bool IsSequential => true;
        public override int InputSize { get; }
	    public override int OutputSize { get; }

	    public override IReadOnlyList<IReadOnlyList<int>> GetBuckets()
        {
            return _rowDepth
                .Select((r, i) => (r, i))
                .GroupBy(t => t.Item1)
                .Select(g => g.Select(d => d.Item2).ToList())
                .ToList()
            ;
        }

        public override IMiniBatch Get(IExecutionContext executionContext, IReadOnlyList<int> rows)
        {
            var data = _GetRows(rows)
                .Select(r => ((FloatMatrix)r.Data[_dataColumnIndex[0]], (FloatVector)r.Data[_dataTargetIndex]))
                .ToList()
            ;
            var inputData = new Dictionary<int, List<FloatVector>>();
            foreach (var item in data) {
                var input = item.Item1;
                for (int i = 0, len = input.RowCount; i < len; i++) {
                    if (!inputData.TryGetValue(i, out List<FloatVector> temp))
                        inputData.Add(i, temp = new List<FloatVector>());
                    temp.Add(input.Row[i]);
                }
            }

            var miniBatch = new MiniBatch(rows, this);
            var outputVector = _lap.CreateMatrix(data.Count, OutputSize, (x, y) => data[x].Item2.Data[y]);
            foreach (var item in inputData.OrderBy(kv => kv.Key)) {
                var input = _lap.CreateMatrixFromRows(item.Value);
                var type = (item.Key == 0)
                    ? MiniBatchSequenceType.SequenceStart
                    : item.Key == (inputData.Count - 1)
                        ? MiniBatchSequenceType.SequenceEnd
                        : MiniBatchSequenceType.Standard
                ;
                var inputList = new List<IGraphData> {
                    new MatrixGraphData(input)
                };
                miniBatch.Add(type, inputList, type == MiniBatchSequenceType.SequenceEnd ? new MatrixGraphData(outputVector) : null);
            }
            return miniBatch;
        }
    }
}
