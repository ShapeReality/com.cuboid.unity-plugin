// Adapted from
// https://github.com/yasirkula/UnityRuntimePreviewGenerator
// 
// MIT License
// 
// Copyright (c) 2017 Süleyman Yasir KULA
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Cuboid.UnityPlugin.Editor
{
    public static class ThumbnailRenderer
    {
        private class CameraSetup
        {
            private Vector3 position;
            private Quaternion rotation;

            private Color backgroundColor;
            private bool orthographic;
            private float orthographicSize;
            private float nearClipPlane;
            private float farClipPlane;
            private float aspect;
            private int cullingMask;
            private CameraClearFlags clearFlags;

            private RenderTexture targetTexture;

            public void GetSetup(Camera camera)
            {
                position = camera.transform.position;
                rotation = camera.transform.rotation;

                backgroundColor = camera.backgroundColor;
                orthographic = camera.orthographic;
                orthographicSize = camera.orthographicSize;
                nearClipPlane = camera.nearClipPlane;
                farClipPlane = camera.farClipPlane;
                aspect = camera.aspect;
                cullingMask = camera.cullingMask;
                clearFlags = camera.clearFlags;

                targetTexture = camera.targetTexture;
            }

            public void ApplySetup(Camera camera)
            {
                camera.transform.SetPositionAndRotation(position, rotation);

                camera.backgroundColor = backgroundColor;
                camera.orthographic = orthographic;
                camera.orthographicSize = orthographicSize;
                camera.aspect = aspect;
                camera.cullingMask = cullingMask;
                camera.clearFlags = clearFlags;

                // Assigning order or nearClipPlane and farClipPlane may matter because Unity clamps near to far and far to near
                if (nearClipPlane < camera.farClipPlane)
                {
                    camera.nearClipPlane = nearClipPlane;
                    camera.farClipPlane = farClipPlane;
                }
                else
                {
                    camera.farClipPlane = farClipPlane;
                    camera.nearClipPlane = nearClipPlane;
                }

                camera.targetTexture = targetTexture;
                targetTexture = null;
            }
        }

        private const string k_SkyboxMaterialPath = "Materials/M_Skybox";
        private const string k_CameraRigPrefabPath = "Prefabs/CameraRig";
        private const string k_PipelineSettingsPath = "Settings/URP_PipelineSettings";
        private const string k_RendererPath = "Settings/URP_Renderer";
        private const string k_InternalCameraName = "Cuboid_ThumbnaiRenderer_InternalCamera";

        private const int k_PreviewLayerIndex = 0;
        private static Vector3 k_PreviewPosition = new Vector3(-250f, -250f, -250f);

        private static Camera renderCamera;
        private static readonly CameraSetup cameraSetup = new CameraSetup();

        private static readonly Vector3[] boundingBoxPoints = new Vector3[8];
        private static readonly Vector3[] localBoundsMinMax = new Vector3[2];

        private static readonly List<Renderer> renderersList = new List<Renderer>(64);
        private static readonly List<int> layersList = new List<int>(64);

        private static Camera _internalCamera = null;
        private static Camera InternalCamera
        {
            get
            {
                _internalCamera = null;
                if (_internalCamera == null)
                {
                    // first try to find a GameObject with the k_InternalCameraName and destroy it
                    GameObject go = GameObject.Find(k_InternalCameraName);
                    if (go != null)
                    {
                        GameObject.DestroyImmediate(go);
                    }

                    // then, create a new one
                    GameObject prefab = Resources.Load<GameObject>(k_CameraRigPrefabPath);
                    if (prefab == null)
                    {
                        throw new Exception($"Camera Rig Prefab not found at {k_CameraRigPrefabPath}");
                    }

                    GameObject cameraRig = GameObject.Instantiate(prefab, null, false);
                    cameraRig.name = k_InternalCameraName;
                    Camera camera = cameraRig.GetComponentInChildren<Camera>();
                    if (camera == null)
                    {
                        throw new Exception("Camera Rig Prefab does not contain Camera component");
                    }
                    _internalCamera = camera;

                    _internalCamera.enabled = false;
                    _internalCamera = camera;
                    _internalCamera.nearClipPlane = 0.01f;
                    _internalCamera.cullingMask = 1 << k_PreviewLayerIndex;
                    _internalCamera.gameObject.hideFlags = HideFlags.HideAndDontSave;
                }

                return _internalCamera;
            }
        }

        private static Camera _previewRenderCamera;
        public static Camera PreviewRenderCamera
        {
            get => _previewRenderCamera;
            set => _previewRenderCamera = value;
        }

        private static Vector3 _previewDirection = new Vector3(-0.57735f, -0.57735f, -0.57735f); // Normalized (-1,-1,-1)
        public static Vector3 PreviewDirection
        {
            get => _previewDirection;
            set => _previewDirection = value.normalized;
        }

        private static float _padding;
        public static float Padding
        {
            get => _padding;
            set => _padding = Mathf.Clamp(value, -0.25f, 0.25f);
        }

        private static Color _backgroundColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        public static Color BackgroundColor
        {
            get => _backgroundColor;
            set => _backgroundColor = value;
        }

        private static bool _orthographicMode = false;
        public static bool OrthographicMode
        {
            get => _orthographicMode;
            set => _orthographicMode = value;
        }

        private static bool _useLocalBounds = false;
        public static bool UseLocalBounds
        {
            get => _useLocalBounds;
            set => _useLocalBounds = value;
        }

        private static float _renderSupersampling = 1f;
        public static float RenderSupersampling
        {
            get => _renderSupersampling;
            set => _renderSupersampling = Mathf.Max(value, 0.1f);
        }

        private static bool _markTextureNonReadable = true;
        public static bool MarkTextureNonReadable
        {
            get => _markTextureNonReadable;
            set => _markTextureNonReadable = value;
        }

        public static Texture2D GenerateModelPreview(GameObject gameObject, int width = 64, int height = 64, bool shouldIgnoreParticleSystems = true)
        {
            return GenerateModelPreviewInternal(gameObject, null, width, height, shouldIgnoreParticleSystems);
        }

        public static void GenerateModelPreviewAsync(Action<Texture2D> callback, GameObject gameObject, int width = 64, int height = 64, bool shouldIgnoreParticleSystems = true)
        {
            GenerateModelPreviewInternal(gameObject, null, width, height, shouldIgnoreParticleSystems, callback);
        }

        private static float _storedAmbientIntensity = 1.0f;
        private static AmbientMode _storedAmbientMode = AmbientMode.Skybox;
        private static Material _storedSkybox = null;
        private static UniversalRenderPipelineAsset _storedPipelineSettings;

        /// <summary>
        /// Function that stores the scene settings, so that they can be set again when
        /// the <see cref="ThumbnailRenderer"/> is done rendering the thumbnails.
        ///
        /// This is made separate from the GenerateModelPreviewInternal to support
        /// batch image rendering without having to store and load these settings
        /// separately for each object. 
        /// </summary>
        private static void StoreSceneSettings()
        {
            _storedAmbientIntensity = RenderSettings.ambientIntensity;
            _storedAmbientMode = RenderSettings.ambientMode;
            _storedSkybox = RenderSettings.skybox;
            _storedPipelineSettings = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        }

        private static void LoadSceneSettings()
        {
            RenderSettings.ambientIntensity = _storedAmbientIntensity;
            RenderSettings.ambientMode = _storedAmbientMode;
            RenderSettings.skybox = _storedSkybox;
            if (_storedPipelineSettings != null)
            {
                GraphicsSettings.defaultRenderPipeline = _storedPipelineSettings;
            }
        }

        private static Texture2D GenerateModelPreviewInternal(GameObject gameObject, string replacementTag, int width, int height, bool shouldIgnoreParticleSystems, Action<Texture2D> asyncCallback = null)
        {
            if (gameObject == null)
            {
                asyncCallback?.Invoke(null); return null;
            }

            StoreSceneSettings();

            Material skyboxMaterial = Resources.Load<Material>(k_SkyboxMaterialPath);
            RenderSettings.skybox = skyboxMaterial;
            RenderSettings.ambientMode = AmbientMode.Skybox;
            DynamicGI.UpdateEnvironment();
            RenderSettings.ambientIntensity = 1.0f;

            UniversalRenderPipelineAsset pipelineSettings = Resources.Load<UniversalRenderPipelineAsset>(k_PipelineSettingsPath);
            GraphicsSettings.defaultRenderPipeline = pipelineSettings;

            Texture2D result = null;

            // instantiate the object
            GameObject previewObject = GameObject.Instantiate(gameObject, null, false);
            Transform previewObjectTransform = previewObject.transform;
            previewObject.hideFlags = HideFlags.HideAndDontSave;

            bool asyncOperationStarted = false;

            try
            {
                SetupCamera();
                SetLayerRecursively(previewObjectTransform);
                previewObjectTransform.SetPositionAndRotation(k_PreviewPosition, Quaternion.identity);

                Quaternion cameraRotation = Quaternion.LookRotation(previewObject.transform.rotation * _previewDirection, previewObject.transform.up);
                Bounds previewBounds = new Bounds();
                if (!CalculateBounds(previewObject.transform, shouldIgnoreParticleSystems, cameraRotation, out previewBounds))
                {
                    LoadSceneSettings(); asyncCallback?.Invoke(null); return null;
                }

                renderCamera.aspect = (float)width / height;
                renderCamera.transform.rotation = cameraRotation;

                CalculateCameraPosition(renderCamera, previewBounds, _padding);

                renderCamera.farClipPlane = (renderCamera.transform.position - previewBounds.center).magnitude + (_useLocalBounds ? (previewBounds.extents.z * 1.01f) : previewBounds.size.magnitude);

                RenderTexture activeRenderTexture = RenderTexture.active;
                RenderTexture renderTexture = null;
                try
                {
                    int supersampledWidth = Mathf.RoundToInt(width * _renderSupersampling);
                    int supersampledHeight = Mathf.RoundToInt(height * _renderSupersampling);

                    renderTexture = RenderTexture.GetTemporary(supersampledWidth, supersampledHeight, 16);
                    RenderTexture.active = renderTexture;
                    if (_backgroundColor.a < 1f)
                    {
                        GL.Clear(true, true, _backgroundColor);
                    }

                    renderCamera.targetTexture = renderTexture;
                    renderCamera.Render();
                    renderCamera.targetTexture = null;

                    if (supersampledWidth != width || supersampledHeight != height)
                    {
                        RenderTexture _renderTexture = null;
                        try
                        {
                            _renderTexture = RenderTexture.GetTemporary(width, height, 16);
                            RenderTexture.active = _renderTexture;
                            if (_backgroundColor.a < 1f)
                            {
                                GL.Clear(true, true, _backgroundColor);
                            }
                            Graphics.Blit(renderTexture, _renderTexture);
                        }
                        finally
                        {
                            if (_renderTexture)
                            {
                                RenderTexture.ReleaseTemporary(renderTexture);
                                renderTexture = _renderTexture;
                            }
                        }
                    }

                    if (asyncCallback != null)
                    {
                        AsyncGPUReadback.Request(renderTexture, 0, _backgroundColor.a < 1f ? TextureFormat.RGBA32 : TextureFormat.RGB24, (asyncResult) =>
                        {
                            try
                            {
                                result = new Texture2D(width, height, _backgroundColor.a < 1f ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
                                if (!asyncResult.hasError)
                                {
                                    result.LoadRawTextureData(asyncResult.GetData<byte>());
                                }
                                else
                                {
                                    Debug.LogWarning("Async thumbnail request failed, falling back to conventional method");

                                    RenderTexture _activeRT = RenderTexture.active;
                                    try
                                    {
                                        RenderTexture.active = renderTexture;
                                        result.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                                    }
                                    finally
                                    {
                                        RenderTexture.active = _activeRT;
                                    }
                                }

                                result.Apply(false, _markTextureNonReadable);
                                asyncCallback(result);
                            }
                            finally
                            {
                                if (renderTexture) { RenderTexture.ReleaseTemporary(renderTexture); }
                            }
                        });

                        asyncOperationStarted = true;
                    }
                    else
                    {
                        result = new Texture2D(width, height, _backgroundColor.a < 1f ? TextureFormat.RGBA32 : TextureFormat.RGB24, false);
                        result.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                        result.Apply(false, _markTextureNonReadable);
                    }
                }
                finally
                {
                    RenderTexture.active = activeRenderTexture;
                    if (renderTexture && !asyncOperationStarted)
                    {
                        RenderTexture.ReleaseTemporary(renderTexture);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            finally
            {
                if (previewObject != null)
                {
                    Object.DestroyImmediate(previewObject);
                }

                if (renderCamera == _previewRenderCamera)
                {
                    cameraSetup.ApplySetup(renderCamera);
                }
            }

            if (!asyncOperationStarted && asyncCallback != null)
            {
                asyncCallback(null);
            }
            LoadSceneSettings();
            return result;
        }

        // Calculates AABB bounds of the target object (AABB will include its child objects)
        public static bool CalculateBounds(Transform target, bool shouldIgnoreParticleSystems, Quaternion cameraRotation, out Bounds bounds)
        {
            renderersList.Clear();
            target.GetComponentsInChildren(renderersList);

            Quaternion inverseCameraRotation = Quaternion.Inverse(cameraRotation);
            Vector3 localBoundsMin = new Vector3(float.MaxValue - 1f, float.MaxValue - 1f, float.MaxValue - 1f);
            Vector3 localBoundsMax = new Vector3(float.MinValue + 1f, float.MinValue + 1f, float.MinValue + 1f);

            bounds = new Bounds();
            bool hasBounds = false;
            for (int i = 0; i < renderersList.Count; i++)
            {
                if (!renderersList[i].enabled) { continue; }
                if (shouldIgnoreParticleSystems && renderersList[i] is ParticleSystemRenderer) { continue; }

                // Local bounds calculation code taken from: https://github.com/Unity-Technologies/UnityCsReference/blob/0355e09029fa1212b7f2e821f41565df8e8814c7/Editor/Mono/InternalEditorUtility.bindings.cs#L710
                if (_useLocalBounds)
                {
                    Bounds localBounds = renderersList[i].localBounds;

                    Transform transform = renderersList[i].transform;
                    localBoundsMinMax[0] = localBounds.min;
                    localBoundsMinMax[1] = localBounds.max;

                    for (int x = 0; x < 2; x++)
                    {
                        for (int y = 0; y < 2; y++)
                        {
                            for (int z = 0; z < 2; z++)
                            {
                                Vector3 point = inverseCameraRotation * transform.TransformPoint(new Vector3(localBoundsMinMax[x].x, localBoundsMinMax[y].y, localBoundsMinMax[z].z));
                                localBoundsMin = Vector3.Min(localBoundsMin, point);
                                localBoundsMax = Vector3.Max(localBoundsMax, point);
                            }
                        }
                    }

                    hasBounds = true;
                }
                else if (!hasBounds)
                {
                    bounds = renderersList[i].bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderersList[i].bounds);
                }
            }

            if (_useLocalBounds && hasBounds)
            {
                bounds = new Bounds(cameraRotation * ((localBoundsMin + localBoundsMax) * 0.5f), localBoundsMax - localBoundsMin);
            }

            return hasBounds;
        }

        // Moves camera in a way such that it will encapsulate bounds perfectly
        public static void CalculateCameraPosition(Camera camera, Bounds bounds, float padding = 0f)
        {
            Transform cameraTR = camera.transform;

            Vector3 cameraDirection = cameraTR.forward;
            float aspect = camera.aspect;

            if (padding != 0f)
                bounds.size *= 1f + padding * 2f; // Padding applied to both edges, hence multiplied by 2

            Vector3 boundsCenter = bounds.center;
            Vector3 boundsExtents = bounds.extents;
            Vector3 boundsSize = 2f * boundsExtents;

            // Calculate corner points of the Bounds
            if (_useLocalBounds)
            {
                Matrix4x4 localBoundsMatrix = Matrix4x4.TRS(boundsCenter, camera.transform.rotation, Vector3.one);
                Vector3 point = boundsExtents;
                boundingBoxPoints[0] = localBoundsMatrix.MultiplyPoint3x4(point);
                point.x -= boundsSize.x;
                boundingBoxPoints[1] = localBoundsMatrix.MultiplyPoint3x4(point);
                point.y -= boundsSize.y;
                boundingBoxPoints[2] = localBoundsMatrix.MultiplyPoint3x4(point);
                point.x += boundsSize.x;
                boundingBoxPoints[3] = localBoundsMatrix.MultiplyPoint3x4(point);
                point.z -= boundsSize.z;
                boundingBoxPoints[4] = localBoundsMatrix.MultiplyPoint3x4(point);
                point.x -= boundsSize.x;
                boundingBoxPoints[5] = localBoundsMatrix.MultiplyPoint3x4(point);
                point.y += boundsSize.y;
                boundingBoxPoints[6] = localBoundsMatrix.MultiplyPoint3x4(point);
                point.x += boundsSize.x;
                boundingBoxPoints[7] = localBoundsMatrix.MultiplyPoint3x4(point);
            }
            else
            {
                Vector3 point = boundsCenter + boundsExtents;
                boundingBoxPoints[0] = point;
                point.x -= boundsSize.x;
                boundingBoxPoints[1] = point;
                point.y -= boundsSize.y;
                boundingBoxPoints[2] = point;
                point.x += boundsSize.x;
                boundingBoxPoints[3] = point;
                point.z -= boundsSize.z;
                boundingBoxPoints[4] = point;
                point.x -= boundsSize.x;
                boundingBoxPoints[5] = point;
                point.y += boundsSize.y;
                boundingBoxPoints[6] = point;
                point.x += boundsSize.x;
                boundingBoxPoints[7] = point;
            }

            if (camera.orthographic)
            {
                cameraTR.position = boundsCenter;

                float minX = float.PositiveInfinity, minY = float.PositiveInfinity;
                float maxX = float.NegativeInfinity, maxY = float.NegativeInfinity;

                for (int i = 0; i < boundingBoxPoints.Length; i++)
                {
                    Vector3 localPoint = cameraTR.InverseTransformPoint(boundingBoxPoints[i]);
                    if (localPoint.x < minX) { minX = localPoint.x; }
                    if (localPoint.x > maxX) { maxX = localPoint.x; }
                    if (localPoint.y < minY) { minY = localPoint.y; }
                    if (localPoint.y > maxY) { maxY = localPoint.y; }
                }

                float distance = boundsExtents.magnitude + 1f;
                camera.orthographicSize = Mathf.Max(maxY - minY, (maxX - minX) / aspect) * 0.5f;
                cameraTR.position = boundsCenter - cameraDirection * distance;
            }
            else
            {
                Vector3 cameraUp = cameraTR.up, cameraRight = cameraTR.right;

                float verticalFOV = camera.fieldOfView * 0.5f;
                float horizontalFOV = Mathf.Atan(Mathf.Tan(verticalFOV * Mathf.Deg2Rad) * aspect) * Mathf.Rad2Deg;

                // Normals of the camera's frustum planes
                Vector3 topFrustumPlaneNormal = Quaternion.AngleAxis(90f + verticalFOV, -cameraRight) * cameraDirection;
                Vector3 bottomFrustumPlaneNormal = Quaternion.AngleAxis(90f + verticalFOV, cameraRight) * cameraDirection;
                Vector3 rightFrustumPlaneNormal = Quaternion.AngleAxis(90f + horizontalFOV, cameraUp) * cameraDirection;
                Vector3 leftFrustumPlaneNormal = Quaternion.AngleAxis(90f + horizontalFOV, -cameraUp) * cameraDirection;

                // Credit for algorithm: https://stackoverflow.com/a/66113254/2373034
                // 1. Find edge points of the bounds using the camera's frustum planes
                // 2. Create a plane for each edge point that goes through the point and has the corresponding frustum plane's normal
                // 3. Find the intersection line of horizontal edge points' planes (horizontalIntersection) and vertical edge points' planes (verticalIntersection)
                //    If we move the camera along horizontalIntersection, the bounds will always with the camera's width perfectly (similar effect goes for verticalIntersection)
                // 4. Find the closest line segment between these two lines (horizontalIntersection and verticalIntersection) and place the camera at the farthest point on that line
                int leftmostPoint = -1, rightmostPoint = -1, topmostPoint = -1, bottommostPoint = -1;
                for (int i = 0; i < boundingBoxPoints.Length; i++)
                {
                    if (leftmostPoint < 0 && IsOutermostPointInDirection(i, leftFrustumPlaneNormal)) { leftmostPoint = i; }
                    if (rightmostPoint < 0 && IsOutermostPointInDirection(i, rightFrustumPlaneNormal)) { rightmostPoint = i; }
                    if (topmostPoint < 0 && IsOutermostPointInDirection(i, topFrustumPlaneNormal)) { topmostPoint = i; }
                    if (bottommostPoint < 0 && IsOutermostPointInDirection(i, bottomFrustumPlaneNormal)) { bottommostPoint = i; }
                }

                Ray horizontalIntersection = GetPlanesIntersection(new Plane(leftFrustumPlaneNormal, boundingBoxPoints[leftmostPoint]), new Plane(rightFrustumPlaneNormal, boundingBoxPoints[rightmostPoint]));
                Ray verticalIntersection = GetPlanesIntersection(new Plane(topFrustumPlaneNormal, boundingBoxPoints[topmostPoint]), new Plane(bottomFrustumPlaneNormal, boundingBoxPoints[bottommostPoint]));

                Vector3 closestPoint1, closestPoint2;
                FindClosestPointsOnTwoLines(horizontalIntersection, verticalIntersection, out closestPoint1, out closestPoint2);

                cameraTR.position = Vector3.Dot(closestPoint1 - closestPoint2, cameraDirection) < 0 ? closestPoint1 : closestPoint2;
            }
        }

        // Returns whether or not the given point is the outermost point in the given direction among all points of the bounds
        private static bool IsOutermostPointInDirection(int pointIndex, Vector3 direction)
        {
            Vector3 point = boundingBoxPoints[pointIndex];
            for (int i = 0; i < boundingBoxPoints.Length; i++)
            {
                if (i != pointIndex && Vector3.Dot(direction, boundingBoxPoints[i] - point) > 0)
                {
                    return false;
                }
            }
            return true;
        }

        // Credit: https://stackoverflow.com/a/32410473/2373034
        // Returns the intersection line of the 2 planes
        private static Ray GetPlanesIntersection(Plane p1, Plane p2)
        {
            Vector3 p3Normal = Vector3.Cross(p1.normal, p2.normal);
            float det = p3Normal.sqrMagnitude;

            return new Ray(((Vector3.Cross(p3Normal, p2.normal) * p1.distance) + (Vector3.Cross(p1.normal, p3Normal) * p2.distance)) / det, p3Normal);
        }

        // Credit: http://wiki.unity3d.com/index.php/3d_Math_functions
        // Returns the edge points of the closest line segment between 2 lines
        private static void FindClosestPointsOnTwoLines(Ray line1, Ray line2, out Vector3 closestPointLine1, out Vector3 closestPointLine2)
        {
            Vector3 line1Direction = line1.direction;
            Vector3 line2Direction = line2.direction;

            float a = Vector3.Dot(line1Direction, line1Direction);
            float b = Vector3.Dot(line1Direction, line2Direction);
            float e = Vector3.Dot(line2Direction, line2Direction);

            float d = a * e - b * b;

            Vector3 r = line1.origin - line2.origin;
            float c = Vector3.Dot(line1Direction, r);
            float f = Vector3.Dot(line2Direction, r);

            float s = (b * f - c * e) / d;
            float t = (a * f - c * b) / d;

            closestPointLine1 = line1.origin + line1Direction * s;
            closestPointLine2 = line2.origin + line2Direction * t;
        }

        private static void SetupCamera()
        {
            if (_previewRenderCamera)
            {
                cameraSetup.GetSetup(_previewRenderCamera);
                renderCamera = _previewRenderCamera;
                renderCamera.nearClipPlane = 0.01f;
                renderCamera.cullingMask = 1 << k_PreviewLayerIndex;
            }
            else
            {
                renderCamera = InternalCamera;
            }

            renderCamera.backgroundColor = _backgroundColor;
            renderCamera.orthographic = _orthographicMode;
            renderCamera.clearFlags = _backgroundColor.a < 1f ? CameraClearFlags.Depth : CameraClearFlags.Color;
        }

        private static bool IsStatic(Transform obj)
        {
            if (obj.gameObject.isStatic) { return true; }

            for (int i = 0; i < obj.childCount; i++)
            {
                if (IsStatic(obj.GetChild(i))) { return true; }
            }

            return false;
        }

        private static void SetLayerRecursively(Transform obj)
        {
            obj.gameObject.layer = k_PreviewLayerIndex;
            for (int i = 0; i < obj.childCount; i++)
            {
                SetLayerRecursively(obj.GetChild(i));
            }
        }

        private static void GetLayerRecursively(Transform obj)
        {
            layersList.Add(obj.gameObject.layer);
            for (int i = 0; i < obj.childCount; i++)
            {
                GetLayerRecursively(obj.GetChild(i));
            }
        }

        private static void SetLayerRecursively(Transform obj, ref int index)
        {
            obj.gameObject.layer = layersList[index++];
            for (int i = 0; i < obj.childCount; i++)
            {
                SetLayerRecursively(obj.GetChild(i), ref index);
            }
        }
    }
}
