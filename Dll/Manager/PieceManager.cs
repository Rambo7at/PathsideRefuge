using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Utils;

namespace 维修公司.Dll
{
    public class PieceManager
    {
        private static Lazy<PieceManager> _instance = new Lazy<PieceManager>(() => new PieceManager());
        public static PieceManager Instance = _instance.Value;

        private PieceManager() { }

        public System.Collections.Generic.Dictionary<string, PackedScene> m_PieceDict = new System.Collections.Generic.Dictionary<string, PackedScene>();

        /// <summary>初始化建筑部件管理器</summary>
        /// <param name="packedScenes">预制件列表</param>
        public void InitPieceManager(Array<PackedScene> packedScenes)
        {
            m_PieceDict.Clear();
            foreach (var prefab in packedScenes)
            {
                if (prefab == null)
                {
                    GD.PrintErr("[InitPieceManager] 跳过空的预制件");
                    continue;
                }

                string prefabName = ToolUtils.GetResourceName(prefab.ResourcePath);

                if (prefabName == null) continue;

                // 修正2：文案残留 “物品” → “建筑部件”
                if (m_PieceDict.ContainsKey(prefabName))
                {
                    GD.PrintErr($"[InitPieceManager] 建筑部件 {prefabName} 已存在，跳过");
                    continue;
                }

                m_PieceDict.Add(prefabName, prefab);
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