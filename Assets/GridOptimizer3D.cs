using UnityEngine;
using System.Runtime.InteropServices;
using kmty.Util;

namespace NearestNeighbor {

    public class GridOptimizer3D<T> : GridOptimizerBase where T : struct {

        public struct Uint2 {
            public uint x;
            public uint y;
        }

        Vector3 gridDim;

        public GridOptimizer3D(int numObjects, Vector3 range, Vector3 dimension) : base(numObjects) {
            gridDim = dimension;
            numGrid = (int)(dimension.x * dimension.y * dimension.z);
            gridH = range.x / gridDim.x;
            GridSortCS = (ComputeShader)Resources.Load("GridSort3D");
            InitializeBuffer();
            Debug.Log("=== Instantiated Grid Sort === \nRange:" + range + ", NumGrid:" + numGrid + ", GridDim:" + gridDim + ", GridH:" + gridH);
        }

        protected override void InitializeBuffer() {
            gridBuffer = new ComputeBuffer(numObjects, Marshal.SizeOf(typeof(Uint2)));
            gridPingPongBuffer = new ComputeBuffer(numObjects, Marshal.SizeOf(typeof(Uint2)));
            gridIndicesBuffer = new ComputeBuffer(numGrid, Marshal.SizeOf(typeof(Uint2)));
            sortedObjectsBufferOutput = new ComputeBuffer(numObjects, Marshal.SizeOf(typeof(T)));
        }

        protected override void SetCSVariables() {
            GridSortCS.SetVector("_GridDim", gridDim);
            GridSortCS.SetFloat("_GridH", gridH);
        }
    }

    public abstract class GridOptimizerBase {

        BitonicSort bitonicSort;
        protected ComputeBuffer gridBuffer;
        protected ComputeBuffer gridPingPongBuffer;
        protected ComputeBuffer gridIndicesBuffer;
        protected ComputeBuffer sortedObjectsBufferOutput;
        protected int numObjects;
        protected ComputeShader GridSortCS;
        protected static readonly int SIMULATION_BLOCK_SIZE_FOR_GRID = 32;
        protected int threadGroupSize;
        protected int numGrid;
        protected float gridH;

        public GridOptimizerBase(int numObjects) {
            this.numObjects = numObjects;
            this.threadGroupSize = numObjects / SIMULATION_BLOCK_SIZE_FOR_GRID;
            bitonicSort = new BitonicSort(numObjects);
        }

        public float GetGridH() => gridH;
        public ComputeBuffer GetGridIndicesBuffer() => gridIndicesBuffer;

        public void Release() {
            ComputeShaderUtil.Destroy(gridBuffer);
            ComputeShaderUtil.Destroy(gridIndicesBuffer);
            ComputeShaderUtil.Destroy(gridPingPongBuffer);
            ComputeShaderUtil.Destroy(sortedObjectsBufferOutput);
        }

        public void GridSort(ref ComputeBuffer objectsBufferInput) {
            GridSortCS.SetInt("_NumParticles", numObjects);
            SetCSVariables();
            int kernel = 0;

            #region GridOptimization
            // Build Grid
            kernel = GridSortCS.FindKernel("BuildGridCS");
            GridSortCS.SetBuffer(kernel, "_ParticlesBufferRead", objectsBufferInput);
            GridSortCS.SetBuffer(kernel, "_GridBufferWrite", gridBuffer);
            GridSortCS.Dispatch(kernel, threadGroupSize, 1, 1);

            // Sort Grid
            bitonicSort.Sort(ref gridBuffer, ref gridPingPongBuffer);

            // Build Grid Indices
            kernel = GridSortCS.FindKernel("ClearGridIndicesCS");
            GridSortCS.SetBuffer(kernel, "_GridIndicesBufferWrite", gridIndicesBuffer);
            GridSortCS.Dispatch(kernel, (int)(numGrid / SIMULATION_BLOCK_SIZE_FOR_GRID), 1, 1);

            kernel = GridSortCS.FindKernel("BuildGridIndicesCS");
            GridSortCS.SetBuffer(kernel, "_GridBufferRead", gridBuffer);
            GridSortCS.SetBuffer(kernel, "_GridIndicesBufferWrite", gridIndicesBuffer);
            GridSortCS.Dispatch(kernel, threadGroupSize, 1, 1);

            // Rearrange
            kernel = GridSortCS.FindKernel("RearrangeParticlesCS");
            GridSortCS.SetBuffer(kernel, "_GridBufferRead", gridBuffer);
            GridSortCS.SetBuffer(kernel, "_ParticlesBufferRead", objectsBufferInput);
            GridSortCS.SetBuffer(kernel, "_ParticlesBufferWrite", sortedObjectsBufferOutput);
            GridSortCS.Dispatch(kernel, threadGroupSize, 1, 1);
            #endregion GridOptimization

            // Copy buffer
            kernel = GridSortCS.FindKernel("CopyBuffer");
            GridSortCS.SetBuffer(kernel, "_ParticlesBufferRead", sortedObjectsBufferOutput);
            GridSortCS.SetBuffer(kernel, "_ParticlesBufferWrite", objectsBufferInput);
            GridSortCS.Dispatch(kernel, threadGroupSize, 1, 1);
        }

        protected abstract void InitializeBuffer();
        protected abstract void SetCSVariables();
    }
}