using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;

namespace GimmeDOTSGeometry.Samples
{
    public class AllRadiusQuerySystem : MonoBehaviour
    {

        #region Public Fields

        public Gradient heatmap;

        public float yOffset = 0.01f;
        public float pointSize = 0.1f;
        public float ringThickness = 0.05f;
        public float allRadius = 0.5f;

        public int initialPointSize;
        public int maxHeat = 16;

        public GameObject point;

        public Material ringMaterial;
        public Material boundsMaterial;

        public Vector2 maxVelocity;

        public Rect bounds;

        #endregion

        #region Private Fields

        private bool useParallelQuery = false;
        private bool usePresortedQueue = false;

        private int positionCount = 0;
        private int batches = 512;

        private List<MeshRenderer> pointRenderers = new List<MeshRenderer>();
        private List<MeshRenderer> ringRenderers = new List<MeshRenderer>();
        private List<MeshFilter> ringFilter = new List<MeshFilter>();

        private MaterialPropertyBlock mpb;

        private NativeList<UnsafeList<int>> queryResults;
        private NativeArray<SpecialQueryJobs.ShapeEventPoint> presortedQueue;

        private NativeList<float2> positions;
        private NativeList<float2> velocities;

        private Sampler allRadiusQuerySampler = null;

        private TransformAccessArray pointsAccessArray;

        #endregion

        private static readonly string SHADER_COLOR = "_Color";
        private static readonly ProfilerMarker allRadiusQueryMarker = new ProfilerMarker("AllRadiusQuery");


        public bool IsUsingParallelQuery() => this.useParallelQuery;

        public bool IsUsingPresortedQueue() => this.usePresortedQueue;

        public Sampler GetAllRadiusQuerySampler() => this.allRadiusQuerySampler;

        public int GetNrOfPoints() => this.positionCount;

        public void EnableParallelQuery(bool enable)
        {
            this.useParallelQuery = enable;
        }

        private void CreateNewPresortedQueue()
        {
            this.presortedQueue.DisposeIfCreated();

            this.presortedQueue = new NativeArray<SpecialQueryJobs.ShapeEventPoint>(this.positionCount * 2, Allocator.Persistent);

            var presortJob = SpecialQuery.CreateSortedRadiusEventQueue(this.allRadius, this.positions, ref this.presortedQueue);
            presortJob.Complete();
        }

        public void EnablePresortedQueue(bool enable)
        {
            this.usePresortedQueue = enable;
            if(this.usePresortedQueue)
            {
                this.CreateNewPresortedQueue();
            }
        }

        public int GetBatches() => this.batches;

        public void SetBatches(int batches)
        {
            this.batches = batches;
        }

        void Start()
        {
            this.mpb = new MaterialPropertyBlock();
            Color c = this.heatmap.Evaluate(0.0f);
            this.mpb.SetColor(SHADER_COLOR, c);

            this.pointsAccessArray = new TransformAccessArray(this.initialPointSize);
            this.positions = new NativeList<float2>(this.initialPointSize, Allocator.Persistent);
            this.velocities = new NativeList<float2>(this.initialPointSize, Allocator.Persistent);
            this.queryResults = new NativeList<UnsafeList<int>>(this.initialPointSize, Allocator.Persistent);

            var boundsGo = new GameObject("Bounds");
            var mr = boundsGo.AddComponent<MeshRenderer>();
            var mf = boundsGo.AddComponent<MeshFilter>();

            mr.material = this.boundsMaterial;
            mf.mesh = MeshUtil.CreateRectangleOutline(this.bounds, 0.1f);

            var boundsGo2 = new GameObject("Bounds 2");
            mr = boundsGo2.AddComponent<MeshRenderer>();
            mf = boundsGo2.AddComponent<MeshFilter>();

            var outerRect = this.bounds;
            outerRect.Expand(0.5f);

            mr.material = this.boundsMaterial;
            mf.mesh = MeshUtil.CreateRectangleOutline(outerRect, 0.1f);


            this.AddPoints(this.initialPointSize);
        }

        public void SetAllRadius(float radius)
        {
            this.allRadius = radius;

            var mesh = MeshUtil.CreateRing(this.allRadius, this.ringThickness);
            for (int i = 0; i < this.ringFilter.Count; i++)
            {
                this.ringFilter[i].sharedMesh = mesh;
            }
        }

        public void AddPoints(int nrOfPoints)
        {
            this.positionCount += nrOfPoints;
            float pointHalf = this.pointSize / 2.0f;
            for(int i = 0; i < nrOfPoints; i++)
            {
                float rndX = UnityEngine.Random.Range(this.bounds.xMin + pointHalf, this.bounds.xMax - pointHalf);
                float rndY = UnityEngine.Random.Range(this.bounds.yMin + pointHalf, this.bounds.yMax - pointHalf);

                var worldPos = new Vector3(rndX, this.yOffset, rndY);

                var point = GameObject.Instantiate(this.point);
                point.transform.parent = this.transform;
                point.transform.position = worldPos;

                var meshRenderer = point.GetComponentInChildren<MeshRenderer>();
                this.pointRenderers.Add(meshRenderer);

                var ring = new GameObject($"Ring_{this.positionCount + i}");
                ring.transform.parent = point.transform;
                ring.transform.localPosition = Vector3.zero;

                meshRenderer = ring.AddComponent<MeshRenderer>();
                meshRenderer.material = this.ringMaterial;

                var meshFilter = ring.AddComponent<MeshFilter>();
                meshFilter.mesh = MeshUtil.CreateRing(this.allRadius, this.ringThickness);
                this.ringFilter.Add(meshFilter);
                this.ringRenderers.Add(meshRenderer);

                var pos2D = new Vector2(rndX, rndY);

                this.positions.Add(pos2D);

                float velX = UnityEngine.Random.Range(-this.maxVelocity.x, this.maxVelocity.x);
                float velY = UnityEngine.Random.Range(-this.maxVelocity.y, this.maxVelocity.y);

                this.velocities.Add(new float2(velX, velY));

                this.queryResults.Add(new UnsafeList<int>(32, Allocator.Persistent));

                this.pointsAccessArray.Add(point.transform);
            }

            //Queue size changes, new points -> sort again
            this.CreateNewPresortedQueue();
        }


        [BurstCompile]
        private struct UpdatePointsJob : IJobParallelForTransform
        {
            public Rect bounds;

            public float deltaTime;
            public float pointSize;

            [NoAlias]
            public NativeArray<float2> positions;

            [NoAlias]
            public NativeArray<float2> velocities;

            public void Execute(int index, TransformAccess transform)
            {
                var pos = this.positions[index];

                var velocity = this.velocities[index];

                float nextPosX = math.mad(velocity.x, this.deltaTime, pos.x);
                float nextPosY = math.mad(velocity.y, this.deltaTime, pos.y);

                float xMax = this.bounds.xMax - this.pointSize;
                float xMin = this.bounds.xMin + this.pointSize;
                float yMax = this.bounds.yMax - this.pointSize;
                float yMin = this.bounds.yMin + this.pointSize;

                if (Hint.Unlikely(nextPosX > xMax))
                {
                    velocity = math.reflect(velocity, new float2(-1.0f, 0.0f));
                    nextPosX -= (nextPosX - xMax) * 2.0f;
                }
                else if (Hint.Unlikely(nextPosX < xMin))
                {
                    velocity = math.reflect(velocity, new float2(1.0f, 0.0f));
                    nextPosX += (xMin - nextPosX) * 2.0f;
                }

                if (Hint.Unlikely(nextPosY > yMax))
                {
                    velocity = math.reflect(velocity, new float2(0.0f, -1.0f));
                    nextPosY -= (nextPosY - yMax) * 2.0f;
                }
                else if (Hint.Unlikely(nextPosY < yMin))
                {
                    velocity = math.reflect(velocity, new float2(0.0f, 1.0f));
                    nextPosY += (yMin - nextPosY) * 2.0f;
                }

                this.velocities[index] = velocity;
                this.positions[index] = new float2(nextPosX, nextPosY);

                transform.position = new Vector3(nextPosX, transform.position.y, nextPosY);
            }
        }

        private void UpdateColor()
        {

            for(int i = 0; i < this.queryResults.Length; i++)
            {
                var list = this.queryResults[i];
                float percent = list.Length / (float)maxHeat;
                var color = this.heatmap.Evaluate(percent);

                this.mpb.SetColor(SHADER_COLOR, color);
                this.pointRenderers[i].SetPropertyBlock(this.mpb);
                this.ringRenderers[i].SetPropertyBlock(this.mpb);

            }
        }

        void Update()
        {
            var updatePointsJob = new UpdatePointsJob()
            {
                bounds = this.bounds,
                deltaTime = Time.deltaTime,
                pointSize = this.pointSize,
                positions = this.positions.AsArray(),
                velocities = this.velocities.AsArray(),
            };

            updatePointsJob.Schedule(this.pointsAccessArray).Complete();

            if (this.useParallelQuery)
            {
                allRadiusQueryMarker.Begin();

                if (this.usePresortedQueue)
                {
                    var presortedAllRadiusParallelQueryJob = SpecialQuery.PresortedAllRadiusParallelQuery(this.allRadius,
                        this.positions.AsArray(), ref this.queryResults, ref this.presortedQueue, this.batches);
                    presortedAllRadiusParallelQueryJob.Complete();
                }
                else
                {
                    var allRadiusParallelQueryJob = SpecialQuery.AllRadiusParallelQuery(this.allRadius,
                        this.positions.AsArray(), ref this.queryResults, this.batches);
                    allRadiusParallelQueryJob.Complete();
                }

                allRadiusQueryMarker.End();
            }
            else
            {
                allRadiusQueryMarker.Begin();

                var allRadiusQueryJob = SpecialQuery.AllRadiusQuery(this.allRadius, this.positions.AsArray(), ref this.queryResults);
                allRadiusQueryJob.Complete();

                allRadiusQueryMarker.End();
            }

            this.UpdateColor();

            if(this.allRadiusQuerySampler == null || !this.allRadiusQuerySampler.isValid)
            {
                this.allRadiusQuerySampler = Sampler.Get("AllRadiusQuery");
            }
        }



        private void Dispose()
        {
            this.velocities.DisposeIfCreated();
            this.positions.DisposeIfCreated();
            this.presortedQueue.DisposeIfCreated();
            if(this.pointsAccessArray.isCreated)
            {
                this.pointsAccessArray.Dispose();
            }
            if(this.queryResults.IsCreated)
            {
                for(int i = 0; i < this.queryResults.Length; i++)
                {
                    var list = this.queryResults[i];
                    list.Dispose();
                }
                this.queryResults.Dispose();
            }
        }

        private void OnDestroy()
        {
            this.Dispose();
        }
    }
}
