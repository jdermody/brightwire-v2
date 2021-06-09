﻿using System.Collections.Generic;
using System.Linq;
using BrightWire.Models;
using BrightWire.ExecutionGraph.Helper;

namespace BrightWire.ExecutionGraph.DataTableAdaptor
{
    /// <summary>
    /// Base class for data table based data adaptors
    /// </summary>
    /// <typeparam name="T">The type of the cached data</typeparam>
    public abstract class DataTableAdaptorBase<T> : IDataSource
    {
        /// <summary>
        /// The data table columns with attributes
        /// </summary>
        protected readonly int[] _dataColumnIndex;

		/// <summary>
		/// Target column index
		/// </summary>
        protected readonly int _dataTargetIndex;

		/// <summary>
		/// Linear algebra provider
		/// </summary>
        protected readonly ILinearAlgebraProvider _lap;

		/// <summary>
		/// The list of raw row data
		/// </summary>
        protected readonly List<T> _data = new List<T>();

	    /// <inheritdoc />
	    protected DataTableAdaptorBase(ILinearAlgebraProvider lap, IDataTable dataTable)
        {
            _lap = lap;
            _dataTargetIndex = dataTable.TargetColumnIndex;
            _dataColumnIndex = Enumerable.Range(0, dataTable.ColumnCount).Where(ci => ci != _dataTargetIndex).ToArray();
        }

	    /// <inheritdoc />
	    public int InputCount => 1;
	    /// <inheritdoc />
        public abstract bool IsSequential { get; }
	    /// <inheritdoc />
        public abstract int InputSize { get; }
	    /// <inheritdoc />
        public abstract int OutputSize { get; }
	    /// <inheritdoc />
        public virtual int RowCount => _data.Count;

	    /// <inheritdoc />
        public abstract IMiniBatch Get(IExecutionContext executionContext, IReadOnlyList<int> rows);

	    /// <inheritdoc />
        public abstract IDataSource CloneWith(IDataTable dataTable);

	    /// <inheritdoc />
        public virtual IReadOnlyList<IReadOnlyList<int>> GetBuckets()
        {
            return new[] {
                Enumerable.Range(0, _data.Count).ToList()
            };
        }

	    /// <inheritdoc />
        public virtual void OnBatchProcessed(IContext context)
        {
            // nop
        }

		/// <summary>
		/// Returns the row data
		/// </summary>
		/// <param name="rows">List of row indices</param>
        protected IReadOnlyList<T> _GetRows(IReadOnlyList<int> rows)
        {
            return rows.Select(i => _data[i]).ToList();
        }

		/// <summary>
		/// Creates a mini batch
		/// </summary>
		/// <param name="rows">Row indices</param>
		/// <param name="data">List of input/output tuples</param>
        protected IMiniBatch _GetMiniBatch(IReadOnlyList<int> rows, IReadOnlyList<(float[][], float[])> data)
        {
            var inputList = new List<IGraphData>();
            for (int i = 0, len = data.First().Item1.Length; i < len; i++)
            {
	            var i1 = i;
		        inputList.Add(new MatrixGraphData(_lap.CreateMatrix(data.Count, InputSize, (x, y) => data[x].Item1[i1][y])));
	        }

	        var output = OutputSize > 0 ? _lap.CreateMatrix(data.Count, OutputSize, (x, y) => data[x].Item2[y]) : null;
            return new MiniBatch(rows, this, inputList, new MatrixGraphData(output));
        }

		/// <summary>
		/// Creates a sequential mini batch
		/// </summary>
		/// <param name="rows">Row indices</param>
		/// <param name="data">List of input/output matrix tuples</param>
        protected IMiniBatch _GetSequentialMiniBatch(IReadOnlyList<int> rows, IReadOnlyList<(FloatMatrix Input, FloatMatrix Output)> data)
        {
            List<FloatVector> temp;
            var inputData = new Dictionary<int, List<FloatVector>>();
            var outputData = new Dictionary<int, List<FloatVector>>();

            foreach (var item in data) {
                var input = item.Input;
                var output = item.Output;
                for (int i = 0, len = input.RowCount; i < len; i++) {
                    if (!inputData.TryGetValue(i, out temp))
                        inputData.Add(i, temp = new List<FloatVector>());
                    temp.Add(input.Row[i]);

                    if (output != null) {
                        if (!outputData.TryGetValue(i, out temp))
                            outputData.Add(i, temp = new List<FloatVector>());
                        temp.Add(output.Row[i]);
                    }
                }
            }

            var miniBatch = new MiniBatch(rows, this);
            foreach (var item in inputData.OrderBy(kv => kv.Key)) {
                var input = _lap.CreateMatrixFromRows(item.Value);
                IMatrix output = null;
                if (outputData.TryGetValue(item.Key, out temp))
                    output = _lap.CreateMatrixFromRows(temp);
                var type = (item.Key == 0)
                    ? MiniBatchSequenceType.SequenceStart
                    : item.Key == (inputData.Count - 1)
                        ? MiniBatchSequenceType.SequenceEnd
                        : MiniBatchSequenceType.Standard
                ;
                var inputList = new List<IGraphData> {
                    new MatrixGraphData(input)
                };
                miniBatch.Add(type, inputList, new MatrixGraphData(output));
            }
            return miniBatch;
        }
    }
}
