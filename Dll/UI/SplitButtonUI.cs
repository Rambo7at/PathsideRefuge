using Godot;
using System;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.UI;

/// <summary>注：分裂按钮组件，由一大一小两个按钮组合而成，左侧主选，右侧副操作</summary>
public partial class SplitButtonUI : Control
{
	/// <summary>注：左侧主按钮（点击选中/加载）</summary>
	[Export] public ButtonUI MainButton;

	/// <summary>注：右侧副按钮（点击删除/扩展操作）</summary>
	[Export] public ButtonUI ActionButton;

	/// <summary>注：绑定的数据唯一标识（如世界ID或玩家ID）</summary>
	public int DataID { get; set; }

	/// <summary>注：绑定的复杂数据对象（如房间IP、物品数据等）</summary>
	public object UserData { get; set; }

	public override void _Ready()
	{
		if (MainButton == null || ActionButton == null)
		{
			CatLog.Err("[SplitButtonUI._Ready]：缺少节点绑定，请检查 Inspector 面板中的导出属性。");
			return;
		}
	}

	/// <summary>注：设置主按钮的显示文本</summary>
	public void SetMainText(string text) => MainButton.Text = text;

	/// <summary>注：设置副按钮的显示文本</summary>
	public void SetActionText(string text) => ActionButton.Text = text;

	/// <summary>注：绑定主按钮的点击事件，并将自身的 DataID 回传给外部</summary>
	public void SetMainAction(Action<SplitButtonUI> action) => MainButton.Pressed += () => action(this);

	/// <summary>注：绑定副按钮的点击事件，并将自身的 DataID 回传给外部</summary>
	public void SetActionAction(Action<SplitButtonUI> action) => ActionButton.Pressed += () => action(this);
}
