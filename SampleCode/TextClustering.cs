﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BrightWire.Models;
using BrightWire.TrainingData;
using BrightWire.TrainingData.Helper;

namespace BrightWire.SampleCode
{
    partial class Program
    {
        class AAAIDocument
        {
            /// <summary>
            /// Free text description of the document
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            /// Free text; author-generated keywords
            /// </summary>
            public string[] Keyword { get; set; }

            /// <summary>
            /// Free text; author-selected, low-level keywords
            /// </summary>
            public string[] Topic { get; set; }

            /// <summary>
            /// Free text; paper abstracts
            /// </summary>
            public string Abstract { get; set; }

            /// <summary>
            /// Categorical; author-selected, high-level keyword(s)
            /// </summary>
            public string[] Group { get; set; }

            public (string Classification, WeightedIndexList Data) AsClassification(StringTableBuilder stringTable)
            {
                var weightedIndex = new List<WeightedIndexList.WeightedIndex>();
                foreach (var item in Keyword) {
                    weightedIndex.Add(new WeightedIndexList.WeightedIndex {
                        Index = stringTable.GetIndex(item),
                        Weight = 1f
                    });
                }
                foreach (var item in Topic) {
                    weightedIndex.Add(new WeightedIndexList.WeightedIndex {
                        Index = stringTable.GetIndex(item),
                        Weight = 1f
                    });
                }
                return (Title, new WeightedIndexList {
                    IndexList = weightedIndex
                        .GroupBy(d => d.Index)
                        .Select(g => new WeightedIndexList.WeightedIndex {
                            Index = g.Key,
                            Weight = g.Sum(d => d.Weight)
                        })
                        .ToArray()
                });
            }
        }

        static void _WriteClusters(string filePath, IReadOnlyList<IReadOnlyList<IVector>> clusters, Dictionary<IVector, AAAIDocument> documentTable)
        {
            new FileInfo(filePath).Directory.Create();
            using (var writer = new StreamWriter(filePath)) {
                foreach (var cluster in clusters) {
                    foreach (var item in cluster) {
                        var document = documentTable[item];
                        writer.WriteLine(document.Title);
                        writer.WriteLine(String.Join(", ", document.Keyword));
                        writer.WriteLine(String.Join(", ", document.Topic));
                        writer.WriteLine(String.Join(", ", document.Group));
                        writer.WriteLine();
                    }
                    writer.WriteLine("------------------------------------------------------------------");
                }
            }
        }

        /// <summary>
        /// Cluster a tagged set of documents
        /// Can be downloaded from https://archive.ics.uci.edu/ml/machine-learning-databases/00307/
        /// </summary>
        /// <param name="dataFilePath">The path to the data file</param>
        /// <param name="outputPath">A directory to write the output files to</param>
        public static void TextClustering(string dataFilePath, string outputPath)
        {
            Console.WriteLine($"\nRunning {Console.Title = nameof(TextClustering)}\n");

            IDataTable dataTable;
            if (!File.Exists(dataFilePath))
            {
                var dest = new FileInfo(dataFilePath);
                dest.Directory.Create();
                var src = "https://archive.ics.uci.edu/ml/machine-learning-databases/00307/%5bUCI%5d%20AAAI-14%20Accepted%20Papers%20-%20Papers.csv";
                new System.Net.WebClient()
                    .DownloadFile(src, dest.FullName);

            }
            using (var reader = new StreamReader(dataFilePath)) {
                dataTable = reader.ParseCSV();
            }

            var KEYWORD_SPLIT = " \n".ToCharArray();
            var TOPIC_SPLIT = "\n".ToCharArray();

            var docList = new List<AAAIDocument>();
            dataTable.ForEach(row => docList.Add(new AAAIDocument {
                Abstract = row.GetField<string>(5),
                Keyword = row.GetField<string>(3).Split(KEYWORD_SPLIT, StringSplitOptions.RemoveEmptyEntries).Select(str => str.ToLower()).ToArray(),
                Topic = row.GetField<string>(4).Split(TOPIC_SPLIT, StringSplitOptions.RemoveEmptyEntries),
                Group = row.GetField<string>(2).Split(TOPIC_SPLIT, StringSplitOptions.RemoveEmptyEntries),
                Title = row.GetField<string>(0)
            }));
            var docTable = docList.ToDictionary(d => d.Title, d => d);
            var allGroups = new HashSet<string>(docList.SelectMany(d => d.Group));

            var stringTable = new StringTableBuilder();
            var classificationSet = docList.Select(d => d.AsClassification(stringTable)).ToArray();
            var encodings = classificationSet.Vectorise();

            using (var lap = BrightWireProvider.CreateLinearAlgebra()) {
                var lookupTable = encodings.Select(d => Tuple.Create(d, lap.CreateVector(d.Data))).ToDictionary(d => d.Item2, d => docTable[d.Item1.Classification]);
                var vectorList = lookupTable.Select(d => d.Key).ToList();

                Console.WriteLine("Kmeans clustering...");
                _WriteClusters(outputPath + "kmeans.txt", vectorList.KMeans(lap, allGroups.Count), lookupTable);

                Console.WriteLine("NNMF clustering...");
                _WriteClusters(outputPath + "nnmf.txt", vectorList.NNMF(lap, allGroups.Count, 100), lookupTable);

                // create a term/document matrix with terms as columns and documents as rows
                var matrix = lap.CreateMatrixFromRows(vectorList.Select(v => v.Data).ToList());
                vectorList.ForEach(v => v.Dispose());

                Console.WriteLine("Creating random projection...");
                using (var randomProjection = lap.CreateRandomProjection((int)classificationSet.GetMaxIndex() + 1, 512)) {
                    using (var projectedMatrix = randomProjection.Compute(matrix)) {
                        var vectorList2 = Enumerable.Range(0, projectedMatrix.RowCount).Select(i => projectedMatrix.Row(i)).ToList();
                        var lookupTable2 = vectorList2.Select((v, i) => Tuple.Create(v, vectorList[i])).ToDictionary(d => d.Item1, d => lookupTable[d.Item2]);

                        Console.WriteLine("Kmeans clustering of random projection...");
                        _WriteClusters(outputPath + "projected-kmeans.txt", vectorList2.KMeans(lap, allGroups.Count), lookupTable2);
                        vectorList2.ForEach(v => v.Dispose());
                    }
                }

                Console.WriteLine("Building latent term/document space...");
                const int K = 256;
                var kIndices = Enumerable.Range(0, K).ToList();
                var matrixT = matrix.Transpose();
                matrix.Dispose();
                var svd = matrixT.Svd();
                matrixT.Dispose();

                var s = lap.CreateDiagonalMatrix(svd.S.AsIndexable().Values.Take(K).ToList());
                var v2 = svd.VT.GetNewMatrixFromRows(kIndices);
                using (var sv2 = s.Multiply(v2)) {
                    v2.Dispose();
                    s.Dispose();

                    var vectorList3 = sv2.AsIndexable().Columns.ToList();
                    var lookupTable3 = vectorList3.Select((v, i) => Tuple.Create(v, vectorList[i])).ToDictionary(d => (IVector)d.Item1, d => lookupTable[d.Item2]);

                    Console.WriteLine("Kmeans clustering in latent document space...");
                    _WriteClusters(outputPath + "latent-kmeans.txt", vectorList3.KMeans(lap, allGroups.Count), lookupTable3);
                }
            }

            Console.WriteLine("Results saved to {0}", outputPath);
        }
    }
}
