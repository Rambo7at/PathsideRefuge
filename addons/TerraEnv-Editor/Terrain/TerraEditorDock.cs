using Godot;
using TerraEnvEditor.View;

namespace TerraEnvEditor.Terrain;

/// <summary>注：地形编辑工具停靠面板，处理3D视口输入与笔刷显示</summary>
[Tool]
public partial class TerraEditorDock : EditorDock
{
    public enum E_EditorMod
    {
        None,
        ModifyHeight,   // 增高/降低合并
        SetHeight,      // 指定高度
        Flatten,        // 平整
        Smooth          // 平滑
    }

    private MeshInstance3D _brush;          // 笔刷网格实例
    private bool _isPainting;               // 是否正在绘制（左键按下）
    private Vector3 _lastHitPoint;          // 上一次射线命中点（世界坐标）
    private TerrainRegion _terrainRegion;   // 当前编辑的地形区域缓存
    private PackedScene _viewPacked;        // 工具视图场景资源
    private TerraEditorDockView _toolView; // 工具视图实例
    private double _paintTimer;             // 持续绘制计时器
    private const float PaintInterval = 0.03f; // 持续绘制间隔（约30次/秒）
    private Vector2 _lastMousePos;          // 缓存的3D视口鼠标坐标

    /// <summary>注：初始化停靠面板，加载工具视图场景</summary>
    public override void _Ready()
    {
        _viewPacked ??= GD.Load<PackedScene>("res://addons/TerraEnv-Editor/View/TerraEditorDockView.tscn");
        if (_viewPacked == null)
        {
            GD.Print("[TerrainEditorTool]：无法加载工具视图场景");
            return;
        }

        if (_viewPacked.Instantiate() is not TerraEditorDockView view)
        {
            GD.Print("[TerrainEditorTool]：视图类型不匹配");
            return;
        }

        _toolView ??= view;
        AddChild(_toolView);
    }


    /// <summary>注：面板销毁时清理场景节点引用，避免序列化时访问已释放对象</summary>
    public override void _ExitTree()
    {
        if (IsInstanceValid(_brush))
            _brush.QueueFree();
        _brush = null;
        _terrainRegion = null;
        _toolView = null;
    }



    /// <summary>注：初始化停靠面板标题和默认位置</summary>
    public void Init()
    {
        Title = "地形工具";
        DefaultSlot = DockSlot.Bottom;
    }

    /// <summary>注：处理3D视口输入事件，更新笔刷位置并执行绘制</summary>
    public void HandleInput(Camera3D camera, InputEvent @event)
    {
        if (@event is InputEventMouseButton button)
        {
            if (button.ButtonIndex != MouseButton.Left) return;
            _isPainting = button.Pressed;
            if (_isPainting) _paintTimer = 0f; // 按下时重置计时器，立即响应
        }

        if (@event is InputEventMouseMotion motion)
        {
            _lastMousePos = motion.Position;
            TryPaint(camera, motion.Position);
        }
    }

    /// <summary>注：尝试在当前鼠标位置执行绘制（射线检测+笔刷更新+修改地形）</summary>
    private void TryPaint(Camera3D camera, Vector2 mousePos)
    {
        var origin = camera.ProjectRayOrigin(mousePos);
        var dir = camera.ProjectRayNormal(mousePos);
        var rayResult = camera.GetWorld3D().DirectSpaceState.IntersectRay(PhysicsRayQueryParameters3D.Create(origin, origin + dir * 1000f));

        if (rayResult.Count == 0) return;

        Vector3 hitPoint = (Vector3)rayResult["position"];

        if (!_isPainting && hitPoint.DistanceTo(_lastHitPoint) < 0.01f) return;

        _lastHitPoint = hitPoint;

        Node collider = rayResult["collider"].As<Node>();
        FindTerrainRegionUpwards(collider);
        if (!IsInstanceValid(_terrainRegion)) return;
        if (_toolView == null || !IsInstanceValid(_toolView)) return;


        EnsureBrush(_terrainRegion);
        float radius = _toolView.RadiusSlider.GetSliderValue();
        if (_brush.Mesh is PlaneMesh plane) plane.Size = new Vector2(radius * 2f, radius * 2f);

        _brush.Visible = true;
        _brush.GlobalPosition = hitPoint + Vector3.Up * 1f;

        if (_isPainting) ModifyTerrainHeight(_terrainRegion, hitPoint);
    }

    /// <summary>注：持续绘制：按住左键不动时，定时重新射线检测并修改地形</summary>
    public override void _Process(double delta)
    {
        if (!_isPainting) return;
        if (_toolView == null || !IsInstanceValid(_terrainRegion)) return;

        _paintTimer += delta;
        if (_paintTimer < PaintInterval) return;
        _paintTimer = 0f;

        var camera = EditorInterface.Singleton?.GetEditorViewport3D(0)?.GetCamera3D();
        if (camera == null) return;

        TryPaint(camera, _lastMousePos);
    }


    /// <summary>注：根据当前工具设置修改地形高度</summary>
    private void ModifyTerrainHeight(TerrainRegion parent, Vector3 hitPoint)
    {
        if (_toolView == null) return;

        float radius = _toolView.RadiusSlider.GetSliderValue();

        switch (_toolView.GetCurrentMode())
        {
            case E_EditorMod.ModifyHeight:
                {
                    float delta = _toolView.HeightDeltaSlider.GetSliderValue();
                    parent.RaiseHeightPoint(hitPoint, radius, delta);
                    break;
                }
            case E_EditorMod.SetHeight:
                {
                    float targetHeight = _toolView.TargetHeightSlider.GetSliderValue();
                    parent.ModifyHeightPoint(hitPoint, radius, targetHeight);
                    break;
                }
            case E_EditorMod.Smooth:
                {
                    float strength = _toolView.SmoothSlider.GetSliderValue();
                    parent.SmoothHeightPoint(hitPoint, radius, strength);
                    break;
                }
            case E_EditorMod.Flatten:
                {
                    float strength = _toolView.SmoothSlider.GetSliderValue();
                    parent.FlattenHeightPoint(hitPoint, radius, strength);
                    break;
                }

        }
    }


    /// <summary>注：从碰撞体向上查找 TerrainRegion，并缓存有效引用</summary>
    private void FindTerrainRegionUpwards(Node node)
    {
        if (IsInstanceValid(_terrainRegion) && _terrainRegion.IsInsideTree())
            return;

        _terrainRegion = null;

        if (node == null) return;

        Node current = node;
        for (int i = 0; i < 5; i++)
        {
            if (current == null) break;
            if (current is TerrainRegion region)
            {
                _terrainRegion = region;
                break;
            }
            current = current.GetParent();
        }
    }

    /// <summary>注：确保笔刷网格存在，若无效则重新创建并添加到地形区域</summary>
    private void EnsureBrush(TerrainRegion parent)
    {
        if (IsInstanceValid(_brush)) return;

        _brush = new MeshInstance3D { Visible = false };

        var plane = new PlaneMesh { Size = new Vector2(4, 4) };
        _brush.Mesh = plane;

        // 生成圆形透明纹理
        var img = Image.CreateEmpty(128, 128, false, Image.Format.Rgba8);
        img.Fill(Colors.Transparent);
        for (int x = 0; x < 128; x++)
            for (int y = 0; y < 128; y++)
            {
                float dx = x - 64, dy = y - 64;
                if (dx * dx + dy * dy < 60 * 60)
                    img.SetPixel(x, y, Colors.White);
            }
        var tex = ImageTexture.CreateFromImage(img);

        var mat = new StandardMaterial3D
        {
            AlbedoTexture = tex,
            AlbedoColor = new Color(1, 0, 0, 0.5f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled
        };
        _brush.SetSurfaceOverrideMaterial(0, mat);

        parent.AddChild(_brush);
    }


}

