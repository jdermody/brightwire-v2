﻿using BrightWire.Helper;

namespace BrightWire.TrainingData.Artificial
{
    /// <summary>
    /// Simple XOR training data
    /// </summary>
    public static class Xor
    {
        /// <summary>
        /// Generates a data table containing XOR training data
        /// </summary>
        /// <returns></returns>
        public static IDataTable Get()
        {
            var builder = new DataTableBuilder();
            builder.AddColumn(ColumnType.Float, "X");
            builder.AddColumn(ColumnType.Float, "Y");
            builder.AddColumn(ColumnType.Float, "XOR", true);

            builder.Add(0.0f, 0.0f, 0.0f);
            builder.Add(1.0f, 0.0f, 1.0f);
            builder.Add(0.0f, 1.0f, 1.0f);
            builder.Add(1.0f, 1.0f, 0.0f);

            return builder.Build();
        }
    }
}
