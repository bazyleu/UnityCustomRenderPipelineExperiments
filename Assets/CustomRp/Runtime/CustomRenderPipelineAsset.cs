using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRp.Runtime
{
    [CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
    public class CustomRenderPipelineAsset : RenderPipelineAsset
    {
        [SerializeField]
        private CustomRenderPipelineData data;

        protected override RenderPipeline CreatePipeline()
        {
            return new CustomRenderPipeline(data);
        }
    }
}
