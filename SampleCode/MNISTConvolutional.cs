﻿using BrightWire.ExecutionGraph;
using BrightWire.Models;
using BrightWire.TrainingData.WellKnown;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BrightWire.SampleCode
{
    public partial class Program
    {
        /// <summary>
        /// Trains a feed forward neural net on the MNIST data set (handwritten digit recognition)
        /// The data files can be downloaded from http://yann.lecun.com/exdb/mnist/
        /// </summary>
        /// <param name="dataFilesPath">The path to a directory with the four extracted data files</param>
        /// <param name="outputModelPath">Optional path to save the best model to</param>
        static void MNISTConvolutional(string dataFilesPath, string outputModelPath = null)
        {
            using (var lap = BrightWireGpuProvider.CreateLinearAlgebra()) {
                var graph = new GraphFactory(lap);

                Console.Write("Loading training data...");
                var mnistTraining = Mnist.Load(dataFilesPath + "train-labels.idx1-ubyte", dataFilesPath + "train-images.idx3-ubyte");
                var mnistTest = Mnist.Load(dataFilesPath + "t10k-labels.idx1-ubyte", dataFilesPath + "t10k-images.idx3-ubyte");
                var trainingData = _BuildTensors(graph, null, mnistTraining/*.Where(d => d.Label < 2).ToList()*/);
                var testData = _BuildTensors(graph, trainingData, mnistTest/*.Where(d => d.Label < 2).ToList()*/);
                Console.WriteLine($"done - {trainingData.RowCount} training images and {testData.RowCount} test images loaded");

                // one hot encoding uses the index of the output vector's maximum value as the classification label
                var errorMetric = graph.ErrorMetric.OneHotEncoding;

                // configure the network properties
                graph.CurrentPropertySet
                    .Use(graph.GradientDescent.Adam)
                    .Use(graph.GaussianWeightInitialisation(false, 0.1f, GaussianVarianceCalibration.SquareRoot2N))
                ;

                // create the network
                const int HIDDEN_LAYER_SIZE = 1024, BATCH_SIZE = 128, TRAINING_ITERATIONS = 20;
                const float LEARNING_RATE = 0.05f;
                var engine = graph.CreateTrainingEngine(trainingData, LEARNING_RATE, BATCH_SIZE);
                if (!String.IsNullOrWhiteSpace(outputModelPath) && File.Exists(outputModelPath)) {
                    Console.WriteLine("Loading existing model from: " + outputModelPath);
                    using (var file = new FileStream(outputModelPath, FileMode.Open, FileAccess.Read)) {
                        var model = Serializer.Deserialize<GraphModel>(file);
                        engine = graph.CreateTrainingEngine(trainingData, model.Graph, LEARNING_RATE, BATCH_SIZE);
                    }
                } else {
                    graph.Connect(engine)
                     .AddConvolutional(filterCount: 16, padding: 2, filterWidth: 5, filterHeight: 5, xStride: 1, yStride: 1, shouldBackpropagate: false)
                     .Add(graph.LeakyReluActivation())
                     .AddMaxPooling(filterWidth: 2, filterHeight: 2, xStride: 2, yStride: 2)
                     .AddConvolutional(filterCount: 32, padding: 2, filterWidth: 5, filterHeight: 5, xStride: 1, yStride: 1)
                     .Add(graph.LeakyReluActivation())
                     .AddMaxPooling(filterWidth: 2, filterHeight: 2, xStride: 2, yStride: 2)
                     .Transpose()
                     .AddFeedForward(HIDDEN_LAYER_SIZE)
                     .Add(graph.LeakyReluActivation())
                     .AddDropOut(dropOutPercentage: 0.5f)
                     .AddFeedForward(trainingData.OutputSize)
                     .Add(graph.SoftMaxActivation())
                     .AddBackpropagation(errorMetric)
                    ;
                }

                // lower the learning rate over time
                engine.LearningContext.ScheduleLearningRate(15, LEARNING_RATE / 2);

                // train the network for twenty iterations, saving the model on each improvement
                Models.ExecutionGraph bestGraph = null;
                engine.Train(TRAINING_ITERATIONS, testData, errorMetric, model => {
                    bestGraph = model.Graph;
                    if (!String.IsNullOrWhiteSpace(outputModelPath)) {
                        using (var file = new FileStream(outputModelPath, FileMode.Create, FileAccess.Write)) {
                            Serializer.Serialize(file, model);
                        }
                    }
                });

                // export the final model and execute it on the training set
                var executionEngine = graph.CreateEngine(bestGraph ?? engine.Graph);
                var output = executionEngine.Execute(testData);
                Console.WriteLine($"Final accuracy: {output.Average(o => o.CalculateError(errorMetric)):P2}");

                // execute the model with a single image
                var tensor = mnistTest.First().AsFloatTensor.Tensor;
                var singleData = graph.CreateDataSource(new[] { tensor });
                var result = executionEngine.Execute(singleData);
                var prediction = result.Single().Output.Single().MaximumIndex();
            }
        }

        static IDataSource _BuildTensors(GraphFactory graph, IDataSource existing, IReadOnlyList<Mnist.Image> images)
        {
            // convolutional neural networks expect a 3D tensor => vector mapping
            var dataTable = BrightWireProvider.CreateDataTableBuilder();
            dataTable.AddColumn(ColumnType.Tensor, "Image");
            dataTable.AddColumn(ColumnType.Vector, "Target", isTarget: true);

            foreach (var image in images) {
                var data = image.AsFloatTensor;
                dataTable.Add(data.Tensor, data.Label);
            }

            // reuse the network used for training when building the test data source
            if (existing != null)
                return existing.CloneWith(dataTable.Build());
            else
                return graph.CreateDataSource(dataTable.Build());
        }
    }
}
