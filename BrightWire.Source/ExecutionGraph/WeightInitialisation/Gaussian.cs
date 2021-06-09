﻿using MathNet.Numerics.Distributions;
using System;

namespace BrightWire.ExecutionGraph.WeightInitialisation
{
    /// <summary>
    /// Initialises weights randomly based on a gaussian distribution
    /// </summary>
    class Gaussian : IWeightInitialisation
    {
        readonly IContinuousDistribution _distribution;
        readonly GaussianVarianceCalibration _varianceCalibration;
        readonly GaussianVarianceCount _varianceCount;
        readonly ILinearAlgebraProvider _lap;
        readonly bool _zeroBias;

        public Gaussian(
            ILinearAlgebraProvider lap, 
            bool zeroInitialBias = true, 
            double stdDev = 0.1, 
            GaussianVarianceCalibration varianceCalibration = GaussianVarianceCalibration.SquareRootN,
            GaussianVarianceCount varianceCount = GaussianVarianceCount.FanIn
        ) {
            _lap = lap;
            _zeroBias = zeroInitialBias;
            _varianceCalibration = varianceCalibration;
            _varianceCount = varianceCount;
            _distribution = lap.IsStochastic ? new Normal(0, stdDev) : new Normal(0, stdDev, new Random(0));
        }

        float _GetBias()
        {
            return _zeroBias ? 0f : Convert.ToSingle(_distribution.Sample());
        }

        float _GetWeight(int inputSize, int outputSize, int i, int j)
        {
            var n = 0;
            if (_varianceCount == GaussianVarianceCount.FanIn || _varianceCount == GaussianVarianceCount.FanInFanOut)
                n += inputSize;
            if (_varianceCount == GaussianVarianceCount.FanOut || _varianceCount == GaussianVarianceCount.FanInFanOut)
                n += outputSize;

            double sample = _distribution.Sample();
            if(n > 0) {
                if (_varianceCalibration == GaussianVarianceCalibration.SquareRootN)
                    sample /= Math.Sqrt(n);
                else if (_varianceCalibration == GaussianVarianceCalibration.SquareRoot2N)
                    sample *= Math.Sqrt(2.0 / n);
            }

            return Convert.ToSingle(sample);
        }

        public IVector CreateBias(int size)
        {
            return _lap.CreateVector(size, x => _GetBias());
        }

        public IMatrix CreateWeight(int rows, int columns)
        {
            return _lap.CreateMatrix(rows, columns, (x, y) => _GetWeight(rows, columns, x, y));
        }
    }
}
