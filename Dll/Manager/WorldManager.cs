using Godot;
using System.Collections.Generic;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Data.SceneData;

namespace 途畔归所.Dll.Manager
{
    public class WorldManager
    {
        private static WorldManager _instance;
        public static WorldManager Instance => _instance ??= new WorldManager();

        public Dictionary<int, PackedScene> SceneDict = [];

        private WorldData _worldData;
        public WorldData m_worldData { get => _worldData ??= SaveManager.Instance.GetSelectedWorldData(); private set => _worldData = value; }

        private SceneBase _currentScene;
        private Camera3D m_GameCamera;

        public WorldManager()
        {
            m_GameCamera = new Camera3D
            {
                Name = "GameCamera",
                Current = false
            };
        }

        /// <summary>注：加载资源</summary>
        public void Init() { }

        public Node GetPackedScene(int hash)
        {
            if (!SceneDict.TryGetValue(hash, out var packedScene))
            {
                CatLog.Err("[SceneManager.GetPackedScene]：未有获取到对应的场景");
                return null;
            }

            Node node = packedScene.Instantiate();

            if (node is not SceneBase sceneBase)
            {
                CatLog.Err($"[SceneManager.GetPackedScene]：查询哈希值{hash}-非游戏场景-资源路径：{packedScene.ResourcePath}");
                return null;
            }

            // 修复：SceneType 枚举位于 SceneData 内部
            if (sceneBase.m_sceneData.m_sceneType == SceneData.SceneType.GameScene)
            {
                sceneBase.m_sceneData.m_sceneName = sceneBase.Name;
                sceneBase.m_sceneData.m_sceneHash = hash;
            }

            return sceneBase;
        }

        public bool ChangeScene(Node node, string name)
        {
            var scene = GetPackedScene(CatUtils.GetStableHashCode(name));
            if (scene == null) return false;

            string oldSceneName = string.Empty;
            if (_currentScene != null) oldSceneName = _currentScene.Name;
            CatLog.Info($"[WorldManager] 场景切换至： {oldSceneName} -> {name}");

            node.GetTree().ChangeSceneToNode(scene);
            return true;
        }

        public SceneData LoadSceneData(SceneBase scene)
        {
            if (m_worldData == null)
            {
                CatLog.Err("[WorldManager.LoadSaveData]：WorldManager没有存档数据，但是触发了加载场景，问题严重，请排查");
                return null;
            }

            if (!m_worldData.m_sceneDataDict.TryGetValue(scene.m_sceneData.m_sceneHash, out var sceneData))
            {
                m_worldData.m_sceneDataDict[scene.m_sceneData.m_sceneHash] = scene.m_sceneData;
                return scene.m_sceneData;
            }

            return sceneData;
        }

        /// <summary>注：获取相机</summary>
        public Camera3D GetCamera()
        {
            if (_currentScene.m_sceneData.m_sceneType != SceneType.GameScene)
            {
                return null;
            }
            return m_GameCamera;
        }

        /// <summary>注：场景根节点在 _Ready 时调用，汇报当前场景</summary>
        public void SetCurrentSceneType(SceneBase node3D)
        {
            if (node3D == null) return;

            if (node3D.m_sceneData.m_sceneType == SceneData.SceneType.ViewScene)
            {
                Node parent = m_GameCamera.GetParent();
                if (parent != null)
                {
                    parent.RemoveChild(m_GameCamera);
                }
                m_GameCamera.Current = false;
                Input.MouseMode = Input.MouseModeEnum.Visible;
            }

            _currentScene = node3D;
        }

        public SceneBase GetCurrentScene() => _currentScene;

        public int GetCurrentScenehash() => _currentScene.m_sceneData.m_sceneHash;




        /// <summary> 保存当前场景数据到世界 </summary>
        public void PersistScene(SceneBase sceneBase)
        {
            if (sceneBase == null)
            {
                CatLog.Err("[WorldManager.SaveSceneData]：传入的 SceneData 为空，无法保存。");
                return;
            }

            if (m_worldData == null) return;

            var data = sceneBase.m_sceneData;
            if (data == null) return;
            if (data.m_sceneType != SceneData.SceneType.GameScene) return;

            sceneBase.FlushNetStates();

            var objarr = NetObjectRegistry.Instance.GetNetObjectsForCurrentScene(data.m_sceneHash);

            if (objarr != null && objarr.Count != 0)
            {
                data.m_NetObjectArr.Clear();
                foreach (var netojbs in objarr)
                {
                    CatLog.Warn("保存的哈希对象:"+ netojbs.PrefabHash);
                    data.m_NetObjectArr.Add(netojbs);
                }
            }

            CatLog.Warn("保存的对象是否是新场景:" + data.m_newScene);
            

            m_worldData.m_sceneDataDict[data.m_sceneHash] = data.DeepCopy(); 
            CatLog.Debug($"[WorldManager] 场景 {data.m_sceneName} (Hash:{data.m_sceneHash}) 数据已写入世界。");
        }


        public WorldData PersistCurrentScene()
        {
            PersistScene(_currentScene);

            return _worldData;
        }



    }

}
