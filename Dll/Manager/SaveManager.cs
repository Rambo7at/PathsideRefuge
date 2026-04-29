using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;

namespace 途畔归所.Dll.Manager
{
	/// <summary> 注：存档管理器 </summary>
	public class SaveManager
	{
		public SaveData DATA;

		private const string Path = "res://Save/GameSave.res";

		public SaveManager()
		{
			LoadData();
			SaveDataCheck();
		}



		/// <summary> 注：加载游戏数据 </summary>
		private void LoadData()
		{
			if (!FileAccess.FileExists(Path))
			{
				GD.Print($"目录{Path}中未有存档，准备执行新建");
				SaveData();
				return;
			}

			SaveData data = GD.Load<SaveData>(Path);
			if (data != null)
			{
				DATA = data;
			}
			else
			{
				GD.Print($"获取的存档数据为空，准备执行新建");
				SaveData();
			}
		}

		/// <summary>注：保存数据至本地</summary>
		public void SaveData()
		{
			if (DATA == null)
			{
				DATA = new SaveData();
				ResourceSaver.Save(DATA, Path);
				return;
			}
			if (UpdateData())
			{
				ResourceSaver.Save(DATA, Path);
				GD.Print("成功保存");
			}

		}

		/// <summary>注：更新存档数据</summary>
		private bool UpdateData()
		{

			return true;

		}





		private void SaveDataCheck()
		{
			GD.Print($"开始存档检测------");
			if (DATA.playerDatas == null)
			{

				GD.Print($"目前还未有玩家存档");
				return;
			}
			GD.Print($"检查到有{DATA.playerDatas.Count}个存档");

			foreach (var item in DATA.playerDatas)
			{
				GD.Print($"玩家名{item.m_Name}");
				GD.Print($"玩家背包库存{item.CheckPlayerInventoryCount()}");
			}

		}
	}
}
