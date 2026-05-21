using Godot;
using System.Collections.Generic;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Data.SceneData;

namespace 途畔归所.Dll.Manager
{
	public class SceneManager
	{
		private static SceneManager _instance;
		public static SceneManager Instance => _instance ??= new SceneManager();

		public Dictionary<int, PackedScene> SceneDict = [];

		public Dictionary<int, SceneBase> activeScene = [];



		/// <summary>注：加载资源</summary>
		/// <param name="packedScene">预制件列表</param>
		public void Init() { }


		public Node GetPackedScene(int hash)
		{
			
			if (activeScene.ContainsKey(hash)) return activeScene[hash];


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


			if (sceneBase.m_sceneData.m_sceneType == SceneType.GameScene)
			{
				sceneBase.m_sceneData.m_sceneName = sceneBase.Name;
				sceneBase.m_sceneData.m_sceneHash = hash;



				activeScene[hash] = sceneBase;
			}

			return sceneBase;

		}






	}
}
