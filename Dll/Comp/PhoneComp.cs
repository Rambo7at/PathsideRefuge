using Godot;
using System;
using System.Runtime.CompilerServices;
using 维修公司.Dll;
using 维修公司.Utils;
using 途畔归所.Dll.Core;

public partial class PhoneComp : Control
{

	/// <summary>注：手机显示的时间</summary>
	[Export] private Label m_Tiem;
	/// <summary>注：商城按钮</summary>
	[Export] private Button m_ShopButton;

	/// <summary>注：动画播放器</summary>
	[Export] private AnimationPlayer m_Animation;


	private Control m_OnlineShopUI;

	/// <summary>UI状态</summary>
	private bool m_UiState = false;

	/// <summary>操作锁：标记UI是否正在播放动画</summary>
	private bool m_AnimLock = false;

	public override void _Ready()
	{
		if (m_Tiem == null || m_ShopButton == null)
		{
			GD.PrintErr("[PhoneComp._Ready]：检测 m_Tiem 或 m_ShopButton 是空 ");
			return;
		}

		if (m_OnlineShopUI != null) return;

		m_OnlineShopUI = UIManager.Instance.GetUI("OnlineShopUI");
		m_OnlineShopUI.Visible = false;
		this.AddChild(m_OnlineShopUI);
	}

	public override void _Process(double delta)
	{
		if (this.Visible == true) SyncTime();
	}

	/// <summary>回调函数，动画执行完成后的逻辑</summary>
	/// <param name="animName"></param>
	private void AnimFinished(StringName animName)
	{
		if (animName == "手机滑动")
		{
			if (m_UiState)
			{
				m_AnimLock = false;
			}
			else
			{
				this.Visible = false;
				m_AnimLock = false;
			}
			
		}

	}

	public void ToggleUI()
	{
		if (m_AnimLock) return;

		if (this.Visible == false)
		{
			m_AnimLock = true;
			m_UiState = true;
			this.Visible = true;
			m_Animation.Play("手机滑动");
		}
		else if (this.Visible == true)
		{
			m_AnimLock = true;
			m_UiState = false;
			m_OnlineShopUI.Visible = false;
			m_Animation.PlayBackwards("手机滑动");
		}
	}

	/// <summary>注：同步时间</summary>
	private void SyncTime() => m_Tiem.Text = "tameSTRING";


	/// <summary> 回调函数：打开商店</summary>
	private void OpenShopUI()
	{

		m_OnlineShopUI.Visible = !m_OnlineShopUI.Visible;


	}
}
