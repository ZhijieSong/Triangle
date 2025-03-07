﻿using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Triangle.Core;
using Triangle.Core.Enums;
using Triangle.Core.Graphics;
using Triangle.Core.Helpers;
using Triangle.Render.Contracts.Materials;
using Triangle.Render.Models;

namespace Triangle.Render.Materials.Chapter6;

public class DiffuseVertexLevelMat(TrContext context) : GlobalMat(context, "DiffuseVertexLevel")
{
    #region Uniforms
    [StructLayout(LayoutKind.Explicit)]
    private struct UniMaterial
    {
        [FieldOffset(0)]
        public Vector4D<float> Diffuse;
    }
    #endregion

    private TrBuffer<UniMaterial> uboMaterial = null!;

    public Vector4D<float> Diffuse { get; set; } = new(1.0f, 1.0f, 1.0f, 1.0f);

    protected override TrRenderPass CreateRenderPass()
    {
        uboMaterial = new(Context);

        using TrShader vert = new(Context, TrShaderType.Vertex, "Resources/Shaders/Chapter6/DiffuseVertexLevel/DiffuseVertexLevel.vert.spv".Path());
        using TrShader frag = new(Context, TrShaderType.Fragment, "Resources/Shaders/Chapter6/DiffuseVertexLevel/DiffuseVertexLevel.frag.spv".Path());

        TrRenderPipeline renderPipeline = new(Context, [vert, frag]);
        renderPipeline.SetRenderLayer(TrRenderLayer.Opaque);

        return new TrRenderPass(Context, [renderPipeline]);
    }

    protected override void AssemblePipeline(TrRenderPipeline renderPipeline, GlobalParameters globalParameters)
    {
        uboMaterial.SetData(new UniMaterial()
        {
            Diffuse = Diffuse
        });

        renderPipeline.BindUniformBlock(UniformBufferBindingStart + 0, uboMaterial);
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
        Vector4D<float> diffuse = Diffuse;
        ImGuiHelper.ColorEdit4("Diffuse", ref diffuse);
        Diffuse = diffuse;
    }

    protected override void DestroyCore(bool disposing = false)
    {
        uboMaterial.Dispose();
    }
}
