using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRp.Runtime
{
    public class CustomRenderPipeline : RenderPipeline
    {
        private readonly CameraRenderer renderer = new CameraRenderer();
        private readonly CustomRenderPipelineData data;

        public CustomRenderPipeline(CustomRenderPipelineData data)
        {
            this.data = data;
        }

        protected override void Render(ScriptableRenderContext context, Camera[] cameras)
        {
            for (var i = 0; i < cameras.Length; i++)
            {
                renderer.Render(i, data, context, cameras[i]);
            }
        }
    }
}