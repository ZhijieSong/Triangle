﻿using System.ComponentModel;
using Silk.NET.Input;
using Silk.NET.Maths;
using Triangle.Core;
using Triangle.Core.Graphics;
using Triangle.Core.Helpers;
using Triangle.Render.Contracts.Tutorials;
using Triangle.Render.Materials.Chapter7;

namespace Triangle.Render.Tutorials;

[DisplayName("Ramp 贴图")]
[Description("Ramp 贴图实现风格化渲染。")]
public class Tutorial06(IInputContext input, TrContext context) : BaseTutorial(input, context)
{
    #region Meshes
    private TrMesh[] knotMeshes = null!;
    #endregion

    #region Materials
    private RampTextureMat rampTextureMat = null!;
    #endregion

    #region Models
    private MeshModel knot = null!;
    #endregion

    protected override void Loaded()
    {
        knotMeshes = Context.AssimpParsing("Resources/Models/Knot.FBX".PathFormatter());

        rampTextureMat = new(Context);

        knot = new(TransformController, "Knot", knotMeshes, rampTextureMat);
        knot.SetTranslation(new Vector3D<float>(0, 2.0f, 0));
        knot.SetRotationByDegree(new Vector3D<float>(90.0f, 180.0f, 0));
        knot.SetScale(new Vector3D<float>(0.05f, 0.05f, 0.05f));

        PickupController.Add(knot);
    }

    protected override void UpdateScene(double deltaSeconds)
    {
    }

    protected override void RenderScene(double deltaSeconds)
    {
        knot.Render(GetBaseParameters());
    }

    protected override void EditProperties()
    {
    }

    protected override void Destroy(bool disposing = false)
    {
        rampTextureMat.Dispose();

        knotMeshes.Dispose();
    }
}