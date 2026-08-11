using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Text.Json;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.NetWork;

namespace 途畔归所.Dll.Data;

/// <summary>注：场景数据 </summary>
[GlobalClass]
public partial class SceneData : Resource, ISerializable
{
    [Export] public string SceneName { get; set; }
    [Export] public int SceneHash { get; set; }
    [Export] public bool IsNewScene { get; set; } = true;
    [Export] public Array<NetObject> NetObjectList { get; set; } = [];

    public SceneData DeepCopy() => this.DuplicateDeep() as SceneData;

    private struct SceneDataDto
    {
        public string SceneName { get; set; }
        public int SceneHash { get; set; }
        public bool IsNewScene { get; set; }
        public List<byte[]> NetObjects { get; set; }
    }

    public byte[] Serialize()
    {
        List<byte[]> serializedObjects = [];

        foreach (var netObj in NetObjectList)
        {
            if (netObj == null) continue;
            serializedObjects.Add(netObj.Serialize());
        }

        var dto = new SceneDataDto
        {
            SceneName = SceneName,
            SceneHash = SceneHash,
            IsNewScene = IsNewScene,
            NetObjects = serializedObjects
        };

        return JsonSerializer.SerializeToUtf8Bytes(dto);
    }

    public void Deserialize(byte[] data)
    {
        var dto = JsonSerializer.Deserialize<SceneDataDto>(data);

        SceneName = dto.SceneName ?? string.Empty;
        SceneHash = dto.SceneHash;
        IsNewScene = dto.IsNewScene;

        NetObjectList.Clear();
        foreach (var objData in dto.NetObjects)
        {
            var obj = new NetObject();
            obj.Deserialize(objData);

            NetObjectList.Add((obj));
        }
    }


}
