﻿using System.Runtime.InteropServices;
using Silk.NET.Maths;
using Triangle.Core.Enums;
using Triangle.Core.GameObjects;
using Triangle.Core.Graphics;
using Triangle.Core.Helpers;
using AttribLocation = uint;

namespace Triangle.Core.Materials;

internal sealed class DirectionalLightMat(TrContext context) : TrMaterial(context, "DirectionalLight")
{
    public const AttribLocation InPosition = 0;

    #region Uniforms
    [StructLayout(LayoutKind.Explicit)]
    private struct UniTransforms
    {
        [FieldOffset(0)]
        public Matrix4X4<float> Model;

        [FieldOffset(64)]
        public Matrix4X4<float> View;

        [FieldOffset(128)]
        public Matrix4X4<float> Projection;

        [FieldOffset(192)]
        public Matrix4X4<float> ObjectToWorld;

        [FieldOffset(256)]
        public Matrix4X4<float> ObjectToClip;

        [FieldOffset(320)]
        public Matrix4X4<float> WorldToObject;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct UniParameters
    {
        [FieldOffset(0)]
        public Vector4D<float> Color;
    }
    #endregion

    private TrBuffer<UniTransforms> uboTransforms = null!;
    private TrBuffer<UniParameters> uboParameters = null!;

    protected override TrRenderPass CreateRenderPass()
    {
        uboTransforms = new(Context);
        uboParameters = new(Context);

        using TrShader vert = new(Context, TrShaderType.Vertex, "Resources/Shaders/DirectionalLight/DirectionalLight.vert.spv".Path());
        using TrShader frag = new(Context, TrShaderType.Fragment, "Resources/Shaders/DirectionalLight/DirectionalLight.frag.spv".Path());

        TrRenderPipeline renderPipeline = new(Context, [vert, frag]);
        renderPipeline.SetRenderLayer(TrRenderLayer.Opaque);
        renderPipeline.IsCullFace = false;
        renderPipeline.Polygon = new(TrTriangleFace.FrontAndBack, TrPolygonMode.Line);

        return new TrRenderPass(Context, [renderPipeline]);
    }

    /// <summary>
    /// Draw directional light mesh.
    /// </summary>
    /// <param name="meshes">meshes</param>
    /// <param name="args">
    /// args:
    /// args[0] - Transform matrix
    /// args[1] - Camera
    /// args[2] - Color vec3
    /// </param>
    public override void Draw(TrMesh[] meshes, params object[] args)
    {
        if (args.Length != 3 || args[0] is not Matrix4X4<float> model || args[1] is not TrCamera camera || args[2] is not Vector3D<float> color)
        {
            throw new ArgumentException("Invalid arguments.");
        }

        foreach (TrRenderPipeline renderPipeline in RenderPass.RenderPipelines)
        {
            renderPipeline.Bind();

            uboTransforms.SetData(new UniTransforms()
            {
                Model = model,
                View = camera.Transform.View,
                Projection = camera.Projection,
                ObjectToWorld = model,
                ObjectToClip = model * camera.Transform.View * camera.Projection,
                WorldToObject = model.Invert()
            });
            uboParameters.SetData(new UniParameters()
            {
                Color = new(color.X, color.Y, color.Z, 1.0f)
            });

            renderPipeline.BindUniformBlock(0, uboTransforms);
            renderPipeline.BindUniformBlock(1, uboParameters);

            foreach (TrMesh mesh in meshes)
            {
                mesh.Draw();
            }

            renderPipeline.Unbind();
        }
    }

    /// <summary>
    /// Draw directional light model.
    /// </summary>
    /// <param name="models">models</param>
    /// <param name="args">
    /// args:
    /// args[0] - Camera
    /// args[1] - Color vec3
    /// </param>
    public override void Draw(TrModel[] models, params object[] args)
    {
        foreach (TrModel model in models)
        {
            Draw([.. model.Meshes], [model.Transform.Model, .. args]);
        }
    }

    protected override void ControllerCore()
    {
    }

    protected override void Destroy(bool disposing = false)
    {
        RenderPass.Dispose();

        uboTransforms.Dispose();
        uboParameters.Dispose();
    }
}
