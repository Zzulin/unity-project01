using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeCloudEffect : ScriptableRendererFeature
{
    [System.Serializable]
    public class VolumeCloudSettings
    {
        public Material cloudMaterial = null;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        [Range(0, 1)] public float cloudDensity = 0.5f;
        [Range(0, 1)] public float cloudCoverage = 0.5f;
        public Vector3 cloudScale = new Vector3(1, 0.5f, 1);
        public Vector3 cloudSpeed = new Vector3(0.1f, 0.05f, 0.1f);
        public Color cloudColor = Color.white;
        public Color cloudEmission = new Color(0.2f, 0.2f, 0.2f);
        public float cloudSharpness = 0.5f;
        [Range(0, 1)] public float cloudSoftness = 0.2f;
        public float cloudHeight = 100f;
        public float cloudThickness = 50f;
        public int raySteps = 64;
    }

    public VolumeCloudSettings settings = new VolumeCloudSettings();
    
    private VolumeCloudPass volumeCloudPass;

    public override void Create()
    {
        volumeCloudPass = new VolumeCloudPass(settings);
        volumeCloudPass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.cloudMaterial != null)
        {
            volumeCloudPass.Setup(renderer);
            renderer.EnqueuePass(volumeCloudPass);
        }
    }

    class VolumeCloudPass : ScriptableRenderPass
    {
        private VolumeCloudSettings settings;
        private RenderTargetIdentifier source;
        private RenderTargetHandle tempTexture;
        private ScriptableRenderer renderer;

        public VolumeCloudPass(VolumeCloudSettings settings)
        {
            this.settings = settings;
            tempTexture.Init("_TempCloudTexture");
        }

        public void Setup(ScriptableRenderer renderer)
        {
            this.renderer = renderer;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(tempTexture.id, cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.cloudMaterial == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get("Volume Cloud Effect");
            cmd.Clear();

            source = renderer.cameraColorTarget;
            
            // 设置材质参数
            settings.cloudMaterial.SetFloat("_CloudDensity", settings.cloudDensity);
            settings.cloudMaterial.SetFloat("_CloudCoverage", settings.cloudCoverage);
            settings.cloudMaterial.SetVector("_CloudScale", settings.cloudScale);
            settings.cloudMaterial.SetVector("_CloudSpeed", settings.cloudSpeed);
            settings.cloudMaterial.SetColor("_CloudColor", settings.cloudColor);
            settings.cloudMaterial.SetColor("_CloudEmission", settings.cloudEmission);
            settings.cloudMaterial.SetFloat("_CloudSharpness", settings.cloudSharpness);
            settings.cloudMaterial.SetFloat("_CloudSoftness", settings.cloudSoftness);
            settings.cloudMaterial.SetFloat("_CloudHeight", settings.cloudHeight);
            settings.cloudMaterial.SetFloat("_CloudThickness", settings.cloudThickness);
            settings.cloudMaterial.SetInt("_RaySteps", settings.raySteps);
            
            // 传递摄像机参数
            Camera camera = renderingData.cameraData.camera;
            settings.cloudMaterial.SetMatrix("_InverseViewMatrix", camera.cameraToWorldMatrix);
            settings.cloudMaterial.SetMatrix("_InverseProjectionMatrix", GL.GetGPUProjectionMatrix(camera.projectionMatrix, true).inverse);
            
            // 传递时间参数
            settings.cloudMaterial.SetFloat("_Time", Time.time);
            
            // 执行Blit操作
            Blit(cmd, source, tempTexture.Identifier(), settings.cloudMaterial);
            Blit(cmd, tempTexture.Identifier(), source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempTexture.id);
        }
    }
}