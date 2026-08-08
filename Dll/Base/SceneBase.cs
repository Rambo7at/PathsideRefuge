using Godot;
using System;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Data.SceneData;

namespace 途畔归所.Dll.Base;

/// <summary>注：场景基类，所有游戏场景根节点需继承此类，负责场景的初始化、网络对象恢复与数据持久化</summary>
public partial class SceneBase : Node3D
{
    public enum E_SceneType
    {
        GameScene = 0,
        ViewScene = 1,
    }


    [Export] public SceneData SceneData { get; set; }          // 场景数据（场景名、哈希、网络对象列表等）

    [Export] public E_SceneType SceneType { get; set; }


    public event Action OnSaveState;                            // 触发时，订阅者应将自身状态保存到 NetObj.m_customData

    public override void _EnterTree()
    {
        SetupCurrentScene();                                    // 初始化场景上下文并加载存档数据
        RestoreNetObjects();                                    // 从场景存档中恢复网络对象
    }

    /// <summary>注：触发所有订阅者保存状态，并将场景标记为"非新场景"。</summary>
    public void SaveAllStates()
    {
        if (SceneType == E_SceneType.ViewScene) return;

        OnSaveState?.Invoke();

        var netObjects = NetObjectRegistry.Instance.GetNetObjectsForScene(SceneData.SceneHash);
        SceneData.NetObjectList.Clear();

        foreach (var obj in netObjects) SceneData.NetObjectList.Add(obj);

        SceneData.IsNewScene = false;
    }

    /// <summary>注：初始化当前场景，向 WorldManager 汇报场景引用，并加载场景数据。</summary>
    private void SetupCurrentScene()
    {
        WorldManager.Instance.SetCurrentSceneType(this);

        if (SceneType == E_SceneType.ViewScene) return;

        if (NetCore.Instance.IsClient) return;

        if (WorldManager.Instance.LoadSceneData(this) is not SceneData sceneData) return;

        SceneData = sceneData.DeepCopy();
    }

    /// <summary>注：从场景存档中恢复网络对象，跳过玩家对象（由 PlayerManager 独立管理）。</summary>
    private void RestoreNetObjects()
    {
        if (SceneData.NetObjectList.Count == 0) return;

        foreach (var netObject in SceneData.NetObjectList)
        {
            if (netObject.PrefabHash == PlayerManager.Instance.PlayerHash) continue;
            NetObjectManager.Instance.SpawnObject(netObject.Position, netObject.Rotation, 0, null, netObject);
        }
    }
}
