using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class L16RainScreenFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public sealed class Settings
    {
        public Material material;
        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public Settings settings = new Settings();

    private L16RainScreenPass pass;

    public override void Create()
    {
        pass = new L16RainScreenPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material == null || renderingData.cameraData.cameraType != CameraType.Game)
        {
            return;
        }

        renderer.EnqueuePass(pass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        if (settings.material == null)
        {
            return;
        }

        pass.Setup(renderer.cameraColorTargetHandle);
    }

    protected override void Dispose(bool disposing)
    {
        pass?.Dispose();
    }

    private sealed class L16RainScreenPass : ScriptableRenderPass
    {
        private readonly Settings settings;
        private RTHandle source;
        private RTHandle tempTexture;

        public L16RainScreenPass(Settings settings)
        {
            this.settings = settings;
            renderPassEvent = settings.passEvent;
            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        public void Setup(RTHandle source)
        {
            this.source = source;
            renderPassEvent = settings.passEvent;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cameraTextureDescriptor.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, cameraTextureDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_L16RainScreenTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (settings.material == null || source == null || tempTexture == null)
            {
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("L16 Rain Screen Pass");
            Blitter.BlitCameraTexture(cmd, source, tempTexture, settings.material, 0);
            Blitter.BlitCameraTexture(cmd, tempTexture, source);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            tempTexture?.Release();
        }
    }
}
