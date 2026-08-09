using Godot;
using System.Diagnostics;
using System.Text.Json;
using 途畔归所.Dll.Comp;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
namespace 维修公司.Dll.data;

    /// <summary>注：物品数据资源类，定义所有物品的基础属性与装备特性。</summary>
    [GlobalClass]
    public partial class ItemData : Resource, ISerializable
    {
        /// <summary>注：物品大类（背包底层分类）</summary>
        public enum E_ItemType
        {
            None = 0,       // 无
            Consumable = 1, // 消耗品
            Prop = 2,       // 道具
            Tool = 3,       // 工具
            Equip = 4       // 装备
        }

        /// <summary>注：具体装备种类（决定模型、动画、攻击风格）</summary>
        public enum E_EquipType
        {
            None = 0,        // 无
            Knife = 1,       // 刀
            Sword = 2,       // 单手剑
            Axe = 3,         // 单手斧
            Mace = 4,        // 锤
            Shield = 5,      // 盾牌
            Staff = 6,       // 法杖
            TwoHandSword = 7,// 双手剑
            TwoHandAxe = 8   // 双手斧
        }

        /// <summary>注：装备槽位可用性（由 EquipType 自动推导）</summary>
        public enum E_EquipAVL
        {
            None = 0,       // 无
            MainHand = 1,   // 仅主手
            OffHand = 2,    // 仅副手
            BothHands = 3,  // 主手或副手均可
            TwoHand = 4     // 双手武器（占用主手且副手禁用）
        }

        /// <summary>ItemData序列化数据</summary>
        private struct ItemDataDto
        {
            // 基础字段
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
            // 装备字段
            public int _EquipType { get; set; }
            public int _Damage { get; set; }
            public int _AttackAnimIndex { get; set; }
            // AI 字段
            public float _AttackDistance { get; set; }
            public float _AttackInterval { get; set; }
        }

        [ExportGroup("基础")]
        [Export] public string ID { get; set; }                     // 预制名
        [Export] public string Name { get; set; } = string.Empty;   // 物品名称
        [Export] public E_ItemType Type { get; set; } = E_ItemType.Prop;  // 物品大类
        [Export] public string Description { get; set; } = string.Empty;   // 物品描述
        [Export] public Texture2D Icon { get; set; }                // 物品图标
        [Export] public int Stack { get; set; } = 1;                // 当前堆叠数量
        [Export] public int MaxStack { get; set; } = 1;             // 最大堆叠数量
        [Export] public float Weight { get; set; } = 1f;            // 物品重量
        [Export] public int Volume { get; set; } = 1;               // 物品体积
        [Export] public int Capacity { get; set; } = 1;             // 当前容量
        [Export] public int MaxCapacity { get; set; } = 1;          // 最大容量

        [ExportGroup("装备")]
        [Export] public E_EquipType EquipType { get; set; }         // 装备种类（武器/盾牌等）

        [ExportSubgroup("属性")]
        [Export] public int Damage { get; set; }                    // 攻击力/伤害值
        [Export] public int AttackAnimIndex { get; set; }          // 默认攻击动画索引（单持）
        [Export] public int DualWieldIndex { get; set; }           // 双持攻击动画索引（-1 表示不支持）

        [ExportGroup("AI")]
        [Export] public float AttackDistance { get; set; } = 1f;    // AI 攻击距离
        [Export] public float AttackInterval { get; set; } = 5f;    // AI 攻击间隔（秒）

        /// <summary>注：是否为装备</summary>
        public bool IsEquip => Type == E_ItemType.Equip;
        /// <summary>注：是否为武器（EquipType 不为 None）</summary>
        public bool IsWeapon => !(EquipType == E_EquipType.None || EquipType == E_EquipType.Shield);

        /// <summary>注：是否可堆叠</summary>
        public bool CanStack => Stack < MaxStack;
        /// <summary>注：自动获取槽位可用性</summary>
        public E_EquipAVL EquipAVL => GetEquipAVL();

        /// <summary>注：能否装备到主手</summary>
        public bool CanEquipMainHand => EquipAVL == E_EquipAVL.MainHand || EquipAVL == E_EquipAVL.BothHands || EquipAVL == E_EquipAVL.TwoHand;

        /// <summary>注：能否装备到副手</summary>
        public bool CanEquipOffHand => EquipAVL == E_EquipAVL.OffHand || EquipAVL == E_EquipAVL.BothHands;

        public bool IsTwoHandWeapon => EquipAVL == E_EquipAVL.TwoHand;



        /// <summary>注：深拷贝当前物品数据</summary>
        public ItemData DeepCopy() => this.DuplicateDeep() as ItemData;

        /// <summary>注：生成可拾取的物品实例（ItemComp）</summary>
        public ItemComp ToDrop()
        {
            if (ItemManager.Instance.GetItemDrop(ID) is not ItemComp comp) return null;
            comp.Data = DeepCopy();
            return comp;
        }

        /// <summary>注：获取当前可堆叠的空余数量</summary>
        public int GetStackSpace() => Mathf.Max(0, MaxStack - Stack);

        /// <summary>注：尝试将另一个物品堆叠到当前物品上</summary>
        public bool TryStack(ItemData outData)
        {
            if (outData == null || outData.ID != ID) return false;

            if (!CanStack) return false;

            // 循环堆叠直到当前物品满或源物品为空
            while (CanStack && outData.Stack > 0)
            {
                Stack++;
                outData.Stack--;
            }

            return outData.Stack <= 0;
        }

        /// <summary>注：在指定位置生成掉落物</summary>
        public void TryDropItem(Vector3 DropPos)
        {
            if (ToDrop() is not ItemComp drop) return;
            NetObjectManager.Instance.SpawnObject(drop,DropPos, new Vector3());
        }

        // ISerializable 接口实现
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
                _EquipType = (int)EquipType,
                _Damage = Damage,
                _AttackAnimIndex = AttackAnimIndex,
                _AttackDistance = AttackDistance,
                _AttackInterval = AttackInterval
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
            EquipType = (E_EquipType)dto._EquipType;
            Damage = dto._Damage;
            AttackAnimIndex = dto._AttackAnimIndex;
            AttackDistance = dto._AttackDistance;
            AttackInterval = dto._AttackInterval;
        }

        /// <summary>注：根据 EquipType 自动获取对应的槽位可用性</summary>
        private E_EquipAVL GetEquipAVL()
        {
            switch (EquipType)
            {
                case E_EquipType.Knife: return E_EquipAVL.BothHands;
                case E_EquipType.Sword: return E_EquipAVL.BothHands;
                case E_EquipType.Axe: return E_EquipAVL.BothHands;
                case E_EquipType.Mace: return E_EquipAVL.BothHands;
                case E_EquipType.Shield: return E_EquipAVL.OffHand;
                case E_EquipType.Staff: return E_EquipAVL.TwoHand;
                case E_EquipType.TwoHandSword: return E_EquipAVL.TwoHand;
                case E_EquipType.TwoHandAxe: return E_EquipAVL.TwoHand;
                default: return E_EquipAVL.None;
            }
        }
    }


