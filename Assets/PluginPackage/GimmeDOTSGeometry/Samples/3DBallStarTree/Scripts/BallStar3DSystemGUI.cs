using UnityEngine;

namespace GimmeDOTSGeometry.Samples
{
    public class BallStar3DSystemGUI : SystemGUI
    {
        public BallStar3DSystem system;

        private Vector3 rayStart;
        private Vector3 rayEnd;

        private void OnEnable()
        {
            var boundsMin = this.system.bounds.min;
            var boundsMax = this.system.bounds.max;

            this.rayStart = boundsMin;
            this.rayEnd = boundsMax;
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            var areaRect = new Rect(0, 0, 350, 900);
            GUI.Box(areaRect, string.Empty);
            GUILayout.BeginArea(areaRect);
            GUI.DrawTexture(areaRect, this.background, ScaleMode.StretchToFill);
            GUILayout.Box("Ball*-Tree 3D GUI");
            GUILayout.Label($"Current Spheres: {this.system.GetNrOfSpheres()}", this.textStyle);

            if (this.system.GetUpdateSphereSampler() != null)
            {
                var sampler = this.system.GetUpdateSphereSampler();
                var recorder = sampler.GetRecorder();

                if (recorder != null)
                {
                    GUILayout.Label($"Syncing Spheres (ms): {recorder.elapsedNanoseconds / 10e5f}", this.textStyle);
                }
            }

            if (this.system.GetOptimizeSampler() != null)
            {
                var sampler = this.system.GetOptimizeSampler();
                var recorder = sampler.GetRecorder();

                if (recorder != null)
                {
                    GUILayout.Label($"Optimize (ms): {recorder.elapsedNanoseconds / 10e5f}", this.textStyle);
                }
            }

            if (this.system.GetRadiusQuerySampler() != null)
            {
                var sampler = this.system.GetRadiusQuerySampler();
                var recorder = sampler.GetRecorder();

                if (recorder != null)
                {
                    GUILayout.Label($"Search (ms): {recorder.elapsedNanoseconds / 10e5f}", this.textStyle);
                }
            }

            if (this.GUIButton("Toggle Trails"))
            {
                this.system.ToggleTrailRenderers();
            }

            float oldValue = this.system.searchRadius;
            this.system.searchRadius = GUILayout.HorizontalSlider(this.system.searchRadius, 0.1f, 10.0f);
            if(this.system.searchRadius != oldValue)
            {
                this.system.UpdateSearchRingRadius();
                this.system.UpdateSearchBounds();
            }

            if(this.system.IsShowingQuery())
            {
                if(this.GUIButton("Hide Query?"))
                {
                    this.system.ShowQuery(false);
                }
            } else
            {
                if(this.GUIButton("Show Query?"))
                {
                    this.system.ShowQuery(true);
                }
            }

            if (this.system.IsDoingMultiQuery())
            {
                if (this.GUIButton("Do Mono-Query?"))
                {
                    this.system.EnableMultiQuery(false);
                }
            }
            else
            {
                if (this.GUIButton("Do Multi-Query?"))
                {
                    this.system.EnableMultiQuery(true);
                }
            }

            if(this.system.IsDoingBoundsQuery())
            {
                if(this.GUIButton("Do Radius Query?"))
                {
                    this.system.EnableBoundsQuery(false);
                }
            } else
            {
                if(this.GUIButton("Do Bounds Query?"))
                {
                    this.system.EnableBoundsQuery(true);
                }
            }

            if(this.system.IsDoingOverlappingQuery())
            {
                if(this.GUIButton("Without Overlap"))
                {
                    this.system.EnableOverlappingQuery(false);
                }
            }
            else
            {
                if(this.GUIButton("With Overlap"))
                {
                    this.system.EnableOverlappingQuery(true);
                }
            }

            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical();
            if (this.GUIButton("Add 1 Sphere"))
            {
                this.system.AddRandomSpheres(1);
            }

            if (this.GUIButton("Add 10 Spheres"))
            {
                this.system.AddRandomSpheres(10);
            }

            if (this.GUIButton("Add 100 Spheres"))
            {
                this.system.AddRandomSpheres(100);
            }

            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            if (this.GUIButton("Remove 1 Sphere"))
            {
                this.system.RemoveRandomSpheres(1);
            }

            if (this.GUIButton("Remove 10 Spheres"))
            {
                this.system.RemoveRandomSpheres(10);
            }

            if (this.GUIButton("Remove 100 Spheres"))
            {
                this.system.RemoveRandomSpheres(100);
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            if (this.system.IsDoingAttractorMovement())
            {
                if(this.GUIButton("Disable Attractor"))
                {
                    this.system.EnableAttractor(false);
                }

                GUILayout.Label("Attractor Strength");
                this.system.attractorStrength = GUILayout.HorizontalSlider(this.system.attractorStrength, 0.0f, 1.0f);
                GUILayout.Label("Attractor A");
                this.system.attractorA = GUILayout.HorizontalSlider(this.system.attractorA, -10.0f, 10.0f);
                GUILayout.Label("Attractor B");
                this.system.attractorB = GUILayout.HorizontalSlider(this.system.attractorB, -10.0f, 10.0f);
                GUILayout.Label("Attractor F");
                this.system.attractorF = GUILayout.HorizontalSlider(this.system.attractorF, -10.0f, 10.0f);
                GUILayout.Label("Attractor G");
                this.system.attractorG = GUILayout.HorizontalSlider(this.system.attractorG, -10.0f, 10.0f);

            } else
            {
                if(this.GUIButton("Enable Attractor"))
                {
                    this.system.EnableAttractor(true);
                }
            }

            if(this.system.IsDoingFrustumQuery())
            {
                if(this.GUIButton("Stop Frustum Query"))
                {
                    this.system.EnableFrustumQuery(false);
                }

                if (this.system.GetFrustumQuerySampler() != null)
                {
                    var sampler = this.system.GetFrustumQuerySampler();
                    var recorder = sampler.GetRecorder();

                    if (recorder != null)
                    {
                        GUILayout.Label($"Frustum Query (ms): {recorder.elapsedNanoseconds / 10e5f}", this.textStyle);
                    }
                }

            } else
            {
                if(this.GUIButton("Do Frustum Query"))
                {
                    this.system.EnableFrustumQuery(true);
                }

            }

            if(this.system.IsDoingRaycast())
            {

                if(this.GUIButton("Stop Raycast"))
                {
                    this.system.EnableRaycast(false);
                }

                var raycastType = this.system.CurrentRaycastType();
                if (raycastType == BallStar3DSystem.RaycastType.SPHERE)
                {
                    if (this.GUIButton("Line"))
                    {
                        this.system.SetRaycastType(BallStar3DSystem.RaycastType.LINE);
                    }
                } else
                {
                    if(this.GUIButton("Sphere"))
                    {
                        this.system.SetRaycastType(BallStar3DSystem.RaycastType.SPHERE);
                    }
                }

                var boundsMin = this.system.bounds.min;
                var boundsMax = this.system.bounds.max;

                GUILayout.Space(10.0f);
                GUILayout.Label("Ray Origin");
                this.rayStart.x = GUILayout.HorizontalSlider(this.rayStart.x, boundsMin.x, this.rayEnd.x);
                this.rayStart.y = GUILayout.HorizontalSlider(this.rayStart.y, boundsMin.y, this.rayEnd.y);
                this.rayStart.z = GUILayout.HorizontalSlider(this.rayStart.z, boundsMin.z, this.rayEnd.z);
                GUILayout.Label("Ray End");
                this.rayEnd.x = GUILayout.HorizontalSlider(this.rayEnd.x, this.rayStart.x, boundsMax.x);
                this.rayEnd.y = GUILayout.HorizontalSlider(this.rayEnd.y, this.rayStart.y, boundsMax.y);
                this.rayEnd.z = GUILayout.HorizontalSlider(this.rayEnd.z, this.rayStart.z, boundsMax.z);

                this.system.SetRaycastParameters(this.rayStart, this.rayEnd);

                if(raycastType == BallStar3DSystem.RaycastType.SPHERE)
                {
                    GUILayout.Label("Sphere Cast Radius");
                    this.system.sphereCastRadius = GUILayout.HorizontalSlider(this.system.sphereCastRadius,
                        this.system.sphereCastRadiusRange.x, this.system.sphereCastRadiusRange.y);
                }

                GUILayout.Space(5.0f);


                if(this.system.GetRaycastSampler() != null)
                {
                    var sampler = this.system.GetRaycastSampler();
                    var recorder = sampler.GetRecorder();

                    if(recorder != null)
                    {
                        GUILayout.Label($"Raycast (ms): {recorder.elapsedNanoseconds / 10e5f}", this.textStyle);
                    }
                }

            } else
            {
                if(this.GUIButton("Start Raycast"))
                {
                    this.system.EnableRaycast(true);
                }
            }

            GUILayout.EndArea();
        }
    }
}
