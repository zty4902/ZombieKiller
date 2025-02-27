using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GimmeDOTSGeometry
{
    public unsafe partial struct Native2DRStarTree<T> : IDisposable
        where T : unmanaged, IBoundingRect, IIdentifiable, IEquatable<T>
    {

        [BurstCompile]
        private struct GetRectanglesInRectangleJob : IJob
        {
            public Rect searchRect;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode2D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeList<T> result;

            private void AddSubtree(RStarNode2D node)
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


            private void SearchRTreeRecursion(RStarNode2D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (this.searchRect.Contains(leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (this.searchRect.Overlaps(leftNode.Bounds))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (this.searchRect.Contains(rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (this.searchRect.Overlaps(rightNode.Bounds))
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

                        if (this.searchRect.Contains(child.Bounds))
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
        private struct GetOverlappingRectanglesInRectangleJob : IJob
        {
            public Rect searchRect;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode2D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeList<T> result;

            private void AddSubtree(RStarNode2D node)
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


            private void SearchRTreeRecursion(RStarNode2D node) 
            { 
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (this.searchRect.Contains(leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (this.searchRect.Overlaps(leftNode.Bounds))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (this.searchRect.Contains(rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (this.searchRect.Overlaps(rightNode.Bounds))
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

                        if (this.searchRect.Overlaps(child.Bounds))
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
        private struct GetRectanglesInRectanglesJob : IJobParallelFor
        {
            [NoAlias, ReadOnly]
            public NativeArray<Rect> searchRectangles;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode2D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeParallelHashSet<T>.ParallelWriter result;

            private Rect searchRect;

            private void AddSubtree(RStarNode2D node)
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


            private void SearchRTreeRecursion(RStarNode2D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (this.searchRect.Contains(leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (this.searchRect.Overlaps(leftNode.Bounds))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (this.searchRect.Contains(rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (this.searchRect.Overlaps(rightNode.Bounds))
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

                        if (this.searchRect.Contains(child.Bounds))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute(int index)
            {
                this.searchRect = this.searchRectangles[index];
                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }

        [BurstCompile]
        private struct GetOverlappingRectanglesInRectanglesJob : IJobParallelFor
        {
            [NoAlias, ReadOnly]
            public NativeArray<Rect> searchRectangles;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode2D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeParallelHashSet<T>.ParallelWriter result;

            private Rect searchRect;

            private void AddSubtree(RStarNode2D node)
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


            private void SearchRTreeRecursion(RStarNode2D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (this.searchRect.Contains(leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (this.searchRect.Overlaps(leftNode.Bounds))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (this.searchRect.Contains(rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (this.searchRect.Overlaps(rightNode.Bounds))
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

                        if (this.searchRect.Overlaps(child.Bounds))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute(int index)
            {
                this.searchRect = this.searchRectangles[index];
                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }

        [BurstCompile]
        private struct GetRectanglesInRadiusJob : IJob
        {
            public float radius;
            public float2 center;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode2D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeList<T> result;

            private float radiusSquared;

            private void AddSubtree(RStarNode2D node)
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


            private void SearchRTreeRecursion(RStarNode2D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (ShapeOverlap.CircleContainsRectangle(this.center, this.radiusSquared, leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (ShapeOverlap.RectangleCircleOverlap(leftNode.Bounds, this.center, this.radiusSquared))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (ShapeOverlap.CircleContainsRectangle(this.center, this.radiusSquared, rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (ShapeOverlap.RectangleCircleOverlap(rightNode.Bounds, this.center, this.radiusSquared))
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

                        if (ShapeOverlap.CircleContainsRectangle(this.center, this.radiusSquared, child.Bounds))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute()
            {
                this.result.Clear();

                this.radiusSquared = this.radius * this.radius;

                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }

        [BurstCompile]
        private struct GetRectanglesInRadiiJob : IJobParallelFor
        {

            public int root;

            [ReadOnly, NoAlias]
            public NativeArray<float> radii;

            [ReadOnly, NoAlias]
            public NativeArray<float2> centers;


            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode2D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeParallelHashSet<T>.ParallelWriter result;

            private float radiusSquared;
            private float2 center;

            private void AddSubtree(RStarNode2D node)
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


            private void SearchRTreeRecursion(RStarNode2D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (ShapeOverlap.CircleContainsRectangle(this.center, this.radiusSquared, leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (ShapeOverlap.RectangleCircleOverlap(leftNode.Bounds, this.center, this.radiusSquared))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (ShapeOverlap.CircleContainsRectangle(this.center, this.radiusSquared, rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (ShapeOverlap.RectangleCircleOverlap(rightNode.Bounds, this.center, this.radiusSquared))
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

                        if (ShapeOverlap.CircleContainsRectangle(this.center, this.radiusSquared, child.Bounds))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute(int index)
            {
                float radius = this.radii[index];

                this.radiusSquared = radius * radius;
                this.center = this.centers[index];

                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }

        [BurstCompile]
        private struct GetOverlappingRectanglesInRadiusJob : IJob
        {
            public float radius;
            public float2 center;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode2D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeList<T> result;

            private float radiusSquared;

            private void AddSubtree(RStarNode2D node)
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


            private void SearchRTreeRecursion(RStarNode2D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (ShapeOverlap.CircleContainsRectangle(this.center, this.radiusSquared, leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (ShapeOverlap.RectangleCircleOverlap(leftNode.Bounds, this.center, this.radiusSquared))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (ShapeOverlap.CircleContainsRectangle(this.center, this.radiusSquared, rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (ShapeOverlap.RectangleCircleOverlap(rightNode.Bounds, this.center, this.radiusSquared))
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

                        if (ShapeOverlap.RectangleCircleOverlap(child.Bounds, this.center, this.radiusSquared))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute()
            {
                this.result.Clear();

                this.radiusSquared = this.radius * this.radius;

                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode);
            }
        }


        [BurstCompile]
        private struct GetOverlappingRectanglesInRadiiJob : IJobParallelFor
        {

            public int root;

            [ReadOnly, NoAlias]
            public NativeArray<float> radii;

            [ReadOnly, NoAlias]
            public NativeArray<float2> centers;


            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode2D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeParallelHashSet<T>.ParallelWriter result;

            private float radiusSquared;
            private float2 center;

            private void AddSubtree(RStarNode2D node)
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


            private void SearchRTreeRecursion(RStarNode2D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (ShapeOverlap.CircleContainsRectangle(this.center, this.radiusSquared, leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode);
                    }
                    else if (ShapeOverlap.RectangleCircleOverlap(leftNode.Bounds, this.center, this.radiusSquared))
                    {
                        this.SearchRTreeRecursion(leftNode);
                    }

                    if (ShapeOverlap.CircleContainsRectangle(this.center, this.radiusSquared, rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode);
                    }
                    else if (ShapeOverlap.RectangleCircleOverlap(rightNode.Bounds, this.center, this.radiusSquared))
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

                        if (ShapeOverlap.RectangleCircleOverlap(child.Bounds, this.center, this.radiusSquared))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute(int index)
            {
                float radius = this.radii[index];

                this.radiusSquared = radius * radius;
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

            public Ray2D ray;

            //You can define your own comparers to sort the points in reverse order for example
            //However, the majority of people will want to have them sorted by increasing distance
            public IntersectionHit2D<T>.RayComparer comparer;

            [NoAlias]
            public NativeList<IntersectionHit2D<T>> result;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode2D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            private LineSegment2D lineSegment;

            private void IntersectRecursively(RStarNode2D node)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (ShapeOverlap.LineSegmentRectangleOverlap(this.lineSegment, leftNode.Bounds))
                    {
                        this.IntersectRecursively(leftNode);
                    }

                    if (ShapeOverlap.LineSegmentRectangleOverlap(this.lineSegment, rightNode.Bounds))
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

                        int intersections = ShapeIntersection.LineSegmentRectangleIntersections(this.lineSegment, child.Bounds,
                            out float2 intersection0, out float2 intersection1, out _);

                        if (intersections > 0)
                        {
                            var hitPoints = new FixedList32Bytes<float2>
                            {
                                intersection0
                            };
                            if (intersections > 1) hitPoints.Add(intersection1);

                            var intersectionHit = new IntersectionHit2D<T>()
                            {
                                boundingArea = child,
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

                this.lineSegment = new LineSegment2D()
                {
                    a = this.ray.origin,
                    b = this.ray.origin + this.ray.direction.normalized * this.distance
                };

                var rootNode = this.nodes[this.root];

                this.IntersectRecursively(rootNode);
                this.result.Sort(this.comparer);
            }
        }



        [BurstCompile]
        private struct GetRectanglesInPolygonJob : IJob
        {
            public Matrix4x4 trs;

            public NativePolygon2D polygon;

            public int root;

            [ReadOnly, NoAlias]
            public NativeParallelHashMap<int, T> data;

            [ReadOnly, NoAlias]
            public NativeList<RStarNode2D> nodes;

            [ReadOnly, NoAlias]
            public NativeList<FixedList128Bytes<int>> childrenBuffer;

            [WriteOnly, NoAlias]
            public NativeList<T> result;

            #region Private Fields

            private NativePolygon2D offsetedPolygon;

            private Rect polygonBounds;

            #endregion

            private void AddSubtree(RStarNode2D node, NativeArray<int> offsets)
            {
                if (node.children >= 0)
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        var child = this.data[childIdx];
                        if (this.offsetedPolygon.IsPointInsideInternal(child.Bounds.center, offsets))
                        {
                            this.result.Add(this.data[childIdx]);
                        }
                    }
                    return;
                }

                var leftNode = this.nodes[node.left];
                var rightNode = this.nodes[node.right];

                this.AddSubtree(leftNode, offsets);
                this.AddSubtree(rightNode, offsets);
            }


            private void SearchRTreeRecursion(RStarNode2D node, NativeArray<int> offsets)
            {
                if (node.left >= 0)
                {
                    var leftNodeIdx = node.left;
                    var rightNodeIx = node.right;

                    var leftNode = this.nodes[leftNodeIdx];
                    var rightNode = this.nodes[rightNodeIx];

                    if (this.polygonBounds.Contains(leftNode.Bounds))
                    {
                        this.AddSubtree(leftNode, offsets);
                    }
                    else if (this.polygonBounds.Overlaps(leftNode.Bounds))
                    {
                        this.SearchRTreeRecursion(leftNode, offsets);
                    }

                    if (this.polygonBounds.Contains(rightNode.Bounds))
                    {
                        this.AddSubtree(rightNode, offsets);
                    }
                    else if (this.polygonBounds.Overlaps(rightNode.Bounds))
                    {
                        this.SearchRTreeRecursion(rightNode, offsets);
                    }
                }
                else
                {
                    var childrenList = this.childrenBuffer[node.children];
                    for (int i = 0; i < childrenList.Length; i++)
                    {
                        int childIdx = childrenList[i];
                        var child = this.data[childIdx];

                        if (this.offsetedPolygon.IsPointInsideInternal(child.Bounds.center, offsets))
                        {
                            this.result.Add(child);
                        }
                    }

                }
            }

            public void Execute()
            {
                this.result.Clear();

                this.offsetedPolygon = new NativePolygon2D(Allocator.Temp, this.polygon.points, this.polygon.separators);
                for (int i = 0; i < this.polygon.points.Length; i++)
                {
                    Vector3 pos3D = new Vector3();
                    pos3D.x = this.offsetedPolygon.points[i].x;
                    pos3D.y = this.offsetedPolygon.points[i].y;
                    var transformedPos = this.trs.MultiplyPoint(pos3D);

                    float2 point = new float2();
                    point.x = transformedPos.x;
                    point.y = transformedPos.y;

                    this.offsetedPolygon.points[i] = point;
                }

                this.polygonBounds = this.offsetedPolygon.GetBoundingRect();
                var offsets = this.offsetedPolygon.PrepareOffsets(Allocator.Temp);

                var rootNode = this.nodes[this.root];
                this.SearchRTreeRecursion(rootNode, offsets);
            }

        }
    
    }
}
