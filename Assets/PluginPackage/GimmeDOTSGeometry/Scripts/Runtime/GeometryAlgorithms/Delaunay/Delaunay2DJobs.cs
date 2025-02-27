
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace GimmeDOTSGeometry
{
    public static class Delaunay2DJobs 
    {

        public struct DelaunayTriangleData
        {
            public int3 triangle;

            public int halfEdgeIdx;
        }

        [BurstCompile]
        public struct SwapBackJob : IJob
        {
            [NoAlias]
            public NativeReference<int> swappedIdx;

            [NoAlias]
            public NativeArray<float2> points;

            [NoAlias]
            public NativeList<int3> triangulation;

            public void Execute()
            {
                int idx = this.swappedIdx.Value;
                this.points.Swap(0, idx);
                for(int i = 0; i < this.triangulation.Length; i++)
                {
                    var triangle = this.triangulation[i];

                    if (triangle.x == idx) triangle.x = 0;
                    else if (triangle.x == 0) triangle.x = idx;

                    if (triangle.y == idx) triangle.y = 0;
                    else if(triangle.y == 0) triangle.y = idx;

                    if (triangle.z == idx) triangle.z = 0;
                    else if (triangle.z == 0) triangle.z = idx;

                    this.triangulation[i] = triangle;
                }
            }
        }

        [BurstCompile]
        public struct FindLexicographicLargestJob : IJob
        {
            [NoAlias]
            public NativeArray<float2> points;

            [NoAlias]
            public NativeReference<int> swappedIdx;


            public void Execute()
            {
                int largestIdx = -1;
                float2 largest = new float2(float.NegativeInfinity, float.NegativeInfinity);

                for(int i = 0; i < this.points.Length; i++)
                {
                    var point = this.points[i];
                    int cmp = point.y.CompareTo(largest.y);
                    if(cmp > 0)
                    {
                        largestIdx = i;
                        largest = point;
                    } else if(cmp == 0)
                    {
                        cmp = point.x.CompareTo(largest.x);
                        if(cmp > 0)
                        {
                            largestIdx = i;
                            largest = point;
                        }
                    }
                }

                this.swappedIdx.Value = largestIdx;
                this.points.Swap(0, largestIdx);
            }
        }

        //Somewhat exhaustive with half edges but not complicated at all (comparing it to Fortune's Algorithm ^^)
        [BurstCompile(FloatPrecision = FloatPrecision.High)]
        public struct DelaunayTriangulationJob : IJob
        {

            [NoAlias, ReadOnly]
            public NativeArray<float2> points;

            [NoAlias]
            public NativeGraph dag;

            [NoAlias]
            public NativeList<int3> triangulation;

            [NoAlias]
            public NativeList<HalfEdge> halfEdges;

            [NoAlias]
            public NativeList<DelaunayTriangleData> triangleBuffer;

            [NoAlias]
            public NativeParallelHashMap<int, int> halfEdgeToTriangleMap;

            public bool IsConvexWithSymbolicVertex(int symbolicIdx, int edgeAIdx, int edgeBIdx, int pointToCheckIdx)
            {
                var pointToCheck = this.points[pointToCheckIdx];

                if (!this.IsPointLeftOfSymbolicEdge(edgeAIdx, edgeBIdx, pointToCheck)) return false;

                int cmpA = pointToCheck.y.CompareTo(this.points[edgeAIdx].y);
                int cmpB = pointToCheck.y.CompareTo(this.points[edgeBIdx].y);
                if (symbolicIdx == -1) return cmpA >= 0 && cmpB <= 0;
                return cmpA <= 0 && cmpB >= 0;

            }

            public bool IsConvex(int idxA, int idxB, int idxC, int idxD)
            {
                if ((idxA < 0 || idxB < 0) && (idxC < 0 || idxD < 0)) return false;

                else if((idxA < 0 || idxB < 0) ^ (idxC < 0 || idxD < 0))
                {
                    int symbolicIdx, edgeA, edgeB, pointToCheckIdx;
                    if (idxA < 0 || idxB < 0)
                    {
                        if (idxA < 0) 
                        {
                            symbolicIdx = idxA;
                            edgeA = idxC;
                            edgeB = idxD;
                            pointToCheckIdx = idxB;
                        }
                        else
                        {
                            symbolicIdx = idxB;
                            edgeA = idxD;
                            edgeB = idxC;
                            pointToCheckIdx = idxA;
                        }

                    } else
                    {
                        if(idxC < 0)
                        {
                            symbolicIdx = idxC;
                            edgeA = idxB;
                            edgeB = idxA;
                            pointToCheckIdx = idxD;
                        } else
                        {
                            symbolicIdx = idxD;
                            edgeA = idxA;
                            edgeB = idxB;
                            pointToCheckIdx = idxC;
                        }
                    }
                    return this.IsConvexWithSymbolicVertex(symbolicIdx, edgeA, edgeB, pointToCheckIdx);
                } else
                {
                    var lsAB = new LineSegment2D(this.points[idxA], this.points[idxB]);
                    var lsCD = new LineSegment2D(this.points[idxC], this.points[idxD]);

                    return ShapeIntersection.LineSegmentIntersection(lsAB, lsCD, out _);
                }
            }

            public void LegalizeEdge(int pointIdx, int halfEdgeIdx)
            {

                var halfEdge = this.halfEdges[halfEdgeIdx];

                //Edges on the outermost triangle are always legal
                if (halfEdge.vertexFwd <= 0 && halfEdge.vertexBack <= 0) return;

                int twinEdgeIdx = halfEdge.twin;
                var twinEdge = this.halfEdges[twinEdgeIdx];
                var twinEdgeFwdIdx = twinEdge.fwd;
                var fwdTwinEdge = this.halfEdges[twinEdgeFwdIdx];
                int halfEdgeFwdIdx = halfEdge.fwd;
                var fwdHalfEdge = this.halfEdges[halfEdge.fwd];

                int idxA = halfEdge.vertexBack;
                int idxB = halfEdge.vertexFwd;
                int idxC = fwdHalfEdge.vertexFwd;
                int idxD = fwdTwinEdge.vertexFwd;

                if (!IsConvex(idxA, idxB, idxC, idxD)) return;

                if (idxA < 0 || idxB < 0 || idxC < 0 || idxD < 0)
                {

                    if (math.min(idxC, idxD) < math.min(idxA, idxB)) return;
                }
                else
                {
                    float2 a = this.points[idxA];
                    float2 b = this.points[idxB];
                    float2 c = this.points[idxC];
                    float2 d = this.points[idxD];

                    
                    var triangle = new NativeTriangle2D(a, b, c);

                    //The method works as well, but is unstable, because for small-angled triangles (usually at the border)
                    //the circumcircle is far, far away.
                    //NativeTriangle2D.CalculateCircumcircle(triangle, out float2 center, out float radiusSq);

                    //float dist = math.distancesq(center, d);
                    //if (dist >= radiusSq) return;
                    

                    if (!NativeTriangle2D.IsInsideTriangleCircumcircle(triangle, d)) return;
                }

                int halfEdgeBackIdx = halfEdge.back;
                var backHalfEdge = this.halfEdges[halfEdgeBackIdx];

                int twinEdgeBackIdx = twinEdge.back;
                var backTwinEdge = this.halfEdges[twinEdgeBackIdx];

                //Flip
                halfEdge.vertexBack = idxD;
                halfEdge.vertexFwd = pointIdx;
                halfEdge.back = twinEdgeFwdIdx;
                halfEdge.fwd = halfEdgeBackIdx;

                fwdHalfEdge.fwd = twinEdgeIdx;
                fwdHalfEdge.back = twinEdgeBackIdx;

                backHalfEdge.fwd = twinEdgeFwdIdx;
                backHalfEdge.back = halfEdgeIdx;

                twinEdge.vertexBack = pointIdx;
                twinEdge.vertexFwd = idxD;
                twinEdge.back = halfEdgeFwdIdx;
                twinEdge.fwd = twinEdgeBackIdx;

                fwdTwinEdge.fwd = halfEdgeIdx;
                fwdTwinEdge.back = halfEdgeBackIdx;

                backTwinEdge.fwd = halfEdgeFwdIdx;
                backTwinEdge.back = twinEdgeIdx;

                this.halfEdges[halfEdgeIdx] = halfEdge;
                this.halfEdges[twinEdgeIdx] = twinEdge;

                this.halfEdges[halfEdgeFwdIdx] = fwdHalfEdge;
                this.halfEdges[halfEdgeBackIdx] = backHalfEdge;

                this.halfEdges[twinEdgeFwdIdx] = fwdTwinEdge;
                this.halfEdges[twinEdgeBackIdx] = backTwinEdge;

                int triangleIdx0 = this.halfEdgeToTriangleMap[halfEdgeIdx];
                int triangleIdx1 = this.halfEdgeToTriangleMap[twinEdgeIdx];

                int triangleNextIdx0 = this.triangleBuffer.Length;
                int triangleNextIdx1 = this.triangleBuffer.Length + 1;

                this.triangleBuffer.Add(new DelaunayTriangleData()
                {
                    halfEdgeIdx = halfEdgeIdx,
                    triangle = new int3(halfEdge.vertexFwd, backHalfEdge.vertexFwd, fwdTwinEdge.vertexFwd),
                });

                this.triangleBuffer.Add(new DelaunayTriangleData()
                {
                    halfEdgeIdx = twinEdgeIdx,
                    triangle = new int3(twinEdge.vertexFwd, backTwinEdge.vertexFwd, fwdHalfEdge.vertexFwd),
                });

                this.halfEdgeToTriangleMap[halfEdgeIdx] = triangleNextIdx0;
                this.halfEdgeToTriangleMap[twinEdgeFwdIdx] = triangleNextIdx0;
                this.halfEdgeToTriangleMap[halfEdgeBackIdx] = triangleNextIdx0;

                this.halfEdgeToTriangleMap[twinEdgeIdx] = triangleNextIdx1;
                this.halfEdgeToTriangleMap[halfEdgeFwdIdx] = triangleNextIdx1;
                this.halfEdgeToTriangleMap[twinEdgeBackIdx] = triangleNextIdx1;


                this.dag.AddVertex(triangleNextIdx0);
                this.dag.AddVertex(triangleNextIdx1);

                this.dag.AddEdge(triangleIdx0, triangleNextIdx0);
                this.dag.AddEdge(triangleIdx0, triangleNextIdx1);
                this.dag.AddEdge(triangleIdx1, triangleNextIdx0);
                this.dag.AddEdge(triangleIdx1, triangleNextIdx1);

                this.LegalizeEdge(pointIdx, twinEdgeFwdIdx);
                this.LegalizeEdge(pointIdx, twinEdgeBackIdx);
                
            }

            public bool AreCollinear(int edgeIdxA, int edgeIdxB, float2 point)
            {
                if (edgeIdxA < 0 || edgeIdxB < 0) return false;

                float2 halfEdgeA = this.points[edgeIdxA];
                float2 halfEdgeB = this.points[edgeIdxB];

                float2 dirA = math.normalize(halfEdgeB - halfEdgeA);
                float2 dirC = math.normalize(point - halfEdgeB);

                return math.abs(math.dot(dirA, dirC)) >= 1.0f;
            }

            public bool IsPointLeftOfSymbolicEdge(int idxA, int idxB, float2 point)
            {
                if (idxA >= 0 && idxB >= 0)
                {
                    float2 pointA = this.points[idxA];
                    float2 pointB = this.points[idxB];

                    float2 dirAB = (pointB - pointA).Perpendicular();
                    float2 dirToPoint = point - pointA;

                    return math.dot(dirAB, dirToPoint) > 0.0f;
                } else
                {
                    if (idxA <= 0 && idxB <= 0) return true;

                    if(idxA > 0)
                    {
                        float2 pointA = this.points[idxA];
                        int cmp = point.y.CompareTo(pointA.y);
                        if(cmp == 0) cmp = point.x.CompareTo(pointA.x);

                        if (idxB == -1) return cmp > 0;
                        else return cmp <= 0;
                    }
                    else
                    {
                        float2 pointB = this.points[idxB];
                        int cmp = point.y.CompareTo(pointB.y);
                        if (cmp == 0) cmp = point.x.CompareTo(pointB.x);

                        if (idxA == -2) return cmp > 0;
                        else return cmp <= 0;
                    }
                    
                }
            }


            public bool IsPointInsideSymbolicTriangle(int3 indices, float2 point)
            {
                return this.IsPointLeftOfSymbolicEdge(indices.x, indices.y, point)
                     && this.IsPointLeftOfSymbolicEdge(indices.y, indices.z, point)
                     && this.IsPointLeftOfSymbolicEdge(indices.z, indices.x, point);
            }

            public unsafe int FindTriangleRecursion(UnsafeList<int> children, float2 point)
            {
                //Copying UnsafeLists is expensive because of the number of fields - pointers are faster
                GraphVertex* closestChild = null;
                float closestDist = float.NegativeInfinity;
                for (int i = 0; i < children.Length; i++)
                {
                    int childIdx = children[i];
                    var child = this.dag.vertices[childIdx];
                    int triangleIdx = child.dataPtr;
                    var triangleData = this.triangleBuffer[triangleIdx];
                    var triangleIndices = triangleData.triangle;

                    if (math.all(triangleIndices >= 0))
                    {
                        var triangle = new NativeTriangle2D(this.points[triangleIndices.x], this.points[triangleIndices.y], this.points[triangleIndices.z]);
                        if(triangle.IsPointInside(point))
                        {
                            if (child.vertexPointers.Length == 0) return child.dataPtr;
                            return FindTriangleRecursion(child.vertexPointers, point);
                        } else
                        {
                            //This chooses a triangle such that the distance between the point and
                            //the edges of the triangle is minimized
                            //This effectively eliminates a lot of floating-point precision problems at the
                            //cost of slightly sub-optimal triangulation (which should not really matter too much)
                            var barycentricCoords = triangle.CalculateBarycentricCoordinates(point);
                            float barycentricDist = math.cmin(barycentricCoords);
                            if(barycentricDist > closestDist || closestChild == null)
                            {
                                closestChild = &child;
                                closestDist = barycentricDist;
                            }
                        }

                    } else if(this.IsPointInsideSymbolicTriangle(triangleIndices, point))
                    {
                        //Doing it this way saves one recursion step btw. I.e. less method calls
                        if (child.vertexPointers.Length == 0) return child.dataPtr;
                        return FindTriangleRecursion(child.vertexPointers, point);
                    }
                }

                //If there are three collinear points in a row on the convex hull and a fourth one very close by,
                //the child might stay null. In that case -> Simply choose the first one
                if (closestChild == null) return children[0];
                if (closestChild->vertexPointers.Length == 0) return closestChild->dataPtr;
                return FindTriangleRecursion(closestChild->vertexPointers, point);
            }



            public int FindTriangle(int pointIdx)
            {
                int idx = 0;

                float2 point = this.points[pointIdx];
                var currentNode = this.dag.vertices[idx];

                if (currentNode.vertexPointers.Length == 0) return currentNode.dataPtr;
                return FindTriangleRecursion(currentNode.vertexPointers, point);
            }

            public int2 AddTriangleEdges(int halfEdgeIdx, int pointIdx)
            {
                var halfEdge = this.halfEdges[halfEdgeIdx];
                int nextEdge = this.halfEdges.Length;
                int nextNextEdge = this.halfEdges.Length + 1;

                var halfEdgeAP = new HalfEdge()
                {
                    back = halfEdgeIdx,
                    fwd = nextNextEdge,
                    vertexBack = halfEdge.vertexFwd,
                    vertexFwd = pointIdx,
                };

                var halfEdgePC = new HalfEdge()
                {
                    back = nextEdge,
                    fwd = halfEdgeIdx,
                    vertexBack = pointIdx,
                    vertexFwd = halfEdge.vertexBack,
                };

                this.halfEdges.Add(halfEdgeAP);
                this.halfEdges.Add(halfEdgePC);

                return new int2(nextEdge, nextNextEdge);
            }

            public void SplitIntoFour(int pointIdx, int halfEdgeIdx, int triangleBufferIdx)
            {
                var halfEdgeA = this.halfEdges[halfEdgeIdx];
                var halfEdgeB = this.halfEdges[halfEdgeA.fwd];
                var halfEdgeC = this.halfEdges[halfEdgeB.fwd];

                int halfEdgeIdxA = halfEdgeIdx;
                int halfEdgeIdxB = halfEdgeA.fwd;
                int halfEdgeIdxC = halfEdgeB.fwd;

                int twinEdgeIdxA = halfEdgeA.twin;

                int otherTriangleBufferIdx = this.halfEdgeToTriangleMap[twinEdgeIdxA];

                var twinEdgeA = this.halfEdges[twinEdgeIdxA];
                var twinEdgeB = this.halfEdges[twinEdgeA.fwd];
                var twinEdgeC = this.halfEdges[twinEdgeB.fwd];

                int twinEdgeIdxB = twinEdgeA.fwd;
                int twinEdgeIdxC = twinEdgeB.fwd;

                halfEdgeA.vertexFwd = pointIdx;
                twinEdgeA.vertexBack = pointIdx;

                int2 edgesB = this.AddTriangleEdges(halfEdgeIdxB, pointIdx);
                int2 twinEdgesC = this.AddTriangleEdges(twinEdgeIdxC, pointIdx);

                int nextEdge = this.halfEdges.Length;
                int nextNextEdge = this.halfEdges.Length + 1;

                var halfEdgePD = new HalfEdge()
                {
                    back = halfEdgeIdxA,
                    fwd = halfEdgeIdxC,
                    vertexBack = pointIdx,
                    vertexFwd = halfEdgeC.vertexBack,
                    twin = edgesB.x,
                };

                var halfEdgeBP = new HalfEdge()
                {
                    back = twinEdgeIdxB,
                    fwd = twinEdgeIdxA,
                    vertexBack = twinEdgeB.vertexFwd,
                    vertexFwd = pointIdx,
                    twin = twinEdgesC.y,
                };

                this.halfEdges.Add(halfEdgePD);
                this.halfEdges.Add(halfEdgeBP);

                halfEdgeA.fwd = nextEdge;
                halfEdgeC.back = nextEdge;

                twinEdgeA.back = nextNextEdge;
                twinEdgeB.fwd = nextNextEdge;

                halfEdgeB.fwd = edgesB.x;
                halfEdgeB.back = edgesB.y;

                twinEdgeC.fwd = twinEdgesC.x;
                twinEdgeC.back = twinEdgesC.y;

                var halfEdgeB0 = this.halfEdges[edgesB.x];
                var halfEdgeB1 = this.halfEdges[edgesB.y];
                var twinEdgeC0 = this.halfEdges[twinEdgesC.x];
                var twinEdgeC1 = this.halfEdges[twinEdgesC.y];

                //Twinning
                halfEdgeB0.twin = nextEdge;
                halfEdgeB1.twin = twinEdgesC.x;
                twinEdgeC0.twin = edgesB.y;
                twinEdgeC1.twin = nextNextEdge;

                this.halfEdges[halfEdgeIdxA] = halfEdgeA;
                this.halfEdges[halfEdgeIdxB] = halfEdgeB;
                this.halfEdges[halfEdgeIdxC] = halfEdgeC;

                this.halfEdges[twinEdgeIdxA] = twinEdgeA;
                this.halfEdges[twinEdgeIdxB] = twinEdgeB;
                this.halfEdges[twinEdgeIdxC] = twinEdgeC;

                this.halfEdges[edgesB.x] = halfEdgeB0;
                this.halfEdges[edgesB.y] = halfEdgeB1;
                this.halfEdges[twinEdgesC.x] = twinEdgeC0;
                this.halfEdges[twinEdgesC.y] = twinEdgeC1;


                int triangleAIdx = this.triangleBuffer.Length;
                int triangleBIdx = this.triangleBuffer.Length + 1;
                int triangleCIdx = this.triangleBuffer.Length + 2;
                int triangleDIdx = this.triangleBuffer.Length + 3;

                this.halfEdgeToTriangleMap[halfEdgeIdxA] = triangleAIdx;
                this.halfEdgeToTriangleMap[halfEdgeIdxB] = triangleBIdx;
                this.halfEdgeToTriangleMap[halfEdgeIdxC] = triangleAIdx;
                this.halfEdgeToTriangleMap[twinEdgeIdxA] = triangleCIdx;
                this.halfEdgeToTriangleMap[twinEdgeIdxB] = triangleCIdx;
                this.halfEdgeToTriangleMap[twinEdgeIdxC] = triangleDIdx;

                this.halfEdgeToTriangleMap.Add(nextEdge, triangleAIdx);
                this.halfEdgeToTriangleMap.Add(edgesB.x, triangleBIdx);
                this.halfEdgeToTriangleMap.Add(edgesB.y, triangleBIdx);
                this.halfEdgeToTriangleMap.Add(twinEdgesC.x, triangleDIdx);
                this.halfEdgeToTriangleMap.Add(twinEdgesC.y, triangleDIdx);
                this.halfEdgeToTriangleMap.Add(nextNextEdge, triangleCIdx);

                this.triangleBuffer.Add(new DelaunayTriangleData()
                {
                    halfEdgeIdx = halfEdgeIdxA,
                    triangle = new int3(halfEdgeA.vertexFwd, halfEdgePD.vertexFwd, halfEdgeC.vertexFwd),
                });

                this.triangleBuffer.Add(new DelaunayTriangleData()
                {
                    halfEdgeIdx = halfEdgeIdxB,
                    triangle = new int3(halfEdgeB.vertexFwd, halfEdgeB0.vertexFwd, halfEdgeB1.vertexFwd),
                });

                this.triangleBuffer.Add(new DelaunayTriangleData()
                {
                    halfEdgeIdx = twinEdgeIdxA,
                    triangle = new int3(twinEdgeA.vertexFwd, twinEdgeB.vertexFwd, halfEdgeBP.vertexFwd),
                });

                this.triangleBuffer.Add(new DelaunayTriangleData()
                {
                    halfEdgeIdx = twinEdgeIdxC,
                    triangle = new int3(twinEdgeC.vertexFwd, twinEdgeC0.vertexFwd, twinEdgeC1.vertexFwd),
                });

                this.dag.AddVertex(triangleAIdx);
                this.dag.AddVertex(triangleBIdx);
                this.dag.AddVertex(triangleCIdx);
                this.dag.AddVertex(triangleDIdx);

                this.dag.AddEdge(triangleBufferIdx, triangleAIdx);
                this.dag.AddEdge(triangleBufferIdx, triangleBIdx);
                this.dag.AddEdge(otherTriangleBufferIdx, triangleCIdx);
                this.dag.AddEdge(otherTriangleBufferIdx, triangleDIdx);

                this.LegalizeEdge(pointIdx, halfEdgeIdxB);
                this.LegalizeEdge(pointIdx, halfEdgeIdxC);
                this.LegalizeEdge(pointIdx, twinEdgeIdxB);
                this.LegalizeEdge(pointIdx, twinEdgeIdxC);
            }

            public void SplitIntoThree(int pointIdx, int halfEdgeIdx, int triangleBufferIdx)
            {

                var halfEdgeA = this.halfEdges[halfEdgeIdx];
                var halfEdgeB = this.halfEdges[halfEdgeA.fwd];
                var halfEdgeC = this.halfEdges[halfEdgeB.fwd];

                int halfEdgeIdxA = halfEdgeIdx;
                int halfEdgeIdxB = halfEdgeA.fwd;
                int halfEdgeIdxC = halfEdgeB.fwd;

                int2 edgesA = this.AddTriangleEdges(halfEdgeIdxA, pointIdx);
                int2 edgesB = this.AddTriangleEdges(halfEdgeIdxB, pointIdx);
                int2 edgesC = this.AddTriangleEdges(halfEdgeIdxC, pointIdx);

                var halfEdgeA0 = this.halfEdges[edgesA.x];
                var halfEdgeA1 = this.halfEdges[edgesA.y];
                var halfEdgeB0 = this.halfEdges[edgesB.x];
                var halfEdgeB1 = this.halfEdges[edgesB.y];
                var halfEdgeC0 = this.halfEdges[edgesC.x];
                var halfEdgeC1 = this.halfEdges[edgesC.y];

                halfEdgeA0.twin = edgesB.y;
                halfEdgeA1.twin = edgesC.x;
                halfEdgeB0.twin = edgesC.y;
                halfEdgeB1.twin = edgesA.x;
                halfEdgeC0.twin = edgesA.y;
                halfEdgeC1.twin = edgesB.x;

                this.halfEdges[edgesA.x] = halfEdgeA0;
                this.halfEdges[edgesA.y] = halfEdgeA1;
                this.halfEdges[edgesB.x] = halfEdgeB0;
                this.halfEdges[edgesB.y] = halfEdgeB1;
                this.halfEdges[edgesC.x] = halfEdgeC0;
                this.halfEdges[edgesC.y] = halfEdgeC1;

                halfEdgeA.fwd = edgesA.x;
                halfEdgeA.back = edgesA.y;

                halfEdgeB.fwd = edgesB.x;
                halfEdgeB.back = edgesB.y;

                halfEdgeC.fwd = edgesC.x;
                halfEdgeC.back = edgesC.y;

                this.halfEdges[halfEdgeIdxA] = halfEdgeA;
                this.halfEdges[halfEdgeIdxB] = halfEdgeB;
                this.halfEdges[halfEdgeIdxC] = halfEdgeC;

                int triangleAIdx = this.triangleBuffer.Length;
                int triangleBIdx = this.triangleBuffer.Length + 1;
                int triangleCIdx = this.triangleBuffer.Length + 2;

                this.halfEdgeToTriangleMap[halfEdgeIdxA] = triangleAIdx;
                this.halfEdgeToTriangleMap[halfEdgeIdxB] = triangleBIdx;
                this.halfEdgeToTriangleMap[halfEdgeIdxC] = triangleCIdx;

                this.halfEdgeToTriangleMap.Add(edgesA.x, triangleAIdx);
                this.halfEdgeToTriangleMap.Add(edgesA.y, triangleAIdx);
                this.halfEdgeToTriangleMap.Add(edgesB.x, triangleBIdx);
                this.halfEdgeToTriangleMap.Add(edgesB.y, triangleBIdx);
                this.halfEdgeToTriangleMap.Add(edgesC.x, triangleCIdx);
                this.halfEdgeToTriangleMap.Add(edgesC.y, triangleCIdx);

                this.triangleBuffer.Add(new DelaunayTriangleData()
                {
                    halfEdgeIdx = halfEdgeIdxA,
                    triangle = new int3(halfEdgeA.vertexFwd, halfEdgeA0.vertexFwd, halfEdgeA1.vertexFwd),
                });

                this.triangleBuffer.Add(new DelaunayTriangleData()
                {
                    halfEdgeIdx = halfEdgeIdxB,
                    triangle = new int3(halfEdgeB.vertexFwd, halfEdgeB0.vertexFwd, halfEdgeB1.vertexFwd),
                });

                this.triangleBuffer.Add(new DelaunayTriangleData()
                {
                    halfEdgeIdx = halfEdgeIdxC,
                    triangle = new int3(halfEdgeC.vertexFwd, halfEdgeC0.vertexFwd, halfEdgeC1.vertexFwd),
                });

                this.dag.AddVertex(triangleAIdx);
                this.dag.AddVertex(triangleBIdx);
                this.dag.AddVertex(triangleCIdx);

                this.dag.AddEdge(triangleBufferIdx, triangleAIdx);
                this.dag.AddEdge(triangleBufferIdx, triangleBIdx);
                this.dag.AddEdge(triangleBufferIdx, triangleCIdx);

                this.LegalizeEdge(pointIdx, halfEdgeIdxA);
                this.LegalizeEdge(pointIdx, halfEdgeIdxB);
                this.LegalizeEdge(pointIdx, halfEdgeIdxC);

            }

            public void CreateTriangulation()
            {
                NativeBitArray markedEdges = new NativeBitArray(this.halfEdges.Length, Allocator.Temp);

                for(int i = 0; i < this.halfEdges.Length; i++)
                {
                    this.halfEdgeToTriangleMap[i] = -1;
                }

                for (int i = 0; i < this.halfEdges.Length; i++)
                {
                    if (markedEdges.IsSet(i)) continue;

                    int3 triangle = new int3();
                    int3 triangleEdges = new int3();

                    markedEdges.Set(i, true);
                    var halfEdge = this.halfEdges[i];

                    int count = 0;

                    if (halfEdge.vertexFwd < 0 || halfEdge.vertexBack < 0) continue;

                    triangle[count] = halfEdge.vertexFwd;
                    triangleEdges[count] = i;

                    while (halfEdge.fwd != i)
                    {
                        int fwd = halfEdge.fwd;

                        halfEdge = this.halfEdges[halfEdge.fwd];
                        markedEdges.Set(fwd, true);
                        if (halfEdge.vertexFwd < 0 || halfEdge.vertexBack < 0) break;

                        count++;
                        triangle[count] = halfEdge.vertexFwd;
                        triangleEdges[count] = fwd;
                    }

                    if (count == 2)
                    {
                        this.triangulation.Add(triangle);

                        this.halfEdgeToTriangleMap[triangleEdges.x] = this.triangulation.Length - 1;
                        this.halfEdgeToTriangleMap[triangleEdges.y] = this.triangulation.Length - 1;
                        this.halfEdgeToTriangleMap[triangleEdges.z] = this.triangulation.Length - 1;
                    }
                }
            }

            public void Execute()
            {
                this.triangulation.Clear();

                int p0 = 0;
                int pMinusOne = -1;
                int pMinusTwo = -2;

                this.triangleBuffer.Add(new DelaunayTriangleData()
                {
                    triangle = new int3(p0, pMinusOne, pMinusTwo),
                    halfEdgeIdx = 0
                });

                this.dag.AddVertex(0);

                this.halfEdges.Add(new HalfEdge()
                {
                    back = 2,
                    fwd = 1,
                    twin = -1,
                    vertexBack = -1,
                    vertexFwd = 0
                });

                this.halfEdges.Add(new HalfEdge()
                {
                    back = 0,
                    fwd = 2,
                    twin = -1,
                    vertexBack = 0,
                    vertexFwd = -2,
                });

                this.halfEdges.Add(new HalfEdge() {
                    back = 1,
                    fwd = 0,
                    twin = -1,
                    vertexBack = -2,
                    vertexFwd = -1
                });

                this.halfEdgeToTriangleMap.Clear();

                this.halfEdgeToTriangleMap.Add(0, 0);
                this.halfEdgeToTriangleMap.Add(1, 0);
                this.halfEdgeToTriangleMap.Add(2, 0);

                for(int i = 1; i < this.points.Length; i++)
                {
                    float2 point = this.points[i];


                    int triangleBufferIdx = this.FindTriangle(i);

                    var triangleData = this.triangleBuffer[triangleBufferIdx];

                    var halfEdgeIdx = triangleData.halfEdgeIdx;
                    var halfEdge = this.halfEdges[halfEdgeIdx];
                    int collinearEdgeIdx = halfEdgeIdx;
                    bool isOnEdge = this.AreCollinear(halfEdge.vertexBack, halfEdge.vertexFwd, point);
                    if (!isOnEdge)
                    {
                        while (halfEdge.fwd != halfEdgeIdx)
                        {
                            collinearEdgeIdx = halfEdge.fwd;
                            halfEdge = this.halfEdges[halfEdge.fwd];
                            if (this.AreCollinear(halfEdge.vertexBack, halfEdge.vertexFwd, point))
                            {
                                isOnEdge = true;
                                break;
                            }
                        }
                    }


                    if (isOnEdge)
                    {

                        this.SplitIntoFour(i, collinearEdgeIdx, triangleBufferIdx);

                    } else
                    {
                        this.SplitIntoThree(i, halfEdgeIdx, triangleBufferIdx);
                    }
                }

                this.CreateTriangulation();
            }
        }
    }
}
