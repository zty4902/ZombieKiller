using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace GimmeDOTSGeometry
{

    public static class LineIntersectionJobs {

        [BurstCompile]
        public struct FindLSPlaneIntersectionsCombinatorialParallel : IJobParallelFor
        {
            public float epsilon;

            public Plane intersectionPlane;

            [NoAlias, ReadOnly]
            public NativeList<LineSegment3D> segments;

            [NoAlias]
            public NativeList<float3>.ParallelWriter intersections;

            public void Execute(int index)
            {
                var segment = this.segments[index];
                float3 dir = segment.b - segment.a;
                float3 normalizedDir = math.normalize(dir);
                float3 normal = this.intersectionPlane.normal;
                float dist = this.intersectionPlane.distance;

                //Partly from the Raycast Method in Plane, but converted to DOTS
                float dot = math.dot(normalizedDir, normal);
                if (math.abs(dot) > this.epsilon)
                {
                    float dot2 = -math.dot(segment.a, normal) + dist;
                    float intersectionDist = dot2 / dot;
                    float lengthSq = math.lengthsq(dir);

                    if (intersectionDist >= 0.0f && intersectionDist * intersectionDist < lengthSq)
                    {
                        this.intersections.AddNoResize((float3)segment.a + normalizedDir * intersectionDist);
                    }
                }
            }
        }

        [BurstCompile]
        public struct FindLineSegmentIntersectionsCombinatorialParallel : IJobParallelFor
        {
            [NoAlias, ReadOnly]
            public NativeList<LineSegment2D> segments;

            //In the worst case, there are n� intersections (each line intersects each other line)
            //Therefore in order for the parallel job to work all the time, that amount
            //of capacity should be reserved in the list before starting
            [NoAlias]
            public NativeList<float2>.ParallelWriter intersections;

            public void Execute(int index)
            {
                var s0 = this.segments[index];
                for(int i = index + 1; i < this.segments.Length; i++)
                {
                    var s1 = this.segments[i];

                    if(ShapeIntersection.LineSegmentIntersection(s0, s1, out var intersection))
                    {
                        this.intersections.AddNoResize(intersection);
                    }
                }
            }
        }

        [BurstCompile]
        public struct FindLineSegmentIntersectionsCombinatorial : IJob
        {
            [NoAlias, ReadOnly]
            public NativeList<LineSegment2D> segments;

            [NoAlias, WriteOnly]
            public NativeList<float2> intersections;

            public void Execute()
            {
                this.intersections.Clear();
                for (int i = 0; i < this.segments.Length; i++)
                {
                    var s0 = this.segments[i];
                    for (int j = i + 1; j < this.segments.Length; j++)
                    {
                        var s1 = this.segments[j];

                        if(ShapeIntersection.LineSegmentIntersection(s0, s1, out var intersection))
                        {
                            this.intersections.Add(intersection);
                        }
                    }
                }
            }
        }

        [BurstCompile]
        public struct PrepareLineSegmentsSweep : IJobParallelFor
        {
            public float epsilon;

            [NoAlias, NativeDisableParallelForRestriction]
            public NativeList<LineSegment2D> segments;


            public void Execute(int index)
            {
                var segment = this.segments[index];
                if (segment.a.y > segment.b.y)
                {
                    float2 a = segment.a;
                    segment.a = segment.b;
                    segment.b = a;
                } else if(Hint.Unlikely(segment.a.y == segment.b.y))
                {
                    if(segment.a.x > segment.b.x)
                    {
                        float2 a = segment.a;
                        segment.a = segment.b;
                        segment.b = a;
                    }
                }

                this.segments[index] = segment;
            }
        }

        public struct SegmentTreeCode
        {
            public LineSegment2D segment;
            public float nodeCode;
            public int elementIdx;
        }

        public struct EventPoint
        {
            public float2 point;

            public int hash;

            public override int GetHashCode()
            {
                return (this.point, this.hash).GetHashCode();
            }

            public override string ToString()
            {
                string format = "{0:0.00}";
                return $"{string.Format(format, this.point.x)}, {string.Format(format, this.point.y)}";
            }
        }

        public struct EventPointComparer : IComparer<EventPoint>
        {
            public float epsilon;

            public int Compare(EventPoint a, EventPoint b)
            {
                float2 diff = a.point - b.point;
                if(math.abs(diff.y) > this.epsilon)
                {
                    return (int)math.sign(diff.y);
                } else 
                {
                    return (diff.x > this.epsilon ? 1 : 0) - (diff.x < -this.epsilon ? 1 : 0);
                }
            }
        }

        public struct SweepLineComparer : IComparer<LineSegment2D>
        {
            public float epsilon;

            public float2 sweepPoint;

            public int Compare(LineSegment2D l0, LineSegment2D l1)
            {
                float2 p0, p1;

                if (Hint.Unlikely(math.abs(l0.a.y - l0.b.y) < this.epsilon))
                {
                    float minL0 = math.min(l0.a.x, l0.b.x);
                    if (math.abs(this.sweepPoint.y - l0.a.y) < this.epsilon)
                    {
                        float maxL0 = math.max(l0.a.x, l0.b.x);
                        //We shouldn't go too far backwards so to speak on a horizontal line
                        if (this.sweepPoint.x > minL0 && this.sweepPoint.x < maxL0) minL0 = this.sweepPoint.x;
                    }

                    //minL0 = math.max(minL0, this.sweepPoint.x);
                    p0 = new float2(minL0, this.sweepPoint.y);
                }
                else
                {
                    float t0 = math.unlerp(l0.a.y, l0.b.y, this.sweepPoint.y + this.epsilon);
                    p0 = math.lerp(l0.a, l0.b, t0);
                }

                if (Hint.Unlikely(math.abs(l1.a.y - l1.b.y) < this.epsilon))
                {
                    float minL1 = math.min(l1.a.x, l1.b.x);
                    if (math.abs(this.sweepPoint.y - l1.a.y) < this.epsilon)
                    {
                        float maxL1 = math.max(l1.a.x, l1.b.x);
                        //We shouldn't go too far backwards so to speak on a horizontal line
                        if (this.sweepPoint.x > minL1 && this.sweepPoint.x < maxL1) minL1 = this.sweepPoint.x;
                    }

                    p1 = new float2(minL1, this.sweepPoint.y);
                }
                else
                {
                    float t1 = math.unlerp(l1.a.y, l1.b.y, this.sweepPoint.y + this.epsilon);
                    p1 = math.lerp(l1.a, l1.b, t1);
                }

                float diffX = p0.x - p1.x;
                if (Hint.Likely(math.abs(diffX) > this.epsilon))
                {
                    return (int)math.sign(diffX);
                }
                else
                {
                    var dir0 = math.normalize(l0.b - l0.a);
                    var dir1 = math.normalize(l1.b - l1.a);
                    return (int)math.sign(dir0.x - dir1.x);
                }
            }
        }

        [BurstCompile(FloatPrecision = FloatPrecision.High)]
        public struct FindLineIntersectionsSweep : IJob
        {
            public bool restart;

            public float epsilon;

            //Segments must be prepared with PrepareLineSegmentsSweep
            [NoAlias, ReadOnly]
            public NativeList<LineSegment2D> segments;

            [NoAlias, WriteOnly]
            public NativeList<float2> intersections;

            public void InsertSegment(LineSegment2D segment, int hash, 
                NativeParallelMultiHashMap<int, LineSegment2D> lowerSegments, 
                ref NativeAVLTree<EventPoint, EventPointComparer> eventQueue)
            {
                var lowerEventPoint = new EventPoint()
                {
                    hash = hash,
                    point = segment.a
                };
                var upperEventPoint = new EventPoint()
                {
                    hash = -1,
                    point = segment.b
                };

                //The assumption here is that most segments don't start at the same point
                if (Hint.Likely(!eventQueue.Contains(lowerEventPoint)))
                {
                    lowerSegments.Add(hash, segment);
                    eventQueue.Insert(lowerEventPoint);

                } else {
                    int idx = eventQueue.GetElementIdx(lowerEventPoint);
                    var elements = eventQueue.Elements;
                    var elem = elements[idx];
                    if(elem.value.hash < 0)
                    {
                        elem.value.hash = hash;
                        elements[idx] = elem;
                    }
                    lowerSegments.Add(elem.value.hash, segment);
                }

                if(Hint.Likely(!eventQueue.Contains(upperEventPoint))) {
                    eventQueue.Insert(upperEventPoint);
                }
            }

            private void FindSegmentsContainingPointRecursion(float2 point, [AssumeRange(0, int.MaxValue)] int currentNode,
                ref NativeList<LineSegment2D> innerSegments,
                ref NativeList<LineSegment2D> upperSegments,
                ref NativeAVLTree<LineSegment2D, SweepLineComparer> status)
            {
                var elem = status.Elements[currentNode];
                var segment = elem.value;
                bool isUpperPoint = math.all(math.abs((float2)segment.b - point) < this.epsilon);
                if (Hint.Unlikely(isUpperPoint))
                {
                    upperSegments.Add(segment);
                }

                bool hasLeft = elem.left >= 0;
                bool hasRight = elem.right >= 0;

                float diffXA = point.x - segment.a.x;
                float diffXB = point.x - segment.b.x;

                diffXA = math.select(diffXA, 0.0f, math.abs(diffXA) < this.epsilon);
                diffXB = math.select(diffXB, 0.0f, math.abs(diffXB) < this.epsilon);

                int cmp = (int)(math.sign(diffXA) + math.sign(diffXB));

                //Point is strictly to the left of the segment -> go left
                if (hasLeft && cmp < -1)
                {
                    this.FindSegmentsContainingPointRecursion(point, elem.left, ref innerSegments, ref upperSegments, ref status);
                }
                else if (hasRight && cmp > 1)
                {
                    this.FindSegmentsContainingPointRecursion(point, elem.right, ref innerSegments, ref upperSegments, ref status);
                }
                else
                {
                    if (Hint.Likely(!isUpperPoint))
                    {
                        if (point.y > segment.a.y && point.y < segment.b.y)
                        {
                            float t = math.unlerp(segment.a.y, segment.b.y, point.y);
                            float2 p = math.lerp(segment.a, segment.b, t);

                            float diff = p.x - point.x;
                            if (math.abs(diff) < this.epsilon)
                            {
                                innerSegments.Add(segment);
                            }
                        }
                        //Point is on horizontal line
                        else if (point.y == segment.a.y && point.y == segment.b.y && cmp == 0)
                        {
                            innerSegments.Add(segment);
                        }
                    }

                    if(hasLeft)
                    {
                        this.FindSegmentsContainingPointRecursion(point, elem.left, ref innerSegments, ref upperSegments, ref status);
                    }

                    if(hasRight)
                    {
                        this.FindSegmentsContainingPointRecursion(point, elem.right, ref innerSegments, ref upperSegments, ref status);
                    }
                }
            }

            private void FindSegmentsContainingPoint(float2 point,
                NativeList<LineSegment2D> innerSegments,
                NativeList<LineSegment2D> upperSegments,
                NativeAVLTree<LineSegment2D, SweepLineComparer> status)
            {
                int currentNode = status.RootIdx;
                if(currentNode >= 0)
                {
                    this.FindSegmentsContainingPointRecursion(point, currentNode, ref innerSegments, ref upperSegments, ref status);
                }

            }

            private int CompareSegmentWithPoint(ref LineSegment2D segment, float2 point)
            {
                float2 p0;

                if(math.abs(segment.a.y - segment.b.y) < this.epsilon)
                {
                    p0 = segment.a;
                } else
                {
                    float t0 = math.unlerp(segment.a.y, segment.b.y, point.y);
                    p0 = math.lerp(segment.a, segment.b, t0);
                }
                return (int)math.sign(p0.x - point.x);
            }

            private void FindLeftAndRightMost(int hash, out LineSegment2D leftMost, out LineSegment2D rightMost,
                out int leftMostIdx, out int rightMostIdx,
                ref NativeParallelMultiHashMap<int, LineSegment2D> lowerSegments,
                ref NativeList<LineSegment2D> innerSegments,
                ref NativeList<SegmentTreeCode> treePositions,
                ref NativeAVLTree<LineSegment2D, SweepLineComparer> status)
            {
                leftMost = new LineSegment2D();
                rightMost = new LineSegment2D();
                leftMostIdx = -1;
                rightMostIdx = -1;

                var segmentTreeCode = new SegmentTreeCode() { };

                if(lowerSegments.TryGetFirstValue(hash, out var lowerSegment, out var it)) {

                    float treeCode = status.GetTreeCode(lowerSegment, out int nodeIdx);

                    segmentTreeCode.nodeCode = treeCode;
                    segmentTreeCode.segment = lowerSegment;
                    segmentTreeCode.elementIdx = nodeIdx;

                    treePositions.Add(segmentTreeCode);

                    while(lowerSegments.TryGetNextValue(out lowerSegment, ref it))
                    {
                        treeCode = status.GetTreeCode(lowerSegment, out nodeIdx);

                        segmentTreeCode.nodeCode = treeCode;
                        segmentTreeCode.segment = lowerSegment;
                        segmentTreeCode.elementIdx = nodeIdx;

                        treePositions.Add(segmentTreeCode);

                    }
                }

                for(int i = 0; i < innerSegments.Length; i++)
                {
                    var segment = innerSegments[i];

                    float treeCode = status.GetTreeCode(segment, out int nodeIdx);

                    segmentTreeCode.nodeCode = treeCode;
                    segmentTreeCode.segment = segment;
                    segmentTreeCode.elementIdx = nodeIdx;

                    treePositions.Add(segmentTreeCode);
                }

                float smallest = float.PositiveInfinity, biggest = float.NegativeInfinity;

                if(!treePositions.IsEmpty)
                {
                    for(int i = 0; i < treePositions.Length; i++)
                    {
                        var pos = treePositions[i];

                        if(Hint.Unlikely(pos.nodeCode < smallest))
                        {
                            smallest = pos.nodeCode;
                            leftMost = pos.segment;
                            leftMostIdx = pos.elementIdx;
                        }

                        if(Hint.Unlikely(pos.nodeCode > biggest))
                        {
                            biggest = pos.nodeCode;
                            rightMost = pos.segment;
                            rightMostIdx = pos.elementIdx;
                        }
                    }
                }
            }

            private int GetLeftNeighbour(int nodeIdx, ref NativeAVLTree<LineSegment2D, SweepLineComparer> status)
            {
                if(nodeIdx >= 0)
                {
                    int leftNode = status.GetFirstLeftEntry(nodeIdx);
                    if (leftNode >= 0) return leftNode;
                }
                return nodeIdx;
            }

            private int GetRightNeighbour(int nodeIdx, ref NativeAVLTree<LineSegment2D, SweepLineComparer> status)
            {
                if(nodeIdx >= 0)
                {
                    int rightNode = status.GetFirstRightEntry(nodeIdx);
                    if (rightNode >= 0) return rightNode;
                }
                return nodeIdx;
            }

            private unsafe int GetSegmentToTheLeft(float2 eventPoint, ref NativeAVLTree<LineSegment2D, SweepLineComparer> status)
            {
                int currentNode = status.RootIdx;
                int closestLeft = -1;
                var basePtr = (NativeAVLTree<LineSegment2D, SweepLineComparer>.TreeNode*)status.Elements.GetUnsafePtr();

                while(currentNode >= 0)
                {
                    var nodePtr = basePtr + currentNode;
                    if(this.CompareSegmentWithPoint(ref nodePtr->value, eventPoint) >= 0)
                    {
                        currentNode = nodePtr->left;
                    } else
                    {
                        closestLeft = currentNode;
                        currentNode = nodePtr->right;
                    }
                }
                return closestLeft;
            }

            private unsafe int GetSegmentToTheRight(float2 eventPoint, ref NativeAVLTree<LineSegment2D, SweepLineComparer> status)
            {
                int currentNode = status.RootIdx;
                int closestRight = -1;
                var basePtr = (NativeAVLTree<LineSegment2D, SweepLineComparer>.TreeNode*)status.Elements.GetUnsafePtr();

                while (currentNode >= 0)
                {
                    var nodePtr = basePtr + currentNode;
                    if (this.CompareSegmentWithPoint(ref nodePtr->value, eventPoint) <= 0)
                    {
                        currentNode = nodePtr->right;
                    } else
                    {
                        closestRight = currentNode;
                        currentNode = nodePtr->left;
                    }
                }
                return closestRight;
            }

            private void FindNewEvent(LineSegment2D left, LineSegment2D right, float2 eventPoint, ref NativeAVLTree<EventPoint, EventPointComparer> eventQueue)
            {

                if(ShapeIntersection.LineSegmentIntersection(right, left, out var intersection)
                    && (intersection.y > eventPoint.y
                    || (intersection.y == eventPoint.y && intersection.x > eventPoint.x)))
                {
                    var newEvent = new EventPoint()
                    {
                        point = intersection,
                        hash = -1,
                    };

                    if(!eventQueue.Contains(newEvent))
                    {
                        eventQueue.Insert(newEvent);
                    }
                }
            }

            public bool HandleEventPoint(EventPoint eventPoint, ref NativeList<LineSegment2D> faultySegments,
                ref NativeParallelMultiHashMap<int, LineSegment2D> lowerSegments,
                ref NativeList<LineSegment2D> upperSegments,
                ref NativeList<LineSegment2D> innerSegments,
                ref NativeList<SegmentTreeCode> treePositions,
                ref NativeAVLTree<LineSegment2D, SweepLineComparer> status,
                ref NativeAVLTree<EventPoint, EventPointComparer> eventQueue)
            {
                //var marker = new ProfilerMarker("Finding");


                bool wasAbleToRemove = true;

                this.FindSegmentsContainingPoint(eventPoint.point, innerSegments, upperSegments, status);


                for (int i = 0; i < upperSegments.Length; i++)
                {
                    var upperSegment = upperSegments[i];
                    if (!status.Remove(upperSegment))
                    {
                        wasAbleToRemove = false;
                        if (this.restart)
                        {
                            faultySegments.Add(upperSegment);
                        }
                    }
                }

                for(int i = 0; i < innerSegments.Length; i++)
                {
                    var innerSegment = innerSegments[i];
                    if (!status.Remove(innerSegment))
                    {
                        wasAbleToRemove = false;
                    }
                }

                //Update Sweepline
                var comparer = status.comparer;
                comparer.sweepPoint = eventPoint.point;
                status.comparer = comparer;

                int lowerSegmentsCount = 0;
                //Adding and resorting the relevant segments based on the new sweep position
                if (lowerSegments.TryGetFirstValue(eventPoint.hash, out var lowerSegment, out var it))  {

                    status.Insert(lowerSegment);
                    lowerSegmentsCount++;
                    while(lowerSegments.TryGetNextValue(out lowerSegment, ref it))
                    {
                        status.Insert(lowerSegment);
                        lowerSegmentsCount++;
                    }
                }

                for(int i = 0;i < innerSegments.Length; i++)
                {
                    var innerSegment = innerSegments[i];
                    status.Insert(innerSegment);
                }

                if (lowerSegmentsCount + upperSegments.Length + innerSegments.Length > 1)
                {
                    this.intersections.Add(eventPoint.point);
                }

                if (wasAbleToRemove)
                {
                    if(lowerSegmentsCount + innerSegments.Length == 0)
                    {
                        int leftSegmentIdx = this.GetSegmentToTheLeft(eventPoint.point, ref status);
                        int rightSegmentIdx = this.GetSegmentToTheRight(eventPoint.point, ref status);

                        if(leftSegmentIdx >= 0 && rightSegmentIdx >= 0)
                        {
                            LineSegment2D left = status.Elements[leftSegmentIdx].value;
                            LineSegment2D right = status.Elements[rightSegmentIdx].value;
                            this.FindNewEvent(left, right, eventPoint.point, ref eventQueue);
                        }

                    } else
                    {

                        this.FindLeftAndRightMost(eventPoint.hash, out var leftMost, out var rightMost, out var leftMostIdx, out var rightMostIdx,
                            ref lowerSegments, ref innerSegments, ref treePositions, ref status);

                        int toTheLeft = this.GetLeftNeighbour(leftMostIdx, ref status);
                        int toTheRight = this.GetRightNeighbour(rightMostIdx, ref status);

                        if(toTheLeft >= 0)
                        {
                            this.FindNewEvent(status.Elements[toTheLeft].value, leftMost, eventPoint.point, ref eventQueue);
                        }

                        if(toTheRight >= 0)
                        {
                            this.FindNewEvent(status.Elements[toTheRight].value, rightMost, eventPoint.point, ref eventQueue);
                        }
                    }
                }

                return wasAbleToRemove;
            }

            public void Restart(NativeList<LineSegment2D> faultySegments, 
                NativeList<int> allElements, 
                NativeList<LineSegment2D> uniqueSegments,
                NativeList<EventPoint> tempQueueCopy,
                ref NativeAVLTree<LineSegment2D, SweepLineComparer> status)
            {
                status.GetAllTreeElements(ref allElements, TreeTraversal.INORDER);
                for (int i = 0; i < allElements.Length; i++)
                {
                    var elem = status.Elements[allElements[i]];
                    if(!uniqueSegments.Contains(elem.value) && !faultySegments.Contains(elem.value))
                    {
                        uniqueSegments.Add(elem.value);
                    }
                }

                status.Clear();
                for(int i = 0; i < uniqueSegments.Length; i++)
                {
                    status.Insert(uniqueSegments[i]);
                }

                faultySegments.Clear();
                allElements.Clear();
                uniqueSegments.Clear();
                tempQueueCopy.Clear();
            }

            public EventPoint FetchNextEvent(ref NativeAVLTree<EventPoint, EventPointComparer> eventQueue)
            {
                int leftmostNode = eventQueue.GetLeftmostNode();
                var nextEvent = eventQueue.Elements[leftmostNode].value;

                //Special remove, to make really, really sure we remove the left element
                //This is because the sorting of three or more elements can come out of order for very small values
                //(e.g. a > b, b > c, c > a) -> floating point precision and stuff
                eventQueue.RemoveNode(leftmostNode);

                return nextEvent;
            }

            public void Execute()
            {


                this.intersections.Clear();

                var sweepComparer = new SweepLineComparer()
                {

                    epsilon = this.epsilon
                };

                var eventComparer = new EventPointComparer()
                {
                    //So - if we would have the same error rate in the event points and the sweep line, then it could happen that
                    //we are unlucky, and one point that is close to epsilon lower (in y) could come after an event point, messing
                    //up the detection of intersections or the finding of some segments...
                    //To be safe it is a whole magnitude smaller
                    epsilon = this.epsilon * 0.1f
                };

                var eventQueue = new NativeAVLTree<EventPoint, EventPointComparer>(eventComparer, Allocator.Temp);
                var status = new NativeAVLTree<LineSegment2D, SweepLineComparer>(sweepComparer, Allocator.Temp);

                var lowerSegments = new NativeParallelMultiHashMap<int, LineSegment2D>(this.segments.Length, Allocator.Temp);

                var upperSegments = new NativeList<LineSegment2D>(Allocator.Temp);
                var innerSegments = new NativeList<LineSegment2D>(Allocator.Temp);

                var treePositions = new NativeList<SegmentTreeCode>(Allocator.Temp);

                for (int i = 0; i < this.segments.Length; i++)
                {
                    this.InsertSegment(this.segments[i], i, lowerSegments, ref eventQueue);
                }

                var faultySegments = new NativeList<LineSegment2D>(Allocator.Temp);
                var allElements = new NativeList<int>(Allocator.Temp);

                //If you use the collections package with a version > 2.0, you can replace this with a HashSet
                //(however, right now this version is in preview only, and HashSet is not suitable for
                //single-threaded use)
                var uniqueSegments = new NativeList<LineSegment2D>(1, Allocator.Temp);
                var tempQueueCopy = new NativeList<EventPoint>(Allocator.Temp);

                while (!eventQueue.IsEmpty())
                {
                    var nextEvent = this.FetchNextEvent(ref eventQueue);
                    bool success = this.HandleEventPoint(nextEvent, ref faultySegments,
                        ref lowerSegments, ref upperSegments, ref innerSegments, ref treePositions,
                        ref status, ref eventQueue);

                    if (this.restart && !success)
                    {
                        this.Restart(faultySegments, allElements, uniqueSegments, tempQueueCopy,
                            ref status);
                    }

                    upperSegments.Clear();
                    innerSegments.Clear();
                    treePositions.Clear();
                }

            }
        }

    }
}
