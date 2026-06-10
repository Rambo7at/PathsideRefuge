using Godot;
using System;
using 途畔归所.Dll.Utils;

public partial class HudView : Control
{

	[Export] private TextureProgressBar m_HPG;

	[Export] private TextureProgressBar m_HPR;

	private Random m_random;

	public double m_maxHP { get => m_HPG.MaxValue; set => m_HPG.MaxValue = value; }


	public override void _Ready()
	{
		if (m_HPG == null || m_HPR == null)
		{
			CatLog.Err($"[HudComp._Ready]：血条UI未有挂载正确的对象，已销毁");
			CatUtils.StopAndExit(this);
			return;
		}

		m_HPR.MaxValue = m_maxHP;
		m_HPG.MaxValue = m_maxHP;
		m_HPR.Value = m_maxHP;
		m_HPG.Value = m_maxHP;

		m_random ??= new Random();

	}

	public override void _Process(double delta)
	{
		if (m_HPR.Value == m_HPG.Value) return;
		m_HPR.Value = Mathf.MoveToward(m_HPR.Value, m_HPG.Value, (float)delta * 30);
	}

	private void 扣血()
	{
		m_HPG.Value -= m_random.Next(5, 40);
	}

}
