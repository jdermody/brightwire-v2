﻿using System.Collections.Generic;
using System.Linq;
using BrightWire.Models;
using BrightWire.ExecutionGraph.Helper;

namespace BrightWire.ExecutionGraph.DataTableAdaptor
{
    /// <summary>
    /// Adapts data tables that classify tensors (volumes)
    /// </summary>
    class TensorBasedDataTableAdaptor : RowBasedDataTableAdaptorBase, IVolumeDataSource
    {
        readonly int _inputSize, _outputSize;

        public TensorBasedDataTableAdaptor(ILinearAlgebraProvider lap, IDataTable dataTable)
            : base(lap, dataTable)
        {
            var firstRow = dataTable.GetRow(0);
            var input = (FloatTensor)firstRow.Data[_dataColumnIndex[0]];
            var output = (FloatVector)firstRow.Data[_dataTargetIndex];
            _outputSize = output.Size;
            _inputSize = input.Size;
            Height = input.RowCount;
            Width = input.ColumnCount;
            Depth = input.Depth;
        }

        TensorBasedDataTableAdaptor(ILinearAlgebraProvider lap, IDataTable dataTable, int inputSize, int outputSize, int rows, int columns, int depth) 
            :base(lap, dataTable)
        {
            _inputSize = inputSize;
            _outputSize = outputSize;
            Height = rows;
            Width = columns;
            Depth = depth;
        }

        public override IDataSource CloneWith(IDataTable dataTable)
        {
            return new TensorBasedDataTableAdaptor(_lap, dataTable, _inputSize, _outputSize, Height, Width, Depth);
        }

        public override bool IsSequential => false;
        public override int InputSize => _inputSize;
        public override int OutputSize => _outputSize;
        public int Width { get; }
	    public int Height { get; }
	    public int Depth { get; }

	    public override IMiniBatch Get(IExecutionContext executionContext, IReadOnlyList<int> rows)
        {
            var data = _GetRows(rows)
                .Select(r => (_dataColumnIndex.Select(i => ((FloatTensor)r.Data[i]).GetAsRaw()).ToList(), ((FloatVector)r.Data[_dataTargetIndex]).Data))
                .ToList()
            ;
            var inputList = new List<IGraphData>();
            for (var i = 0; i < _dataColumnIndex.Length; i++) {
	            var i1 = i;
	            var input = _lap.CreateMatrix(InputSize, data.Count, (x, y) => data[y].Item1[i1][x]);
                var tensor = new Tensor4DGraphData(input, Height, Width, Depth);
                inputList.Add(tensor);
            }
            var output = OutputSize > 0 ? _lap.CreateMatrix(data.Count, OutputSize, (x, y) => data[x].Item2[y]) : null;
            
            return new MiniBatch(rows, this, inputList, new MatrixGraphData(output));
        }
    }
}
