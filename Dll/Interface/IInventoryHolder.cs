using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 维修公司.Dll.data;

namespace 途畔归所.Dll.Interface
{
    /// <summary>注：通用库存持有者接口。任何拥有 InventoryComp 的节点都应实现此接口。</summary>
    public partial interface IInventoryHolder
    {
        CanvasLayer GetCanvasLayer();

        Vector3 GetDropPosition();

        /// <summary>从存档恢复此持有者的库存数据</summary>
        Godot.Collections.Dictionary<int, ItemData> LoadInventory();
        /// <summary>保存此持有者的库存数据</summary>
        //void SaveInventory(Array<SlotComp> slotComps);
    }
}
