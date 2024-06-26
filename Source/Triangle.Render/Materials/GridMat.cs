﻿using System.Runtime.InteropServices;
using Triangle.Core;
using Triangle.Core.Enums;
using Triangle.Core.Graphics;
using Triangle.Core.Helpers;
using Triangle.Render.Contracts.Materials;
using Triangle.Render.Models;

namespace Triangle.Render.Materials;

public class GridMat(TrContext context) : GlobalMat(context, "Grid")
{
    #region Uniforms
    [StructLayout(LayoutKind.Explicit)]
    private struct UniParameters
    {
        [FieldOffset(0)]
        public float Near;

        [FieldOffset(4)]
        public float Far;

        [FieldOffset(8)]
        public float PrimaryScale;

        [FieldOffset(12)]
        public float SecondaryScale;

        [FieldOffset(16)]
        public float GridIntensity;

        [FieldOffset(20)]
        public float AxisIntensity;

        [FieldOffset(24)]
        public float Fade;
    }
    #endregion

    private TrBuffer<UniParameters> uboParameters = null!;

    public float Distance { get; set; } = 6.0f;

    protected override TrRenderPass CreateRenderPass()
    {
        uboParameters = new(Context);

        using TrShader vert = new(Context, TrShaderType.Vertex, "Resources/Shaders/Grid/Grid.vert.spv".Path());
        using TrShader frag = new(Context, TrShaderType.Fragment, "Resources/Shaders/Grid/Grid.frag.spv".Path());

        TrRenderPipeline renderPipeline = new(Context, [vert, frag]);
        renderPipeline.SetRenderLayer(TrRenderLayer.Transparent);

        return new TrRenderPass(Context, [renderPipeline]);
    }

    protected override void AssemblePipeline(TrRenderPipeline renderPipeline, GlobalParameters globalParameters)
    {
        double logDistance = Math.Log2(Distance);
        double upperDistance = Math.Pow(2.0, Math.Floor(logDistance) + 1);
        double lowerDistance = Math.Pow(2.0, Math.Floor(logDistance));
        float fade = Convert.ToSingle((Distance - lowerDistance) / (upperDistance - lowerDistance));

        double level = -Math.Floor(logDistance);
        float primaryScale = Convert.ToSingle(Math.Pow(2.0, level));
        float secondaryScale = Convert.ToSingle(Math.Pow(2.0, level + 1));
        float axisIntensity = 0.3f / primaryScale;

        uboParameters.SetData(new UniParameters()
        {
            Near = globalParameters.Camera.Near,
            Far = globalParameters.Camera.Far * 0.2f,
            PrimaryScale = primaryScale,
            SecondaryScale = secondaryScale,
            GridIntensity = 0.2f,
            AxisIntensity = axisIntensity,
            Fade = fade
        });

        renderPipeline.BindUniformBlock(UniformBufferBindingStart + 0, uboParameters);
    }

    protected override void RenderPipeline(TrRenderPipeline renderPipeline, TrMesh[] meshes, GlobalParameters globalParameters)
    {
        foreach (TrMesh mesh in meshes)
        {
            mesh.Draw();
        }
    }

    protected override void ControllerCore()
    {
        float distance = Distance;
        ImGuiHelper.SliderFloat("Distance", ref distance, 0.0f, 10.0f);
        Distance = distance;
    }

    protected override void DestroyCore(bool disposing = false)
    {
        uboParameters.Dispose();
    }
}
