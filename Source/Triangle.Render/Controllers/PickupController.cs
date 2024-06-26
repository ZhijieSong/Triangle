﻿using Silk.NET.Input;
using Silk.NET.Maths;
using Triangle.Core;
using Triangle.Core.Contracts;
using Triangle.Core.Controllers;
using Triangle.Core.GameObjects;
using Triangle.Core.Graphics;
using Triangle.Core.Helpers;
using Triangle.Render.Materials;
using Triangle.Render.Models;

namespace Triangle.Render.Controllers;

public class PickupController : Disposable
{
    // 选中的颜色。
    public static readonly Vector4D<byte> PickupColor = new(255, 255, 255, 255);

    private readonly TrContext _context;
    private readonly TrScene _scene;
    private readonly SceneController _sceneController;

    private readonly TrFrame _frame;
    private readonly SolidColorInstancedMat _solidColorInstancedMat;

    private readonly TrFrame _pickupFrame;
    private readonly TrMesh _pickupMesh;
    private readonly EdgeDetectionMat _edgeDetectionMat;

    private TrModel[] models = null!;
    private Vector4D<float>[] modelColors = null!;

    private TrModel[] selectedModels = null!;

    public PickupController(TrContext context, TrScene scene, SceneController sceneController)
    {
        _context = context;
        _scene = scene;
        _sceneController = sceneController;
        _sceneController.ObjectsChanged += UpdateModels;
        _sceneController.SelectedObjectsChanged += UpdateSelectedModels;

        _frame = new TrFrame(context);
        _solidColorInstancedMat = new SolidColorInstancedMat(context);

        _pickupFrame = new TrFrame(context);
        _pickupMesh = context.GetCanvas();
        _edgeDetectionMat = new EdgeDetectionMat(context);

        UpdateModels();
        UpdateSelectedModels();

        void UpdateModels()
        {
            models = [.. _sceneController.Objects.Where(x => x is TrModel).Cast<TrModel>()];
            modelColors = [.. models.Select(item => item.ColorId.ToSingle())];
        }

        void UpdateSelectedModels()
        {
            selectedModels = [.. _sceneController.SelectedObjects.Where(x => x is TrModel).Cast<TrModel>()];
        }
    }

    /// <summary>
    /// 绘制描边效果。
    /// </summary>
    /// <param name="baseParameters">baseParameters</param>
    public void PostEffects(GlobalParameters baseParameters)
    {
        _edgeDetectionMat.Channel0 = _pickupFrame.Texture;

        _edgeDetectionMat.Draw([_pickupMesh], baseParameters);
    }

    /// <summary>
    /// 更新拾取。
    /// </summary>
    public void Update()
    {
        _frame.Update(_scene.Width, _scene.Height);
        _pickupFrame.Update(_scene.Width, _scene.Height, _scene.Samples);

        if (_scene.IsFocused && _scene.IsLeftClicked && !_scene.IsOperationTools)
        {
            Vector2D<float> point = new(_scene.Mouse.X, _scene.Mouse.Y);

            Rectangle<float> rectangle = new(0.0f, 0.0f, _scene.Width, _scene.Height);

            if (rectangle.Contains(point))
            {
                List<TrModel> temp = [.. selectedModels];

                bool anySelected = false;
                bool isMultiSelect = _scene.KeyPressed(Key.ControlLeft) || _scene.KeyPressed(Key.ControlRight);

                Vector4D<byte> pickColor = _frame.GetPixel(Convert.ToInt32(point.X), Convert.ToInt32(point.Y));

                foreach (TrModel model in models)
                {
                    if (model.ColorId == pickColor)
                    {
                        anySelected = true;

                        bool isSelected = temp.Contains(model);

                        if (isMultiSelect)
                        {
                            temp.Remove(model);
                        }
                        else
                        {
                            temp.Clear();
                        }

                        if (!isMultiSelect || isMultiSelect && !isSelected)
                        {
                            temp.Add(model);
                        }

                        break;
                    }
                }

                if (!anySelected)
                {
                    temp.Clear();
                }

                _sceneController.SelectObjects([.. temp]);
            }
        }
    }

    /// <summary>
    /// 绘制可被拾取的模型和已拾取的模型。
    /// </summary>
    /// <param name="baseParameters"></param>
    public void Render(GlobalParameters baseParameters)
    {
        _frame.Bind();
        {
            _context.Clear();

            _solidColorInstancedMat.Color = modelColors;
            _solidColorInstancedMat.Draw(models, baseParameters);
        }
        _frame.Unbind();

        _pickupFrame.Bind();
        {
            _context.Clear();

            _solidColorInstancedMat.Color = [PickupColor.ToSingle()];
            _solidColorInstancedMat.Draw(selectedModels, baseParameters);
        }
        _pickupFrame.Unbind();
    }

    protected override void Destroy(bool disposing = false)
    {
        _frame.Dispose();
        _solidColorInstancedMat.Dispose();

        _pickupFrame.Dispose();
        _edgeDetectionMat.Dispose();
    }
}
