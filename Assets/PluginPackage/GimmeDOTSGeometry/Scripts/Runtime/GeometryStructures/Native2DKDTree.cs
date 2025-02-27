using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GimmeDOTSGeometry
{
    public unsafe struct Native2DKDTree : IDisposable
    {

        #region Private Variables

        private int axis0;
        private int axis1;

        private NativeArray<float3> nodes;

        private Rect bounds;

        #endregion

        public float3* GetRoot()
        {
            if (this.nodes.IsCreated)
            {
                return (float3*)this.nodes.GetUnsafePtr();
            }
            return null;
        }

        public NativeArray<float3> GetNodes() => this.nodes;

        public int GetAxis0() => this.axis0;
        public int GetAxis1() => this.axis1;

        public int Count => this.nodes.Length;

        public Rect GetBounds() => this.bounds;

        private void ConstructKDTreeRecursion(int index, NativeList<float3> sortedList0, NativeList<float3> sortedList1, int depth)
        {

            int count = sortedList0.Length;
            if (count <= 0) return;

            if (count == 1)
            {
                this.nodes[index] = sortedList0[0];
            }
            else
            {
                float3 median = float3.zero;

                int treeHeight = Mathf.CeilToInt(Mathf.Log(sortedList0.Length + 1, 2.0f));
                int max = 1 << treeHeight;
                int min = 1 << (treeHeight - 1);
                int half = (max + min) / 2;

                int medianIdx;

                if(sortedList0.Length < half)
                {
                    int diff = sortedList0.Length - min;
                    medianIdx = (min / 2) + diff;
                } else
                {
                    medianIdx = (max / 2) - 1;
                }

                if (depth % 2 == 0)
                {
                    var comp1 = new Composite2DComparer() { axis0 = this.axis1, axis1 = this.axis0 };

                    median = sortedList0[medianIdx];

                    var sortedList0Left = new NativeList<float3>(medianIdx, Allocator.TempJob);
                    var sortedList0Right = new NativeList<float3>(sortedList0.Length - medianIdx - 1, Allocator.TempJob);

                    var sortedList1Left = new NativeList<float3>(medianIdx, Allocator.TempJob);
                    var sortedList1Right = new NativeList<float3>(sortedList0.Length - medianIdx - 1, Allocator.TempJob);

                    for(int i = 0; i < medianIdx; i++)
                    {
                        sortedList0Left.Add(sortedList0[i]);
                        sortedList1Left.Add(sortedList0[i]);
                    }

                    for(int i = medianIdx + 1; i < sortedList0.Length; i++)
                    {
                        sortedList0Right.Add(sortedList0[i]);
                        sortedList1Right.Add(sortedList0[i]);
                    }

                    sortedList1Left.Sort(comp1);
                    sortedList1Right.Sort(comp1);

                    this.ConstructKDTreeRecursion(index * 2 + 1, sortedList0Left, sortedList1Left, depth + 1);
                    this.ConstructKDTreeRecursion(index * 2 + 2, sortedList0Right, sortedList1Right, depth + 1);

                    sortedList0Left.Dispose();
                    sortedList0Right.Dispose();

                    sortedList1Left.Dispose();
                    sortedList1Right.Dispose();
                }
                else
                {
                    var comp0 = new Composite2DComparer() { axis0 = this.axis0, axis1 = this.axis1 };

                    median = sortedList1[medianIdx];

                    var sortedList0Left = new NativeList<float3>(medianIdx, Allocator.TempJob);
                    var sortedList0Right = new NativeList<float3>(sortedList0.Length - medianIdx - 1, Allocator.TempJob);

                    var sortedList1Left = new NativeList<float3>(medianIdx, Allocator.TempJob);
                    var sortedList1Right = new NativeList<float3>(sortedList0.Length - medianIdx - 1, Allocator.TempJob);

                    for (int i = 0; i < medianIdx; i++)
                    {
                        sortedList0Left.Add(sortedList1[i]);
                        sortedList1Left.Add(sortedList1[i]);
                    }

                    for (int i = medianIdx + 1; i < sortedList0.Length; i++)
                    {
                        sortedList0Right.Add(sortedList1[i]);
                        sortedList1Right.Add(sortedList1[i]);
                    }

                    sortedList0Left.Sort(comp0);
                    sortedList0Right.Sort(comp0);

                    this.ConstructKDTreeRecursion(index * 2 + 1, sortedList0Left, sortedList1Left, depth + 1);
                    this.ConstructKDTreeRecursion(index * 2 + 2, sortedList0Right, sortedList1Right, depth + 1);

                    sortedList0Left.Dispose();
                    sortedList0Right.Dispose();

                    sortedList1Left.Dispose();
                    sortedList1Right.Dispose();
                }

                this.nodes[index] = median;
            }
            
        }


        private struct Composite2DComparer : IComparer<float3>
        {
            public int axis0;
            public int axis1;

            public int Compare(float3 a, float3 b)
            {
                int comp = a[this.axis0].CompareTo(b[this.axis0]);
                if (comp != 0) return comp;
                return a[this.axis1].CompareTo(b[this.axis1]);
            }
        }

        private void ConstructKDTree(NativeArray<float3> positions)
        {
            float xMin = float.PositiveInfinity;
            float xMax = float.NegativeInfinity;
            float yMin = float.PositiveInfinity;
            float yMax = float.NegativeInfinity;

            for(int i = 0; i < positions.Length; i++)
            {
                var pos = positions[i];
                xMin = Mathf.Min(pos[this.axis0], xMin);
                xMax = Mathf.Max(pos[this.axis0], xMax);
                yMin = Mathf.Min(pos[this.axis1], yMin);
                yMax = Mathf.Max(pos[this.axis1], yMax);
            }

            this.bounds.Set(xMin, yMin, xMax - xMin, yMax - yMin);

            NativeList<float3> sortedList0 = new NativeList<float3>(positions.Length, Allocator.TempJob);
            NativeList<float3> sortedList1 = new NativeList<float3>(positions.Length, Allocator.TempJob);

            sortedList0.CopyFrom(positions);
            sortedList1.CopyFrom(positions);

            var comp0 = new Composite2DComparer() { axis0 = this.axis0, axis1 = this.axis1 };
            var comp1 = new Composite2DComparer() { axis0 = this.axis1, axis1 = this.axis0 };

            var sortJob0 = sortedList0.SortJob(comp0);
            var sortJob1 = sortedList1.SortJob(comp1);

            var handle0 = sortJob0.Schedule();
            var handle1 = sortJob1.Schedule();

            handle0.Complete();
            handle1.Complete();

            this.ConstructKDTreeRecursion(0, sortedList0, sortedList1, 0);

            sortedList0.Dispose();
            sortedList1.Dispose();
        }
        

        public Native2DKDTree(Vector3[] positions, CardinalPlane sortMode, Allocator allocator)
        {
            if(positions == null || positions.Length == 0)
            {
                Debug.LogError("Tried to construct native 2D KD tree with an empty array!");
            }

            var nativePositions = new NativeArray<float3>(positions.Length, Allocator.TempJob);
            var reinterpretation = nativePositions.Reinterpret<Vector3>();

            reinterpretation.CopyFrom(positions);

            this.nodes = new NativeArray<float3>(positions.Length, allocator);

            var axisIndices = sortMode.GetAxisIndices();
            this.axis0 = axisIndices.x;
            this.axis1 = axisIndices.y;

            this.bounds = new Rect(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity);

            this.ConstructKDTree(nativePositions);

            nativePositions.Dispose();
        }

        public Native2DKDTree(List<Vector3> positions, CardinalPlane sortMode, Allocator allocator) : this(positions.ToArray(), sortMode, allocator) { }

        public Native2DKDTree(NativeArray<Vector3> positions, CardinalPlane sortMode, Allocator allocator) : this(positions.Reinterpret<float3>(), sortMode, allocator) { }

        public Native2DKDTree(NativeArray<float3> positions, CardinalPlane sortMode, Allocator allocator)
        {
            if (positions.Length == 0)
            {
                Debug.LogError("Tried to construct native 2D KD tree with an empty array!");
            }

            this.nodes = new NativeArray<float3>(positions.Length, allocator);

            var axisIndices = sortMode.GetAxisIndices();
            this.axis0 = axisIndices.x;
            this.axis1 = axisIndices.y;

            this.bounds = new Rect(float.PositiveInfinity, float.PositiveInfinity, float.NegativeInfinity, float.NegativeInfinity);

            this.ConstructKDTree(positions);
        }



        [BurstCompile]
        private struct GetPointsInRadiusJob : IJob
        {
            public float radius;

            public int axis0;
            public int axis1;

            public Vector3 position;

            public Rect kdTreeBounds;


            [NoAlias, ReadOnly]
            public NativeArray<float3> nodes;

            [NoAlias, WriteOnly]
            public NativeList<float3> result;

            private float radiusSquared;

            private float2 planePos;

            private void AddSubtree(int nodeIdx)
            {

                this.result.Add(this.nodes[nodeIdx]);

                int left = nodeIdx * 2 + 1;
                int right = nodeIdx * 2 + 2;
                if (left < this.nodes.Length)
                {
                    this.AddSubtree(left);
                }

                if (right < this.nodes.Length)
                {
                    this.AddSubtree(right);
                }
            }

            private void SearchKDTreeRecursion(int currentNodeIdx, float xMin, float xMax, float yMin, float yMax, int axis)
            {

                var pos = new float2();
                var currentNode = this.nodes[currentNodeIdx];
                pos.x = currentNode[this.axis0];
                pos.y = currentNode[this.axis1];

                if (math.distancesq(this.planePos, pos) <= this.radiusSquared)
                {
                    this.result.Add(currentNode);
                }

                int left = currentNodeIdx * 2 + 1;
                int right = currentNodeIdx * 2 + 2;

                bool leftValid = left < this.nodes.Length;
                bool rightValid = right < this.nodes.Length;

                if (leftValid || rightValid)
                {
                    float splitPlane = currentNode[axis];

                    float xMax0 = xMax;
                    float yMax0 = yMax;

                    float xMin1 = xMin;
                    float yMin1 = yMin;

                    int nextAxis;
                    if (axis == this.axis0)
                    {
                        xMax0 = splitPlane;
                        xMin1 = splitPlane;
                        nextAxis = this.axis1;
                    }
                    else
                    {
                        yMax0 = splitPlane;
                        yMin1 = splitPlane;
                        nextAxis = this.axis0;
                    }

                    var bounds0 = Rect.MinMaxRect(xMin, yMin, xMax0, yMax0);
                    var bounds1 = Rect.MinMaxRect(xMin1, yMin1, xMax, yMax);

                    if (leftValid)
                    {
                        //Min and max inside the circle -> rectangle is completely contained inside the circle
                        if (ShapeOverlap.CircleContainsRectangle(this.planePos, this.radiusSquared, bounds0))
                        {
                            this.AddSubtree(left);
                        }
                        else if (ShapeOverlap.RectangleCircleOverlap(bounds0, this.planePos, this.radiusSquared))
                        {
                            this.SearchKDTreeRecursion(left, xMin, xMax0, yMin, yMax0, nextAxis);
                        }
                    }

                    if (rightValid)
                    {
                        //Min and max inside the circle -> rectangle is completely contained inside the circle
                        if (ShapeOverlap.CircleContainsRectangle(this.planePos, this.radiusSquared, bounds1))
                        {
                            this.AddSubtree(right);
                        }
                        else if (ShapeOverlap.RectangleCircleOverlap(bounds1, this.planePos, this.radiusSquared))
                        {
                            this.SearchKDTreeRecursion(right, xMin1, xMax, yMin1, yMax, nextAxis);
                        }
                    }

                }
            }

            public void Execute()
            {
                float xMin, xMax, yMin, yMax;

                xMin = this.kdTreeBounds.xMin;
                xMax = this.kdTreeBounds.xMax;
                yMin = this.kdTreeBounds.yMin;
                yMax = this.kdTreeBounds.yMax;

                this.radiusSquared = this.radius * this.radius;

                this.planePos = new float2();
                this.planePos.x = this.position[this.axis0];
                this.planePos.y = this.position[this.axis1];

                this.SearchKDTreeRecursion(0, xMin, xMax, yMin, yMax, this.axis0);
            }
        }

        [BurstCompile]
        private struct GetPointsInRectangleJob : IJob
        {

            public int axis0;
            public int axis1;

            public Rect searchRect;
            public Rect kdTreeBounds;

            [NoAlias, ReadOnly]
            public NativeArray<float3> nodes;

            [NoAlias, WriteOnly]
            public NativeList<float3> result;


            private void AddSubtree(int nodeIdx)
            {

                this.result.Add(this.nodes[nodeIdx]);

                int left = nodeIdx * 2 + 1;
                int right = nodeIdx * 2 + 2;
                if (left < this.nodes.Length)
                {
                    this.AddSubtree(left);
                }

                if (right < this.nodes.Length)
                {
                    this.AddSubtree(right);
                }
            }

            private void SearchKDTreeRecursion(int currentNodeIdx, float xMin, float xMax, float yMin, float yMax, int axis)
            {

                var position = new float3();
                var currentNode = this.nodes[currentNodeIdx];
                position.x = currentNode[this.axis0];
                position.y = currentNode[this.axis1];

                if (this.searchRect.Contains(position))
                {
                    this.result.Add(currentNode);
                }

                int left = currentNodeIdx * 2 + 1;
                int right = currentNodeIdx * 2 + 2;

                bool leftValid = left < this.nodes.Length;
                bool rightValid = right < this.nodes.Length;

                if (leftValid || rightValid)
                {
                    float splitPlane = currentNode[axis];

                    float xMax0 = xMax;
                    float yMax0 = yMax;

                    float xMin1 = xMin;
                    float yMin1 = yMin;

                    int nextAxis;
                    if (axis == this.axis0)
                    {
                        xMax0 = splitPlane;
                        xMin1 = splitPlane;
                        nextAxis = this.axis1;
                    }
                    else
                    {
                        yMax0 = splitPlane;
                        yMin1 = splitPlane;
                        nextAxis = this.axis0;
                    }

                    var bounds0 = Rect.MinMaxRect(xMin, yMin, xMax0, yMax0);
                    var bounds1 = Rect.MinMaxRect(xMin1, yMin1, xMax, yMax);

                    if (leftValid)
                    {
                        if (this.searchRect.Contains(bounds0))
                        {
                            this.AddSubtree(left);
                        }
                        else if (this.searchRect.Overlaps(bounds0))
                        {
                            this.SearchKDTreeRecursion(left, xMin, xMax0, yMin, yMax0, nextAxis);
                        }
                    }

                    if (rightValid)
                    {
                        if (this.searchRect.Contains(bounds1))
                        {
                            this.AddSubtree(right);
                        }
                        else if (this.searchRect.Overlaps(bounds1))
                        {
                            this.SearchKDTreeRecursion(right, xMin1, xMax, yMin1, yMax, nextAxis);
                        }
                    }
                    
                }
            }


            public void Execute()
            {
                float xMin, xMax, yMin, yMax;

                xMin = this.kdTreeBounds.xMin;
                xMax = this.kdTreeBounds.xMax;
                yMin = this.kdTreeBounds.yMin;
                yMax = this.kdTreeBounds.yMax;

                this.SearchKDTreeRecursion(0, xMin, xMax, yMin, yMax, this.axis0);
            }
        }

        //This is the point where you'd wish default implementations were a thing in earlier versions uf Unity as well...
        //TODO: Replace methods with IKDTreePointsJob-Interface default implementation in 2-3 years
        [BurstCompile]
        private struct GetPointsInRadiiJob : IJobParallelFor
        {
            public int axis0;
            public int axis1;

            public Rect kdTreeBounds;

            [NoAlias, ReadOnly]
            public NativeArray<float> radii;

            [NoAlias, ReadOnly]
            public NativeArray<float3> positions;

            [NoAlias, ReadOnly]
            public NativeArray<float3> nodes;

            [NoAlias, WriteOnly]
            public NativeParallelHashSet<float3>.ParallelWriter result;

            private float radiusSquared;

            private float3 planePos;

            private void AddSubtree(int nodeIdx)
            {

                this.result.Add(this.nodes[nodeIdx]);

                int left = nodeIdx * 2 + 1;
                int right = nodeIdx * 2 + 2;
                if (left < this.nodes.Length)
                {
                    this.AddSubtree(left);
                }

                if (right < this.nodes.Length)
                {
                    this.AddSubtree(right);
                }
            }

            private void SearchKDTreeRecursion(int currentNodeIdx, float xMin, float xMax, float yMin, float yMax, int axis)
            {

                var position = new float3();
                var currentNode = this.nodes[currentNodeIdx];
                position.x = currentNode[this.axis0];
                position.y = currentNode[this.axis1];

                if (math.distancesq(this.planePos, position) <= this.radiusSquared)
                {
                    this.result.Add(currentNode);
                }

                int left = currentNodeIdx * 2 + 1;
                int right = currentNodeIdx * 2 + 2;

                bool leftValid = left < this.nodes.Length;
                bool rightValid = right < this.nodes.Length;
                if (leftValid || rightValid)
                {
                    float splitPlane = currentNode[axis];

                    float xMax0 = xMax;
                    float yMax0 = yMax;

                    float xMin1 = xMin;
                    float yMin1 = yMin;

                    int nextAxis;
                    if (axis == this.axis0)
                    {
                        xMax0 = splitPlane;
                        xMin1 = splitPlane;
                        nextAxis = this.axis1;
                    }
                    else
                    {
                        yMax0 = splitPlane;
                        yMin1 = splitPlane;
                        nextAxis = this.axis0;
                    }

                    var bounds0 = Rect.MinMaxRect(xMin, yMin, xMax0, yMax0);
                    var bounds1 = Rect.MinMaxRect(xMin1, yMin1, xMax, yMax);

                    float2 circleCenter = this.planePos.xy;

                    if (leftValid)
                    {
                        //Min and max inside the circle -> rectangle is completely contained inside the circle
                        if (ShapeOverlap.CircleContainsRectangle(circleCenter, this.radiusSquared, bounds0))
                        {
                            this.AddSubtree(left);
                        }
                        else if (ShapeOverlap.RectangleCircleOverlap(bounds0, circleCenter, this.radiusSquared))
                        {
                            this.SearchKDTreeRecursion(left, xMin, xMax0, yMin, yMax0, nextAxis);
                        }
                    }

                    if (rightValid)
                    {
                        //Min and max inside the circle -> rectangle is completely contained inside the circle
                        if (ShapeOverlap.CircleContainsRectangle(circleCenter, this.radiusSquared, bounds1))
                        {
                            this.AddSubtree(right);
                        }
                        else if (ShapeOverlap.RectangleCircleOverlap(bounds1, circleCenter, this.radiusSquared))
                        {
                            this.SearchKDTreeRecursion(right, xMin1, xMax, yMin1, yMax, nextAxis);
                        }
                    }

                }
            }

            public void Execute(int index)
            {
                float xMin, xMax, yMin, yMax;

                xMin = this.kdTreeBounds.xMin;
                xMax = this.kdTreeBounds.xMax;
                yMin = this.kdTreeBounds.yMin;
                yMax = this.kdTreeBounds.yMax;

                this.radiusSquared = this.radii[index] * this.radii[index];

                this.planePos = new Vector3();
                this.planePos.x = this.positions[index][this.axis0];
                this.planePos.y = this.positions[index][this.axis1];

                this.SearchKDTreeRecursion(0, xMin, xMax, yMin, yMax, this.axis0);
            }
        }


        //TODO: Replace methods with IKDTreePointsJob-Interface default implementation in 2-3 years
        [BurstCompile]
        private struct GetPointsInRectanglesJob : IJobParallelFor
        {

            public int axis0;
            public int axis1;

            public Rect kdTreeBounds;

            [NoAlias, ReadOnly]
            public NativeArray<Rect> searchRectangles;

            [NoAlias, ReadOnly]
            public NativeArray<float3> nodes;

            [NoAlias, WriteOnly]
            public NativeParallelHashSet<float3>.ParallelWriter result;

            private Rect searchRect;

            private void AddSubtree(int nodeIdx)
            {

                this.result.Add(this.nodes[nodeIdx]);

                int left = nodeIdx * 2 + 1;
                int right = nodeIdx * 2 + 2;
                if (left < this.nodes.Length)
                {
                    this.AddSubtree(left);
                }

                if (right < this.nodes.Length)
                {
                    this.AddSubtree(right);
                }
            }

            private void SearchKDTreeRecursion(int currentNodeIdx, float xMin, float xMax, float yMin, float yMax, int axis)
            {

                var position = new float3();
                var currentNode = this.nodes[currentNodeIdx];
                position.x = currentNode[this.axis0];
                position.y = currentNode[this.axis1];

                if (this.searchRect.Contains(position))
                {
                    this.result.Add(currentNode);
                }

                int left = currentNodeIdx * 2 + 1;
                int right = currentNodeIdx * 2 + 2;


                bool leftValid = left < this.nodes.Length;
                bool rightValid = right < this.nodes.Length;

                if (leftValid || rightValid)
                {
                    float splitPlane = currentNode[axis];

                    float xMax0 = xMax;
                    float yMax0 = yMax;

                    float xMin1 = xMin;
                    float yMin1 = yMin;

                    int nextAxis;
                    if (axis == this.axis0)
                    {
                        xMax0 = splitPlane;
                        xMin1 = splitPlane;
                        nextAxis = this.axis1;
                    }
                    else
                    {
                        yMax0 = splitPlane;
                        yMin1 = splitPlane;
                        nextAxis = this.axis0;
                    }

                    var bounds0 = Rect.MinMaxRect(xMin, yMin, xMax0, yMax0);
                    var bounds1 = Rect.MinMaxRect(xMin1, yMin1, xMax, yMax);

                    if (leftValid)
                    {
                        if (this.searchRect.Contains(bounds0))
                        {
                            this.AddSubtree(left);
                        }
                        else if (this.searchRect.Overlaps(bounds0))
                        {
                            this.SearchKDTreeRecursion(left, xMin, xMax0, yMin, yMax0, nextAxis);
                        }
                    }

                    if (rightValid)
                    {
                        if (this.searchRect.Contains(bounds1))
                        {
                            this.AddSubtree(right);
                        }
                        else if (this.searchRect.Overlaps(bounds1))
                        {
                            this.SearchKDTreeRecursion(right, xMin1, xMax, yMin1, yMax, nextAxis);
                        }
                    }

                }
            }


            public void Execute(int index)
            {
                float xMin, xMax, yMin, yMax;

                xMin = this.kdTreeBounds.xMin;
                xMax = this.kdTreeBounds.xMax;
                yMin = this.kdTreeBounds.yMin;
                yMax = this.kdTreeBounds.yMax;

                this.searchRect = this.searchRectangles[index];

                this.SearchKDTreeRecursion(0, xMin, xMax, yMin, yMax, this.axis0);
            }
        }


        [BurstCompile]
        private struct GetPointsInPolygonJob : IJob
        {

            public Matrix4x4 trs;

            public int axis0;
            public int axis1;

            public Rect kdTreeBounds;

            [NoAlias, ReadOnly]
            public NativeArray<float3> nodes;

            [NoAlias, WriteOnly]
            public NativeList<float3> result;

            [NoAlias, ReadOnly]
            public NativePolygon2D polygon;

            private NativePolygon2D offsetedPolygon;

            private Rect searchRect;

            private void AddSubtree(int node, NativeArray<int> offsets)
            {
                var pos = new float2();
                var nodePos = this.nodes[node];
                pos.x = nodePos[this.axis0];
                pos.y = nodePos[this.axis1];

                if(this.offsetedPolygon.IsPointInsideInternal(pos, offsets))
                {
                    this.result.Add(nodePos);
                }

                int left = node * 2 + 1;
                int right = node * 2 + 2;
                if (left < this.nodes.Length)
                {
                    this.AddSubtree(left, offsets);
                }

                if (right < this.nodes.Length)
                {
                    this.AddSubtree(right, offsets);
                }
            }

            private void SearchKDTreeRecursion(int currentNodeIdx, float xMin, float xMax, float yMin, float yMax, int axis, NativeArray<int> offsets)
            {

                var position = new float3();
                var currentNode = this.nodes[currentNodeIdx];
                position.x = currentNode[this.axis0];
                position.y = currentNode[this.axis1];

                if (this.searchRect.Contains(position)
                    && this.offsetedPolygon.IsPointInsideInternal(position.xy, offsets))
                {
                    this.result.Add(currentNode);
                }

                int left = currentNodeIdx * 2 + 1;
                int right = currentNodeIdx * 2 + 2;

                bool leftValid = left < this.nodes.Length;
                bool rightValid = right < this.nodes.Length;

                if (leftValid || rightValid)
                {
                    float splitPlane = currentNode[axis];

                    float xMax0 = xMax;
                    float yMax0 = yMax;

                    float xMin1 = xMin;
                    float yMin1 = yMin;

                    int nextAxis;
                    if (axis == this.axis0)
                    {
                        xMax0 = splitPlane;
                        xMin1 = splitPlane;
                        nextAxis = this.axis1;
                    }
                    else
                    {
                        yMax0 = splitPlane;
                        yMin1 = splitPlane;
                        nextAxis = this.axis0;
                    }

                    var bounds0 = Rect.MinMaxRect(xMin, yMin, xMax0, yMax0);
                    var bounds1 = Rect.MinMaxRect(xMin1, yMin1, xMax, yMax);

                    if (leftValid)
                    {
                        if (this.searchRect.Contains(bounds0))
                        {
                            this.AddSubtree(left, offsets);
                        }
                        else if (this.searchRect.Overlaps(bounds0))
                        {
                            this.SearchKDTreeRecursion(left, xMin, xMax0, yMin, yMax0, nextAxis, offsets);
                        }
                    }

                    if (rightValid)
                    {
                        if (this.searchRect.Contains(bounds1))
                        {
                            this.AddSubtree(right, offsets);
                        }
                        else if (this.searchRect.Overlaps(bounds1))
                        {
                            this.SearchKDTreeRecursion(right, xMin1, xMax, yMin1, yMax, nextAxis, offsets);
                        }
                    }

                }
            }


            public void Execute()
            {
                this.offsetedPolygon = new NativePolygon2D(Allocator.Temp, this.polygon.points, this.polygon.separators);
                for(int i = 0; i < this.polygon.points.Length; i++)
                {
                    Vector3 pos3D = new Vector3();
                    pos3D[this.axis0] = this.offsetedPolygon.points[i].x;
                    pos3D[this.axis1] = this.offsetedPolygon.points[i].y;
                    var transformedPos = this.trs.MultiplyPoint(pos3D);

                    float2 point = new float2();
                    point.x = transformedPos[this.axis0];
                    point.y = transformedPos[this.axis1];

                    this.offsetedPolygon.points[i] = point;
                }

                this.searchRect = this.offsetedPolygon.GetBoundingRect();
                var offsets = this.offsetedPolygon.PrepareOffsets(Allocator.Temp);

                float xMin, xMax, yMin, yMax;

                xMin = this.kdTreeBounds.xMin;
                xMax = this.kdTreeBounds.xMax;
                yMin = this.kdTreeBounds.yMin;
                yMax = this.kdTreeBounds.yMax;

                this.SearchKDTreeRecursion(0, xMin, xMax, yMin, yMax, this.axis0, offsets);
            }
        }

        [BurstCompile]
        public struct GetNearestNeighborJob : IJob
        {

            public int axis0;
            public int axis1;


            [NoAlias, ReadOnly]
            public NativeArray<float3> nodes;

            [NoAlias, ReadOnly]
            public NativeArray<float3> queryPoints;

            [NoAlias, WriteOnly]
            public NativeArray<float3> result;


            private int GetClosest(int currentNodeIdx, float2 planePos, int axis)
            {
                var pos = new float2();
                var currentNode = this.nodes[currentNodeIdx];
                pos.x = currentNode[this.axis0];
                pos.y = currentNode[this.axis1];

                int left = currentNodeIdx * 2 + 1;
                int right = currentNodeIdx * 2 + 2;

                bool leftValid = left < this.nodes.Length;
                bool rightValid = right < this.nodes.Length;

                if (leftValid || rightValid)
                {
                    float splitPlane = currentNode[axis];

                    int nextAxis;
                    int cmpAxis;
                    if (axis == this.axis0)
                    {
                        nextAxis = this.axis1;
                        cmpAxis = 0;
                    }
                    else
                    {
                        nextAxis = this.axis0;
                        cmpAxis = 1;
                    }

                    if (leftValid && planePos[cmpAxis] < splitPlane)
                    {
                        int leftClosest = this.GetClosest(left, planePos, nextAxis);
                        float3 leftPos = this.nodes[leftClosest];
                        float2 leftPlanePos = new float2();
                        leftPlanePos.x = leftPos[this.axis0];
                        leftPlanePos.y = leftPos[this.axis1];

                        float dist = math.distance(planePos, pos);
                        float bestDist = math.distance(planePos, leftPlanePos);

                        if (dist < bestDist)
                        {
                            leftClosest = currentNodeIdx;
                            bestDist = dist;
                        }

                        if (rightValid && planePos[cmpAxis] + bestDist > splitPlane)
                        {
                            int rightClosest = this.GetClosest(right, planePos, nextAxis);
                            float3 rightPos = this.nodes[rightClosest];
                            float2 rightPlanePos = new float2();
                            rightPlanePos.x = rightPos[this.axis0];
                            rightPlanePos.y = rightPos[this.axis1];

                            float rightDist = math.distance(planePos, rightPlanePos);
                            if (rightDist < bestDist)
                            {
                                leftClosest = rightClosest;
                            }
                        }

                        return leftClosest;

                    }
                    else if(rightValid)
                    {
                        int rightClosest = this.GetClosest(right, planePos, nextAxis);
                        float3 rightPos = this.nodes[rightClosest];
                        float2 rightPlanePos = new float2();
                        rightPlanePos.x = rightPos[this.axis0];
                        rightPlanePos.y = rightPos[this.axis1];

                        float dist = math.distance(planePos, pos);
                        float bestDist = math.distance(planePos, rightPlanePos);
 
                        if (dist < bestDist)
                        {
                            rightClosest = currentNodeIdx;
                            bestDist = dist;
                        }

                        if (leftValid && planePos[cmpAxis] - bestDist < splitPlane)
                        {
                            int leftClosest = this.GetClosest(left, planePos, nextAxis);
                            float3 leftPos = this.nodes[leftClosest];
                            float2 leftPlanePos = new float2();
                            leftPlanePos.x = leftPos[this.axis0];
                            leftPlanePos.y = leftPos[this.axis1];

                            float leftDist = math.distance(planePos, leftPlanePos);
                            if (leftDist < bestDist)
                            {
                                rightClosest = leftClosest;
                            }
                        }
                        return rightClosest;
                    }

                }

                return currentNodeIdx;
                
            }


            public void Execute()
            {

                var planePos = new float2();

                for (int i = 0; i < this.queryPoints.Length; i++)
                {
                    var position = this.queryPoints[i];
                    planePos.x = position[this.axis0];
                    planePos.y = position[this.axis1];

                    int closest = this.GetClosest(0, planePos, this.axis0);
                    this.result[i] = this.nodes[closest];
                }
            }

        }


        public JobHandle GetNearestNeighbors(NativeArray<float3> queryPoints, ref NativeArray<float3> nearestNeighbors, JobHandle dependsOn = default)
        {
            var nearestNeighborJob = new GetNearestNeighborJob()
            {
                axis0 = this.axis0,
                axis1 = this.axis1,
                nodes = this.nodes,
                queryPoints = queryPoints,
                result = nearestNeighbors,
            };

            return nearestNeighborJob.Schedule(dependsOn);
        }


        public JobHandle GetPointsInRectangle(Rect rect, ref NativeList<float3> result, JobHandle dependsOn = default)
        {

            var pointsInRectangleJob = new GetPointsInRectangleJob()
            {
                nodes = this.nodes,
                searchRect = rect,
                result = result,
                axis0 = this.axis0,
                axis1 = this.axis1,
                kdTreeBounds = this.bounds
            };

            return pointsInRectangleJob.Schedule(dependsOn);
        }

        public JobHandle GetPointsInRectangles(NativeArray<Rect> rectangles, ref NativeParallelHashSet<float3> result, 
            JobHandle dependsOn = default, int innerBatchLoopCount = 1)
        {
            var pointsInRectanglesJob = new GetPointsInRectanglesJob()
            {
                nodes = this.nodes,
                searchRectangles = rectangles,
                result = result.AsParallelWriter(),
                axis0 = this.axis0,
                axis1 = this.axis1,
                kdTreeBounds = this.bounds
            };

            return pointsInRectanglesJob.Schedule(rectangles.Length, innerBatchLoopCount, dependsOn);
        }

        public JobHandle GetPointsInRadius(float3 center, float radius, ref NativeList<float3> result, JobHandle dependsOn = default)
        {
            var pointsInRadiusJob = new GetPointsInRadiusJob()
            {
                nodes = this.nodes,
                axis0 = this.axis0,
                axis1 = this.axis1,
                position = center,
                radius = radius,
                result = result,
                kdTreeBounds = this.bounds
            };

            return pointsInRadiusJob.Schedule(dependsOn);
        }

        public JobHandle GetPointsInRadii(NativeArray<float3> centers, NativeArray<float> radii, ref NativeParallelHashSet<float3> result,
            JobHandle dependsOn = default, int innerBatchLoopCount = 1)
        {

            if(centers.Length != radii.Length)
            {
                Debug.LogError("Centers and Radii arrays do not match in size!");
            }

            var pointsInRadiiJob = new GetPointsInRadiiJob()
            {
                axis0 = this.axis0,
                axis1 = this.axis1,
                kdTreeBounds = this.bounds,
                nodes = this.nodes,
                positions = centers,
                radii = radii,
                result = result.AsParallelWriter()
            };

            return pointsInRadiiJob.Schedule(centers.Length, innerBatchLoopCount, dependsOn);
        }

        public JobHandle GetPointsInPolygon(NativePolygon2D polygon, Matrix4x4 trs, ref NativeList<float3> result,
            JobHandle dependsOn = default)
        {
            var pointsInPolygonJob = new GetPointsInPolygonJob()
            {
                axis0 = this.axis0,
                axis1 = this.axis1,
                kdTreeBounds = this.bounds,
                nodes = this.nodes,
                result = result,
                polygon = polygon,
                trs = trs,
            };
            return pointsInPolygonJob.Schedule(dependsOn);
        }




        public bool IsCreated => this.nodes.IsCreated;

        public void Dispose()
        {
            if (this.nodes.IsCreated)
            {
                this.nodes.Dispose();
            }
        }
    }
}
