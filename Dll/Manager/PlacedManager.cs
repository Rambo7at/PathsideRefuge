using Godot;
using Godot.Collections;
using System;
using 维修公司.Utils;

namespace 途畔归所.Dll.Manager
{
    public class PlacedManager
    {
        private static Lazy<PlacedManager> _instance = new Lazy<PlacedManager>(() => new PlacedManager());
        public static PlacedManager Instance = _instance.Value;

        private PlacedManager() { }

        public Dictionary<string, PackedScene> m_PieceDict = [];

        /// <summary>初始化建筑部件管理器</summary>
        /// <param name="packedScenes">预制件列表</param>
        public void Init(Array<PackedScene> packedScenes)
        {
            if (ResourceManager.Instance.m_PlacedAssetList == null || ResourceManager.Instance.m_PlacedAssetList.Count == 0) return;

            foreach (var Placed in ResourceManager.Instance.m_PlacedAssetList)
            {
                string prefabName = ToolUtils.GetResourceName(Placed.ResourcePath);
                if (string.IsNullOrEmpty(prefabName)) continue;
                if (m_PieceDict.ContainsKey(prefabName))
                {
                    GD.Print($"[PlacedManager.Init]：物件 {prefabName} 已存在，跳过");
                    continue;
                }

                m_PieceDict.Add(prefabName, Placed);

            }
        }

        /// <summary>获取建筑部件预制件</summary>
        /// <param name="pieceName">建筑部件名称</param>
        /// <returns>MeshInstance3D实例，失败返回null</returns>
        public MeshInstance3D GetPiece(string pieceName)
        {
            // 修正4：日志文案 [GetItem] → [GetPiece]
            if (!m_PieceDict.TryGetValue(pieceName, out var prefab))
            {
                GD.PrintErr($"[GetPiece] 建筑部件 {pieceName} 不存在");
                return null;
            }

            Node itemNode = prefab.Instantiate();
            if (itemNode == null)
            {
                GD.PrintErr($"[GetPiece] 建筑部件 {pieceName} 实例化失败");
                return null;
            }

            MeshInstance3D pieceInstance = itemNode as MeshInstance3D;
            if (pieceInstance == null)
            {
                GD.PrintErr($"[GetPiece] 建筑部件 {pieceName} 根节点不是 MeshInstance3D");
                itemNode.QueueFree();
                return null;
            }

            return pieceInstance;
        }
    }
}