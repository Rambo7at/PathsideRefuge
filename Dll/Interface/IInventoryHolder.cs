using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;

namespace 途畔归所.Dll.Interface
{
    /// <summary>注：通用库存持有者接口。任何拥有 InventoryComp 的节点都应实现此接口。</summary>
    public partial interface IInventoryHolder
    {
        InventoryData InventoryData { get; set; }

        Vector3 DropPos { get; }
    }
}
