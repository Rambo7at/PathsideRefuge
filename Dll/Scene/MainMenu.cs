using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Core.GameCore;

public partial class MainMenu : SceneBase
{
	private readonly SceneType _sceneType = SceneType.MainMenu;
	public override void _Ready()
	{
		GameCore.Instance.SetCurrentSceneType(_sceneType,this);
	}


	public override void _Process(double delta)
	{

	}

}
