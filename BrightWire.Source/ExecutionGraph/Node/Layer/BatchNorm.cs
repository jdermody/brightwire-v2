﻿using System;
using BrightWire.ExecutionGraph.Node.Input;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightWire.ExecutionGraph.Node.Operation;
using BrightWire.Helper;
using BrightWire.Models;

namespace BrightWire.ExecutionGraph.Node.Layer
{
	/// <summary>
	/// Batch normalisation layer
	/// https://arxiv.org/abs/1502.03167
	/// </summary>
	class BatchNorm : NodeBase
	{
		int _inputSize;
		INode _input, _output;
		VectorInput _gamma, _beta;
		VectorBasedStatistics _statistics;
		OneToMany _start;
		IMatrix _gammaCached, _betaCached, _meanCached, _stdDevCached;

		public BatchNorm(GraphFactory graph, int inputSize, string name = null) : base(name)
		{
			_Create(graph, inputSize, null, null, null, null, 0);
		}

		void _Create(GraphFactory graph, int inputSize, float[] gamma, float[] beta, float[] derivedMean, float[] derivedM2, int count)
        {
            _inputSize = inputSize;
	        _statistics = new VectorBasedStatistics(graph.LinearAlgebraProvider, inputSize, derivedMean, derivedM2, count);

	        if (gamma == null) {
		        gamma = new float[inputSize];
		        for (var i = 0; i < inputSize; i++)
			        gamma[i] = 1f;
	        }
	        if (beta == null) {
		        beta = new float[inputSize];
	        }

	        _input = new FlowThrough();
	        _gamma = new VectorInput(gamma, "gamma");
	        _beta = new VectorInput(beta, "beta");

	        var mean = graph.Connect(inputSize, _input).Add(new BatchMean());
	        var subtractMean = graph.Subtract(inputSize, _input, mean.LastNode);
			var subtractMeanOutput = subtractMean.LastNode;

	        var stdDev = subtractMean
		        .Add(new InputSquared())
		        .Add(new BatchMean())
		        .Add(new SquareRootOfInput())
		        .Add(new OneDividedByInput());

	        var normalised = graph.Multiply(_inputSize, subtractMeanOutput, stdDev.LastNode)/*.Add(new InspectSignals(data => {
		        var matrix = data.GetMatrix().AsIndexable();
		        var statistics = matrix.Columns.Select(v => (v, v.Average())).Select(v2 => (v2.Item2, v2.Item1.StdDev(v2.Item2))).ToList();
	        }))*/;
	        var adjusted = graph.Multiply(_inputSize, _gamma, normalised.LastNode);
	        _output = graph.Add(_inputSize, _beta, adjusted.LastNode).LastNode;

	        _start = new OneToMany(SubNodes, bp => { });
        }

		public override IEnumerable<INode> SubNodes
		{
			get
			{
				yield return _input;
				yield return _gamma;
				yield return _beta;
			}
		}

		public override List<IWire> Output => _output.Output;

		public override void ExecuteForward(IContext context)
		{
			var data = context.Data;
			IMatrix input;
			IReadOnlyList<IVector> samples;
			var lap = context.LinearAlgebraProvider;
			var shouldDispose = false;

			if (data.Depth > 1) {
				// reshape the tensor by depth slice
				var tensor4D = data.Get4DTensor();
				var slots = new List<IVector>[data.Depth];
				for (var i = 0; i < data.Depth; i++)
					slots[i] = new List<IVector>();

				foreach (var tensor in tensor4D.ReshapeAsMatrix().ColumnVectors()) {
					var depthSlices = tensor.ReshapeAsColumnMatrix().ReshapeAsVector().Split(data.Depth);
					for (var i = 0; i < data.Depth; i++)
						slots[i].Add(depthSlices[i]);
				}

				// TODO: create row vectors from slots and matrix from rows

				//input = lap.CreateMatrixFromRows(list);
				//samples = list;
				shouldDispose = true;
				throw new NotImplementedException();
			}
			else {
				input = context.Data.GetMatrix();
				samples = input.RowVectors();
			}

			// collect statistics
			if (context.IsTraining && _statistics != null) {
				foreach (var vector in samples) {
					_statistics.Update(vector);
					vector.Dispose();
				}
			}

			if (context.IsTraining) {
				_start.ExecuteForward(context);

				// invalidate the cache
				if (_gammaCached != null) {
					_gammaCached.Dispose();
					_gammaCached = null;
					_betaCached?.Dispose();
					_betaCached = null;
					_meanCached?.Dispose();
					_meanCached = null;
					_stdDevCached?.Dispose();
					_stdDevCached = null;
				}
			}
			else if(_statistics != null) {
				if (_gammaCached?.IsValid != true || _gammaCached?.RowCount != input.RowCount || _gammaCached?.ColumnCount != input.ColumnCount)
					_gammaCached = context.LinearAlgebraProvider.CreateMatrix(input.RowCount, input.ColumnCount, (x, y) => _gamma.Data[y]);
				if(_betaCached?.IsValid != true || _betaCached?.RowCount != input.RowCount || _betaCached?.ColumnCount != input.ColumnCount)
					_betaCached = context.LinearAlgebraProvider.CreateMatrix(input.RowCount, input.ColumnCount, (x, y) => _beta.Data[y]);
				if (_meanCached?.IsValid != true || _meanCached?.RowCount != input.RowCount || _meanCached?.ColumnCount != input.ColumnCount) {
					var mean = _statistics.Mean;
					_meanCached = context.LinearAlgebraProvider.CreateMatrixFromRows(Enumerable.Repeat(mean, input.RowCount).ToList());
				}
				if (_stdDevCached?.IsValid != true || _stdDevCached?.RowCount != input.RowCount || _stdDevCached?.ColumnCount != input.ColumnCount) {
					using (var variance = _statistics.GetSampleVariance())
					using (var stdDev = variance.Sqrt()) {
						_stdDevCached = context.LinearAlgebraProvider.CreateMatrixFromRows(Enumerable.Repeat(stdDev, input.RowCount).ToList());
					}
				}
				
				input.SubtractInPlace(_meanCached);
				using (var xHat = input.PointwiseDivide(_stdDevCached)) {
					using (var ret = xHat.PointwiseMultiply(_gammaCached)) {
						ret.AddInPlace(_betaCached);
						_AddNextGraphAction(context, context.Data.ReplaceWith(ret), null);
					}
				}
			}

			if (shouldDispose)
				input.Dispose();
			foreach (var item in samples)
				item.Dispose();
		}

		protected override (string Description, byte[] Data) _GetInfo()
		{
			return ("BN", _WriteData(WriteTo));
		}

		public override void WriteTo(BinaryWriter writer)
		{
			writer.Write(_inputSize);
			_gamma.WriteTo(writer);
			_beta.WriteTo(writer);

			writer.Write(_statistics.Count);
			_statistics.Mean.Data.WriteTo(writer);
			_statistics.M2.Data.WriteTo(writer);
		}

		public override void ReadFrom(GraphFactory factory, BinaryReader reader)
		{
			var inputSize = reader.ReadInt32();
			var gamma = FloatVector.ReadFrom(reader).Data;
			var beta = FloatVector.ReadFrom(reader).Data;

			var count = reader.ReadInt32();
			var mean = FloatVector.ReadFrom(reader).Data;
			var m2 = FloatVector.ReadFrom(reader).Data;

			_Create(factory, inputSize, gamma, beta, mean, m2, count);
		}
	}
}
