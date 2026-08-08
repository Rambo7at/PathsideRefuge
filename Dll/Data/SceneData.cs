using Godot;
using Godot.Collections;
using 途畔归所.Dll.NetWork;

namespace 途畔归所.Dll.Data;

/// <summary>注：场景数据 </summary>
[GlobalClass]
public partial class SceneData : Resource
{


    [Export] public string SceneName { get; set; }

    [Export] public int SceneHash { get; set; }

    [Export] public bool IsNewScene { get; set; } = true;

    [Export] public Array<NetObject> NetObjectList { get; set; } = [];



    public SceneData DeepCopy() => this.DuplicateDeep() as SceneData;
}

