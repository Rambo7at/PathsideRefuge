using Godot;
using System;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

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

    public bool IsReady { get; private set; } = false;  // 场景是否已完成初始化/加载

    public event Action OnSaveState;                            // 触发时，订阅者应将自身状态保存到 NetObj.m_customData

    public override void _EnterTree()
    {
        if (NetCore.Instance.IsMultiplayer && NetCore.Instance.IsClient && SceneType == E_SceneType.GameScene)
        {
            SetupCurrentScene();

            NetObjectRegistry.Instance.RequestSceneData(SceneData.SceneHash);

            IsReady = true;
            return;
        }

        SetupCurrentScene();
        RestoreNetObjects();
    }

    /// <summary>注：调试打印场景数据详情（仅 debug 构建）</summary>
    private void DebugPrintSceneData()
    {
        if (SceneData == null)
        {
            CatLog.Debug("[SceneBase] SceneData 为空");
            return;
        }

        CatLog.Debug($"[SceneBase] ┌─ 场景数据详情 ──");
        CatLog.Debug($"[SceneBase] │ SceneName   : {SceneData.SceneName}");
        CatLog.Debug($"[SceneBase] │ SceneHash   : {SceneData.SceneHash}");
        CatLog.Debug($"[SceneBase] │ IsNewScene  : {SceneData.IsNewScene}");
        CatLog.Debug($"[SceneBase] │ NetObjectCount : {SceneData.NetObjectList.Count}");

        if (SceneData.NetObjectList.Count > 0)
        {
            for (int i = 0; i < SceneData.NetObjectList.Count; i++)
            {
                var obj = SceneData.NetObjectList[i];
                if (obj != null)
                {
                    CatLog.Debug($"[SceneBase] │ [{i}] PrefabHash={obj.PrefabHash}, Owner={obj.OwnerPeerID}, sceneHash={obj.sceneHash}, Pos={obj.Position}");
                }
                else
                {
                    CatLog.Debug($"[SceneBase] │ [{i}] null");
                }
            }
        }
        CatLog.Debug($"[SceneBase] └─────────────────");
    }

    //////////////下方代码不动

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
            NetObjectManager.Instance.SpawnObject(netObject, netObject.Position, netObject.Rotation);
        }

        IsReady = true;
    }


}
