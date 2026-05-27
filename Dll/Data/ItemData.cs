using Godot;
using System.Text.Json;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;

namespace 维修公司.Dll.data
{

    [GlobalClass]
    public partial class ItemData : Resource, ISerializable
    {
        public enum ItemType
        {
            消耗品 = 0,
            工具 = 1,
            武器 = 2
        }

        public enum WeaponType
        {
            刀 = 0
        }
        public enum AttackType
        {
            消耗品 = 0,
            工具 = 1,
            武器 = 2
        }

        [ExportGroup("基础")]
        [Export] public string m_ID { get; set; }
        [Export] public string m_Name { get; set; } = string.Empty;
        [Export] public ItemType m_Type { get; set; }
        [Export] public string m_Description { get; set; }
        [Export] public Texture2D m_Icon { get; set; }
        [Export] public int m_Stack { get; set; } = 1;
        [Export] public int m_MaxStack { get; set; } = 1;
        [Export] public float m_Weight { get; set; } = 1f;
        [Export] public int m_Volume { get; set; } = 1;
        [Export] public int m_Capacity { get; set; } = 1;
        [Export] public int m_MaxCapacity { get; set; } = 1;



        [ExportGroup("武器")]
        [Export] public WeaponType m_WeaponType { get; set; }
        [Export] public int m_Damage { get; set; }

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



        public bool m_IsStackable => m_Stack < m_MaxStack;


        public ItemData DeepCopy() => this.DuplicateDeep() as ItemData;

        public RigidBody3D DataToDrop()
        {
            var drop = ItemManager.Instance.GetItemDrop(this.m_ID);
            if (drop is not ItemComp comp) return null;

            comp.m_ItemData = this;
            return drop;
        }

        public int GetStackNum() => Mathf.Max(0, m_MaxStack - m_Stack);

        public bool TryStack(ItemData outData)
        {
            if (outData == null || outData.m_ID != m_ID) return false;

            if (!m_IsStackable) return false;

            while (m_IsStackable && outData.m_Stack > 0)
            {
                m_Stack++;
                outData.m_Stack--;
            }

            return outData.m_Stack <= 0;
        }

        public byte[] Serialize()
        {
            var dto = new ItemDataDto
            {
                _ID = m_ID ?? string.Empty,
                _Name = m_Name ?? string.Empty,
                _ItemType = (int)m_Type,
                _Description = m_Description ?? string.Empty,
                _Icon = m_Icon?.ResourcePath ?? string.Empty,
                _Stack = m_Stack,
                _MaxStack = m_MaxStack,
                _Weight = m_Weight,
                _Volume = m_Volume,
                _Capacity = m_Capacity,
                _MaxCapacity = m_MaxCapacity,
                _WeaponType = (int)m_WeaponType,
                _Damage = m_Damage
            };

            return JsonSerializer.SerializeToUtf8Bytes(dto);
        }

        public void Deserialize(byte[] data)
        {
            var dto = JsonSerializer.Deserialize<ItemDataDto>(data);

            m_ID = dto._ID;
            m_Name = dto._Name ?? string.Empty;
            m_Type = (ItemType)dto._ItemType;
            m_Description = dto._Description ?? string.Empty;
            m_Icon = string.IsNullOrEmpty(dto._Icon) ? null : GD.Load<Texture2D>(dto._Icon);
            m_Stack = dto._Stack;
            m_MaxStack = dto._MaxStack;
            m_Weight = dto._Weight;
            m_Volume = dto._Volume;
            m_Capacity = dto._Capacity;
            m_MaxCapacity = dto._MaxCapacity;
            m_WeaponType = (WeaponType)dto._WeaponType;
            m_Damage = dto._Damage;
        }
    }




}
