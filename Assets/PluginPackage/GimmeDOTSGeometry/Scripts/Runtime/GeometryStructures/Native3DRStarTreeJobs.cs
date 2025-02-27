using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GimmeDOTSGeometry
{
    public unsafe partial struct Native3DRStarTree<T> : IDisposable
        where T : unmanaged, IBoundingBox, IIdentifiable, IEquatable<T>
    {

        [BurstCompile]
        private struct GetBoundsInBoundsJob : IJob
        {

            public Bounds searchBounds;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode3D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeList<T> result;


            private void AddSubtree(RStarNode3D node)
            {
                if (node.children >= 0)
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        this.result.Add(this.data[childIdx]);
                    }
                    return;
                }

                var leftNode = this.nodes[node.left];
                var rightNode = this.nodes[node.right];

                this.AddSubtree(leftNode);
                this.AddSubtree(rightNode);
            }

            private void SearchRTreeRecursion(RStarNode3D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (this.searchBounds.Contains(leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (this.searchBounds.Overlaps(leftNode.Bounds))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (this.searchBounds.Contains(rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (this.searchBounds.Overlaps(rightNode.Bounds))
                    {
                        this.SearchRTreeRecursion(rightNode);
                    }
                }
                else
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        var child = this.data[childIdx];

                        if (this.searchBounds.Contains(child.Bounds))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute()
            {
                this.result.Clear();

                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }


        [BurstCompile]
        private struct GetOverlappingBoundsInBoundsJob : IJob
        {

            public Bounds searchBounds;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode3D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeList<T> result;


            private void AddSubtree(RStarNode3D node)
            {
                if (node.children >= 0)
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        this.result.Add(this.data[childIdx]);
                    }
                    return;
                }

                var leftNode = this.nodes[node.left];
                var rightNode = this.nodes[node.right];

                this.AddSubtree(leftNode);
                this.AddSubtree(rightNode);
            }

            private void SearchRTreeRecursion(RStarNode3D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (this.searchBounds.Contains(leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (this.searchBounds.Overlaps(leftNode.Bounds))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (this.searchBounds.Contains(rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (this.searchBounds.Overlaps(rightNode.Bounds))
                    {
                        this.SearchRTreeRecursion(rightNode);
                    }
                }
                else
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        var child = this.data[childIdx];

                        if (this.searchBounds.Overlaps(child.Bounds))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute()
            {
                this.result.Clear();

                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }

        [BurstCompile]
        private struct GetBoundsInMultipleBoundsJob : IJobParallelFor
        {
            [NoAlias, ReadOnly]
            public NativeArray<Bounds> searchBoundaries;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode3D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeParallelHashSet<T>.ParallelWriter result;

            private Bounds searchBounds;

            private void AddSubtree(RStarNode3D node)
            {
                if (node.children >= 0)
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        this.result.Add(this.data[childIdx]);
                    }
                    return;
                }

                var leftNode = this.nodes[node.left];
                var rightNode = this.nodes[node.right];

                this.AddSubtree(leftNode);
                this.AddSubtree(rightNode);
            }


            private void SearchRTreeRecursion(RStarNode3D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (this.searchBounds.Contains(leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (this.searchBounds.Overlaps(leftNode.Bounds))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (this.searchBounds.Contains(rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (this.searchBounds.Overlaps(rightNode.Bounds))
                    {
                        this.SearchRTreeRecursion(rightNode);
                    }
                }
                else
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        var child = this.data[childIdx];

                        if (this.searchBounds.Contains(child.Bounds))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute(int index)
            {
                this.searchBounds = this.searchBoundaries[index];
                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }



        [BurstCompile]
        private struct GetOverlappingBoundsInMultipleBoundsJob : IJobParallelFor
        {
            [NoAlias, ReadOnly]
            public NativeArray<Bounds> searchBoundaries;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode3D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeParallelHashSet<T>.ParallelWriter result;

            private Bounds searchBounds;

            private void AddSubtree(RStarNode3D node)
            {
                if (node.children >= 0)
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        this.result.Add(this.data[childIdx]);
                    }
                    return;
                }

                var leftNode = this.nodes[node.left];
                var rightNode = this.nodes[node.right];

                this.AddSubtree(leftNode);
                this.AddSubtree(rightNode);
            }


            private void SearchRTreeRecursion(RStarNode3D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (this.searchBounds.Contains(leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (this.searchBounds.Overlaps(leftNode.Bounds))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (this.searchBounds.Contains(rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (this.searchBounds.Overlaps(rightNode.Bounds))
                    {
                        this.SearchRTreeRecursion(rightNode);
                    }
                }
                else
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        var child = this.data[childIdx];

                        if (this.searchBounds.Overlaps(child.Bounds))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute(int index)
            {
                this.searchBounds = this.searchBoundaries[index];
                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }


        [BurstCompile]
        private struct GetBoundsInRadiusJob : IJob
        {

            public float radius;
            public float3 center;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode3D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeList<T> result;

            private float radiusSq;

            private void AddSubtree(RStarNode3D node)
            {
                if (node.children >= 0)
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        this.result.Add(this.data[childIdx]);
                    }
                    return;
                }

                var leftNode = this.nodes[node.left];
                var rightNode = this.nodes[node.right];

                this.AddSubtree(leftNode);
                this.AddSubtree(rightNode);
            }

            private void SearchRTreeRecursion(RStarNode3D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (ShapeOverlap.SphereContainsCuboid(this.center, this.radiusSq, leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (ShapeOverlap.CuboidSphereOverlap(leftNode.Bounds, this.center, this.radiusSq))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (ShapeOverlap.SphereContainsCuboid(this.center, this.radiusSq, rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (ShapeOverlap.CuboidSphereOverlap(rightNode.Bounds, this.center, this.radiusSq))
                    {
                        this.SearchRTreeRecursion(rightNode);
                    }
                }
                else
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        var child = this.data[childIdx];

                        if (ShapeOverlap.SphereContainsCuboid(this.center, this.radiusSq, child.Bounds))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute()
            {
                this.result.Clear();

                this.radiusSq = this.radius * this.radius;

                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }

        [BurstCompile]
        private struct GetBoundsInRadiiJob : IJobParallelFor
        {
            [ReadOnly, NoAlias]
            public NativeArray<float> radii;

            [ReadOnly, NoAlias]
            public NativeArray<float3> centers;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode3D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeParallelHashSet<T>.ParallelWriter result;


            private float radiusSq;
            private float3 center;


            private void AddSubtree(RStarNode3D node)
            {
                if (node.children >= 0)
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        this.result.Add(this.data[childIdx]);
                    }
                    return;
                }

                var leftNode = this.nodes[node.left];
                var rightNode = this.nodes[node.right];

                this.AddSubtree(leftNode);
                this.AddSubtree(rightNode);
            }

            private void SearchRTreeRecursion(RStarNode3D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (ShapeOverlap.SphereContainsCuboid(this.center, this.radiusSq, leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (ShapeOverlap.CuboidSphereOverlap(leftNode.Bounds, this.center, this.radiusSq))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (ShapeOverlap.SphereContainsCuboid(this.center, this.radiusSq, rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (ShapeOverlap.CuboidSphereOverlap(rightNode.Bounds, this.center, this.radiusSq))
                    {
                        this.SearchRTreeRecursion(rightNode);
                    }
                }
                else
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        var child = this.data[childIdx];

                        if (ShapeOverlap.SphereContainsCuboid(this.center, this.radiusSq, child.Bounds))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute(int index)
            {
                float radius = this.radii[index];

                this.radiusSq = radius * radius;
                this.center = this.centers[index];

                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }

        [BurstCompile]
        private struct GetOverlappingBoundsInRadiusJob : IJob
        {

            public float radius;
            public float3 center;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode3D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeList<T> result;

            private float radiusSq;

            private void AddSubtree(RStarNode3D node)
            {
                if (node.children >= 0)
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        this.result.Add(this.data[childIdx]);
                    }
                    return;
                }

                var leftNode = this.nodes[node.left];
                var rightNode = this.nodes[node.right];

                this.AddSubtree(leftNode);
                this.AddSubtree(rightNode);
            }

            private void SearchRTreeRecursion(RStarNode3D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (ShapeOverlap.SphereContainsCuboid(this.center, this.radiusSq, leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (ShapeOverlap.CuboidSphereOverlap(leftNode.Bounds, this.center, this.radiusSq))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (ShapeOverlap.SphereContainsCuboid(this.center, this.radiusSq, rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (ShapeOverlap.CuboidSphereOverlap(rightNode.Bounds, this.center, this.radiusSq))
                    {
                        this.SearchRTreeRecursion(rightNode);
                    }
                }
                else
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        var child = this.data[childIdx];

                        if (ShapeOverlap.CuboidSphereOverlap(child.Bounds, this.center, this.radiusSq))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute()
            {
                this.result.Clear();

                this.radiusSq = this.radius * this.radius;

                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }



        [BurstCompile]
        private struct GetOverlappingBoundsInRadiiJob : IJobParallelFor
        {
            [ReadOnly, NoAlias]
            public NativeArray<float> radii;

            [ReadOnly, NoAlias]
            public NativeArray<float3> centers;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode3D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeParallelHashSet<T>.ParallelWriter result;


            private float radiusSq;
            private float3 center;


            private void AddSubtree(RStarNode3D node)
            {
                if (node.children >= 0)
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        this.result.Add(this.data[childIdx]);
                    }
                    return;
                }

                var leftNode = this.nodes[node.left];
                var rightNode = this.nodes[node.right];

                this.AddSubtree(leftNode);
                this.AddSubtree(rightNode);
            }

            private void SearchRTreeRecursion(RStarNode3D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (ShapeOverlap.SphereContainsCuboid(this.center, this.radiusSq, leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (ShapeOverlap.CuboidSphereOverlap(leftNode.Bounds, this.center, this.radiusSq))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (ShapeOverlap.SphereContainsCuboid(this.center, this.radiusSq, rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (ShapeOverlap.CuboidSphereOverlap(rightNode.Bounds, this.center, this.radiusSq))
                    {
                        this.SearchRTreeRecursion(rightNode);
                    }
                }
                else
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        var child = this.data[childIdx];

                        if (ShapeOverlap.CuboidSphereOverlap(child.Bounds, this.center, this.radiusSq))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute(int index)
            {
                float radius = this.radii[index];

                this.radiusSq = radius * radius;
                this.center = this.centers[index];

                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }


        [BurstCompile]
        private struct RaycastJob : IJob
        {
            public float distance;

            public int root;

            public Ray ray;

            //You can define your own comparers to sort the points in reverse order for example
            //However, the majority of people will want to have them sorted by increasing distance
            public IntersectionHit3D<T>.RayComparer comparer;

            [NoAlias]
            public NativeList<IntersectionHit3D<T>> result;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode3D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            private LineSegment3D lineSegment;

            private void IntersectRecursively(RStarNode3D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (ShapeOverlap.LineSegmentCuboidOverlap(this.lineSegment, leftNode.Bounds))
                    {
                        this.IntersectRecursively(leftNode);
                    }

                    if (ShapeOverlap.LineSegmentCuboidOverlap(this.lineSegment, rightNode.Bounds))
                    {
                        this.IntersectRecursively(rightNode);
                    }

                }
                else
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        var child = this.data[childIdx];

                        int intersections = ShapeIntersection.LineSegmentCuboidIntersections(this.lineSegment, child.Bounds,
                            out float3 intersection0, out float3 intersection1, out _);

                        if (intersections > 0)
                        {
                            var hitPoints = new FixedList32Bytes<float3>
                            {
                                intersection0
                            };
                            if (intersections > 1) hitPoints.Add(intersection1);

                            var intersectionHit = new IntersectionHit3D<T>()
                            {
                                boundingVolume = child,
                                hitPoints = hitPoints
                            };

                            this.result.Add(intersectionHit);
                        }
                    }
                }
            }

            public void Execute()
            {
                this.result.Clear();

                this.lineSegment = new LineSegment3D()
                {
                    a = this.ray.origin,
                    b = this.ray.origin + this.ray.direction.normalized * this.distance
                };

                var rootNode = this.nodes[this.root];

                this.IntersectRecursively(rootNode);
                this.result.Sort(this.comparer);
            }
        }

    
    }
}
