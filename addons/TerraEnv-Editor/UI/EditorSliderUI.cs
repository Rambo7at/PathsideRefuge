using Godot;
using System;



namespace Env.UI;
/// <summary>注：编辑器滑块UI，包含名称、数值输入框、滑条和单位显示</summary>
[Tool]
public partial class EditorSliderUI : HBoxContainer
{
	[Export] private string SpinName { get; set; }          // 滑块显示名称
	[Export] private string Unit { get; set; } = "m";       // 数值单位
	[Export] private float MinValue { get; set; } = 0;
	[Export] private float MaxValue { get; set; } = 100f;
	[Export] private float Step { get; set; } = 1f;

	[ExportGroup("组件子节点")]
	[Export] private Label NameText { get; set; }           // 名称文本
	[Export] private LineEdit ValueEdit { get; set; }       // 数值输入框
	[Export] private HSlider Slider { get; set; }           // 滑块
	[Export] private Label UnitLabel { get; set; }          // 单位文本

	/// <summary>注：初始化时检查子节点完整性并同步默认值</summary>
	public override void _Ready()
	{
		if (ValueEdit == null || Slider == null || NameText == null || UnitLabel == null)
		{
			GD.Print("[Env.EditorSliderUI]：检测有子UI组件为空。已销毁");
			SetProcess(false);
			QueueFree();
			return;
		}

		Slider.MaxValue = MaxValue;
		Slider.MinValue = MinValue;
		Slider.Step = Step;
		NameText.Text = SpinName;
		UnitLabel.Text = Unit;

		Slider.ValueChanged += OnSliderValueChanged;
		ValueEdit.EditingToggled += OnValueEditEditingToggled;
	}

	/// <summary>注：数值输入框编辑结束回调</summary>
	private void OnValueEditEditingToggled(bool toggledOn)
	{
		if (!toggledOn)
		{
			if (float.TryParse(ValueEdit.Text, out float value))
			{
				Slider.Value = value;
				return;
			}
			OnSliderValueChanged(Slider.Value);
		}
	}

	/// <summary>注：滑块数值变化回调</summary>
	private void OnSliderValueChanged(double value)
	{
		float v1 = (float)Math.Round(value, 1);
		if (ValueEdit != null)
			ValueEdit.Text = v1.ToString();
	}

	/// <summary>注：编辑器下每帧同步名称与单位显示</summary>
	public override void _Process(double delta)
	{
		if (!Engine.IsEditorHint()) return;

		if (ValueEdit == null || Slider == null || NameText == null || UnitLabel == null) return;

		if (NameText.Text != SpinName) NameText.Text = SpinName;
		if (UnitLabel.Text != Unit) UnitLabel.Text = Unit;
		if (Slider.MaxValue != MaxValue) Slider.MaxValue = MaxValue;
		if (Slider.MinValue != MinValue) Slider.MinValue = MinValue;
		if (Slider.Step != Step) Slider.Step = Step;
	}

	/// <summary>注：获取滑块当前值（保留一位小数）</summary>
	public float GetSliderValue()
	{
		if (Slider == null) return 0;
		return (float)Math.Round(Slider.Value, 1);
	}
}
