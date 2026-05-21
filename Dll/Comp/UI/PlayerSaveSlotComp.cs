using Godot;
using System;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Manager;

public partial class PlayerSaveSlotComp : UIPanelBase
{

	[Export] Button button;

	public int m_PlayerID { get; set; }
 
	public override void _Ready()
	{
		if (button == null) return;

		if (m_PlayerID == default) return;

		button.Text = "ID：" + m_PlayerID.ToString();
	}

	private void Pick() => SaveManager.Instance.m_selPlayerIdx = m_PlayerID;

}
