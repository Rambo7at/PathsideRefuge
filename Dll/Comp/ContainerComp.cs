using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 维修公司.Dll.Interface;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.Comp
{
    public partial class ContainerComp : Node3D, IInventoryHolder, IInteractable
    {

        public InventoryComp m_ContainerComp;

        public override void _Ready() => InitInventory();


        private void InitInventory()
        {
            if (m_ContainerComp != null) return;

            var UI = UIManager.Instance.GetUI("ContainerUI");
            if (UI == null) return;
            if (UI is not InventoryComp script) return;
            script.Holder = this;
            m_ContainerComp = script;
        }

        public CanvasLayer GetCanvasLayer()
        {
            throw new System.NotImplementedException();
        }

        public Vector3 GetDropPosition()
        {
            throw new System.NotImplementedException();
        }

        public Dictionary<int, ItemData> LoadInventory()
        {
            throw new System.NotImplementedException();
        }

        public void SaveInventory(Array<SlotComp> slotComps)
        {
            throw new System.NotImplementedException();
        }

        public void PlayerInteract(bool InputE, bool InputF, Player player)
        {
            throw new System.NotImplementedException();
        }
    }
}
