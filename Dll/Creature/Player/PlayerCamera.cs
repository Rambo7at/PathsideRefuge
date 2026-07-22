using Godot;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

public partial class PlayerCamera : SpringArm3D
{

    [Export] private float m_MouseSensitivity = 0.005f;
    [Export] private float m_VerticalLimit = 1.4f;

    [Export] private Node3D CameraHolder;

    private Player m_Plyaer;

    private Camera3D m_Camera3D;  // 引用子节点 Camera3D，可以不导出，通过 GetNode 获取
    public override void _Ready()
    {
        if (GetParent() is not Player pl)
        {
            CatLog.Err($"[PlayerCamera._Ready]：检测挂载对象并非 player ，已返回");
            QueueFree();
            return;
        }



        if (pl.m_IsOwner == false)
        {
            CatLog.Net($"[PlayerCamera._Ready]：非所有组件，已销毁");
            CatUtils.StopAndExit(this);
            return;
        }


        m_Plyaer = pl;
        TopLevel = true;

        var cam = WorldManager.Instance.GetCamera();
        if (cam != null && cam.GetParent() != this)
        {
            cam.GetParent()?.RemoveChild(cam);
            CameraHolder.AddChild(cam);
            m_Camera3D = cam;
            m_Camera3D.Position = new Vector3(0.8f, 0.5f, 0f);
        }
        m_Camera3D.Current = true;

        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta)
    {

        GlobalPosition = m_Plyaer.GlobalPosition + new Vector3(0f, 1.439f, 0f);


    }


    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            if (Input.MouseMode == Input.MouseModeEnum.Visible) return;
            HandleMouseMotion(mouseMotion);
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion mouseMotion)
    {
        // 水平旋转：开了TopLevel后，RotateY 和 GlobalRotate 效果基本一致，保留你原来的写法也可以
        // 推荐改成绕世界Y轴，更严谨，避免极端情况的倾斜
        float yawDelta = -mouseMotion.Relative.X * m_MouseSensitivity;
        GlobalRotate(Vector3.Up, yawDelta);

        // 垂直旋转：保留你原来的逻辑也能用，推荐改成直接赋值，避免相对旋转误差
        float pitchDelta = -mouseMotion.Relative.Y * m_MouseSensitivity;
        float newPitch = Rotation.X + pitchDelta;
        newPitch = Mathf.Clamp(newPitch, -m_VerticalLimit, m_VerticalLimit);

        Rotation = new Vector3(newPitch, Rotation.Y, 0f);
    }
}
