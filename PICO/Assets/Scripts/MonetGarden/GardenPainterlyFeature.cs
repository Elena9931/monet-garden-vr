using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace MonetGarden
{
    public class GardenPainterlyFeature : ScriptableRendererFeature
    {
        public Shader shader;
        PainterlyPass pass;
        Material material;
        public override void Create()
        {
            pass?.Dispose();
            CoreUtils.Destroy(material);
            if (shader) material = CoreUtils.CreateEngineMaterial(shader);
            // URP 14 swaps colour buffers during post-processing. Process the stable transparent
            // scene colour before that swap, so the final blit includes splats and this pass.
            pass = new PainterlyPass(material) { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
        }
        bool Applies(in RenderingData data)
        {
            var experience = GardenExperience.Active;
            return experience && !experience.player.VRControlled && (experience.Progress > 0 || (experience.guide && experience.guide.Expanded && experience.State==GardenExperience.ExperienceState.Exploration)) && experience.player &&
                data.cameraData.camera == experience.player.viewCamera && !data.cameraData.isPreviewCamera;
        }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (material && Applies(renderingData)) renderer.EnqueuePass(pass);
        }
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (material && Applies(renderingData)) pass.SetTarget(renderer.cameraColorTargetHandle);
        }
        protected override void Dispose(bool disposing) { pass?.Dispose(); CoreUtils.Destroy(material); }

        sealed class PainterlyPass : ScriptableRenderPass
        {
            readonly Material material;
            RTHandle source, scratch, reality;
            public PainterlyPass(Material material) { this.material = material; }
            public void SetTarget(RTHandle target) { source = target; }
            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData data)
            {
                var descriptor = data.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0; descriptor.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(ref scratch, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_MonetPainterlyScratch");
                RenderingUtils.ReAllocateIfNeeded(ref reality, descriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_MonetFrozenReality");
                ConfigureTarget(source);
                ConfigureInput(ScriptableRenderPassInput.Color);
            }
            public override void Execute(ScriptableRenderContext context, ref RenderingData data)
            {
                var experience = GardenExperience.Active;
                if (!experience || !material || source == null || scratch == null) return;
                material.SetFloat("_Progress", experience.Progress);
                var artwork = experience.SelectedArtwork;
                bool original = artwork && artwork.HasDocumentedOriginal;
                material.SetFloat("_HasPainting", original ? 1 : 0);
                if (original)
                {
                    material.SetTexture("_Painting", artwork.originalPainting);
                    material.SetFloat("_PaintingAspect", (float)artwork.originalPainting.width / artwork.originalPainting.height);
                }
                material.SetFloat("_ViewAspect", (float)data.cameraData.cameraTargetDescriptor.width / data.cameraData.cameraTargetDescriptor.height);
                var descriptor = data.cameraData.cameraTargetDescriptor;
                material.SetVector("_SourceTexelSize", new Vector4(1f / descriptor.width, 1f / descriptor.height, descriptor.width, descriptor.height));
                var cmd = CommandBufferPool.Get("Monet: reality to painterly");
                if(experience.State==GardenExperience.ExperienceState.Exploration) {
                    Blitter.BlitCameraTexture(cmd, source, reality);
                    cmd.SetGlobalTexture("_MonetGuideBackground",reality.nameID);
                    context.ExecuteCommandBuffer(cmd);CommandBufferPool.Release(cmd);return;
                }
                if (experience.CaptureRequested) { Blitter.BlitCameraTexture(cmd, source, reality); experience.CaptureRequested = false; }
                material.SetTexture("_Reality", reality.rt);
                Blitter.BlitCameraTexture(cmd, source, scratch, material, 0);
                Blitter.BlitCameraTexture(cmd, scratch, source);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                experience.EffectRendered = true;
            }
            public void Dispose() { scratch?.Release(); reality?.Release(); scratch = null; reality = null; }
        }
    }
}
