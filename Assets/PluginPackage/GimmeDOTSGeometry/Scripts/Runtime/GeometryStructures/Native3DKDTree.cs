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
    /// <summary>
    /// Note: Default sorting order is X, then Y, then Z.
    /// </summary>
    public unsafe struct Native3DKDTree : IDisposable
    {

        #region Private Variables

        private NativeArray<float3> nodes;

        private Bounds bounds;

        #endregion

        //Root is at the end of the nodes upon construction
        public float3* GetRoot()
        {
            if(this.nodes.IsCreated)
            {
                return (float3*)this.nodes.GetUnsafePtr();
            }
            return null;
        }

        public int Count => this.nodes.Length;


        public Bounds GetBounds() => this.bounds;

        public NativeArray<float3> GetNodes() => this.nodes;

        private void ConstructKDTreeRecursion(int index, NativeList<float3> sortedList0, NativeList<float3> sortedList1, NativeList<float3> sortedList2, int depth)
        {
            int count = sortedList0.Length;

            if (count <= 0) return;

            if(count == 1)
            {
                this.nodes[index] = sortedList0[0];
            } else
            {
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

                var comp0 = new Composite3DComparer() { axis0 = 0, axis1 = 1, axis2 = 2 };
                var comp1 = new Composite3DComparer() { axis0 = 1, axis1 = 2, axis2 = 0 };
                var comp2 = new Composite3DComparer() { axis0 = 2, axis1 = 0, axis2 = 1 };

                NativeList<float3> currentList = default;
                Composite3DComparer secondComparer = default, thirdComparer = default;

                switch (depth % 3)
                {
                    case 0:
                        currentList = sortedList0;
                        secondComparer = comp1;
                        thirdComparer = comp2;

                        break;
                    case 1:
                        currentList = sortedList1;
                        secondComparer = comp2;
                        thirdComparer = comp0;

                        break;
                    case 2:
                        currentList = sortedList2;
                        secondComparer = comp0;
                        thirdComparer = comp1;

                        break;
                }

                var currentLeft = new NativeList<float3>(medianIdx, Allocator.TempJob);
                var currentRight = new NativeList<float3>(sortedList0.Length - medianIdx - 1, Allocator.TempJob);

                var secondLeft = new NativeList<float3>(medianIdx, Allocator.TempJob);
                var secondRight = new NativeList<float3>(sortedList0.Length - medianIdx - 1, Allocator.TempJob);

                var thirdLeft = new NativeList<float3>(medianIdx, Allocator.TempJob);
                var thirdRight = new NativeList<float3>(sortedList0.Length - medianIdx - 1, Allocator.TempJob);

                float3 median = currentList[medianIdx];

                for(int i = 0; i < medianIdx; i++)
                {
                    var pos = currentList[i];
                    currentLeft.Add(pos);
                    secondLeft.Add(pos);
                    thirdLeft.Add(pos);
                }

                for(int i = medianIdx + 1; i < sortedList0.Length; i++)
                {
                    var pos = currentList[i];
                    currentRight.Add(pos);
                    secondRight.Add(pos);
                    thirdRight.Add(pos);
                }

                secondLeft.Sort(secondComparer);
                secondRight.Sort(secondComparer);

                thirdLeft.Sort(thirdComparer);
                thirdRight.Sort(thirdComparer);

                switch (depth % 3)
                {
                    case 0:
                        this.ConstructKDTreeRecursion(index * 2 + 1, currentLeft, secondLeft, thirdLeft, depth + 1);
                        this.ConstructKDTreeRecursion(index * 2 + 2, currentRight, secondRight, thirdRight, depth + 1);
                        break;
                    case 1:
                        this.ConstructKDTreeRecursion(index * 2 + 1, thirdLeft, currentLeft, secondLeft, depth + 1);
                        this.ConstructKDTreeRecursion(index * 2 + 2, thirdRight, currentRight, secondRight, depth + 1);
                        break;
                    case 2:
                        this.ConstructKDTreeRecursion(index * 2 + 1, secondLeft, thirdLeft, currentLeft, depth + 1);
                        this.ConstructKDTreeRecursion(index * 2 + 2, secondRight, thirdRight, currentRight, depth + 1);
                        break;
                }

                currentLeft.Dispose();
                currentRight.Dispose();

                secondLeft.Dispose();
                secondRight.Dispose();  
                
                thirdLeft.Dispose(); 
                thirdRight.Dispose();

                this.nodes[index] = median;

            }

        }

        private struct Composite3DComparer : IComparer<float3>
        {

            public int axis0;
            public int axis1;
            public int axis2;

            public int Compare(float3 a, float3 b)
            {
                int comp = a[this.axis0].CompareTo(b[this.axis0]);
                if (comp != 0) return comp;
                comp = a[this.axis1].CompareTo(b[this.axis1]);
                if (comp != 0) return comp;
                return a[this.axis2].CompareTo(b[this.axis2]);
            }
        }

        private void ConstructKDTree(NativeArray<float3> positions)
        {
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            for(int i = 0; i < positions.Length; i++)
            {
                var pos = positions[i];

                min = Vector3.Min(pos, min);
                max = Vector3.Max(pos, max);
            }

            this.bounds.SetMinMax(min, max);

            NativeList<float3> sortedList0 = new NativeList<float3>(positions.Length, Allocator.TempJob);
            NativeList<float3> sortedList1 = new NativeList<float3>(positions.Length, Allocator.TempJob);
            NativeList<float3> sortedList2 = new NativeList<float3>(positions.Length, Allocator.TempJob);

            sortedList0.CopyFrom(positions);
            sortedList1.CopyFrom(positions);
            sortedList2.CopyFrom(positions);

            //I believe this way of sorting may either be the most stable way or the most unstable... 
            //Either way, try not to have to many points have the same coordinates ^^
            var comp0 = new Composite3DComparer() { axis0 = 0, axis1 = 1, axis2 = 2 };
            var comp1 = new Composite3DComparer() { axis0 = 1, axis1 = 2, axis2 = 0 };
            var comp2 = new Composite3DComparer() { axis0 = 2, axis1 = 0, axis2 = 1 };

            var sortJob0 = sortedList0.SortJob(comp0);
            var sortJob1 = sortedList1.SortJob(comp1);
            var sortJob2 = sortedList2.SortJob(comp2);

            var handle0 = sortJob0.Schedule();
            var handle1 = sortJob1.Schedule();
            var handle2 = sortJob2.Schedule();

            handle0.Complete();
            handle1.Complete();
            handle2.Complete();

            this.ConstructKDTreeRecursion(0, sortedList0, sortedList1, sortedList2, 0);

            sortedList0.Dispose();
            sortedList1.Dispose();
            sortedList2.Dispose();
        }

        public Native3DKDTree(Vector3[] positions, Allocator allocator)
        {
            if(positions == null || positions.Length == 0)
            {
                Debug.LogError("Tried to construct native 3D KD tree with an empty array!");
            }

            var nativePositions = new NativeArray<float3>(positions.Length, Allocator.TempJob);
            var reinterpretation = nativePositions.Reinterpret<Vector3>();

            reinterpretation.CopyFrom(positions);

            this.nodes = new NativeArray<float3>(positions.Length, allocator);

            this.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            this.ConstructKDTree(nativePositions);

            nativePositions.Dispose();
        }

        public Native3DKDTree(List<Vector3> positions, Allocator allocator) : this(positions.ToArray(), allocator) { }

        public Native3DKDTree(NativeArray<Vector3> positions, Allocator allocator) : this(positions.Reinterpret<float3>(), allocator) { }

        public Native3DKDTree(NativeArray<float3> positions, Allocator allocator)
        {
            if(positions == null || positions.Length == 0)
            {
                Debug.LogError("Tried to construct native 3D KD tree with an empty array!");
            }

            this.nodes = new NativeArray<float3>(positions.Length, allocator);

            this.bounds = new Bounds(Vector3.zero, Vector3.positiveInfinity);

            this.ConstructKDTree(positions);
        }

        [BurstCompile]
        private struct GetPointsInBoundsJob : IJob
        {
            public Bounds searchBounds;
            public Bounds kdTreeBounds;

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

                if(right < this.nodes.Length)
                {
                    this.AddSubtree(right);
                }
            }

            private void SearchKDTreeRecursion(int currentNodeIdx, float3 min, float3 max, int depth)
            {
                var position = this.nodes[currentNodeIdx];

                if(this.searchBounds.Contains(position))
                {
                    this.result.Add(position);
                }

                int left = currentNodeIdx * 2 + 1;
                int right = currentNodeIdx * 2 + 2;

                bool leftValid = left < this.nodes.Length;
                bool rightValid = right < this.nodes.Length;

                if (leftValid || rightValid)
                {
                    int axis = depth % 3;

                    float splitPlane = position[axis];

                    float3 minLeft = min;
                    float3 maxLeft = max;
                    float3 minRight = min;
                    float3 maxRight = max;

                    maxLeft[axis] = splitPlane;
                    minRight[axis] = splitPlane;

                    var boundsLeft = new Bounds();
                    var boundsRight = new Bounds();

                    boundsLeft.SetMinMax(minLeft, maxLeft);
                    boundsRight.SetMinMax(minRight, maxRight);

                    if(leftValid)
                    {
                        if(this.searchBounds.Contains(boundsLeft))
                        {
                            this.AddSubtree(left);
                        }
                        else if(this.searchBounds.Intersects(boundsLeft))
                        {
                            this.SearchKDTreeRecursion(left, minLeft, maxLeft, depth + 1);
                        }
                    }

                    if(rightValid)
                    {
                        if(this.searchBounds.Contains(boundsRight))
                        {
                            this.AddSubtree(right);
                        }
                        else if(this.searchBounds.Intersects(boundsRight))
                        {
                            this.SearchKDTreeRecursion(right, minRight, maxRight, depth + 1);
                        }
                    }
                }
            }

            public void Execute()
            {
                float3 min = this.kdTreeBounds.min;
                float3 max = this.kdTreeBounds.max;

                this.SearchKDTreeRecursion(0, min, max, 0);
            }
        }

        //TODO: Replace methods with IKDTreePointsJob-Interface default implementation in 2-3 years
        [BurstCompile]
        private struct GetPointsInMultipleBoundsJob : IJobParallelFor
        {
            public Bounds kdTreeBounds;

            [NoAlias, ReadOnly]
            public NativeArray<Bounds> searchBounds;

            [NoAlias, ReadOnly]
            public NativeArray<float3> nodes;

            [NoAlias, WriteOnly]
            public NativeParallelHashSet<float3>.ParallelWriter result;

            private Bounds bounds;

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

            private void SearchKDTreeRecursion(int currentNodeIdx, float3 min, float3 max, int depth)
            {
                var position = this.nodes[currentNodeIdx];

                if (this.bounds.Contains(position))
                {
                    this.result.Add(position);
                }

                int left = currentNodeIdx * 2 + 1;
                int right = currentNodeIdx * 2 + 2;

                bool leftValid = left < this.nodes.Length;
                bool rightValid = right < this.nodes.Length;

                if (leftValid || rightValid)
                {
                    int axis = depth % 3;

                    float splitPlane = position[axis];

                    float3 minLeft = min;
                    float3 maxLeft = max;
                    float3 minRight = min;
                    float3 maxRight = max;

                    maxLeft[axis] = splitPlane;
                    minRight[axis] = splitPlane;

                    var boundsLeft = new Bounds();
                    var boundsRight = new Bounds();

                    boundsLeft.SetMinMax(minLeft, maxLeft);
                    boundsRight.SetMinMax(minRight, maxRight);

                    if (leftValid)
                    {
                        if (this.searchBounds.Contains(boundsLeft))
                        {
                            this.AddSubtree(left);
                        }
                        else if (this.bounds.Intersects(boundsLeft))
                        {
                            this.SearchKDTreeRecursion(left, minLeft, maxLeft, depth + 1);
                        }
                    }

                    if (rightValid)
                    {
                        if (this.searchBounds.Contains(boundsRight))
                        {
                            this.AddSubtree(right);
                        }
                        else if (this.bounds.Intersects(boundsRight))
                        {
                            this.SearchKDTreeRecursion(right, minRight, maxRight, depth + 1);
                        }
                    }
                }
            }

            public void Execute(int index)
            {
                float3 min = this.kdTreeBounds.min;
                float3 max = this.kdTreeBounds.max;

                this.bounds = this.searchBounds[index];

                this.SearchKDTreeRecursion(0, min, max, 0);
            }
        }

        [BurstCompile]
        private struct GetPointsInRadiusJob : IJob
        {
            public float radius;

            public float3 position;

            public Bounds kdTreeBounds;

            [NoAlias, ReadOnly]
            public NativeArray<float3> nodes;

            [NoAlias, WriteOnly]
            public NativeList<float3> result;


            private float radiusSquared;

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

            private void SearchKDTreeRecursion(int currentNodeIdx, float3 min, float3 max, int depth)
            {
                var position = this.nodes[currentNodeIdx];

                if (math.distancesq(this.position, position) <= this.radiusSquared)
                {
                    this.result.Add(position);
                }

                int left = currentNodeIdx * 2 + 1;
                int right = currentNodeIdx * 2 + 2;

                bool leftValid = left < this.nodes.Length;
                bool rightValid = right < this.nodes.Length;

                if (leftValid || rightValid)
                {
                    int axis = depth % 3;

                    float splitPlane = position[axis];

                    float3 maxLeft = max;
                    float3 minRight = min;

                    maxLeft[axis] = splitPlane;
                    minRight[axis] = splitPlane;

                    if (leftValid)
                    {
                        if (ShapeOverlap.SphereContainsCuboid(this.position, this.radiusSquared, min, maxLeft))
                        {
                            this.AddSubtree(left);
                        }
                        else if (ShapeOverlap.CuboidSphereOverlap(min, maxLeft, this.position, this.radiusSquared))
                        {
                            this.SearchKDTreeRecursion(left, min, maxLeft, depth + 1);
                        }
                    }

                    if (rightValid)
                    {
                        if (ShapeOverlap.SphereContainsCuboid(this.position, this.radiusSquared, minRight, max))
                        {
                            this.AddSubtree(right);
                        }
                        else if (ShapeOverlap.CuboidSphereOverlap(minRight, max, this.position, this.radiusSquared))
                        {
                            this.SearchKDTreeRecursion(right, minRight, max, depth + 1);
                        }
                    }
                }
            }

            public void Execute()
            {
                this.radiusSquared = this.radius * this.radius;
                

                float3 min = this.kdTreeBounds.min;
                float3 max = this.kdTreeBounds.max;

                this.SearchKDTreeRecursion(0, min, max, 0);
            }
        }

        //TODO: Replace methods with IKDTreePointsJob-Interface default implementation in 2-3 years
        [BurstCompile]
        private struct GetPointsInRadiiJob : IJobParallelFor
        {

            public Bounds kdTreeBounds;

            [NoAlias, ReadOnly]
            public NativeArray<float> radii;

            [NoAlias, ReadOnly]
            public NativeArray<float3> positions;


            [NoAlias, ReadOnly]
            public NativeArray<float3> nodes;

            [NoAlias, WriteOnly]
            public NativeParallelHashSet<float3>.ParallelWriter result;

            private float radiusSquared;

            private float3 circlePos;

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

            private void SearchKDTreeRecursion(int currentNodeIdx, float3 min, float3 max, int depth)
            {
                var position = this.nodes[currentNodeIdx];

                if (math.distancesq(this.circlePos, position) <= this.radiusSquared)
                {
                    this.result.Add(position);
                }

                int left = currentNodeIdx * 2 + 1;
                int right = currentNodeIdx * 2 + 2;

                bool leftValid = left < this.nodes.Length;
                bool rightValid = right < this.nodes.Length;

                if (leftValid || rightValid)
                {
                    int axis = depth % 3;

                    float splitPlane = position[axis];

                    float3 maxLeft = max;
                    float3 minRight = min;

                    maxLeft[axis] = splitPlane;
                    minRight[axis] = splitPlane;

                    if (leftValid)
                    {
                        if (ShapeOverlap.SphereContainsCuboid(this.circlePos, this.radiusSquared, min, maxLeft))
                        {
                            this.AddSubtree(left);
                        }
                        else if (ShapeOverlap.CuboidSphereOverlap(min, maxLeft, this.circlePos, this.radiusSquared))
                        {
                            this.SearchKDTreeRecursion(left, min, maxLeft, depth + 1);
                        }
                    }

                    if (rightValid)
                    {
                        if (ShapeOverlap.SphereContainsCuboid(this.circlePos, this.radiusSquared, minRight, max))
                        {
                            this.AddSubtree(right);
                        }
                        else if (ShapeOverlap.CuboidSphereOverlap(minRight, max, this.circlePos, this.radiusSquared))
                        {
                            this.SearchKDTreeRecursion(right, minRight, max, depth + 1);
                        }
                    }
                }
            }

            public void Execute(int index)
            {
                this.radiusSquared = this.radii[index] * this.radii[index];

                float3 min = this.kdTreeBounds.min;
                float3 max = this.kdTreeBounds.max;

                this.circlePos = this.positions[index];

                this.SearchKDTreeRecursion(0, min, max, 0);
            }
        }


        [BurstCompile]
        public struct GetNearestNeighborJob : IJob
        {

            [NoAlias, ReadOnly]
            public NativeArray<float3> nodes;

            [NoAlias, ReadOnly]
            public NativeArray<float3> queryPoints;

            [NoAlias, WriteOnly]
            public NativeArray<float3> result;


            private int GetClosest(int currentNodeIdx, float3 searchPos, int depth)
            {
                var position = this.nodes[currentNodeIdx];

                int left = currentNodeIdx * 2 + 1;
                int right = currentNodeIdx * 2 + 2;

                bool leftValid = left < this.nodes.Length;
                bool rightValid = right < this.nodes.Length;

                if (leftValid || rightValid)
                {
                    int axis = depth % 3;

                    float splitPlane = position[axis];

                    if (!rightValid || searchPos[axis] < splitPlane)
                    {
                        int leftClosest = this.GetClosest(left, searchPos, depth + 1);
                        float3 leftPos = this.nodes[leftClosest];

                        float dist = math.distance(searchPos, position);
                        float bestDist = math.distance(searchPos, leftPos);

                        if (dist < bestDist)
                        {
                            leftClosest = currentNodeIdx;
                            bestDist = dist;
                        }

                        if (rightValid && searchPos[axis] + bestDist > splitPlane)
                        {
                            int rightClosest = this.GetClosest(right, searchPos, depth + 1);
                            float3 rightPos = this.nodes[rightClosest];

                            float rightDist = math.distance(searchPos, rightPos);
                            if (rightDist < bestDist)
                            {
                                leftClosest = rightClosest;
                            }
                        }

                        return leftClosest;

                    }
                    else
                    {
                        int rightClosest = this.GetClosest(right, searchPos, depth + 1);
                        float3 rightPos = this.nodes[rightClosest];

                        float dist = math.distance(searchPos, position);
                        float bestDist = math.distance(searchPos, rightPos);

                        if (dist < bestDist)
                        {
                            rightClosest = currentNodeIdx;
                            bestDist = dist;
                        }

                        if (leftValid && searchPos[axis] - bestDist < splitPlane)
                        {
                            int leftClosest = this.GetClosest(left, searchPos, depth + 1);
                            float3 leftPos = this.nodes[leftClosest];

                            float leftDist = math.distance(searchPos, leftPos);
                            if (leftDist < bestDist)
                            {
                                rightClosest = leftClosest;
                            }
                        }
                        return rightClosest;
                    }

                }
                else
                {
                    return currentNodeIdx;
                }
            }


            public void Execute()
            {

                for (int i = 0; i < this.queryPoints.Length; i++)
                {
                    var searchPos = this.queryPoints[i];
                    int closest = this.GetClosest(0, searchPos, 0);
                    this.result[i] = this.nodes[closest];
                }
            }

        }

        public JobHandle GetNearestNeighbors(NativeArray<float3> queryPoints, ref NativeArray<float3> nearestNeighbors, JobHandle dependsOn = default)
        {
            var nearestNeighborJob = new GetNearestNeighborJob()
            {
                nodes = this.nodes,
                queryPoints = queryPoints,
                result = nearestNeighbors,
            };
            return nearestNeighborJob.Schedule(dependsOn);
        }

        public JobHandle GetPointsInBounds(Bounds bounds, ref NativeList<float3> result, JobHandle dependsOn = default)
        {
            var pointsInBoundsJob = new GetPointsInBoundsJob()
            {
                kdTreeBounds = this.bounds,
                nodes = this.nodes,
                result = result,
                searchBounds = bounds,
            };

            return pointsInBoundsJob.Schedule(dependsOn);
        }

        public JobHandle GetPointsInBounds(NativeArray<Bounds> bounds, ref NativeParallelHashSet<float3> result,
            JobHandle dependsOn = default, int innerBatchLoopCount = 1)
        {
            var pointsInBoundsJob = new GetPointsInMultipleBoundsJob()
            {
                kdTreeBounds = this.bounds,
                nodes = this.nodes,
                result = result.AsParallelWriter(),
                searchBounds = bounds,
            };

            return pointsInBoundsJob.Schedule(bounds.Length, innerBatchLoopCount, dependsOn);
        }

        public JobHandle GetPointsInRadius(float3 center, float radius, ref NativeList<float3> result, JobHandle dependsOn = default)
        {
            var pointsInRadiusJob = new GetPointsInRadiusJob()
            {
                kdTreeBounds = this.bounds,
                nodes = this.nodes,
                position = center,
                radius = radius,
                result = result,
            };

            return pointsInRadiusJob.Schedule(dependsOn);
        }

        public JobHandle GetPointsInRadii(NativeArray<float3> centers, NativeArray<float> radii, ref NativeParallelHashSet<float3> result,
            JobHandle dependsOn = default, int innerBatchLoopCount = 1)
        {
            if (centers.Length != radii.Length)
            {
                Debug.LogError("Centers and Radii arrays do not match in size!");
            }

            var pointsInRadiiJob = new GetPointsInRadiiJob()
            {
                kdTreeBounds = this.bounds,
                nodes = this.nodes,
                positions = centers,
                radii = radii,
                result = result.AsParallelWriter(),
            };

            return pointsInRadiiJob.Schedule(centers.Length, innerBatchLoopCount, dependsOn);
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
