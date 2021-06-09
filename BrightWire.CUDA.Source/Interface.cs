﻿using ManagedCuda;
using ManagedCuda.BasicTypes;

namespace BrightWire
{
	/// <summary>
	/// Wrapper for a device memory pointer
	/// </summary>
	interface IDeviceMemoryPtr
	{
		int AddRef();
		void Free();
		CudaDeviceVariable<float> DeviceVariable { get; }
		CUdeviceptr DevicePointer { get; }
		int Size { get; }
		void CopyToDevice(float[] source);
		void CopyToDevice(IDeviceMemoryPtr source);
		void CopyToHost(float[] target);
		void Clear();
	}

	interface IHaveDeviceMemory
	{
		IDeviceMemoryPtr Memory { get; }
	}
}
