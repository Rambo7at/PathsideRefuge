using Godot;
using Godot.Collections;
using System;
using 维修公司.Dll.data;
using 途畔归所.Dll.Creature;

namespace 途畔归所.Dll.Interface
{
    public interface IEquipmentHolder
    {
        Equipment Equipment { get; set; }

        Vector3 DropPos { get; }
    }
}
