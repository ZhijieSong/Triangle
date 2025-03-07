﻿using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using Triangle.Core;

namespace Triangle.Render.Contracts.Applications;

public interface IApplication : IDisposable
{
    void Initialize(IWindow window, IInputContext input, TrContext context);

    void Update(double deltaSeconds);

    void Render(double deltaSeconds);

    void ImGuiRender();

    void WindowResize(Vector2D<int> size);
}
