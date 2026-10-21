using Env.UI;
using Godot;
using static TerraEnvEditor.Terrain.TerraEditorDock;

namespace TerraEnvEditor.View;

[Tool]
public partial class TerraEditorDockView : PanelContainer
{
	[Export] public EditorSliderUI TargetHeightSlider { get; set; }
	[Export] public EditorSliderUI HeightDeltaSlider { get; set; }
	[Export] public EditorSliderUI SmoothSlider { get; set; }
	[Export] public EditorSliderUI RadiusSlider { get; set; }

	[Export] public Button ModifyButton { get; set; }     // 增高/降低按钮
	[Export] public Button SetHeightButton { get; set; }  // 指定高度按钮
	[Export] public Button SmoothButton { get; set; }     // 平滑按钮
	[Export] public Button FlattenButton { get; set; }     // 平滑按钮

	private ButtonGroup _modeButtonGroup;                 // 按钮组，实现互斥
	private E_EditorMod _currentMode = E_EditorMod.ModifyHeight; // 当前编辑模式

	/// <summary>注：初始化时检查所有必需子滑块和按钮是否存在</summary>
	public override void _Ready()
	{
		if (TargetHeightSlider == null || HeightDeltaSlider == null || SmoothSlider == null || RadiusSlider == null || ModifyButton == null || SetHeightButton == null || SmoothButton == null || FlattenButton == null)
		{
			GD.Print("[Env.TerrainEditorToolView]：检测有子UI组件为空。已销毁");
			SetProcess(false);
			QueueFree();
			return;
		}

		// 配置按钮组
		_modeButtonGroup = new ButtonGroup();

		ModifyButton.ToggleMode = true;
		SetHeightButton.ToggleMode = true;
		SmoothButton.ToggleMode = true;
		FlattenButton.ToggleMode = true;

		ModifyButton.ButtonGroup = _modeButtonGroup;
		SetHeightButton.ButtonGroup = _modeButtonGroup;
		SmoothButton.ButtonGroup = _modeButtonGroup;
		FlattenButton.ButtonGroup = _modeButtonGroup;

		ModifyButton.Pressed += () => OnModeButtonPressed(E_EditorMod.ModifyHeight);
		SetHeightButton.Pressed += () => OnModeButtonPressed(E_EditorMod.SetHeight);
		SmoothButton.Pressed += () => OnModeButtonPressed(E_EditorMod.Smooth);
		FlattenButton.Pressed += () => OnModeButtonPressed(E_EditorMod.Flatten);
	}

	/// <summary>注：模式按钮按下回调，更新当前模式</summary>
	private void OnModeButtonPressed(E_EditorMod mode)
	{
		_currentMode = mode;
	}

	/// <summary>注：获取当前编辑模式</summary>
	public E_EditorMod GetCurrentMode() => _currentMode;
}
