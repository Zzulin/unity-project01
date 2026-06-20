using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public sealed class L17FrustumVolumetricRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public sealed class Settings
    {
        public bool enabled = true;
        public bool requireSceneController = true;
        [HideInInspector]
        [Range(1, 4)] public int downsample = 2;
        [HideInInspector]
        [Range(16, 128)] public int froxelDepth = 96;
        [HideInInspector]
        [Range(4f, 120f)] public float maxDistance = 58f;
        [HideInInspector]
        [Range(0.5f, 4f)] public float depthDistribution = 1.9f;
        [HideInInspector]
        [Range(0f, 1.5f)] public float density = 0.24f;
        [HideInInspector]
        [Range(0.01f, 4f)] public float extinction = 0.68f;
        [HideInInspector]
        [Range(0f, 10f)] public float intensity = 3.25f;
        [HideInInspector]
        [Range(0f, 0.92f)] public float anisotropy = 0.78f;
        [HideInInspector]
        [Range(0f, 1f)] public float shadowFloor = 0.015f;
        [HideInInspector]
        [Range(0f, 2f)] public float multiScatter = 0.32f;
        [HideInInspector]
        [Range(0.01f, 8f)] public float heightFalloff = 0.22f;
        [HideInInspector]
        [Range(-10f, 10f)] public float heightOrigin = -0.4f;
        [HideInInspector]
        [Range(0f, 1f)] public float noiseStrength = 0f;
        [HideInInspector]
        [Range(0.05f, 8f)] public float noiseScale = 1.25f;
        [HideInInspector]
        public Vector3 volumeBoundsCenter = new Vector3(0f, 3.1f, -0.1f);
        [HideInInspector]
        public Vector3 volumeBoundsSize = new Vector3(15.8f, 6.2f, 16.2f);
        [HideInInspector]
        [Range(0.01f, 3f)] public float volumeBoundsSoftness = 0.45f;
        [HideInInspector]
        public bool temporalAccumulation = true;
        [HideInInspector]
        [Range(0f, 1f)] public float jitterStrength = 0.9f;
        [HideInInspector]
        [Range(0f, 0.98f)] public float temporalBlend = 0.8f;
        [HideInInspector]
        [Range(0.001f, 2f)] public float temporalDepthRejection = 0.12f;
        [HideInInspector]
        [Range(0f, 1f)] public float bilateralDepthScale = 0.08f;
        [HideInInspector]
        [Range(0f, 1f)] public float compositeOpacity = 0.94f;
        [HideInInspector]
        public Color scatteringColor = new Color(1f, 0.84f, 0.52f, 1f);
        public RenderPassEvent passEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    private sealed class L17VolumetricPass : ScriptableRenderPass
    {
        private static readonly int IntegratedTextureId = Shader.PropertyToID("_L17IntegratedTexture");
        private static readonly int HistoryTextureId = Shader.PropertyToID("_L17HistoryTexture");
        private static readonly int LowDepthTextureId = Shader.PropertyToID("_L17LowDepthTexture");
        private static readonly int CameraCopyTextureId = Shader.PropertyToID("_L17CameraCopyTexture");
        private static readonly int BlueNoiseTextureId = Shader.PropertyToID("_L17BlueNoiseTexture");
        private static readonly int FroxelSizeId = Shader.PropertyToID("_L17FroxelSize");
        private static readonly int CameraSizeId = Shader.PropertyToID("_L17CameraSize");
        private static readonly int Params0Id = Shader.PropertyToID("_L17Params0");
        private static readonly int Params1Id = Shader.PropertyToID("_L17Params1");
        private static readonly int Params2Id = Shader.PropertyToID("_L17Params2");
        private static readonly int VolumeBoundsCenterId = Shader.PropertyToID("_L17VolumeBoundsCenter");
        private static readonly int VolumeBoundsSizeId = Shader.PropertyToID("_L17VolumeBoundsSize");
        private static readonly int TemporalParamsId = Shader.PropertyToID("_L17TemporalParams");
        private static readonly int TemporalControlId = Shader.PropertyToID("_L17TemporalControl");
        private static readonly int ScatteringColorId = Shader.PropertyToID("_L17ScatteringColor");
        private static readonly int PreviousViewProjectionId = Shader.PropertyToID("_L17PreviousViewProjection");
        private static readonly int HistoryValidId = Shader.PropertyToID("_L17HistoryValid");
        private static readonly int FrameIndexId = Shader.PropertyToID("_L17FrameIndex");
        private static readonly int FroxelDepthId = Shader.PropertyToID("_L17FroxelDepth");
        private static readonly int CloudShapeNoiseId = Shader.PropertyToID("_L17CloudShapeNoise");
        private static readonly int CloudDetailNoiseId = Shader.PropertyToID("_L17CloudDetailNoise");
        private static readonly int CloudWeatherMapId = Shader.PropertyToID("_L17CloudWeatherMap");
        private static readonly int CloudWorldToLocalId = Shader.PropertyToID("_L17CloudWorldToLocal");
        private static readonly int CloudLocalToWorldId = Shader.PropertyToID("_L17CloudLocalToWorld");
        private static readonly int CloudNoiseWorldSizeId = Shader.PropertyToID("_L17CloudNoiseWorldSize");
        private static readonly int CloudWindId = Shader.PropertyToID("_L17CloudWind");
        private static readonly int CloudParams0Id = Shader.PropertyToID("_L17CloudParams0");
        private static readonly int CloudParams1Id = Shader.PropertyToID("_L17CloudParams1");
        private static readonly int CloudParams2Id = Shader.PropertyToID("_L17CloudParams2");
        private static readonly int CloudMacroGapStrengthId = Shader.PropertyToID("_L17CloudMacroGapStrength");

        private const string IntegratedTextureName = "_L17IntegratedTexture";
        private const string DenoisedTextureName = "_L17DenoisedTexture";
        private const string TemporalTextureName = "_L17TemporalTexture";
        private const string HistoryTextureName = "_L17HistoryTexture";
        private const string LowDepthTextureName = "_L17LowDepthTexture";
        private const string CameraCopyTextureName = "_L17CameraCopyTexture";

        private readonly ProfilingSampler l17ProfilingSampler = new ProfilingSampler("L17 Froxel Volumetric Lighting");
        private readonly Settings featureSettings;
        private readonly Material material;

        private RTHandle source;
        private L17VolumetricLightingController controller;
        private RTHandle integratedTexture;
        private RTHandle denoisedTexture;
        private RTHandle temporalTexture;
        private RTHandle lowDepthTexture;
        private RTHandle cameraCopyTexture;
        private readonly Dictionary<int, CameraHistory> cameraHistories = new Dictionary<int, CameraHistory>();
        private RenderTextureDescriptor currentLowDescriptor;
        private int previousWidth = -1;
        private int previousHeight = -1;

        private sealed class CameraHistory
        {
            public RTHandle texture;
            public Matrix4x4 previousViewProjection = Matrix4x4.identity;
            public Vector3 previousPosition;
            public Quaternion previousRotation = Quaternion.identity;
            public int width = -1;
            public int height = -1;
            public bool poseValid;
            public bool valid;
        }

        public Texture blueNoiseTexture { get; set; }

        public L17VolumetricPass(Material material, Settings featureSettings)
        {
            this.material = material;
            this.featureSettings = featureSettings;
            renderPassEvent = featureSettings.passEvent;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(RTHandle source, L17VolumetricLightingController controller)
        {
            this.source = source;
            this.controller = controller;
            renderPassEvent = featureSettings.passEvent;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            int downsample = Mathf.Max(1, controller != null ? controller.downsample : featureSettings.downsample);
            int lowWidth = Mathf.Max(1, Mathf.CeilToInt(cameraTextureDescriptor.width / (float)downsample));
            int lowHeight = Mathf.Max(1, Mathf.CeilToInt(cameraTextureDescriptor.height / (float)downsample));

            RenderTextureDescriptor lowDescriptor = cameraTextureDescriptor;
            lowDescriptor.width = lowWidth;
            lowDescriptor.height = lowHeight;
            lowDescriptor.volumeDepth = 1;
            lowDescriptor.dimension = TextureDimension.Tex2D;
            lowDescriptor.graphicsFormat = GraphicsFormat.R16G16B16A16_SFloat;
            lowDescriptor.depthBufferBits = 0;
            lowDescriptor.msaaSamples = 1;
            lowDescriptor.useMipMap = false;
            lowDescriptor.autoGenerateMips = false;

            RenderTextureDescriptor depthDescriptor = lowDescriptor;
            depthDescriptor.graphicsFormat = GraphicsFormat.R32_SFloat;

            RenderTextureDescriptor copyDescriptor = cameraTextureDescriptor;
            copyDescriptor.depthBufferBits = 0;
            copyDescriptor.msaaSamples = 1;

            RenderingUtils.ReAllocateIfNeeded(ref integratedTexture, lowDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: IntegratedTextureName);
            RenderingUtils.ReAllocateIfNeeded(ref denoisedTexture, lowDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: DenoisedTextureName);
            RenderingUtils.ReAllocateIfNeeded(ref temporalTexture, lowDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: TemporalTextureName);
            RenderingUtils.ReAllocateIfNeeded(ref lowDepthTexture, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: LowDepthTextureName);
            RenderingUtils.ReAllocateIfNeeded(ref cameraCopyTexture, copyDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: CameraCopyTextureName);

            currentLowDescriptor = lowDescriptor;
            previousWidth = lowWidth;
            previousHeight = lowHeight;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || source == null)
            {
                return;
            }

            Camera camera = renderingData.cameraData.camera;
            if (camera == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("L17 Froxel Volumetric Lighting");
            using (new ProfilingScope(cmd, l17ProfilingSampler))
            {
                CameraType cameraType = renderingData.cameraData.cameraType;
                bool useTemporalHistory = ShouldUseTemporalHistory(cameraType);
                CameraHistory history = useTemporalHistory ? GetCameraHistory(camera.GetInstanceID()) : null;

                if (history != null)
                {
                    EnsureCameraHistoryTexture(history, camera.GetInstanceID());
                    if (history.poseValid)
                    {
                        float positionDelta = Vector3.Distance(camera.transform.position, history.previousPosition);
                        float rotationDelta = Quaternion.Angle(camera.transform.rotation, history.previousRotation);
                        if (positionDelta > 0.015f || rotationDelta > 0.08f)
                        {
                            history.valid = false;
                        }
                    }
                }

                Matrix4x4 viewProjection = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true) * camera.worldToCameraMatrix;
                PushSettings(camera, history, useTemporalHistory, cameraType);

                Blitter.BlitCameraTexture(cmd, source, lowDepthTexture, material, 0);
                Blitter.BlitCameraTexture(cmd, source, integratedTexture, material, 1);

                cmd.SetGlobalTexture(IntegratedTextureId, integratedTexture);
                cmd.SetGlobalTexture(LowDepthTextureId, lowDepthTexture);
                Blitter.BlitCameraTexture(cmd, integratedTexture, denoisedTexture, material, 2);

                cmd.SetGlobalTexture(IntegratedTextureId, denoisedTexture);
                Blitter.BlitCameraTexture(cmd, denoisedTexture, temporalTexture, material, 3);
                cmd.SetGlobalTexture(IntegratedTextureId, temporalTexture);

                Blitter.BlitCameraTexture(cmd, source, cameraCopyTexture);
                Blitter.BlitCameraTexture(cmd, cameraCopyTexture, source, material, 4);

                if (history != null && history.texture != null)
                {
                    cmd.CopyTexture(temporalTexture, history.texture);
                    history.previousViewProjection = viewProjection;
                    history.previousPosition = camera.transform.position;
                    history.previousRotation = camera.transform.rotation;
                    history.poseValid = true;
                    history.valid = true;
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private CameraHistory GetCameraHistory(int cameraId)
        {
            if (!cameraHistories.TryGetValue(cameraId, out CameraHistory history))
            {
                history = new CameraHistory();
                cameraHistories.Add(cameraId, history);
            }

            return history;
        }

        private bool ShouldUseTemporalHistory(CameraType cameraType)
        {
            bool temporalAccumulation = controller != null ? controller.temporalAccumulation : featureSettings.temporalAccumulation;
            float temporalBlend = controller != null ? controller.temporalBlend : featureSettings.temporalBlend;
            if (!temporalAccumulation || temporalBlend <= 0.001f)
            {
                return false;
            }

            return cameraType == CameraType.Game || cameraType == CameraType.SceneView;
        }

        private void EnsureCameraHistoryTexture(CameraHistory history, int cameraId)
        {
            bool resized = RenderingUtils.ReAllocateIfNeeded(
                ref history.texture,
                currentLowDescriptor,
                FilterMode.Bilinear,
                TextureWrapMode.Clamp,
                name: $"{HistoryTextureName}_{cameraId}");

            if (resized || history.width != previousWidth || history.height != previousHeight)
            {
                history.valid = false;
                history.poseValid = false;
                history.width = previousWidth;
                history.height = previousHeight;
            }
        }

        private void PushSettings(Camera camera, CameraHistory history, bool useTemporalHistory, CameraType cameraType)
        {
            bool validHistory = useTemporalHistory && history != null && history.valid && history.texture != null;
            float temporalBlend = validHistory ? GetTemporalBlend() : 0f;
            float frameIndex = validHistory ? Time.renderedFrameCount : 0f;
            GetVolumeBounds(out Vector3 boundsCenter, out Vector3 boundsSize);
            material.SetTexture(HistoryTextureId, validHistory ? history.texture : Texture2D.blackTexture);
            material.SetTexture(BlueNoiseTextureId, blueNoiseTexture != null ? blueNoiseTexture : Texture2D.blackTexture);
            material.SetVector(FroxelSizeId, new Vector4(previousWidth, previousHeight, 1f / Mathf.Max(1, previousWidth), 1f / Mathf.Max(1, previousHeight)));
            material.SetVector(CameraSizeId, new Vector4(camera.pixelWidth, camera.pixelHeight, 1f / Mathf.Max(1, camera.pixelWidth), 1f / Mathf.Max(1, camera.pixelHeight)));
            material.SetVector(Params0Id, new Vector4(GetMaxDistance(), GetDepthDistribution(), GetDensity(), GetExtinction()));
            material.SetVector(Params1Id, new Vector4(GetIntensity(), GetAnisotropy(), GetShadowFloor(), GetMultiScatter()));
            material.SetVector(Params2Id, new Vector4(GetHeightOrigin(), GetHeightFalloff(), GetNoiseScale(), GetNoiseStrength()));
            material.SetVector(VolumeBoundsCenterId, new Vector4(boundsCenter.x, boundsCenter.y, boundsCenter.z, GetVolumeBoundsSoftness()));
            material.SetVector(VolumeBoundsSizeId, new Vector4(boundsSize.x, boundsSize.y, boundsSize.z, 0f));
            material.SetVector(TemporalParamsId, new Vector4(temporalBlend, GetJitterStrength(), GetBilateralDepthScale(), GetCompositeOpacity()));
            material.SetVector(TemporalControlId, new Vector4(
                useTemporalHistory ? 1f : 0f,
                cameraType == CameraType.SceneView ? 1f : 0f,
                GetTemporalAccumulation() ? 1f : 0f,
                controller != null ? controller.forwardPhaseCeiling : 3.5f));
            material.SetColor(ScatteringColorId, GetScatteringColor());
            material.SetMatrix(PreviousViewProjectionId, validHistory ? history.previousViewProjection : Matrix4x4.identity);
            material.SetFloat(HistoryValidId, validHistory ? 1f : 0f);
            material.SetFloat(FrameIndexId, frameIndex);
            material.SetFloat(FroxelDepthId, GetFroxelDepth());
            PushCloudOccluder();
        }

        private void PushCloudOccluder()
        {
            L13VolumeCloudController cloud = controller != null ? controller.GetCloudOccluder() : null;
            Material cloudMaterial = cloud != null ? cloud.cloudMaterial : null;
            bool enabled = cloud != null
                && cloud.isActiveAndEnabled
                && cloudMaterial != null
                && cloud.coverage > 0.0001f
                && cloud.density > 0.0001f
                && cloud.volumetricShadowStrength > 0.0001f;

            material.SetVector(CloudParams2Id, enabled
                ? new Vector4(cloud.anvilBias, cloud.lightAbsorption, cloud.volumetricShadowSteps, cloud.volumetricShadowStrength)
                : Vector4.zero);

            if (!enabled)
            {
                return;
            }

            material.SetTexture(CloudShapeNoiseId, cloudMaterial.GetTexture("_ShapeNoise"));
            material.SetTexture(CloudDetailNoiseId, cloudMaterial.GetTexture("_DetailNoise"));
            material.SetTexture(CloudWeatherMapId, cloudMaterial.GetTexture("_WeatherMap"));
            material.SetMatrix(CloudWorldToLocalId, cloud.transform.worldToLocalMatrix);
            material.SetMatrix(CloudLocalToWorldId, cloud.transform.localToWorldMatrix);
            material.SetVector(CloudNoiseWorldSizeId, cloud.noiseWorldSize);
            material.SetVector(CloudWindId, new Vector4(
                cloud.windDirection.x,
                cloud.windDirection.y,
                cloud.windDirection.z,
                cloud.windSpeed));
            material.SetVector(CloudParams0Id, new Vector4(
                cloud.density,
                cloud.coverage,
                cloud.weatherStrength,
                cloud.shapeScale));
            material.SetVector(CloudParams1Id, new Vector4(
                cloud.detailScale,
                cloud.detailStrength,
                cloud.bottomSoftness,
                cloud.topSoftness));
            material.SetVector(CloudParams2Id, new Vector4(
                cloud.anvilBias,
                cloud.lightAbsorption,
                cloud.volumetricShadowSteps,
                cloud.volumetricShadowStrength));
            material.SetFloat("_L17CloudShadowContrast", cloud.volumetricShadowContrast);
            material.SetFloat(CloudMacroGapStrengthId, cloud.macroGapStrength);
        }

        private int GetFroxelDepth() => controller != null ? controller.froxelDepth : featureSettings.froxelDepth;
        private float GetMaxDistance() => controller != null ? controller.maxDistance : featureSettings.maxDistance;
        private float GetDepthDistribution() => controller != null ? controller.depthDistribution : featureSettings.depthDistribution;
        private float GetDensity() => controller != null ? controller.density : featureSettings.density;
        private float GetExtinction() => controller != null ? controller.extinction : featureSettings.extinction;
        private float GetIntensity() => controller != null ? controller.intensity : featureSettings.intensity;
        private float GetAnisotropy() => controller != null ? controller.anisotropy : featureSettings.anisotropy;
        private float GetShadowFloor() => controller != null ? controller.shadowFloor : featureSettings.shadowFloor;
        private float GetMultiScatter() => controller != null ? controller.multiScatter : featureSettings.multiScatter;
        private float GetHeightOrigin() => controller != null ? controller.heightOrigin : featureSettings.heightOrigin;
        private float GetHeightFalloff() => controller != null ? controller.heightFalloff : featureSettings.heightFalloff;
        private float GetNoiseStrength() => controller != null ? controller.noiseStrength : featureSettings.noiseStrength;
        private float GetNoiseScale() => controller != null ? controller.noiseScale : featureSettings.noiseScale;
        private bool GetTemporalAccumulation() => controller != null ? controller.temporalAccumulation : featureSettings.temporalAccumulation;
        private float GetJitterStrength() => controller != null ? controller.jitterStrength : featureSettings.jitterStrength;
        private float GetTemporalBlend() => controller != null ? controller.temporalBlend : featureSettings.temporalBlend;
        private float GetBilateralDepthScale() => controller != null ? controller.bilateralDepthScale : featureSettings.bilateralDepthScale;
        private float GetCompositeOpacity() => controller != null ? controller.compositeOpacity : featureSettings.compositeOpacity;
        private float GetVolumeBoundsSoftness() => controller != null ? controller.volumeBoundsSoftness : featureSettings.volumeBoundsSoftness;
        private Color GetScatteringColor() => controller != null ? controller.scatteringColor : featureSettings.scatteringColor;

        private void GetVolumeBounds(out Vector3 center, out Vector3 size)
        {
            if (controller != null)
            {
                controller.GetVolumeBounds(out center, out size);
                return;
            }

            center = featureSettings.volumeBoundsCenter;
            size = featureSettings.volumeBoundsSize;
        }

        public void Dispose()
        {
            integratedTexture?.Release();
            denoisedTexture?.Release();
            temporalTexture?.Release();
            lowDepthTexture?.Release();
            cameraCopyTexture?.Release();
            foreach (CameraHistory history in cameraHistories.Values)
            {
                history.texture?.Release();
            }

            cameraHistories.Clear();
        }
    }

    public Settings settings = new Settings();
    [SerializeField] private Shader compositeShader;
    [SerializeField] private Texture2D blueNoiseTexture;
    [HideInInspector]
    [SerializeField] private Material compositeMaterial;
    private static readonly Dictionary<int, L17VolumetricLightingController> activeSceneControllers = new Dictionary<int, L17VolumetricLightingController>();
    private L17VolumetricPass pass;

    public static void RegisterSceneController(L17VolumetricLightingController controller)
    {
        if (controller == null)
        {
            return;
        }

        Scene scene = controller.gameObject.scene;
        if (!scene.IsValid())
        {
            return;
        }

        activeSceneControllers[scene.handle] = controller;
    }

    public static void UnregisterSceneController(L17VolumetricLightingController controller)
    {
        if (controller == null)
        {
            return;
        }

        Scene scene = controller.gameObject.scene;
        if (!scene.IsValid())
        {
            return;
        }

        int sceneHandle = scene.handle;
        if (activeSceneControllers.TryGetValue(sceneHandle, out L17VolumetricLightingController activeController)
            && activeController == controller)
        {
            activeSceneControllers.Remove(sceneHandle);
        }
    }

    public void SetResources(Shader froxelCompositeShader, Texture2D blueNoise)
    {
        compositeShader = froxelCompositeShader;
        blueNoiseTexture = blueNoise;
        Create();
    }

    public override void Create()
    {
        if (compositeShader == null)
        {
            compositeShader = Shader.Find("Hidden/L17/Froxel Volumetric Composite");
        }

        CoreUtils.Destroy(compositeMaterial);
        if (compositeShader != null)
        {
            compositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);
        }

        pass = new L17VolumetricPass(compositeMaterial, settings)
        {
            blueNoiseTexture = blueNoiseTexture
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!TryGetRenderController(renderingData.cameraData.camera, renderingData.cameraData.cameraType, out _))
        {
            return;
        }

        renderer.EnqueuePass(pass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (!TryGetRenderController(renderingData.cameraData.camera, renderingData.cameraData.cameraType, out L17VolumetricLightingController controller))
        {
            return;
        }

        pass.Setup(renderer.cameraColorTargetHandle, controller);
        pass.blueNoiseTexture = blueNoiseTexture;
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        pass = null;
        CoreUtils.Destroy(compositeMaterial);
        compositeMaterial = null;
    }

    private bool TryGetRenderController(Camera camera, CameraType cameraType, out L17VolumetricLightingController controller)
    {
        controller = null;
        if (!settings.enabled || compositeMaterial == null)
        {
            return false;
        }

        if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
        {
            return false;
        }

        if (!settings.requireSceneController)
        {
            return true;
        }

        if (cameraType == CameraType.SceneView)
        {
            return TryGetSceneController(SceneManager.GetActiveScene(), out controller);
        }

        if (camera != null && TryGetSceneController(camera.gameObject.scene, out controller))
        {
            return true;
        }

        return TryGetSceneController(SceneManager.GetActiveScene(), out controller);
    }

    private static bool TryGetSceneController(Scene scene, out L17VolumetricLightingController controller)
    {
        controller = null;
        return scene.IsValid()
            && activeSceneControllers.TryGetValue(scene.handle, out controller)
            && controller != null
            && controller.isActiveAndEnabled;
    }
}
