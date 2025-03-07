﻿using System.ComponentModel;
using Hexa.NET.ImGui;
using Silk.NET.Input;
using Silk.NET.Maths;
using Triangle.Core;
using Triangle.Core.Enums;
using Triangle.Core.GameObjects;
using Triangle.Core.Graphics;
using Triangle.Core.Helpers;
using Triangle.Render.Contracts.Tutorials;
using Triangle.Render.Materials;

namespace Triangle.Render.Tutorials;

[DisplayName("PBR with IBL")]
[Description("Physically Based Rendering (PBR) with Image-Based Lighting (IBL).")]
public class Tutorial07(IInputContext input, TrContext context) : BaseTutorial(input, context)
{
    // PBR: Maximum number of mip levels for prefiltered map (0 to 4)
    private const int MaxMipLevels = 4;

    #region Meshes
    private TrMesh cubeMesh = null!;
    #endregion

    #region Materials
    private EquirectangularToCubemapMat equirectangularToCubemapMat = null!;
    private IrradianceConvolutionMat irradianceConvolutionMat = null!;
    private PrefilterMat prefilterMat = null!;
    private BRDFMat brdfMat = null!;
    private PBRMat[] pbrMats = null!;
    #endregion

    #region Capture Skybox framebuffer
    private TrFrame skyPositiveX = null!;
    private TrFrame skyNegativeX = null!;
    private TrFrame skyPositiveY = null!;
    private TrFrame skyNegativeY = null!;
    private TrFrame skyPositiveZ = null!;
    private TrFrame skyNegativeZ = null!;
    #endregion

    #region Textures And CubeMaps
    private TrTexture flipSky = null!;
    private TrCubeMap envCubeMap = null!;
    private TrCubeMap irradianceMap = null!;
    private TrCubeMap prefilteredMap = null!;
    private TrTexture brdfLUTT = null!;
    #endregion

    #region Models
    private TrModel[] spheres = null!;
    #endregion

    protected override void Loaded()
    {
        cubeMesh = Context.GetCube(1.0f);

        equirectangularToCubemapMat = new(Context);
        irradianceConvolutionMat = new(Context);
        prefilterMat = new(Context);
        brdfMat = new(Context);

        flipSky = new(Context);

        envCubeMap = new(Context)
        {
            TextureMinFilter = TrTextureFilter.LinearMipmapLinear,
            TextureWrap = TrTextureWrap.ClampToEdge
        };
        envCubeMap.UpdateParameters();

        irradianceMap = new(Context)
        {
            TextureWrap = TrTextureWrap.ClampToEdge
        };
        irradianceMap.UpdateParameters();

        prefilteredMap = new(Context)
        {
            TextureMinFilter = TrTextureFilter.LinearMipmapLinear,
            TextureWrap = TrTextureWrap.ClampToEdge
        };
        prefilteredMap.UpdateParameters();

        brdfLUTT = new(Context);

        skyPositiveX = new(Context);
        skyNegativeX = new(Context);
        skyPositiveY = new(Context);
        skyNegativeY = new(Context);
        skyPositiveZ = new(Context);
        skyNegativeZ = new(Context);

        DirectoryInfo pbrDir = new("Resources/Textures/PBR".Path());
        DirectoryInfo[] pbrMaterials = pbrDir.GetDirectories();

        pbrMats = new PBRMat[pbrMaterials.Length];
        spheres = new TrModel[pbrMaterials.Length];
        for (int i = 0; i < pbrMaterials.Length; i++)
        {
            DirectoryInfo directory = pbrMaterials[i];

            PBRMat mat = new(Context)
            {
                Channel0 = TrTextureManager.Texture($"Resources/Textures/PBR/{directory.Name}/Albedo.png".Path()),
                Channel1 = TrTextureManager.Texture($"Resources/Textures/PBR/{directory.Name}/Normal.png".Path()),
                Channel2 = TrTextureManager.Texture($"Resources/Textures/PBR/{directory.Name}/Metallic.png".Path()),
                Channel3 = TrTextureManager.Texture($"Resources/Textures/PBR/{directory.Name}/Roughness.png".Path()),
                Channel4 = TrTextureManager.Texture($"Resources/Textures/PBR/{directory.Name}/AmbientOcclusion.png".Path()),
                Map0 = irradianceMap,
                Map1 = prefilteredMap,
                MaxMipLevels = MaxMipLevels,
                BRDF = brdfLUTT
            };
            pbrMats[i] = mat;

            spheres[i] = new($"Sphere [{directory.Name}]", [Context.GetSphere()], mat);
            spheres[i].Transform.Translate(new Vector3D<float>(-4.0f + (i * 2.0f), 0.0f, 0.0f));

            SceneController.Add(spheres[i]);
        }

        AddPointLight("Point Light [0]", out TrPointLight pointLight0);
        pointLight0.Transform.Translate(new Vector3D<float>(-1.0f, 1.0f, 2.0f));

        AddPointLight("Point Light [1]", out TrPointLight pointLight1);
        pointLight1.Transform.Translate(new Vector3D<float>(1.0f, 1.0f, 2.0f));

        AddPointLight("Point Light [2]", out TrPointLight pointLight2);
        pointLight2.Transform.Translate(new Vector3D<float>(-1.0f, -1.0f, 2.0f));

        AddPointLight("Point Light [3]", out TrPointLight pointLight3);
        pointLight3.Transform.Translate(new Vector3D<float>(1.0f, -1.0f, 2.0f));
    }

    protected override void UpdateScene(double deltaSeconds)
    {
    }

    protected override void RenderScene(double deltaSeconds)
    {
        foreach (var sphere in spheres)
        {
            sphere.Render(Parameters);
        }
    }

    public override void ImGuiRender()
    {
        base.ImGuiRender();

        if (ImGui.Begin("PBR"))
        {
            if (ImGui.Button("Generate PBR Maps"))
            {
                TryGeneratePBRMaps();
            }

            ImGui.End();
        }
    }

    protected override void Destroy(bool disposing = false)
    {
        equirectangularToCubemapMat.Dispose();
        irradianceConvolutionMat.Dispose();
        prefilterMat.Dispose();
        brdfMat.Dispose();
        foreach (PBRMat pbrMat in pbrMats)
        {
            pbrMat.Dispose();
        }

        skyPositiveX.Dispose();
        skyNegativeX.Dispose();
        skyPositiveY.Dispose();
        skyNegativeY.Dispose();
        skyPositiveZ.Dispose();
        skyNegativeZ.Dispose();

        flipSky.Dispose();
        envCubeMap.Dispose();
        irradianceMap.Dispose();
        prefilteredMap.Dispose();
        brdfLUTT.Dispose();
    }

    private void TryGeneratePBRMaps()
    {
        if (SkyMat.Channel0 == null)
        {
            return;
        }

        flipSky.Write(SkyMat.Channel0.Name, true);

        GenerateCubeMap();
        GenerateIrradianceMap();
        GeneratePrefilteredMap();
        GenerateBRDFLUT();
    }

    /// <summary>
    /// PBR: Convert equirectangular map to cubemap
    /// </summary>
    private void GenerateCubeMap()
    {
        const int width = 1024;
        const int height = 1024;

        equirectangularToCubemapMat.Channel0 = flipSky;
        equirectangularToCubemapMat.Projection = Matrix4X4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90.0f), 1.0f, 0.1f, 10.0f);
        equirectangularToCubemapMat.GammaCorrection = SkyMat.GammaCorrection;
        equirectangularToCubemapMat.Gamma = SkyMat.Gamma;
        equirectangularToCubemapMat.Exposure = SkyMat.Exposure;

        skyPositiveX.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyPositiveX.Bind();
        {
            Context.Clear();

            equirectangularToCubemapMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, Vector3D<float>.UnitX, -Vector3D<float>.UnitY);

            equirectangularToCubemapMat.Draw([cubeMesh], Parameters);
        }
        skyPositiveX.Unbind();

        skyNegativeX.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyNegativeX.Bind();
        {
            Context.Clear();

            equirectangularToCubemapMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, -Vector3D<float>.UnitX, -Vector3D<float>.UnitY);

            equirectangularToCubemapMat.Draw([cubeMesh], Parameters);
        }
        skyNegativeX.Unbind();

        skyPositiveY.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyPositiveY.Bind();
        {
            Context.Clear();

            equirectangularToCubemapMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, Vector3D<float>.UnitY, Vector3D<float>.UnitZ);

            equirectangularToCubemapMat.Draw([cubeMesh], Parameters);
        }
        skyPositiveY.Unbind();

        skyNegativeY.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyNegativeY.Bind();
        {
            Context.Clear();

            equirectangularToCubemapMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, -Vector3D<float>.UnitY, -Vector3D<float>.UnitZ);

            equirectangularToCubemapMat.Draw([cubeMesh], Parameters);
        }
        skyNegativeY.Unbind();

        skyPositiveZ.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyPositiveZ.Bind();
        {
            Context.Clear();

            equirectangularToCubemapMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, Vector3D<float>.UnitZ, -Vector3D<float>.UnitY);

            equirectangularToCubemapMat.Draw([cubeMesh], Parameters);
        }
        skyPositiveZ.Unbind();

        skyNegativeZ.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyNegativeZ.Bind();
        {
            Context.Clear();

            equirectangularToCubemapMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, -Vector3D<float>.UnitZ, -Vector3D<float>.UnitY);

            equirectangularToCubemapMat.Draw([cubeMesh], Parameters);
        }
        skyNegativeZ.Unbind();

        envCubeMap.Write(skyPositiveX.Texture, TrCubeMapFace.PositiveX);
        envCubeMap.Write(skyNegativeX.Texture, TrCubeMapFace.NegativeX);
        envCubeMap.Write(skyPositiveY.Texture, TrCubeMapFace.PositiveY);
        envCubeMap.Write(skyNegativeY.Texture, TrCubeMapFace.NegativeY);
        envCubeMap.Write(skyPositiveZ.Texture, TrCubeMapFace.PositiveZ);
        envCubeMap.Write(skyNegativeZ.Texture, TrCubeMapFace.NegativeZ);
        envCubeMap.GenerateMipmap();
    }

    /// <summary>
    /// PBR: Solve diffuse integral by convolution to create an irradiance (cube)map
    /// </summary>
    private void GenerateIrradianceMap()
    {
        const int width = 64;
        const int height = 64;

        irradianceConvolutionMat.Map0 = envCubeMap;
        irradianceConvolutionMat.Projection = Matrix4X4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90.0f), 1.0f, 0.1f, 10.0f);

        skyPositiveX.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyPositiveX.Bind();
        {
            Context.Clear();

            irradianceConvolutionMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, Vector3D<float>.UnitX, -Vector3D<float>.UnitY);

            irradianceConvolutionMat.Draw([cubeMesh], Parameters);
        }
        skyPositiveX.Unbind();

        skyNegativeX.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyNegativeX.Bind();
        {
            Context.Clear();

            irradianceConvolutionMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, -Vector3D<float>.UnitX, -Vector3D<float>.UnitY);

            irradianceConvolutionMat.Draw([cubeMesh], Parameters);
        }
        skyNegativeX.Unbind();

        skyPositiveY.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyPositiveY.Bind();
        {
            Context.Clear();

            irradianceConvolutionMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, Vector3D<float>.UnitY, Vector3D<float>.UnitZ);

            irradianceConvolutionMat.Draw([cubeMesh], Parameters);
        }
        skyPositiveY.Unbind();

        skyNegativeY.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyNegativeY.Bind();
        {
            Context.Clear();

            irradianceConvolutionMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, -Vector3D<float>.UnitY, -Vector3D<float>.UnitZ);

            irradianceConvolutionMat.Draw([cubeMesh], Parameters);
        }
        skyNegativeY.Unbind();

        skyPositiveZ.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyPositiveZ.Bind();
        {
            Context.Clear();

            irradianceConvolutionMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, Vector3D<float>.UnitZ, -Vector3D<float>.UnitY);

            irradianceConvolutionMat.Draw([cubeMesh], Parameters);
        }
        skyPositiveZ.Unbind();

        skyNegativeZ.Update(width, height, pixelFormat: TrPixelFormat.RGB16F);
        skyNegativeZ.Bind();
        {
            Context.Clear();

            irradianceConvolutionMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, -Vector3D<float>.UnitZ, -Vector3D<float>.UnitY);

            irradianceConvolutionMat.Draw([cubeMesh], Parameters);
        }
        skyNegativeZ.Unbind();

        irradianceMap.Write(skyPositiveX.Texture, TrCubeMapFace.PositiveX);
        irradianceMap.Write(skyNegativeX.Texture, TrCubeMapFace.NegativeX);
        irradianceMap.Write(skyPositiveY.Texture, TrCubeMapFace.PositiveY);
        irradianceMap.Write(skyNegativeY.Texture, TrCubeMapFace.NegativeY);
        irradianceMap.Write(skyPositiveZ.Texture, TrCubeMapFace.PositiveZ);
        irradianceMap.Write(skyNegativeZ.Texture, TrCubeMapFace.NegativeZ);
    }

    /// <summary>
    /// PBR: Run a quasi monte-carlo simulation on the environment lighting to create a prefilter (cube)map.
    /// </summary>
    private void GeneratePrefilteredMap()
    {
        const int width = 256;
        const int height = 256;

        prefilteredMap.Initialize(width, height, TrPixelFormat.RGB16F);
        prefilteredMap.GenerateMipmap();

        prefilterMat.Map0 = envCubeMap;
        prefilterMat.Projection = Matrix4X4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90.0f), 1.0f, 0.1f, 10.0f);

        for (int i = 0; i <= MaxMipLevels; i++)
        {
            prefilterMat.Roughness = (float)i / (MaxMipLevels - 1);

            int mipWidth = (int)(width * MathF.Pow(0.5f, i));
            int mipHeight = (int)(height * MathF.Pow(0.5f, i));

            GenerateMipMap(i, mipWidth, mipHeight);
        }

        void GenerateMipMap(int mipLevel, int mipWidth, int mipHeight)
        {
            skyPositiveX.Update(mipWidth, mipHeight, pixelFormat: TrPixelFormat.RGB16F);
            skyPositiveX.Bind();
            {
                Context.Clear();

                prefilterMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, Vector3D<float>.UnitX, -Vector3D<float>.UnitY);

                prefilterMat.Draw([cubeMesh], Parameters);
            }
            skyPositiveX.Unbind();

            skyNegativeX.Update(mipWidth, mipHeight, pixelFormat: TrPixelFormat.RGB16F);
            skyNegativeX.Bind();
            {
                Context.Clear();

                prefilterMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, -Vector3D<float>.UnitX, -Vector3D<float>.UnitY);

                prefilterMat.Draw([cubeMesh], Parameters);
            }
            skyNegativeX.Unbind();

            skyPositiveY.Update(mipWidth, mipHeight, pixelFormat: TrPixelFormat.RGB16F);
            skyPositiveY.Bind();
            {
                Context.Clear();

                prefilterMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, Vector3D<float>.UnitY, Vector3D<float>.UnitZ);

                prefilterMat.Draw([cubeMesh], Parameters);
            }
            skyPositiveY.Unbind();

            skyNegativeY.Update(mipWidth, mipHeight, pixelFormat: TrPixelFormat.RGB16F);
            skyNegativeY.Bind();
            {
                Context.Clear();

                prefilterMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, -Vector3D<float>.UnitY, -Vector3D<float>.UnitZ);

                prefilterMat.Draw([cubeMesh], Parameters);
            }
            skyNegativeY.Unbind();

            skyPositiveZ.Update(mipWidth, mipHeight, pixelFormat: TrPixelFormat.RGB16F);
            skyPositiveZ.Bind();
            {
                Context.Clear();

                prefilterMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, Vector3D<float>.UnitZ, -Vector3D<float>.UnitY);

                prefilterMat.Draw([cubeMesh], Parameters);
            }
            skyPositiveZ.Unbind();

            skyNegativeZ.Update(mipWidth, mipHeight, pixelFormat: TrPixelFormat.RGB16F);
            skyNegativeZ.Bind();
            {
                Context.Clear();

                prefilterMat.View = Matrix4X4.CreateLookAt(Vector3D<float>.Zero, -Vector3D<float>.UnitZ, -Vector3D<float>.UnitY);

                prefilterMat.Draw([cubeMesh], Parameters);
            }
            skyNegativeZ.Unbind();

            prefilteredMap.Write(skyPositiveX.Texture, TrCubeMapFace.PositiveX, mipLevel);
            prefilteredMap.Write(skyNegativeX.Texture, TrCubeMapFace.NegativeX, mipLevel);
            prefilteredMap.Write(skyPositiveY.Texture, TrCubeMapFace.PositiveY, mipLevel);
            prefilteredMap.Write(skyNegativeY.Texture, TrCubeMapFace.NegativeY, mipLevel);
            prefilteredMap.Write(skyPositiveZ.Texture, TrCubeMapFace.PositiveZ, mipLevel);
            prefilteredMap.Write(skyNegativeZ.Texture, TrCubeMapFace.NegativeZ, mipLevel);
        }
    }

    /// <summary>
    /// PBR: Generate a 2D LUT from the BRDF equations used.
    /// </summary>
    private void GenerateBRDFLUT()
    {
        const int width = 512;
        const int height = 512;

        skyPositiveX.Update(width, height, pixelFormat: TrPixelFormat.RG16F);
        skyPositiveX.Bind();
        {
            Context.Clear();

            brdfMat.Draw([cubeMesh], Parameters);
        }
        skyPositiveX.Unbind();

        brdfLUTT.Write(skyPositiveX);
    }
}
