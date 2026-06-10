using Godot;
using System;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

public partial class PlayerCamera : SpringArm3D
{

    [Export] private float m_MouseSensitivity = 0.005f;  
	[Export] private float m_VerticalLimit = 1.4f;       
	[Export] private RayCast3D m_RayCast3D;

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
			AddChild(cam);
			m_Camera3D = cam;
		}
		m_Camera3D.Current = true;
		Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _Process(double delta)
	{
		m_RayCast3D.GlobalRotation = this.GlobalRotation;

        GlobalPosition = m_Plyaer.GlobalPosition + new Vector3(0, 1.439f, 0);

        m_RayCast3D.GlobalRotation = this.GlobalRotation;
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
		// 水平旋转：绕世界 Y 轴旋转（左右看）
		RotateY(-mouseMotion.Relative.X * m_MouseSensitivity);

		// 垂直旋转：绕局部 X 轴旋转（上下看）
		float pitchDelta = -mouseMotion.Relative.Y * m_MouseSensitivity;
		float newPitch = Rotation.X + pitchDelta;

		// 限制垂直角度，避免翻转
		if (Mathf.Abs(newPitch) < m_VerticalLimit)
		{
			RotateObjectLocal(Vector3.Right, pitchDelta);
		}
	}
}
