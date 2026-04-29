using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;



/// <summary>注：控制台组件 </summary>
public partial class ConsoleComp : UIPanelBase
{
	/// <summary> 注：控制台文本框 </summary>
	[Export] public RichTextLabel m_RichTextLabel;
	/// <summary> 注：控制台输入框 </summary>
	[Export] public LineEdit m_LineEdit;

	public List<string> m_Command = [];

	public CharacterBody3D m_player;

	public override void _Ready()
	{
		if (m_RichTextLabel == null || m_LineEdit == null)
		{
			GD.PrintErr("[ConsoleComp._Ready]：检测 RichTextLabel 或 LineEdit 是空 ");
			return;
		}
	}


	public override void _Process(double delta)
	{
	}

	/// <summary>注：获取玩家节点</summary>
	public void GetPlayer(CharacterBody3D player)
	{
		if (m_player != null) return;

		m_player = player;
	}
	/// <summary>回调函数：用户按下enter触发</summary>
	public void Send(string info)
	{
		if (string.IsNullOrWhiteSpace(info)) return;

		// 显示用户输入的原始内容
		m_RichTextLabel.Text += $"[输入] {info}\n";

		m_Command = info.Trim().Split().ToList();

        // 解析执行命令
        ConsoleManager.Instance.ParseCommand(this);

		// 清空输入框
		m_LineEdit.Text = "";
		m_Command.Clear();

	}

	public void ToggleUI() => this.Visible = !this.Visible;
}