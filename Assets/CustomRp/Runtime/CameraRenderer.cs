using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CustomRp.Runtime
{
    public class CameraRenderer
    {
        private const string COMMAND_BUFFER_NAME = "My Render Camera";
        private static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

        private static ShaderTagId[] legacyShaderTagIds =
        {
            new ShaderTagId("Always"),
            new ShaderTagId("ForwardBase"),
            new ShaderTagId("PrepassBase"),
            new ShaderTagId("Vertex"),
            new ShaderTagId("VertexLMRGBM"),
            new ShaderTagId("VertexLM")
        };

        private static Material errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        private readonly CommandBuffer commandBuffer = new CommandBuffer()
        {
            name = COMMAND_BUFFER_NAME
        };

        private int cameraIndex;
        private CustomRenderPipelineData data;
        private ScriptableRenderContext renderContext;
        private Camera camera;
        private CullingResults cullingResults;

        private string sampleName = COMMAND_BUFFER_NAME;

        public void Render(int cameraIndex, CustomRenderPipelineData data, ScriptableRenderContext renderContext, Camera camera)
        {
            this.cameraIndex = cameraIndex;
            this.data = data;
            this.renderContext = renderContext;
            this.camera = camera;

            sampleName = camera.name;

            PrepareBuffer();
            PrepareForSceneWindow();

            if (!Cull())
            {
                return;
            }

            Setup();
            DrawVisibleGeometry();
            DrawUnsupportedShaders();
            DrawGizmos();
            Submit();
        }

        private void Setup()
        {
            var flags = camera.clearFlags;
            renderContext.SetupCameraProperties(camera);
            commandBuffer.ClearRenderTarget(flags <= CameraClearFlags.Depth, flags == CameraClearFlags.Color,
                flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
            commandBuffer.BeginSample(sampleName);
            ExecuteBuffer();
        }
        private void PrepareBuffer () {
            commandBuffer.name = sampleName;
        }


        private bool Cull()
        {
            if (camera.TryGetCullingParameters(out var p))
            {
                cullingResults = renderContext.Cull(ref p);
                return true;
            }

            return false;
        }

        private void PrepareForSceneWindow () {
            if (camera.cameraType == CameraType.SceneView) {
                ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
            }
        }

        private void DrawVisibleGeometry()
        {
            var sortingSettings = new SortingSettings(camera)
            {
                criteria = SortingCriteria.CommonOpaque
            };

            var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
            var filteringSettings = new FilteringSettings(RenderQueueRange.all);

            renderContext.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

            renderContext.DrawSkybox(camera);


            sortingSettings.criteria = SortingCriteria.CommonTransparent;
            drawingSettings.sortingSettings = sortingSettings;
            filteringSettings.renderQueueRange = RenderQueueRange.transparent;

            renderContext.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings);
        }

        private void Submit()
        {
            commandBuffer.EndSample(sampleName);
            ExecuteBuffer();
            renderContext.Submit();
        }

        private void ExecuteBuffer()
        {
            var width = data.ViewportWidth;
            var height = data.ViewportHeigth;
            var maxRow = data.ViewportsInRow;

            var yOffset = cameraIndex / maxRow;
            var xOffset = (width * cameraIndex - (yOffset * (maxRow * width)));

            commandBuffer.SetViewport(new Rect(new Vector2(xOffset, yOffset * height), new Vector2(width, height)));
            renderContext.ExecuteCommandBuffer(commandBuffer);
            commandBuffer.Clear();
        }

        private void DrawUnsupportedShaders()
        {
            var drawingSettings = new DrawingSettings(
                legacyShaderTagIds[0], new SortingSettings(camera)
            )
            {
                overrideMaterial = errorMaterial
            };

            var filteringSettings = FilteringSettings.defaultValue;

            for (var i = 1; i < legacyShaderTagIds.Length; i++)
            {
                drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
            }

            renderContext.DrawRenderers(
                cullingResults, ref drawingSettings, ref filteringSettings
            );
        }

        private void DrawGizmos () {
            if (Handles.ShouldRenderGizmos()) {
                renderContext.DrawGizmos(camera, GizmoSubset.PreImageEffects);
                renderContext.DrawGizmos(camera, GizmoSubset.PostImageEffects);
            }
        }
    }
}