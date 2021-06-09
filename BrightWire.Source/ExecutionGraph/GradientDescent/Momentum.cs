﻿using System.IO;

namespace BrightWire.ExecutionGraph.GradientDescent
{
    /// <summary>
    /// Gradient descent with momentum
    /// https://en.wikipedia.org/wiki/Stochastic_gradient_descent#Momentum
    /// </summary>
    class Momentum : AdaGrad
    {
        protected float _momentum;
        
        public Momentum(float momentum, IMatrix cache, IGradientDescentOptimisation updater) : base(cache, updater)
        {
            _momentum = momentum;
        }

        public override void Update(IMatrix source, IMatrix delta, ILearningContext context)
        {
            _cache.AddInPlace(delta, 1f, _momentum);
            _updater.Update(source, _cache, context);
        }

        public override void ReadFrom(GraphFactory factory, BinaryReader reader)
        {
            base.ReadFrom(factory, reader);
            _momentum = reader.ReadSingle();
        }

        public override void WriteTo(BinaryWriter writer)
        {
            base.WriteTo(writer);
            writer.Write(_momentum);
        }
    }
}
