using Godot;
using Godot.Collections;
using System;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Manager;

/// <summary>注：玩家管理器，负责玩家数据的持有、管理、生成及场景切换入口</summary>
public class PlayerManager
{
    private static PlayerManager _instance;
    public static PlayerManager Instance => _instance ??= new PlayerManager();

    /// <summary>注：玩家预制体资源</summary>
    private PackedScene _playerScene;

    /// <summary>注：所有玩家数据的映射字典</summary>
    private Dictionary<int, CreatureData> PlayerDataDict { get; set; } = [];

    /// <summary>注：当前选中的玩家ID</summary>
    public int SelectedPlayerID { get; set; }

    /// <summary>注：玩家预制体的哈希值</summary>
    public int PlayerHash { get; private set; }

    /// <summary>注：是否存在玩家数据</summary>
    public bool HasPlayers => PlayerDataDict.Count > 0;

    /// <summary>注：玩家数量是否已达上限（超过9个）</summary>
    public bool IsPlayerFull => PlayerDataDict.Count > 9;


    /// <summary>注：本地玩家实例</summary>
    public Player LocalPlayer { get; set; }

    /// <summary>注：本地玩家数据（只读，自动获取当前选中项）</summary>
    public CreatureData LocalPlayerData => GetLocalPlayerData();

    /// <summary>注：是否为主机</summary>
    public bool IsHost => NetCore.Instance.IsHost;


    private PlayerManager()
    {
        PlayerDataDict = SaveManager.Instance.GetPlayerDataDict();
        SelectedPlayerID = SaveManager.Instance.GetSelectedPlayerIndex();

        CatLog.Ok("[PlayerManager] 初始化完成");
    }

    /// <summary>注：注册玩家预制体及其哈希值</summary>
    public void RegisterPlayer(int hash, PackedScene packedScene)
    {
        _playerScene = packedScene;
        PlayerHash = hash;
    }

    /// <summary>注：获取所有有效的玩家ID列表</summary>
    public Array<int> GetAllPlayerIDs()
    {
        Array<int> ints = [];

        foreach (var plData in PlayerDataDict)
        {
            if (plData.Key == default || plData.Value == null) continue;
            ints.Add(plData.Key);
        }
        return ints;
    }

    /// <summary>注：验证指定ID的玩家存档是否存在</summary>
    public bool IsValidPlayer(int playerID) => PlayerDataDict.TryGetValue(playerID, out var _);


    /// <summary>注：创建新玩家并添加到数据字典中</summary>
    public void CreatePlayer(string playerName)
    {
        if (IsPlayerFull) return;
        if (string.IsNullOrWhiteSpace(playerName)) return;

        CreatureData data = new()
        {
            Name = playerName,
            IsPlayer = true,
            PlayerID = Math.Abs(Guid.NewGuid().GetHashCode())
        };

        PlayerDataDict.Add(data.PlayerID, data);
    }


    /// <summary>注：删除指定ID的玩家存档数据</summary>
    public bool RemovePlayerData(int playerID)
    {
        if (!PlayerDataDict.TryGetValue(playerID, out var data)) return false;

        PlayerDataDict.Remove(playerID);
        return true;
    }


    /// <summary>注：获取本地玩家数据（如选中ID无效则自动指向第一个有效玩家）</summary>
    public CreatureData GetLocalPlayerData()
    {
        if (PlayerDataDict.TryGetValue(SelectedPlayerID, out var playerData)) return playerData;
        if (PlayerDataDict.Count == 0) return null;

        foreach (var data in PlayerDataDict)
        {
            if (data.Value == null) continue;
            SelectedPlayerID = data.Key;
            return data.Value;
        }
        return null;
    }

    /// <summary>注：导出所有玩家数据的深拷贝（供 SaveManager 写入磁盘）</summary>
    public Dictionary<int, CreatureData> SavePlayerDataDict()
    {
        Dictionary<int, CreatureData> data = [];

        foreach (var item in PlayerDataDict)
        {
            if (item.Value == null) continue;
            data.Add(item.Key, item.Value.DeepCopy());
        }

        return data;
    }

    /// <summary>注：获取所有存档的玩家名（玩家ID -> 玩家名）</summary>
    public Dictionary<int, string> GetAllPlayerNames()
    {
        Dictionary<int, string> data = [];

        foreach (var save in PlayerDataDict)
        {
            if (save.Value == null) continue;

            data.Add(save.Key, save.Value.Name);
        }
        return data;
    }


    /// <summary>注：在指定位置生成本地玩家（若玩家实例无效则自动重建）</summary>
    public void SpawnLocalPlayer(Vector3 Pos, Vector3 rot)
    {
        if (LocalPlayer == null || !GodotObject.IsInstanceValid(LocalPlayer))
        {
            if (_playerScene.Instantiate() is not Player newPlayer) return;
            LocalPlayer = newPlayer;
        }
        else
        {
            if (LocalPlayer.GetParent() != null)
            {
                CatLog.Warn($"[PlayerManager.SpawnLocalPlayer] 玩家仍有父节点 {LocalPlayer.GetParent().Name}，正在移除");
                LocalPlayer.GetParent().RemoveChild(LocalPlayer);
            }
        }

        if (LocalPlayerData == null)
        {
            CatLog.Net("[PlayerManager.SpawnLocalPlayer] LocalPlayerData 为空");
            return;
        }

        LocalPlayer.m_CreatureData = LocalPlayerData.DeepCopy();
        NetObjectInstance.Instance.SpawnObject(LocalPlayer, Pos, rot);
    }


    /// <summary>注：保存当前本地玩家实例的数据到玩家数据字典中</summary>
    public void SaveLocalPlayerData()
    {
        if (LocalPlayer == null) return;

        if (!PlayerDataDict.ContainsKey(LocalPlayer.m_CreatureData.PlayerID)) return;

        PlayerDataDict[LocalPlayer.m_CreatureData.PlayerID] = LocalPlayer.m_CreatureData.DeepCopy();
    }


    /// <summary>注：检查玩家存档中是否有可用的位置信息（场景匹配且有位置）</summary>
    public bool CanUseSavedPosition() => LocalPlayerData?.LastPosition != default && LocalPlayerData?.LastSceneHash == WorldManager.Instance?.GetCurrentScenehash();

    /// <summary>注：从当前世界状态刷新玩家存档数据（场景哈希、位置、旋转）</summary>
    public void RefreshPlayerData()
    {
        if (LocalPlayerData == null)
        {
            CatLog.Warn("[PlayerManager.RefreshPlayerData] LocalPlayerData 为空，跳过刷新");
            return;
        }

        CatLog.Debug("[PlayerManager.RefreshPlayerData] 开始刷新玩家数据");

        var currentScene = WorldManager.Instance.GetCurrentScene();
        if (currentScene != null)
        {
            LocalPlayerData.LastSceneHash = currentScene.SceneData.SceneHash;
            CatLog.Debug($"[PlayerManager.RefreshPlayerData] 场景哈希已更新：{LocalPlayerData.LastSceneHash}");
        }
        else
        {
            CatLog.Warn("[PlayerManager.RefreshPlayerData] 当前场景为空，无法更新场景哈希");
        }

        if (LocalPlayer != null && GodotObject.IsInstanceValid(LocalPlayer))
        {
            LocalPlayerData.LastPosition = LocalPlayer.GlobalPosition;
            LocalPlayerData.LastRotation = LocalPlayer.GlobalRotation;
            CatLog.Debug($"[PlayerManager.RefreshPlayerData] 位置已更新：{LocalPlayerData.LastPosition}，旋转：{LocalPlayerData.LastRotation}");
        }
        else
        {
            CatLog.Warn("[PlayerManager.RefreshPlayerData] 玩家实例无效，无法更新位置和旋转");
        }

        CatLog.Ok("[PlayerManager.RefreshPlayerData] 玩家数据刷新完成");
    }
}
