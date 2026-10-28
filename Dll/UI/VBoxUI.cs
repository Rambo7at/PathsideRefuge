using Godot;
using Godot.Collections;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.UI;


/// <summary>注：通用VBox容器，用于通过字典快捷获取内部子节点，并管理动态生成的控件</summary>
[GlobalClass]
public partial class VBoxUI : VBoxContainer
{
	/// <summary>注：节点名称与控件实例的映射字典（静态绑定的子节点）</summary>
	[Export] public Dictionary<string, Control> ControlDict { get; set; } = [];

	/// <summary>注：当前容器内动态生成的控件列表</summary>
	public Array<Control> ControlArray { get; set; } = [];


	/// <summary>注：根据键名从字典中获取指定类型的子控件</summary>
	public T GetControl<T>(string name) where T : Control
	{
		if (!ControlDict.TryGetValue(name, out var control))
		{
			CatLog.Warn($"[VBoxUI.GetControl] {Name} 字典中未找到键名: {name}");
			return null;
		}

		if (control is not T value)
		{
			CatLog.Warn($"[VBoxUI.GetControl] 键名 {name} 的实际类型不是 {typeof(T).Name}");
			return null;
		}

		return value;
	}

	/// <summary>注：添加一个动态生成的控件到容器和数组中</summary>
	public void AddDynamicControl(Control control)
	{
		if (control == null) return;

		AddChild(control);
		ControlArray.Add(control);
	}

	/// <summary>注：一键清空所有动态生成的控件，并释放内存</summary>
	public void ClearDynamicControls()
	{
		foreach (var control in ControlArray)
		{
			if (IsInstanceValid(control))
			{
				control.QueueFree();
			}
		}
		ControlArray.Clear();
	}
}
