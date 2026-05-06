using Godot;
using System;
using 途畔归所.Dll.Comp.Vegetation;
using 途畔归所.Dll.Creature;

public partial class 武器测试 : Node3D
{

	[Export] public Player m_Player;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}


	public void Target(Node3D TT)
	{
		if (TT == null || m_Player.m_StateMachine.s_PlayerState != StateMachine.PlayerState.Attack) return;

	   if (TT is not TreeComp treeComp) return;

		GD.PrintErr($"目标生命值：{treeComp.m_VegetationData.m_Health}");

		treeComp.m_VegetationData.m_Health -= 1;
		GD.PrintErr($"触发后生命值：{treeComp.m_VegetationData.m_Health}");
	}
}
