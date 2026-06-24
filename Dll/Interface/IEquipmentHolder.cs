using Godot;
using Godot.Collections;
using System;
using 维修公司.Dll.data;

namespace 途畔归所.Dll.Interface
{
    public interface IEquipmentHolder
    {
        Array<ItemData> EquipData { get; set; }

        Vector3 DropPos { get; }
    }
}
