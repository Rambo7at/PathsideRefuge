using Godot;
using System.Text.Json;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;

namespace 维修公司.Dll.data
{

    [GlobalClass]
    public partial class ItemData : Resource, ISerializable
    {
        public enum E_ItemType
        {
            Consumable = 0,   // 消耗品
            Prop = 1,         // 道具
            Tool = 2,         // 工具
            Weapon = 3        // 武器
        }
        public enum E_WeaponType
        {
            Knife = 0,        // 刀
            Axe = 1           // 斧
        }
        private struct ItemDataDto
        {
            public string _ID { get; set; }
            public string _Name { get; set; }
            public int _ItemType { get; set; }
            public string _Description { get; set; }
            public string _Icon { get; set; }
            public int _Stack { get; set; }
            public int _MaxStack { get; set; }
            public float _Weight { get; set; }
            public int _Volume { get; set; }
            public int _Capacity { get; set; }
            public int _MaxCapacity { get; set; }
            public int _WeaponType { get; set; }
            public int _Damage { get; set; }
        }

        [ExportGroup("基础")]
        [Export] public string ID { get; set; }
        [Export] public string Name { get; set; } = string.Empty;
        [Export] public E_ItemType Type { get; set; } = E_ItemType.Prop;
        [Export] public string Description { get; set; } = string.Empty;
        [Export] public Texture2D Icon { get; set; }
        [Export] public int Stack { get; set; } = 1;
        [Export] public int MaxStack { get; set; } = 1;
        [Export] public float Weight { get; set; } = 1f;
        [Export] public int Volume { get; set; } = 1;
        [Export] public int Capacity { get; set; } = 1;
        [Export] public int MaxCapacity { get; set; } = 1;



        [ExportGroup("武器")]
        [Export] public E_WeaponType WeaponType { get; set; }
        [Export] public int AttackAnimIndex { get; set; }
        [Export] public int Damage { get; set; }
         
        public bool IsEquip => Type == E_ItemType.Weapon;



        public bool m_IsStackable => Stack < MaxStack;


        public ItemData DeepCopy() => this.DuplicateDeep() as ItemData;

        public ItemComp DataToDrop()
        {
            if (ItemManager.Instance.GetItemDrop(ID) is not ItemComp comp) return null;
            comp.m_ItemData = DeepCopy();
            return comp;
        }

        public int GetStackNum() => Mathf.Max(0, MaxStack - Stack);

        public bool TryStack(ItemData outData)
        {
            if (outData == null || outData.ID != ID) return false;

            if (!m_IsStackable) return false;

            while (m_IsStackable && outData.Stack > 0)
            {
                Stack++;
                outData.Stack--;
            }

            return outData.Stack <= 0;
        }

        public void TryDropItem(Vector3 DropPos)
        {
            if (DataToDrop() is not ItemComp drop) return;
            NetObjectManager.Instance.SpawnObject(DropPos, new Vector3(), default, drop);
        }

        public byte[] Serialize()
        {
            var dto = new ItemDataDto
            {
                _ID = ID ?? string.Empty,
                _Name = Name ?? string.Empty,
                _ItemType = (int)Type,
                _Description = Description ?? string.Empty,
                _Icon = Icon?.ResourcePath ?? string.Empty,
                _Stack = Stack,
                _MaxStack = MaxStack,
                _Weight = Weight,
                _Volume = Volume,
                _Capacity = Capacity,
                _MaxCapacity = MaxCapacity,
                _WeaponType = (int)WeaponType,
                _Damage = Damage
            };

            return JsonSerializer.SerializeToUtf8Bytes(dto);
        }

        public void Deserialize(byte[] data)
        {
            var dto = JsonSerializer.Deserialize<ItemDataDto>(data);

            ID = dto._ID;
            Name = dto._Name ?? string.Empty;
            Type = (E_ItemType)dto._ItemType;
            Description = dto._Description ?? string.Empty;
            Icon = string.IsNullOrEmpty(dto._Icon) ? null : GD.Load<Texture2D>(dto._Icon);
            Stack = dto._Stack;
            MaxStack = dto._MaxStack;
            Weight = dto._Weight;
            Volume = dto._Volume;
            Capacity = dto._Capacity;
            MaxCapacity = dto._MaxCapacity;
            WeaponType = (E_WeaponType)dto._WeaponType;
            Damage = dto._Damage;
        }
    }

}
