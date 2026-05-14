using Godot;
using System;

public partial class TestHud : Control
{
	[Export] public TextureProgressBar _HPG;

	[Export] public TextureProgressBar _HPR;

	[Export] public double speed = 1;

	private Random random;

	public double MAX_HP { get => _HPG.MaxValue; set => _HPG.MaxValue = value; }

	public double HP { get => _HPG.Value; set => _HPG.Value = value; }

	


	public override void _Ready()
	{
		random ??= new Random();
		MAX_HP = 100;

		_HPR.MaxValue = MAX_HP;
		_HPR.Value = HP;
	}

	public override void _Process(double delta)
	{
		if (_HPR.Value == HP) return;

		_HPR.Value -= 0.5;

	}

	public void 伤害()
	{
		if (HP <= 0) return;

		HP -= random.Next(5,50);
	}


}
