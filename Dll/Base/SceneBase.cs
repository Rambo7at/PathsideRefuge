using Godot;
using System;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Base
{
    public partial class SceneBase : Node3D
    {
        [Export] public SceneData m_sceneData;

        public event Action OnFlushNetState;

        public override void _EnterTree()
        {
            WorldManager.Instance.SetCurrentSceneType(this);

            if (m_sceneData.m_sceneType == SceneData.SceneType.ViewScene) return;

            var data = WorldManager.Instance.LoadSceneData(this);
            if (data == null) return;

            m_sceneData = data.DeepCopy();

            if (m_sceneData.m_NetObjectArr == null || m_sceneData.m_NetObjectArr.Count == 0)
            {
                CatLog.Debug($"[SceneBase._EnterTree] 场景中无待恢复的网络对象，场景:{Name}");
                return;
            }

            int spawnedCount = 0;
            foreach (var netObject in m_sceneData.m_NetObjectArr)
            {
                if (netObject.PrefabHash == PlayerManager.Instance.m_playerHash)
                {
                    CatLog.Debug($"[SceneBase._EnterTree] 跳过玩家对象，PrefabHash:{netObject.PrefabHash}");
                    continue;
                }

                NetObjectManager.Instance.SpawnObject(netObject.Position, netObject.Rotation, 0, null, netObject);
                spawnedCount++;
            }

        }

        public override void _ExitTree()
        {
            FlushNetStates();
        }

        /// <summary>注：刷新场景内的对象数据。</summary>
        public void FlushNetStates()
        {
            if (m_sceneData.m_sceneType == SceneData.SceneType.ViewScene) return;

            m_sceneData.m_newScene = false;
            OnFlushNetState?.Invoke();
        }

    }
}
