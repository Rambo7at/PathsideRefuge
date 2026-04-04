using Godot;
using System;
using System.ComponentModel;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;

public partial class ConsoleComp : UIPanelBase
{
	/// <summary> 注：控制台文本框 </summary>
	[Export] private RichTextLabel m_RichTextLabel;
	/// <summary> 注：控制台输入框 </summary>
	[Export] private LineEdit m_LineEdit;

	private CharacterBody3D m_player;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (m_RichTextLabel == null || m_LineEdit == null)
		{
			GD.PrintErr("[ConsoleComp._Ready]：检测 RichTextLabel 或 LineEdit 是空 ");
			return;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	/// <summary>注：获取玩家节点</summary>
	public void GetPlayer(CharacterBody3D player)
	{
		if (m_player != null) return;

		m_player = player;
	}

	public void ToggleUI()
	{
		this.Visible = !this.Visible;
	}


	/// <summary>回调代码：将控制台信息发送出去</summary>
	public void Send(string info)
	{
		if (string.IsNullOrWhiteSpace(info)) return;

		// 显示用户输入的原始内容
		m_RichTextLabel.Text += $"[输入] {info}\n";

		// 解析并执行命令（核心逻辑都在这里）
		ParseCommand(info.Trim());

		// 核心逻辑3：清空输入框，方便下次输入
		m_LineEdit.Text = "";
	}

	/// <summary>极简命令解析（内部辅助方法）</summary>
	private void ParseCommand(string command)
	{
		string[] parts = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 0)
			return;

		// 处理spawn命令
		if (parts[0].Equals("spawn", StringComparison.OrdinalIgnoreCase))
		{
			if (parts.Length < 2)
			{
				m_RichTextLabel.Text += "[错误] 缺少物品名，用法：spawn 物品名\n";
				return;
			}

			GameCore.Instance.m_ConsoleManager.Spawn(parts[1], m_player.GlobalPosition);
			m_RichTextLabel.Text += $"[成功] 尝试生成物品：{parts[1]}\n";
		}
		// 清空控制台
		else if (parts[0].Equals("clear", StringComparison.OrdinalIgnoreCase))
		{
			m_RichTextLabel.Text = "";
			m_RichTextLabel.Text += "[成功] 控制台已清空\n";
		}
		// 帮助命令
		else if (parts[0].Equals("help", StringComparison.OrdinalIgnoreCase))
		{
			m_RichTextLabel.Text += "===== 控制台命令 =====\n";
			m_RichTextLabel.Text += "spawn 物品名 - 生成指定物品\n";
			m_RichTextLabel.Text += "clear - 清空控制台\n";
		}
		// 未知命令
		else
		{
			m_RichTextLabel.Text += $"[错误] 未知命令：{parts[0]}，输入help查看可用命令\n";
		}
	}
}
