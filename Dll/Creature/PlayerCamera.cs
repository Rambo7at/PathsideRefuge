using Godot;
using System;

public partial class PlayerCamera : SpringArm3D
{

	[Export] private Camera3D m_Camera3D;  // 引用子节点 Camera3D，可以不导出，通过 GetNode 获取
	[Export] private float m_MouseSensitivity = 0.005f;  // 鼠标灵敏度，弧度/像素
	[Export] private float m_VerticalLimit = 1.4f;       // 垂直旋转限制（约80度），单位弧度

	[Export] private RayCast3D m_RayCast3D;


	public override void _Ready()
	{
		// 可选：启动时捕获鼠标
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Process(double delta)
	{
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
