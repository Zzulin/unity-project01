using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class L17FrustumVolumetricRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public sealed class Settings
    {
        public bool enabled = true;
        [Range(1, 4)] public int downsample = 2;
        [Range(16, 128)] public int froxelDepth = 96;
        [Range(4f, 120f)] public float maxDistance = 58f;
        [Range(0.5f, 4f)] public float depthDistribution = 1.9f;
        [Range(0f, 1.5f)] public float density = 0.24f;
        [Range(0.01f, 4f)] public float extinction = 0.68f;
        [Range(0f, 10f)] public float intensity = 3.25f;
        [Range(0f, 0.92f)] public float anisotropy = 0.78f;
        [Range(0f, 1f)] public float shadowFloor = 0.035f;
        [Range(0f, 2f)] public float multiScatter = 0.32f;
        [Range(0.01f, 8f)] public float heightFalloff = 0.22f;
        [Range(-10f, 10f)] public float heightOrigin = -0.4f;
        [Range(0f, 1f)] public float noiseStrength = 0f;
        [Range(0.05f, 8f)] public float noiseScale = 1.25f;
        public bool temporalAccumulation = true;
        [Range(0f, 1f)] public float jitterStrength = 0.9f;
        [Range(0f, 0.98f)] public float temporalBlend = 0.8f;
        [Range(0.001f, 2f)] public float temporalDepthRejection = 0.12f;
        [Range(0f, 1f)] public float bilateralDepthScale = 0.08f;
        [Range(0f, 1f)] public float compositeOpacity = 0.94f;
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
        private static readonly int TemporalParamsId = Shader.PropertyToID("_L17TemporalParams");
        private static readonly int TemporalControlId = Shader.PropertyToID("_L17TemporalControl");
        private static readonly int ScatteringColorId = Shader.PropertyToID("_L17ScatteringColor");
        private static readonly int PreviousViewProjectionId = Shader.PropertyToID("_L17PreviousViewProjection");
        private static readonly int HistoryValidId = Shader.PropertyToID("_L17HistoryValid");
        private static readonly int FrameIndexId = Shader.PropertyToID("_L17FrameIndex");

        private const string IntegratedTextureName = "_L17IntegratedTexture";
        private const string DenoisedTextureName = "_L17DenoisedTexture";
        private const string TemporalTextureName = "_L17TemporalTexture";
        private const string HistoryTextureName = "_L17HistoryTexture";
        private const string LowDepthTextureName = "_L17LowDepthTexture";
        private const string CameraCopyTextureName = "_L17CameraCopyTexture";

        private readonly ProfilingSampler l17ProfilingSampler = new ProfilingSampler("L17 Froxel Volumetric Lighting");
        private readonly Settings settings;
        private readonly Material material;

        private RTHandle source;
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

        public L17VolumetricPass(Material material, Settings settings)
        {
            this.material = material;
            this.settings = settings;
            renderPassEvent = settings.passEvent;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public void Setup(RTHandle source)
        {
            this.source = source;
            renderPassEvent = settings.passEvent;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            int downsample = Mathf.Max(1, settings.downsample);
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
            if (!settings.temporalAccumulation || settings.temporalBlend <= 0.001f)
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
            float temporalBlend = validHistory ? settings.temporalBlend : 0f;
            float frameIndex = validHistory ? Time.renderedFrameCount : 0f;
            material.SetTexture(HistoryTextureId, validHistory ? history.texture : Texture2D.blackTexture);
            material.SetTexture(BlueNoiseTextureId, blueNoiseTexture != null ? blueNoiseTexture : Texture2D.blackTexture);
            material.SetVector(FroxelSizeId, new Vector4(previousWidth, previousHeight, 1f / Mathf.Max(1, previousWidth), 1f / Mathf.Max(1, previousHeight)));
            material.SetVector(CameraSizeId, new Vector4(camera.pixelWidth, camera.pixelHeight, 1f / Mathf.Max(1, camera.pixelWidth), 1f / Mathf.Max(1, camera.pixelHeight)));
            material.SetVector(Params0Id, new Vector4(settings.maxDistance, settings.depthDistribution, settings.density, settings.extinction));
            material.SetVector(Params1Id, new Vector4(settings.intensity, settings.anisotropy, settings.shadowFloor, settings.multiScatter));
            material.SetVector(Params2Id, new Vector4(settings.heightOrigin, settings.heightFalloff, settings.noiseScale, settings.noiseStrength));
            material.SetVector(TemporalParamsId, new Vector4(temporalBlend, settings.jitterStrength, settings.bilateralDepthScale, settings.compositeOpacity));
            material.SetVector(TemporalControlId, new Vector4(useTemporalHistory ? 1f : 0f, cameraType == CameraType.SceneView ? 1f : 0f, settings.temporalAccumulation ? 1f : 0f, 0f));
            material.SetColor(ScatteringColorId, settings.scatteringColor);
            material.SetMatrix(PreviousViewProjectionId, validHistory ? history.previousViewProjection : Matrix4x4.identity);
            material.SetFloat(HistoryValidId, validHistory ? 1f : 0f);
            material.SetFloat(FrameIndexId, frameIndex);
            material.SetFloat(Shader.PropertyToID("_L17FroxelDepth"), settings.froxelDepth);
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
    [SerializeField] private Material compositeMaterial;
    private L17VolumetricPass pass;

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
        if (!settings.enabled || compositeMaterial == null)
        {
            return;
        }

        CameraType cameraType = renderingData.cameraData.cameraType;
        if (cameraType != CameraType.Game && cameraType != CameraType.SceneView)
        {
            return;
        }

        renderer.EnqueuePass(pass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (!settings.enabled || compositeMaterial == null)
        {
            return;
        }

        pass.Setup(renderer.cameraColorTargetHandle);
        pass.blueNoiseTexture = blueNoiseTexture;
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
        pass = null;
        CoreUtils.Destroy(compositeMaterial);
        compositeMaterial = null;
    }
}
