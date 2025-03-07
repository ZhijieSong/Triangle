﻿using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using Silk.NET.OpenGL;
using StbiSharp;
using Triangle.Core.Contracts.Graphics;
using Triangle.Core.Enums;
using Triangle.Core.Helpers;

namespace Triangle.Core.Graphics;

public unsafe class TrTexture : TrGraphics<TrContext>
{
    public TrTexture(TrContext context) : base(context)
    {
        GL gl = Context.GL;

        Handle = gl.CreateTexture(GLEnum.Texture2D);
        Name = $"Texture {Handle}";

        UpdateParameters();
    }

    public string Name { get; set; }

    public uint Width { get; private set; }

    public uint Height { get; private set; }

    public TrPixelFormat PixelFormat { get; private set; }

    public int Anisotropy { get; set; } = 16;

    public TrTextureFilter TextureMinFilter { get; set; } = TrTextureFilter.Linear;

    public TrTextureFilter TextureMagFilter { get; set; } = TrTextureFilter.Linear;

    public TrTextureWrap TextureWrap { get; set; } = TrTextureWrap.Repeat;

    public bool IsGenerateMipmap { get; set; }

    protected override void Destroy(bool disposing = false)
    {
        GL gl = Context.GL;

        gl.DeleteTexture(Handle);
    }

    public void Write(string file, bool flip = false)
    {
        Name = Path.GetFileName(file);

        (uint width, uint height, TrPixelFormat pixelFormat, nint data) = ReadImageAndAllocateMemory(file, flip);

        Write(width, height, pixelFormat, (void*)data);

        Marshal.FreeHGlobal(data);
    }

    public void EnqueueWrite(string file, bool flip = false)
    {
        Name = Path.GetFileName(file);

        Task.Run(() =>
        {
            (uint width, uint height, TrPixelFormat pixelFormat, nint data) = ReadImageAndAllocateMemory(file, flip);

            Context.Enqueue(() =>
            {
                Write(width, height, pixelFormat, (void*)data);

                Stbi.Free((void*)data);
            });
        });
    }

    public void Write(TrFrame frame)
    {
        byte[] pixels = frame.GetPixels();

        fixed (byte* ptr = pixels)
        {
            Write((uint)frame.Width, (uint)frame.Height, frame.PixelFormat, ptr);
        }
    }

    public void Write(uint width, uint height, TrPixelFormat pixelFormat, void* data)
    {
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;

        GL gl = Context.GL;

        (GLEnum Target, GLEnum Format, GLEnum Type) = PixelFormat.ToGL();

        uint levels = 1;
        if (IsGenerateMipmap)
        {
            levels = (uint)MathF.Floor(MathF.Log2(MathF.Max(Width, Height))) + 1;
        }

        UnpackAlignment(Width);
        {
            gl.TextureStorage2D(Handle, levels, Target, Width, Height);
            gl.TextureSubImage2D(Handle, 0, 0, 0, Width, Height, Format, Type, data);
        }
        ReUnpackAlignment();

        UpdateParameters();
    }

    public void SubWrite(int x, int y, uint width, uint height, TrPixelFormat pixelFormat, void* data)
    {
        GL gl = Context.GL;

        (GLEnum _, GLEnum Format, GLEnum Type) = pixelFormat.ToGL();

        UnpackAlignment(width);
        {
            gl.TextureSubImage2D(Handle, 0, x, y, width, height, Format, Type, data);
        }
        ReUnpackAlignment();
    }

    public void SubWrite(int x, int y, uint width, uint height, TrPixelFormat pixelFormat, TrPixelBuffer pixelBuffer)
    {
        GL gl = Context.GL;

        (GLEnum _, GLEnum Format, GLEnum Type) = pixelFormat.ToGL();

        UnpackAlignment(width);
        {
            gl.BindBuffer(GLEnum.PixelUnpackBuffer, pixelBuffer.Handle);
            gl.TextureSubImage2D(Handle, 0, x, y, width, height, Format, Type, (void*)0);
            gl.BindBuffer(GLEnum.PixelUnpackBuffer, 0);
        }
        ReUnpackAlignment();
    }

    public void Clear(uint width, uint height, TrPixelFormat pixelFormat)
    {
        Width = width;
        Height = height;
        PixelFormat = pixelFormat;

        GL gl = Context.GL;

        (GLEnum Target, GLEnum Format, GLEnum Type) = pixelFormat.ToGL();

        gl.BindTexture(GLEnum.Texture2D, Handle);
        gl.TexImage2D(GLEnum.Texture2D, 0, (int)Target, Width, Height, 0, Format, Type, null);
        gl.BindTexture(GLEnum.Texture2D, 0);
    }

    public byte[] GetPixels()
    {
        GL gl = Context.GL;

        byte[] pixels = new byte[Width * Height * PixelFormat.Size()];

        fixed (byte* ptr = pixels)
        {
            (GLEnum _, GLEnum Format, GLEnum Type) = PixelFormat.ToGL();

            gl.BindTexture(GLEnum.Texture2D, Handle);
            gl.GetTexImage(GLEnum.Texture2D, 0, Format, Type, ptr);
            gl.BindTexture(GLEnum.Texture2D, 0);
        }

        return pixels;
    }

    public void AdjustImGuiProperties()
    {
        ImGui.PushID(GetHashCode());

        ImGui.SeparatorText($"{Name}");

        ImGui.Text($"Size: {Width}x{Height}, Format: {PixelFormat}");

        int anisotropy = Anisotropy;
        ImGui.DragInt("Anisotropy", ref anisotropy, 1, 0, 16);
        Anisotropy = anisotropy;

        TrTextureFilter textureMinFilter = TextureMinFilter;
        ImGuiHelper.EnumCombo("Texture Min Filter", ref textureMinFilter);
        TextureMinFilter = textureMinFilter;

        TrTextureFilter textureMagFilter = TextureMagFilter;
        ImGuiHelper.EnumCombo("Texture Mag Filter", ref textureMagFilter);
        TextureMagFilter = textureMagFilter;

        TrTextureWrap textureWrap = TextureWrap;
        ImGuiHelper.EnumCombo("Texture Wrap", ref textureWrap);
        TextureWrap = textureWrap;

        bool isGenerateMipmap = IsGenerateMipmap;
        ImGui.Checkbox("Generate Mipmap", ref isGenerateMipmap);
        IsGenerateMipmap = isGenerateMipmap;

        ImGuiHelper.Image(this);

        UpdateParameters();

        ImGui.PopID();
    }

    public void UpdateParameters()
    {
        GL gl = Context.GL;

        gl.BindTexture(GLEnum.Texture2D, Handle);

        gl.TexParameter(GLEnum.Texture2D, GLEnum.MaxTextureMaxAnisotropy, Anisotropy);
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMinFilter, (int)TextureMinFilter.ToGL());
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureMagFilter, (int)TextureMagFilter.ToGL());
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapS, (int)TextureWrap.ToGL());
        gl.TexParameter(GLEnum.Texture2D, GLEnum.TextureWrapT, (int)TextureWrap.ToGL());
        if (IsGenerateMipmap)
        {
            gl.GenerateMipmap(GLEnum.Texture2D);
        }

        gl.BindTexture(GLEnum.Texture2D, 0);
    }

    private (uint width, uint height, TrPixelFormat pixelFormat, nint data) ReadImageAndAllocateMemory(string file, bool flip = false)
    {
        byte[] bytes = File.ReadAllBytes(file);

        fixed (byte* ptr = bytes)
        {
            int length = bytes.Length;
            int width;
            int height;
            int comp;
            void* pixels;
            TrPixelFormat pixelFormat;

            Stbi.SetFlipVerticallyOnLoad(flip);

            if (Stbi.IsHdrFromMemory(ptr, length))
            {
                pixels = Stbi.LoadFFromMemory(ptr, length, out width, out height, out comp, 0);

                if (comp == 1)
                {
                    pixelFormat = TrPixelFormat.R16F;
                }
                else if (comp == 2)
                {
                    pixelFormat = TrPixelFormat.RG16F;
                }
                else if (comp == 3)
                {
                    pixelFormat = TrPixelFormat.RGB16F;
                }
                else
                {
                    pixelFormat = TrPixelFormat.RGBA16F;
                }
            }
            else
            {
                pixels = Stbi.LoadFromMemory(ptr, length, out width, out height, out comp, 0);

                if (comp == 1)
                {
                    pixelFormat = TrPixelFormat.R8;
                }
                else if (comp == 2)
                {
                    pixelFormat = TrPixelFormat.RG8;
                }
                else if (comp == 3)
                {
                    pixelFormat = TrPixelFormat.RGB8;
                }
                else
                {
                    pixelFormat = TrPixelFormat.RGBA8;
                }
            }

            return ((uint)width, (uint)height, pixelFormat, (nint)pixels);
        }
    }

    private void UnpackAlignment(uint width)
    {
        GL gl = Context.GL;

        gl.PixelStore(GLEnum.UnpackAlignment, width % 4 == 0 ? 4 : 1);
    }

    private void ReUnpackAlignment()
    {
        GL gl = Context.GL;

        gl.PixelStore(GLEnum.UnpackAlignment, 4);
    }
}
