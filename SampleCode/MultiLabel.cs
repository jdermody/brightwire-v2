﻿using BrightWire.ExecutionGraph;
using BrightWire.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrightWire.SampleCode
{
	public partial class Program
	{
		const int CLASSIFICATION_COUNT = 6;

		static IDataTable _LoadEmotionData(string dataFilePath)
		{
			// read the data as CSV, skipping the header
			using (var reader = new StreamReader(dataFilePath)) {
				while (!reader.EndOfStream) {
					var line = reader.ReadLine();
					if (line == "@data")
						break;
				}
				return reader.ReadToEnd().ParseCSV(',', false);
			}
		}

		/// <summary>
		/// Trains a feed forward neural net on the emotion dataset
		/// http://lpis.csd.auth.gr/publications/tsoumakas-ismir08.pdf
		/// The data files can be downloaded from https://downloads.sourceforge.net/project/mulan/datasets/emotions.rar
		/// </summary>
		/// <param name="dataFilePath"></param>
		public static void MultiLabelSingleClassifier(string dataFilePath)
		{
			Console.WriteLine($"\nRunning {nameof(MultiLabelSingleClassifier)}\n");

			if (!File.Exists(dataFilePath))
            {
				var url = "https://downloads.sourceforge.net/project/mulan/datasets/emotions.rar";
				Console.WriteLine($"You must download data files to {dataFilePath} to run this example");
				Console.WriteLine($" => Data files can be downloaded from {url}");
				return;
            }

			var emotionData = _LoadEmotionData(dataFilePath);
			var attributeColumns = Enumerable.Range(0, emotionData.ColumnCount - CLASSIFICATION_COUNT).ToList();
			var classificationColumns = Enumerable.Range(emotionData.ColumnCount - CLASSIFICATION_COUNT, CLASSIFICATION_COUNT).ToList();

			// create a new data table with a vector input column and a vector output column
			var dataTableBuilder = BrightWireProvider.CreateDataTableBuilder();
			dataTableBuilder.AddColumn(ColumnType.Vector, "Attributes");
			dataTableBuilder.AddColumn(ColumnType.Vector, "Target", isTarget: true);
			emotionData.ForEach(row => {
				var input = FloatVector.Create(row.GetFields<float>(attributeColumns).ToArray());
				var target = FloatVector.Create(row.GetFields<float>(classificationColumns).ToArray());
				dataTableBuilder.Add(input, target);
				return true;
			});
			var data = dataTableBuilder.Build().Split(0);

			// train a neural network
			using (var lap = BrightWireProvider.CreateLinearAlgebra(false)) {
				var graph = new GraphFactory(lap);

				// binary classification rounds each output to 0 or 1 and compares each output against the binary classification targets
				var errorMetric = graph.ErrorMetric.BinaryClassification;

				// configure the network properties
				graph.CurrentPropertySet
					.Use(graph.GradientDescent.Adam)
					.Use(graph.WeightInitialisation.Xavier)
				;

				// create a training engine
				const float TRAINING_RATE = 0.3f;
				var trainingData = graph.CreateDataSource(data.Training);
				var testData = trainingData.CloneWith(data.Test);
				var engine = graph.CreateTrainingEngine(trainingData, TRAINING_RATE, 128);

				// build the network
				const int HIDDEN_LAYER_SIZE = 64, TRAINING_ITERATIONS = 2000;
				var network = graph.Connect(engine)
					.AddFeedForward(HIDDEN_LAYER_SIZE)
					.Add(graph.SigmoidActivation())
					.AddDropOut(dropOutPercentage: 0.5f)
					.AddFeedForward(engine.DataSource.OutputSize)
					.Add(graph.SigmoidActivation())
					.AddBackpropagation(errorMetric)
				;

				// train the network
                Models.ExecutionGraph bestGraph = null;
				engine.Train(TRAINING_ITERATIONS, testData, errorMetric, model => bestGraph = model.Graph, 50);

                // export the final model and execute it on the training set
                var executionEngine = graph.CreateEngine(bestGraph ?? engine.Graph);
                var output = executionEngine.Execute(testData);

				// output the results
                var rowIndex = 0;
                foreach (var item in output) {
                    var sb = new StringBuilder();
                    foreach (var classification in item.Output.Zip(item.Target, (o, t) => (Output: o, Target: t))) {
                        var columnIndex = 0;
                        sb.AppendLine($"{rowIndex++}) ");
						foreach (var column in classification.Output.Data.Zip(classification.Target.Data,
                            (o, t) => (Output: o, Target: t))) {
                            var prediction = column.Output >= 0.5f ? "true" : "false";
                            var actual = column.Target >= 0.5f ? "true" : "false";
                            sb.AppendLine($"\t{columnIndex++}) predicted {prediction} (expected {actual})");
                        }
                    }
					Console.WriteLine(sb.ToString());
                }
            }
		}

		/// <summary>
		/// Trains multiple classifiers on the emotion data set
		/// http://lpis.csd.auth.gr/publications/tsoumakas-ismir08.pdf
		/// The data files can be downloaded from https://downloads.sourceforge.net/project/mulan/datasets/emotions.rar
		/// </summary>
		/// <param name="dataFilePath"></param>
		public static void MultiLabelMultiClassifiers(string dataFilePath)
		{
			Console.WriteLine($"\nRunning {nameof(MultiLabelSingleClassifier)}\n");

			if (!File.Exists(dataFilePath))
			{
				var url = "https://downloads.sourceforge.net/project/mulan/datasets/emotions.rar";
				Console.WriteLine($"You must download data files to {dataFilePath} to run this example");
				Console.WriteLine($" => Data files can be downloaded from {url}");
				return;
			}

			var emotionData = _LoadEmotionData(dataFilePath);
			var attributeCount = emotionData.ColumnCount - CLASSIFICATION_COUNT;
			var attributeColumns = Enumerable.Range(0, attributeCount).ToList();
			var classificationColumns = Enumerable.Range(emotionData.ColumnCount - CLASSIFICATION_COUNT, CLASSIFICATION_COUNT).ToList();
			var classificationLabel = new[] {
				"amazed-suprised",
				"happy-pleased",
				"relaxing-calm",
				"quiet-still",
				"sad-lonely",
				"angry-aggresive"
			};

			// create six separate datasets to train, each with a separate classification column
			var dataSets = Enumerable.Range(attributeCount, CLASSIFICATION_COUNT).Select(targetIndex => {
				var dataTableBuider = BrightWireProvider.CreateDataTableBuilder();
				for (var i = 0; i < attributeCount; i++)
					dataTableBuider.AddColumn(ColumnType.Float);
				dataTableBuider.AddColumn(ColumnType.Float, "", true);

				return emotionData.Project(row => row.GetFields<float>(attributeColumns)
					.Concat(new[] { row.GetField<float>(targetIndex) })
					.Cast<object>()
					.ToList()
				);
			}).Select(ds => ds.Split(0)).ToList();

			// train classifiers on each training set
			using (var lap = BrightWireProvider.CreateLinearAlgebra(false)) {
				var graph = new GraphFactory(lap);

				// binary classification rounds each output to 0 or 1 and compares each output against the binary classification targets
				var errorMetric = graph.ErrorMetric.BinaryClassification;

				// configure the network properties
				graph.CurrentPropertySet
					.Use(graph.GradientDescent.Adam)
					.Use(graph.WeightInitialisation.Xavier)
				;

				for (var i = 0; i < CLASSIFICATION_COUNT; i++) {
					var trainingSet = dataSets[i].Training;
					var testSet = dataSets[i].Test;
					Console.WriteLine("Training on {0}", classificationLabel[i]);

					// train and evaluate a naive bayes classifier
					var naiveBayes = trainingSet.TrainNaiveBayes().CreateClassifier();
					Console.WriteLine("\tNaive bayes accuracy: {0:P}", testSet
						.Classify(naiveBayes)
						.Average(d => d.Row.GetField<string>(attributeCount) == d.Classification ? 1.0 : 0.0)
					);

					// train a logistic regression classifier
					var logisticRegression = trainingSet
						.TrainLogisticRegression(lap, 2500, 0.25f, 0.01f)
						.CreatePredictor(lap)
						.ConvertToRowClassifier(attributeColumns)
					;
					Console.WriteLine("\tLogistic regression accuracy: {0:P}", testSet
						.Classify(logisticRegression)
						.Average(d => d.Row.GetField<string>(attributeCount) == d.Classification ? 1.0 : 0.0)
					);

					// train and evaluate k nearest neighbours
					var knn = trainingSet.TrainKNearestNeighbours().CreateClassifier(lap, 10);
					Console.WriteLine("\tK nearest neighbours accuracy: {0:P}", testSet
						.Classify(knn)
						.Average(d => d.Row.GetField<string>(attributeCount) == d.Classification ? 1.0 : 0.0)
					);

					// create a training engine
					const float TRAINING_RATE = 0.1f;
					var trainingData = graph.CreateDataSource(trainingSet);
					var testData = trainingData.CloneWith(testSet);
					var engine = graph.CreateTrainingEngine(trainingData, TRAINING_RATE, 64);

					// build the network
					const int HIDDEN_LAYER_SIZE = 64, TRAINING_ITERATIONS = 2000;
					var network = graph.Connect(engine)
						.AddFeedForward(HIDDEN_LAYER_SIZE)
						.Add(graph.SigmoidActivation())
						.AddDropOut(dropOutPercentage: 0.5f)
						.AddFeedForward(engine.DataSource.OutputSize)
						.Add(graph.SigmoidActivation())
						.AddBackpropagation(errorMetric)
					;

					// train the network
					engine.Train(TRAINING_ITERATIONS, testData, errorMetric, null, 200);
				}
			}
		}
	}
}
