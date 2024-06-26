﻿using System.Numerics;
using Hexa.NET.ImGui;
using Hexa.NET.ImGuizmo;
using Silk.NET.Input;
using Silk.NET.Maths;
using Triangle.Core.Contracts.Graphics;
using Triangle.Core.GameObjects;
using Triangle.Core.Helpers;
using Triangle.Core.Structs;

namespace Triangle.Core.Graphics;

public class TrScene : TrGraphics<TrContext>
{
    public event Action<Vector2D<int>>? FramebufferResize;
    public event Action? DrawContentInWindow;

    private readonly IMouse _mouse;
    private readonly IKeyboard _keyboard;
    private readonly TrFrame _frame;

    private bool firstMove = true;
    private Vector2D<float> lastPos;

    private ImGuiWindowFlags gizmoWindowFlags = ImGuiWindowFlags.NoMove;

    public TrScene(IInputContext input, TrContext context, string name) : base(context)
    {
        Name = name;
        Camera = new TrCamera("Main Camera")
        {
            Fov = 45.0f
        };
        Camera.Transform.Translate(new Vector3D<float>(0.0f, 2.0f, 8.0f));

        _mouse = input.Mice[0];
        _keyboard = input.Keyboards[0];
        _frame = new TrFrame(Context);
    }

    public string Name { get; }

    public TrFrame Frame => _frame;

    public string HostName => $"{Name} - Frame Id: {_frame.Handle}";

    public TrCamera Camera { get; }

    public int Left { get; private set; }

    public int Top { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public bool IsVisible { get; private set; }

    public bool IsHovered { get; private set; }

    public bool IsFocused { get; private set; }

    public bool IsLeftClicked { get; private set; }

    public bool IsRightClicked { get; private set; }

    public bool IsMiddleClicked { get; private set; }

    public Vector4D<float> Mouse { get; private set; }

    public Vector4D<float> Date { get; private set; }

    public float Time { get; private set; }

    public float DeltaTime { get; private set; }

    public float FrameRate { get; private set; }

    public int FrameCount { get; private set; }

    public bool IsClosed { get; private set; }

    public bool IsOperationTools { get; private set; }

    public int Samples { get; set; } = 4;

    public bool UseTools { get; set; } = true;

    public ImGuizmoOperation GizmosOperation { get; set; } = ImGuizmoOperation.Translate;

    public ImGuizmoMode GizmosSpace { get; set; } = ImGuizmoMode.Local;

    public TrSceneData SceneData => new(new Vector2D<float>(Width, Height), Mouse, Date, Time, DeltaTime, FrameRate, FrameCount);

    public void Update(double deltaSeconds)
    {
        if (IsHovered)
        {
            if (MousePressed(MouseButton.Right))
            {
                Vector2D<float> vector = new(_mouse.Position.X, _mouse.Position.Y);

                if (firstMove)
                {
                    lastPos = vector;

                    firstMove = false;
                }
                else
                {
                    float deltaX = vector.X - lastPos.X;
                    float deltaY = vector.Y - lastPos.Y;

                    Camera.Transform.Rotate(new Vector3D<float>(-deltaY * Camera.Sensitivity, -deltaX * Camera.Sensitivity, 0.0f));

                    lastPos = vector;
                }
            }
            else
            {
                firstMove = true;
            }

            if (KeyPressed(Key.W))
            {
                Camera.Transform.Translate(Camera.Speed * (float)deltaSeconds * TrTransform.DefaultForward);
            }

            if (KeyPressed(Key.A))
            {
                Camera.Transform.Translate(Camera.Speed * (float)deltaSeconds * TrTransform.DefaultLeft);
            }

            if (KeyPressed(Key.S))
            {
                Camera.Transform.Translate(Camera.Speed * (float)deltaSeconds * TrTransform.DefaultBackward);
            }

            if (KeyPressed(Key.D))
            {
                Camera.Transform.Translate(Camera.Speed * (float)deltaSeconds * TrTransform.DefaultRight);
            }

            if (KeyPressed(Key.Q))
            {
                Camera.Transform.Translate(Camera.Speed * (float)deltaSeconds * TrTransform.DefaultDown);
            }

            if (KeyPressed(Key.E))
            {
                Camera.Transform.Translate(Camera.Speed * (float)deltaSeconds * TrTransform.DefaultUp);
            }

            if (KeyPressed(Key.Number1))
            {
                GizmosOperation = ImGuizmoOperation.Translate;
            }

            if (KeyPressed(Key.Number2))
            {
                GizmosOperation = ImGuizmoOperation.Rotate;
            }

            if (KeyPressed(Key.Number3))
            {
                GizmosOperation = ImGuizmoOperation.Scale;
            }
        }

        Camera.Width = Width;
        Camera.Height = Height;
    }

    public void Begin()
    {
        _frame.Update(Width, Height, Samples);

        _frame.Bind();
    }

    public void End()
    {
        _frame.Unbind();
    }

    public void DrawHost()
    {
        if (IsClosed)
        {
            return;
        }

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));
        {
            bool isOpen = true;
            if (IsVisible = ImGui.Begin(HostName, ref isOpen, ImGuiWindowFlags.NoSavedSettings | gizmoWindowFlags))
            {
                ImGuizmo.SetDrawlist();
                ImGuizmo.SetRect(Left, Top, Width, Height);

                // 鼠标在窗口中的位置。
                Vector2 pos = ImGui.GetMousePos() - ImGui.GetCursorScreenPos();
                bool isLeftDown = ImGui.IsMouseDown(ImGuiMouseButton.Left);
                bool isRightDown = ImGui.IsMouseDown(ImGuiMouseButton.Right);

                // 日期。
                DateTime now = DateTime.Now;
                TimeSpan timeSinceMidnight = now - now.Date;
                float seconds = Convert.ToSingle(timeSinceMidnight.TotalMilliseconds / 1000.0);

                IsHovered = ImGui.IsWindowHovered();
                IsFocused = ImGui.IsWindowFocused();
                IsLeftClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
                IsRightClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Right);
                IsMiddleClicked = ImGui.IsMouseClicked(ImGuiMouseButton.Middle);
                Mouse = IsFocused ? new Vector4D<float>(pos.X, pos.Y, Convert.ToSingle(isLeftDown), Convert.ToSingle(isRightDown)) : Vector4D<float>.Zero;

                Date = new Vector4D<float>(now.Year, now.Month, now.Day, seconds);
                Time = Convert.ToSingle(ImGui.GetTime());
                DeltaTime = ImGui.GetIO().DeltaTime;
                FrameRate = ImGui.GetIO().Framerate;
                FrameCount = ImGui.GetFrameCount();

                // 移动窗口。
                {
                    ImGuiWindowPtr window = ImGui.GetCurrentWindow();

                    gizmoWindowFlags = IsHovered && ImGui.IsMouseHoveringRect(window.InnerRect.Min, window.InnerRect.Max) ? ImGuiWindowFlags.NoMove : ImGuiWindowFlags.None;
                }

                // 窗口位置。
                {
                    Vector2 offset = ImGui.GetCursorScreenPos();

                    Left = Convert.ToInt32(offset.X);
                    Top = Convert.ToInt32(offset.Y);
                }

                // 窗口大小。
                {
                    Vector2 size = ImGui.GetContentRegionAvail();

                    int newWidth = Convert.ToInt32(size.X);
                    int newHeight = Convert.ToInt32(size.Y);

                    if (newWidth - Width != 0 || newHeight - Height != 0)
                    {
                        Width = newWidth;
                        Height = newHeight;

                        FramebufferResize?.Invoke(new Vector2D<int>(Width, Height));
                    }
                }

                Vector2 startPos = ImGui.GetCursorPos();

                ImGuiHelper.Frame(_frame);

                // 窗口小部件。
                if (UseTools)
                {
                    IsOperationTools = false;

                    ImGui.SetCursorPos(startPos + new Vector2(8.0f, 8.0f));
                    ImGuiHelper.ButtonSelected($"{FontAwesome6.IconArrowsUpDownLeftRight}", () => GizmosOperation = ImGuizmoOperation.Translate, GizmosOperation == ImGuizmoOperation.Translate);
                    ImGuiHelper.ShowHelpMarker(ImGuizmoOperation.Translate.ToString());
                    IsOperationTools |= ImGui.IsItemClicked();

                    ImGui.SetCursorPos(startPos + new Vector2(8.0f + 30.0f, 8.0f));
                    ImGuiHelper.ButtonSelected($"{FontAwesome6.IconArrowsSpin}", () => GizmosOperation = ImGuizmoOperation.Rotate, GizmosOperation == ImGuizmoOperation.Rotate);
                    ImGuiHelper.ShowHelpMarker(ImGuizmoOperation.Rotate.ToString());
                    IsOperationTools |= ImGui.IsItemClicked();

                    ImGui.SetCursorPos(startPos + new Vector2(8.0f + 60.0f, 8.0f));
                    ImGuiHelper.ButtonSelected($"{FontAwesome6.IconGroupArrowsRotate}", () => GizmosOperation = ImGuizmoOperation.Scale, GizmosOperation == ImGuizmoOperation.Scale);
                    ImGuiHelper.ShowHelpMarker(ImGuizmoOperation.Scale.ToString());
                    IsOperationTools |= ImGui.IsItemClicked();

                    ImGui.SetCursorPos(startPos + new Vector2(8.0f + 90.0f, 8.0f));
                    if (GizmosSpace == ImGuizmoMode.Local)
                    {
                        ImGuiHelper.Button($"{FontAwesome6.IconCube}", () => GizmosSpace = ImGuizmoMode.World);
                    }
                    else if (GizmosSpace == ImGuizmoMode.World)
                    {
                        ImGuiHelper.Button($"{FontAwesome6.IconGlobe}", () => GizmosSpace = ImGuizmoMode.Local);
                    }
                    ImGuiHelper.ShowHelpMarker(GizmosSpace.ToString());
                    IsOperationTools |= ImGui.IsItemClicked();

                    float[] view = Camera.Transform.View.ToArray();

                    ImGuizmo.ViewManipulate(ref view[0],
                                            Camera.Transform.Position.Length,
                                            new Vector2(Left + Width - 64.0f, Top),
                                            new Vector2(64.0f, 64.0f),
                                            ImGui.GetColorU32(Vector4.Zero));

                    Camera.DecomposeView(view.ToMatrix());

                    IsOperationTools |= ImGuizmo.IsOver();
                }

                ImGui.SetCursorPos(startPos);

                DrawContentInWindow?.Invoke();

                ImGui.End();
            }

            IsClosed = !isOpen;
        }
        ImGui.PopStyleVar();
    }

    public bool MousePressed(MouseButton button)
    {
        return _mouse.IsButtonPressed(button);
    }

    public bool KeyPressed(Key key)
    {
        return _keyboard.IsKeyPressed(key);
    }

    protected override void Destroy(bool disposing = false)
    {
        _frame.Dispose();
    }
}
