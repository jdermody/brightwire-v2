﻿using BrightWire.Models;
using System;
using System.Collections.Generic;

namespace BrightWire
{
	/// <summary>
	/// Helper methods when using the GPU linear algebra provider
	/// </summary>
	public interface IGpuLinearAlgebraProvider
	{
		/// <summary>
		/// Binds the current thread to the cuda context (when using the same cuda provider from multiple threads)
		/// </summary>
		void BindThread();

		/// <summary>
		/// Amount of free memory on the device in bytes
		/// </summary>
		long FreeMemory { get; }

		/// <summary>
		/// Amount of total memory on the device in bytes
		/// </summary>
		long TotalMemory { get; }
	}

	/// <summary>
	/// Provides linear algebra functionality
	/// </summary>
	public interface ILinearAlgebraProvider : IDisposable
	{
		/// <summary>
		/// Creates a new vector
		/// </summary>
		/// <param name="length">Length of the vector</param>
		/// <param name="setToZero">True to initialise the data to zero (otherwise it might be anything)</param>
		IVector CreateVector(int length, bool setToZero = false);

		/// <summary>
		/// Creates a vector
		/// </summary>
		/// <param name="length">Size of the vector</param>
		/// <param name="init">Callback to initialise each element of the vector</param>
		IVector CreateVector(int length, Func<int, float> init);

		/// <summary>
		/// Creates a matrix
		/// </summary>
		/// <param name="rows">The number of rows</param>
		/// <param name="columns">The number of columns</param>
		/// <param name="setToZero">True to initialise the data to zero (otherwise it might be anything)</param>
		IMatrix CreateMatrix(int rows, int columns, bool setToZero = false);

		/// <summary>
		/// Creates a matrix
		/// </summary>
		/// <param name="rows">The number of rows</param>
		/// <param name="columns">The number of columns</param>
		/// <param name="init">Callback to initialise each element of the matrix</param>
		IMatrix CreateMatrix(int rows, int columns, Func<int, int, float> init);

		/// <summary>
		/// Creates a matrix from a list of vectors. Each vector will become a row in the new matrix
		/// </summary>
		/// <param name="vectorRows">List of vectors for each row</param>
		/// <returns></returns>
		IMatrix CreateMatrixFromRows(IReadOnlyList<IVector> vectorRows);

		/// <summary>
		/// Creates a matrix from a list of vectors. Each vector will become a column in the new matrix
		/// </summary>
		/// <param name="vectorColumns">List of vectors for each column</param>
		/// <returns></returns>
		IMatrix CreateMatrixFromColumns(IReadOnlyList<IVector> vectorColumns);

		/// <summary>
		/// Creates a 3D tensor
		/// </summary>
		/// <param name="rows">Number of rows</param>
		/// <param name="columns">Number of columns</param>
		/// <param name="depth">Number of depth slices</param>
		/// <param name="setToZero">True to initialise the data to zero (otherwise it might be anything)</param>
		I3DTensor Create3DTensor(int rows, int columns, int depth, bool setToZero = false);

		/// <summary>
		/// Creates a 3D tensor from a list of matrices
		/// </summary>
		/// <param name="matrices">List of matrices</param>
		/// <returns></returns>
		I3DTensor Create3DTensor(IReadOnlyList<IMatrix> matrices);

		/// <summary>
		/// Creates a 4D tensor
		/// </summary>
		/// <param name="rows">Number of rows</param>
		/// <param name="columns">Number of columns</param>
		/// <param name="depth">Number of matrices</param>
		/// <param name="count">Number of 3D tensors</param>
		/// <param name="setToZero">True to initialise the data to zero (otherwise it might be anything)</param>
		I4DTensor Create4DTensor(int rows, int columns, int depth, int count, bool setToZero = false);

		/// <summary>
		/// Creates a 4D tensor from a list of 3D tensors
		/// </summary>
		/// <param name="tensors">List of 3D tensors</param>
		/// <returns></returns>
		I4DTensor Create4DTensor(IReadOnlyList<I3DTensor> tensors);

		/// <summary>
		/// Creates a 4D tensor from a list of 3D tensors
		/// </summary>
		/// <param name="tensors">List of 3D tensors</param>
		/// <returns></returns>
		I4DTensor Create4DTensor(IReadOnlyList<FloatTensor> tensors);

		/// <summary>
		/// Creates a save point in the allocation history
		/// </summary>
		void PushLayer();

		/// <summary>
		/// Releases all allocated memory since the last save point
		/// </summary>
		void PopLayer();

		/// <summary>
		/// Underlying setting for stochastic vs deterministic behaviour for instances created from this provider
		/// </summary>
		bool IsStochastic { get; }

		/// <summary>
		/// True if the provider uses the GPU
		/// </summary>
		bool IsGpu { get; }

		/// <summary>
		/// Calculates the distance of each vector against the comparison vectors - the size of all vectors should be the same
		/// </summary>
		/// <param name="vectors"></param>
		/// <param name="comparison"></param>
		/// <param name="distanceMetric"></param>
		/// <returns></returns>
		IMatrix CalculateDistances(IReadOnlyList<IVector> vectors, IReadOnlyList<IVector> comparison, DistanceMetric distanceMetric);
	}

	/// <summary>
	/// Distance metrics
	/// </summary>
	public enum DistanceMetric
	{
		/// <summary>
		/// Euclidean Distance
		/// </summary>
		Euclidean,

		/// <summary>
		/// Cosine Distance Metric
		/// </summary>
		Cosine,

		/// <summary>
		/// Manhattan Distance
		/// </summary>
		Manhattan,

		/// <summary>
		/// Means Square Error
		/// </summary>
		MeanSquared,

		/// <summary>
		/// Square Euclidean
		/// </summary>
		SquaredEuclidean
	}

	/// <summary>
	/// A vector
	/// </summary>
	public interface IVector : IDisposable
	{
		/// <summary>
		/// Checks if the vector has not been disposed
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// Converts the vector to a column matrix
		/// </summary>
		IMatrix ReshapeAsColumnMatrix();

		/// <summary>
		/// Converts the vector to a row matrix
		/// </summary>
		IMatrix ReshapeAsRowMatrix();

		/// <summary>
		/// The number of elements in the vector
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Converts the vector into protobuf format
		/// </summary>
		FloatVector Data { get; set; }

		/// <summary>
		/// Adds a vector (without in place modification)
		/// </summary>
		/// <param name="vector">The vector to add</param>
		IVector Add(IVector vector);

		/// <summary>
		/// Subtracts a vector (without in place modification)
		/// </summary>
		/// <param name="vector">The vector to subtract</param>
		IVector Subtract(IVector vector);

		/// <summary>
		/// Calculates the absolute values (L1) norm: https://en.wikipedia.org/wiki/Norm_(mathematics)
		/// </summary>
		float L1Norm();

		/// <summary>
		/// Calculates the euclidean (L2) norm: https://en.wikipedia.org/wiki/Norm_(mathematics)
		/// </summary>
		float L2Norm();

		/// <summary>
		/// Returns the index of the vector with the greatest value
		/// </summary>
		int MaximumIndex();

		/// <summary>
		/// Returns the index of the vector with the smallest value
		/// </summary>
		int MinimumIndex();

		/// <summary>
		/// Multiples (in place) by a scalar
		/// </summary>
		/// <param name="scalar">The value to multiple each element</param>
		void Multiply(float scalar);

		/// <summary>
		/// Adds (in place) a scalar
		/// </summary>
		/// <param name="scalar">The value to add to each element</param>
		void Add(float scalar);

		/// <summary>
		/// Adds a vector in place
		/// </summary>
		/// <param name="vector">The target vector to add to the current vector</param>
		/// <param name="coefficient1">A value to multiply each element of the current vector</param>
		/// <param name="coefficient2">A value to multiply each element of the target vector</param>
		void AddInPlace(IVector vector, float coefficient1 = 1.0f, float coefficient2 = 1.0f);

		/// <summary>
		/// Subtracts a vector in place
		/// </summary>
		/// <param name="vector">The target vector to subtract from the current vector</param>
		/// <param name="coefficient1">A value to multiply each element of the current vector</param>
		/// <param name="coefficient2">A value to multiply each element of the target vector</param>
		void SubtractInPlace(IVector vector, float coefficient1 = 1.0f, float coefficient2 = 1.0f);

		/// <summary>
		/// Converts the vector to an indexable vector
		/// </summary>
		IIndexableVector AsIndexable();

		/// <summary>
		/// Pointwise multiplication (without in place modification) with a vector
		/// </summary>
		IVector PointwiseMultiply(IVector vector);

		/// <summary>
		/// The dot product of two vectors
		/// </summary>
		/// <param name="vector">The target vector</param>
		float DotProduct(IVector vector);

		/// <summary>
		/// Returns a new vector from a subset of the vector indices
		/// </summary>
		/// <param name="indices">A list of indexes to use as the source of the new vector</param>
		IVector GetNewVectorFromIndexes(IReadOnlyList<int> indices);

		/// <summary>
		/// Creates a new copy of the vector
		/// </summary>
		IVector Clone();

		/// <summary>
		/// Creates a new vector in which each element is the square root of the current vector
		/// </summary>
		IVector Sqrt();

		/// <summary>
		/// Creates a new vector in which each element is the absolute value of the current vector
		/// </summary>
		IVector Abs();

		/// <summary>
		/// Copies values from the target vector into the current vector
		/// </summary>
		/// <param name="vector"></param>
		void CopyFrom(IVector vector);

		/// <summary>
		/// Calculates the euclidean distance between the current and the target vector
		/// </summary>
		/// <param name="vector">The target vector</param>
		float EuclideanDistance(IVector vector);

		/// <summary>
		/// Calculates the cosine distance between the current and the target vector
		/// </summary>
		/// <param name="vector">The target vector></param>
		float CosineDistance(IVector vector);

		/// <summary>
		/// Calculates the manhattan distance between the current and the target vector
		/// </summary>
		/// <param name="vector">The target vector</param>
		float ManhattanDistance(IVector vector);

		/// <summary>
		/// Calculates the mean squared distance between the current and the target vector
		/// </summary>
		/// <param name="vector">The target vector</param>
		float MeanSquaredDistance(IVector vector);

		/// <summary>
		/// Calculates the squared euclidean distance between the current and the target vector
		/// </summary>
		/// <param name="vector">The target vector</param>
		float SquaredEuclidean(IVector vector);

		/// <summary>
		/// Finds the minimum and maximum values in the current vector
		/// </summary>
		(float Min, float Max) GetMinMax();

		/// <summary>
		/// Calculates the average value from the elements of the current vector
		/// </summary>
		float Average();

		/// <summary>
		/// Calculates the standard deviation from the elements of the current vector
		/// </summary>
		/// <param name="mean">(optional) pre calculated mean</param>
		float StdDev(float? mean);

		/// <summary>
		/// Normalises (in place) the values of the current vector
		/// </summary>
		/// <param name="type">The type of normalisation</param>
		void Normalise(NormalisationType type);

		/// <summary>
		/// Returns the softmax function (without in place modification) applied to the current vector
		/// https://en.wikipedia.org/wiki/Softmax_function
		/// </summary>
		IVector Softmax();

		/// <summary>
		/// Returns the jacobian matrix of the softmax derivative
		/// </summary>
		/// <returns></returns>
		IMatrix SoftmaxDerivative();

		/// <summary>
		/// Returns a vector of distances between the current and target vectors
		/// </summary>
		/// <param name="data">The list of target vectors</param>
		/// <param name="distance">The distance metric</param>
		/// <returns>A vector in which each element n is the distance between the current and the nth target vector</returns>
		IVector FindDistances(IReadOnlyList<IVector> data, DistanceMetric distance);

		/// <summary>
		/// Returns the distance between the current and the target vector
		/// </summary>
		/// <param name="other">The target vector</param>
		/// <param name="distance">The distance metric</param>
		float FindDistance(IVector other, DistanceMetric distance);

		/// <summary>
		/// Returns a vector of the cosine distance between the current and target vectors
		/// </summary>
		/// <param name="data">The list of target vectors</param>
		/// <param name="dataNorm">A buffer to hold the norms of the target vectors</param>
		/// <returns>A vector in which each element n is the cosine distance between the current and the nth target vector</returns>
		IVector CosineDistance(IReadOnlyList<IVector> data, ref float[] dataNorm);

		/// <summary>
		/// Returns a vector (without in place modification) in which each element is the natural log of each element in the current vector
		/// </summary>
		IVector Log();

		/// <summary>
		/// Returns the sigmoid function (without in place modification) applied to the current vector
		/// </summary>
		IVector Sigmoid();

		/// <summary>
		/// Fast conversion to matrix (internal buffer is used directly)
		/// </summary>
		/// <param name="rows">The number of rows in the matrix</param>
		/// <param name="columns">The number of columns in the matrix</param>
		IMatrix ReshapeAsMatrix(int rows, int columns);

		/// <summary>
		/// Converts the vector to a 3D tensor
		/// </summary>
		/// <param name="rows">Number of rows in each matrix</param>
		/// <param name="columns">Number of columns in matrix</param>
		/// <param name="depth">Number of matrices</param>
		/// <returns></returns>
		I3DTensor ReshapeAs3DTensor(int rows, int columns, int depth);

		/// <summary>
		/// Converts the vector to a 4D tensor
		/// </summary>
		/// <param name="rows">Number of rows in each matrix</param>
		/// <param name="columns">Number of columns in matrix</param>
		/// <param name="depth">Number of matrices</param>
		/// <param name="count">Number of 3D tensors</param>
		/// <returns></returns>
		I4DTensor ReshapeAs4DTensor(int rows, int columns, int depth, int count);

		/// <summary>
		/// Splits the vector into a list of vectors
		/// </summary>
		/// <param name="blockCount">The number of sub vectors to split into</param>
		IReadOnlyList<IVector> Split(int blockCount);

		/// <summary>
		/// Rotates values in the vector (both horizontally and vertically within blocks)
		/// </summary>
		/// <param name="blockCount"></param>
		void RotateInPlace(int blockCount = 1);

		/// <summary>
		/// Returns a reversed copy of the vector's values
		/// </summary>
		/// <returns></returns>
		IVector Reverse();

		/// <summary>
		/// Returns the value at the specified index
		/// </summary>
		/// <param name="index">The index of the vector to return</param>
		/// <returns></returns>
		float GetAt(int index);

		/// <summary>
		/// Updates the value at the specified index
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		void SetAt(int index, float value);

		/// <summary>
		/// Checks if every value in the vector is finite (not NaN or positive/negative infinity)
		/// </summary>
		/// <returns></returns>
		bool IsEntirelyFinite();
	}

	/// <summary>
	/// Returns an indexable vector (in which elements can be directly indexed)
	/// </summary>
	public interface IIndexableVector : IVector
	{
		/// <summary>
		/// Returns an element at the specified index
		/// </summary>
		/// <param name="index">The index to retrieve</param>
		float this[int index] { get; set; }

		/// <summary>
		/// Gets the values as an enumerable
		/// </summary>
		IEnumerable<float> Values { get; }

		/// <summary>
		/// Converts the vector to an array
		/// </summary>
		float[] ToArray();

		/// <summary>
		/// Returns the underlying array used as storage (changes to this array will affect the vector as well)
		/// </summary>
		float[] GetInternalArray();

		/// <summary>
		/// Creates a new vector (without in place modification) in which new values are appended onto the end of the current vector
		/// </summary>
		/// <param name="data">The values to append</param>
		IIndexableVector Append(IReadOnlyList<float> data);
	}

	/// <summary>
	/// A matrix
	/// </summary>
	public interface IMatrix : IDisposable
	{
		/// <summary>
		/// Checks if the matrix has not been disposed
		/// </summary>
		bool IsValid { get; }

		/// <summary>
		/// Multiplies the current vector (without in place modification) with the target matrix
		/// </summary>
		/// <param name="matrix">The target matrix</param>
		IMatrix Multiply(IMatrix matrix);

		/// <summary>
		/// The number of columns
		/// </summary>
		int ColumnCount { get; }

		/// <summary>
		/// The number of rows
		/// </summary>
		int RowCount { get; }

		/// <summary>
		/// Returns a column as a vector
		/// </summary>
		/// <param name="index">The column index</param>
		IVector Column(int index);

		/// <summary>
		/// Returns the matrix diagonal as a vector
		/// </summary>
		IVector Diagonal();

		/// <summary>
		/// Returns a row as a vector
		/// </summary>
		/// <param name="index">The row index</param>
		IVector Row(int index);

		/// <summary>
		/// Returns the current matrix (without in place modification) added to the target matrix
		/// </summary>
		/// <param name="matrix">The target matrix</param>
		IMatrix Add(IMatrix matrix);

		/// <summary>
		/// Returns the current matrix  (without in place modification) minus the target matrix
		/// </summary>
		/// <param name="matrix">The target matrix</param>
		IMatrix Subtract(IMatrix matrix);

		/// <summary>
		/// Returns the pointwise product of the current matrix (without in place modification) with the target matrix
		/// </summary>
		/// <param name="matrix">The target matrix</param>
		IMatrix PointwiseMultiply(IMatrix matrix);

		/// <summary>
		/// Returns the current matrix (without in place modification) and multipled with the transposed target matrix
		/// </summary>
		/// <param name="matrix">The target matrix</param>
		IMatrix TransposeAndMultiply(IMatrix matrix);

		/// <summary>
		/// Returns the transpose of the current matrix (without in place modification) multipled with the target matrix
		/// </summary>
		/// <param name="matrix"></param>
		IMatrix TransposeThisAndMultiply(IMatrix matrix);

		/// <summary>
		/// Returns a vector that contains the sum of the elements in each row of the current matrix
		/// </summary>
		IVector RowSums();

		/// <summary>
		/// Returns a vector that contains the sum of the elements in each column of the current matrix
		/// </summary>
		IVector ColumnSums();

		/// <summary>
		/// Returns the transpose of the current matrix
		/// </summary>
		IMatrix Transpose();

		/// <summary>
		/// Multiplies (in place) each element of the matrix by a scalar
		/// </summary>
		/// <param name="scalar">The scalar to multiply each element</param>
		void Multiply(float scalar);

		/// <summary>
		/// Returns the product of the current matrix (without in place modification) with the target vector
		/// </summary>
		/// <param name="vector">The target vector</param>
		IMatrix Multiply(IVector vector);

		/// <summary>
		/// Adds the target matrix to the current matrix (in place)
		/// </summary>
		/// <param name="matrix">The target matrix</param>
		/// <param name="coefficient1">A coefficient to multiply each element of the current matrix</param>
		/// <param name="coefficient2">A coefficient to multipy each element of the target matrix</param>
		void AddInPlace(IMatrix matrix, float coefficient1 = 1.0f, float coefficient2 = 1.0f);

		/// <summary>
		/// Subtracts the target matrix from the current matrix (in place)
		/// </summary>
		/// <param name="matrix">The target matrix</param>
		/// <param name="coefficient1">A coefficient to multiply each element of the current matrix</param>
		/// <param name="coefficient2">A coefficient to multipy each element of the target matrix</param>
		void SubtractInPlace(IMatrix matrix, float coefficient1 = 1.0f, float coefficient2 = 1.0f);

		/// <summary>
		/// Returns a new matrix with the sigmoid function applied to each element
		/// </summary>
		IMatrix SigmoidActivation();

		/// <summary>
		/// Returns a new matrix with the sigmoid derivative of each element
		/// </summary>
		IMatrix SigmoidDerivative();

		/// <summary>
		/// Returns a new matrix with the tanh function applied to each element
		/// </summary>
		IMatrix TanhActivation();

		/// <summary>
		/// Returns a new matrix with the tanh derivative of each element
		/// </summary>
		IMatrix TanhDerivative();

		/// <summary>
		/// Returns a new matrix with the softmax function applied to each row of the matrix
		/// </summary>
		IMatrix SoftmaxActivation();

		/// <summary>
		/// Adds the target vector to each row of the current matrix (in place)
		/// </summary>
		/// <param name="vector">The target vector</param>
		void AddToEachRow(IVector vector);

		/// <summary>
		/// Adds the target vector to each column of the current matrix (in place)
		/// </summary>
		/// <param name="vector">The target vector</param>
		void AddToEachColumn(IVector vector);

		/// <summary>
		/// Converts the current matrix to protobuf format
		/// </summary>
		FloatMatrix Data { get; set; }

		/// <summary>
		/// Converts the matrix to an indexable matrix
		/// </summary>
		IIndexableMatrix AsIndexable();

		/// <summary>
		/// Returns a new matrix from a subset of the current matrix's rows
		/// </summary>
		/// <param name="rowIndexes">The list of row indices</param>
		IMatrix GetNewMatrixFromRows(IReadOnlyList<int> rowIndexes);

		/// <summary>
		/// Returns a new matrix from a subset of the current matrix's columns
		/// </summary>
		/// <param name="columnIndexes">The list of column indices</param>
		IMatrix GetNewMatrixFromColumns(IReadOnlyList<int> columnIndexes);

		/// <summary>
		/// Set to zero the specified rows in the current matrix
		/// </summary>
		/// <param name="indexes">The list of row indices</param>
		void ClearRows(IReadOnlyList<int> indexes);

		/// <summary>
		/// Set to zero the specified columns in the current matrix
		/// </summary>
		/// <param name="indexes">The list of column indices</param>
		void ClearColumns(IReadOnlyList<int> indexes);

		/// <summary>
		/// Returns the RELU function applied to each element of the current matrix
		/// </summary>
		IMatrix ReluActivation();

		/// <summary>
		/// Returns the RELU derivative of each element in the current matrix
		/// </summary>
		IMatrix ReluDerivative();

		/// <summary>
		/// Returns the leaky RELU function applied to each element in the current matrix
		/// </summary>
		IMatrix LeakyReluActivation();

		/// <summary>
		/// Returns the leaky RELU derivative of each element in the current matrix
		/// </summary>
		IMatrix LeakyReluDerivative();

		/// <summary>
		/// Creates a copy of the current matrix
		/// </summary>
		IMatrix Clone();

		/// <summary>
		/// Sets each element to zero
		/// </summary>
		void Clear();

		/// <summary>
		/// Returns the square root of each element in the current matrix
		/// </summary>
		/// <param name="valueAdjustment">Term to add to each element in the result matrix</param>
		IMatrix Sqrt(float valueAdjustment = 1e-8f);

		/// <summary>
		/// Returns each element raised to specified power
		/// </summary>
		/// <param name="power">The power to apply to each element</param>
		IMatrix Pow(float power);

		/// <summary>
		/// Returns the current matrix (not modified in place) divided by the target matrix
		/// </summary>
		/// <param name="matrix">The target matrix</param>
		IMatrix PointwiseDivide(IMatrix matrix);

		/// <summary>
		/// L1 Regularisation applied to each element of the current matrix (in place)
		/// </summary>
		/// <param name="coefficient">The L1 coefficient</param>
		void L1Regularisation(float coefficient);

		/// <summary>
		/// Returns a vector of the L2 norms of each column
		/// </summary>
		IVector ColumnL2Norm();

		/// <summary>
		/// Returns a vector of the L2 norms of each row
		/// </summary>
		IVector RowL2Norm();

		/// <summary>
		/// Pointwise divide each row by the target vector (in place)
		/// </summary>
		/// <param name="vector">The target vector</param>
		void PointwiseDivideRows(IVector vector);

		/// <summary>
		/// Pointwise divide each column by the target vector (in place)
		/// </summary>
		/// <param name="vector">The target vector</param>
		void PointwiseDivideColumns(IVector vector);

		/// <summary>
		/// Constrain each value within the specified min and max values (in place)
		/// </summary>
		/// <param name="min">The minimum allowed value</param>
		/// <param name="max">The maximum allowed value</param>
		void Constrain(float min, float max);

		/// <summary>
		/// Returns a segment from a row of the current matrix
		/// </summary>
		/// <param name="rowIndex">The row index</param>
		/// <param name="columnIndex">The start index to return</param>
		/// <param name="length">The number of elements to return</param>
		IVector GetRowSegment(int rowIndex, int columnIndex, int length);

		/// <summary>
		/// Returns a segment from a column of the current matrix
		/// </summary>
		/// <param name="columnIndex">The column index</param>
		/// <param name="rowIndex">The start index to return</param>
		/// <param name="length">The number of elements to return</param>
		IVector GetColumnSegment(int columnIndex, int rowIndex, int length);

		/// <summary>
		/// Returns a new matrix with the columns of the target matrix appended to each column of the current matrix
		/// </summary>
		/// <param name="bottom">The target matrix</param>
		IMatrix ConcatColumns(IMatrix bottom);

		/// <summary>
		/// Returns a new matrix with the rows of the target matrix appended to each row of the current matrix
		/// </summary>
		/// <param name="right">The target matrix</param>
		IMatrix ConcatRows(IMatrix right);

		/// <summary>
		/// Splits the rows of the current matrix into two matrices
		/// </summary>
		/// <param name="columnIndex">The column index at which to split</param>
		(IMatrix Left, IMatrix Right) SplitAtColumn(int columnIndex);

		/// <summary>
		/// Splits the columns of the current matrix into two matrices
		/// </summary>
		/// <param name="rowIndex">The row index at which to split</param>
		(IMatrix Top, IMatrix Bottom) SplitAtRow(int rowIndex);

		/// <summary>
		/// Singular value decomposition
		/// </summary>
		(IMatrix U, IVector S, IMatrix VT) Svd();

		/// <summary>
		/// Fast conversion to vector (the internal buffer is not modified)
		/// </summary>
		IVector ReshapeAsVector();

		/// <summary>
		/// Reshapes the matrix to a 3D tensor, treating each column as a depth slice in the new 3D tensor
		/// </summary>
		/// <param name="rows">Row count of each sub matrix</param>
		/// <param name="columns">Column count of each sub matrix</param>
		/// <returns></returns>
		I3DTensor ReshapeAs3DTensor(int rows, int columns);

		/// <summary>
		/// Converts the matrix to a 4D tensor, treating each column as a 3D tensor
		/// </summary>
		/// <param name="rows">Row count of each sub matrix</param>
		/// <param name="columns">Column count of each sub matrix</param>
		/// <param name="depth">Depth of each 3D tensor</param>
		/// <returns></returns>
		I4DTensor ReshapeAs4DTensor(int rows, int columns, int depth);

		/// <summary>
		/// Returns the value at the specified row and column index
		/// </summary>
		/// <param name="row">Row index</param>
		/// <param name="column">Column index</param>
		/// <returns></returns>
		float GetAt(int row, int column);

		/// <summary>
		/// Updates the value at the specified row and column index
		/// </summary>
		/// <param name="row">Row index</param>
		/// <param name="column">Column index</param>
		/// <param name="value">Value to set</param>
		void SetAt(int row, int column, float value);

		/// <summary>
		/// Returns the columns of the matrix as vectors
		/// </summary>
		/// <returns></returns>
		IReadOnlyList<IVector> ColumnVectors();

		/// <summary>
		/// Returns the rows of the matrix as vectors
		/// </summary>
		/// <returns></returns>
		IReadOnlyList<IVector> RowVectors();
	}

	/// <summary>
	/// A matrix whose elements can be indexed directly
	/// </summary>
	public interface IIndexableMatrix : IMatrix
	{
		/// <summary>
		/// Returns an element from the current matrix
		/// </summary>
		/// <param name="row">Row index</param>
		/// <param name="column">Column index</param>
		float this[int row, int column] { get; set; }

		/// <summary>
		/// Returns the rows of the current matrix as vectors
		/// </summary>
		IEnumerable<IIndexableVector> Rows { get; }

		/// <summary>
		/// Returns the columns of the current matrix as vectors
		/// </summary>
		IEnumerable<IIndexableVector> Columns { get; }

		/// <summary>
		/// Returns each element in the current matrix as enumerable
		/// </summary>
		IEnumerable<float> Values { get; }

		/// <summary>
		/// Mutates each element of the current matrix
		/// </summary>
		/// <param name="mutator">The function to apply to each element</param>
		/// <returns></returns>
		IIndexableMatrix Map(Func<float, float> mutator);

		/// <summary>
		/// Mutates each element of the current matrix
		/// </summary>
		/// <param name="mutator">The function to apply to each element (rowIndex: int, columnIndex: int, value: float) => float</param>
		/// <returns></returns>
		IIndexableMatrix MapIndexed(Func<int, int, float, float> mutator);

		/// <summary>
		/// Returns the matrix as xml
		/// </summary>
		string AsXml { get; }

		/// <summary>
		/// Returns the underlying array used as storage (changes to this array will affect the matrix as well)
		/// </summary>
		float[] GetInternalArray();
	}

	/// <summary>
	/// A list of matrices
	/// </summary>
	public interface I3DTensor : IDisposable
	{
		/// <summary>
		/// The number of rows in each matrix
		/// </summary>
		int RowCount { get; }

		/// <summary>
		/// The number of columns in each matrix
		/// </summary>
		int ColumnCount { get; }

		/// <summary>
		/// The number of matrices
		/// </summary>
		int Depth { get; }

		/// <summary>
		/// Converts the current tensor to protobuf format
		/// </summary>
		FloatTensor Data { get; set; }

		/// <summary>
		/// Returns a matrix at the specified depth
		/// </summary>
		/// <param name="depth">The depth to query</param>
		/// <returns></returns>
		IMatrix GetMatrixAt(int depth);

		/// <summary>
		/// Returns an indexable 3D tensor
		/// </summary>
		/// <returns></returns>
		IIndexable3DTensor AsIndexable();

		/// <summary>
		/// Adds padding to each matrix
		/// </summary>
		/// <param name="padding">The padding (both vertical and horizontal)</param>
		/// <returns>A new tensor</returns>
		I3DTensor AddPadding(int padding);

		/// <summary>
		/// Removes padding from each matrix
		/// </summary>
		/// <param name="padding">The padding to remove</param>
		/// <returns>A new tensor</returns>
		I3DTensor RemovePadding(int padding);

		/// <summary>
		/// Performs a convolution on each source matrix
		/// </summary>
		/// <param name="filterWidth">The filter width</param>
		/// <param name="filterHeight">The filter height</param>
		/// <param name="xStride">Filter x stride</param>
		/// <param name="yStride">Filter y stride</param>
		/// <returns></returns>
		IMatrix Im2Col(int filterWidth, int filterHeight, int xStride, int yStride);

		/// <summary>
		/// Converts the tensor to a vector
		/// </summary>
		/// <returns></returns>
		IVector ReshapeAsVector();

		/// <summary>
		/// Converts the tensor to a matrix (each depth slice becomes a column in the new matrix)
		/// </summary>
		/// <returns></returns>
		IMatrix ReshapeAsMatrix();

		/// <summary>
		/// Reshapes the 3D tensor into a 4D tensor (the current depth becomes the count of 3D tensors and columns becomes the new depth)
		/// </summary>
		/// <param name="rows">Rows in each 4D tensor</param>
		/// <param name="columns">Columns in each 4D tensor</param>
		I4DTensor ReshapeAs4DTensor(int rows, int columns);

		/// <summary>
		/// Performs a max pooling operation on the tensor
		/// </summary>
		/// <param name="filterWidth">The pooling filter width</param>
		/// <param name="filterHeight">The pooling filter height</param>
		/// <param name="xStride">Filter x stride</param>
		/// <param name="yStride">Filter y stride</param>
		/// <param name="saveIndices">True to save the indices for a future reverse max pool operation</param>
		/// <returns>A max pooled tensor</returns>
		(I3DTensor Result, I3DTensor Indices) MaxPool(int filterWidth, int filterHeight, int xStride, int yStride, bool saveIndices);

		/// <summary>
		/// Reverses a max pooling operation
		/// </summary>
		/// <param name="outputRows">Input rows</param>
		/// <param name="outputColumns">Input columns</param>
		/// <param name="indices">A tensor that contains the indices of each maximum value that was found per filter</param>
		/// <param name="filterWidth">Width of each filter</param>
		/// <param name="filterHeight">Height of each filter</param>
		/// <param name="xStride">Filter x stride</param>
		/// <param name="yStride">Filter y stride</param>
		I3DTensor ReverseMaxPool(I3DTensor indices, int outputRows, int outputColumns, int filterWidth, int filterHeight, int xStride, int yStride);

		/// <summary>
		/// Reverses a im2col operation
		/// </summary>
		/// <param name="filter">The rotated filters</param>
		/// <param name="outputRows">Rows of the input tensor</param>
		/// <param name="outputColumns">Columns of the input tensor</param>
		/// <param name="outputDepth">Depth of the input tensor</param>
		/// <param name="filterHeight">Height of each filter</param>
		/// <param name="filterWidth">Width of each filter</param>
		/// <param name="xStride">Filter x stride</param>
		/// <param name="yStride">Filter y stride</param>
		/// <returns></returns>
		I3DTensor ReverseIm2Col(IMatrix filter, int outputRows, int outputColumns, int outputDepth, int filterWidth, int filterHeight, int xStride, int yStride);

		/// <summary>
		/// Adds each depth slice into a single matrix
		/// </summary>
		/// <returns></returns>
		IMatrix CombineDepthSlices();

		/// <summary>
		/// Adds the other tensor to the current tensor
		/// </summary>
		/// <param name="tensor">Tensor to add</param>
		void AddInPlace(I3DTensor tensor);

		/// <summary>
		/// Multiplies the tensor with the other matrix
		/// </summary>
		/// <param name="matrix">Matrix to multiply with</param>
		/// <returns></returns>
		I3DTensor Multiply(IMatrix matrix);

		/// <summary>
		/// Adds the vector to each row of the tensor
		/// </summary>
		/// <param name="vector">Vector to add to each row</param>
		void AddToEachRow(IVector vector);

		/// <summary>
		/// Transpose each sub matrix in the current tensor before multiplying it with each each sub tensor (converted to a matrix)
		/// </summary>
		/// <param name="tensor">Tensor to multiply with</param>
		I3DTensor TransposeThisAndMultiply(I4DTensor tensor);
	}

	/// <summary>
	/// A 3D tensor that can be directly indexed
	/// </summary>
	public interface IIndexable3DTensor : I3DTensor
	{
		/// <summary>
		/// Returns a value from the tensor
		/// </summary>
		/// <param name="row">The row to query</param>
		/// <param name="column">The column to query</param>
		/// <param name="depth">The depth to query</param>
		float this[int row, int column, int depth] { get; set; }

		/// <summary>
		/// Gets a list of the indexable matrices
		/// </summary>
		IReadOnlyList<IIndexableMatrix> Matrix { get; }

		/// <summary>
		/// Returns the matrix as xml
		/// </summary>
		string AsXml { get; }

		/// <summary>
		/// Returns the underlying array used as storage (changes to this array will affect the tensor as well)
		/// </summary>
		float[] GetInternalArray();
	}

	/// <summary>
	/// A list of 3D tensors
	/// </summary>
	public interface I4DTensor : IDisposable
	{
		/// <summary>
		/// The number of rows in each 3D tensor
		/// </summary>
		int RowCount { get; }

		/// <summary>
		/// The number of columns in each 3D tensor
		/// </summary>
		int ColumnCount { get; }

		/// <summary>
		/// The depth of each 3D tensor
		/// </summary>
		int Depth { get; }

		/// <summary>
		/// The count of 3D tensors
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Returns the tensor at the specified index
		/// </summary>
		/// <param name="index">The index to query</param>
		I3DTensor GetTensorAt(int index);

		/// <summary>
		/// Returns an indexable list of 3D tensors
		/// </summary>
		/// <returns></returns>
		IIndexable4DTensor AsIndexable();

		/// <summary>
		/// Adds padding to the 4D tensor
		/// </summary>
		/// <param name="padding">Padding to add to the left, top, right and bottom edges of the tensor</param>
		/// <returns>A new tensor with the padding added</returns>
		I4DTensor AddPadding(int padding);

		/// <summary>
		/// Removes padding from the 4D tensor
		/// </summary>
		/// <param name="padding">Padding to remove from the left, top, right and bottom edges of the tensor</param>
		/// <returns>A new tensor with the padding removed</returns>
		I4DTensor RemovePadding(int padding);

		/// <summary>
		/// Applies a max pooling operation to the current tensor
		/// </summary>
		/// <param name="filterWidth">Max pool filter width</param>
		/// <param name="filterHeight">Max pool filter height</param>
		/// <param name="xStride">Filter x stride</param>
		/// <param name="yStride">Filter y stride</param>
		/// <param name="saveIndices">True to save the indices for a future reverse pool operation</param>
		(I4DTensor Result, I4DTensor Indices) MaxPool(int filterWidth, int filterHeight, int xStride, int yStride, bool saveIndices);

		/// <summary>
		/// Reverses a max pool operation
		/// </summary>
		/// <param name="outputRows">Input tensor rows</param>
		/// <param name="outputColumns">Input tensor columns</param>
		/// <param name="indices">Tensor of indices from MaxPool operation</param>
		/// <param name="filterWidth">Max pool filter width</param>
		/// <param name="filterHeight">Max pool filter height</param>
		/// <param name="xStride">Filter x stride</param>
		/// <param name="yStride">Filter y stride</param>
		/// <returns></returns>
		I4DTensor ReverseMaxPool(I4DTensor indices, int outputRows, int outputColumns, int filterWidth, int filterHeight, int xStride, int yStride);

		/// <summary>
		/// Applies the convolutional filter to each 3D tensor, producing a 3D tensor which can be multipled by the filter matrix
		/// </summary>
		/// <param name="filterWidth">Filter width</param>
		/// <param name="filterHeight">Filter height</param>
		/// <param name="xStride">Filter x stride</param>
		/// <param name="yStride">Filter y stride</param>
		/// <returns></returns>
		I3DTensor Im2Col(int filterWidth, int filterHeight, int xStride, int yStride);

		/// <summary>
		/// Reverse a previously applied im2Col
		/// </summary>
		/// <param name="filter">List of filters that have been rotated 180 degrees</param>
		/// <param name="outputRows">Rows of the input 4D tensor</param>
		/// <param name="outputColumns">Columns of the input 4D tensor</param>
		/// <param name="outputDepth">Depth of the input 4D tensor</param>
		/// <param name="filterWidth">Filter width</param>
		/// <param name="filterHeight">Filter height</param>
		/// <param name="xStride">Filter x stride</param>
		/// <param name="yStride">Filter y stride</param>
		/// <returns></returns>
		I4DTensor ReverseIm2Col(IMatrix filter, int outputRows, int outputColumns, int outputDepth, int filterWidth, int filterHeight, int xStride, int yStride);

		/// <summary>
		/// Sums the columns of each sub-tensor's sub matrix
		/// </summary>
		IVector ColumnSums();

		/// <summary>
		/// Converts the tensor to a vector
		/// </summary>
		/// <returns></returns>
		IVector ReshapeAsVector();

		/// <summary>
		/// Converts the tensor to a matrix (each 3D tensor becomes a column in the new matrix)
		/// </summary>
		/// <returns></returns>
		IMatrix ReshapeAsMatrix();

		/// <summary>
		/// Converts the current tensor to protobuf format
		/// </summary>
		IReadOnlyList<FloatTensor> Data { get; set; }
	}

	/// <summary>
	/// A 4D tensor that can be directly indexed
	/// </summary>
	public interface IIndexable4DTensor : I4DTensor
	{
		/// <summary>
		/// Returns a value from the tensor
		/// </summary>
		/// <param name="row">The row to query</param>
		/// <param name="column">The column to query</param>
		/// <param name="depth">The depth to query</param>
		/// <param name="index">The tensor index to query</param>
		float this[int row, int column, int depth, int index] { get; set; }

		/// <summary>
		/// Gets a list of the indexable matrices
		/// </summary>
		IReadOnlyList<IIndexable3DTensor> Tensors { get; }

		/// <summary>
		/// Returns the matrix as xml
		/// </summary>
		string AsXml { get; }

		/// <summary>
		/// Returns the underlying array used as storage (changes to this array will affect the tensor as well)
		/// </summary>
		float[] GetInternalArray();
	}
}
