using Godot;
using Godot.Collections;
using System;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;

namespace 途畔归所.Dll.Data
{
    [GlobalClass]
    public partial class CreatureData : Resource
    {
        public enum E_CreatureType
        {
            Humanoid,   // 人形生物（玩家、NPC、骷髅兵）
            Beast,      // 野兽（狼、熊）
            Monster,    // 怪物（史莱姆、恶魔）
            Mechanical, // 机械（魔像、陷阱）
        }

        public enum E_Faction
        {
            Neutral,    // 中立（普通NPC、商人）
            Player,     // 玩家及友方
            Enemy,      // 敌对势力
            Wild,       // 野生生物（被动攻击或中立）
        }

        [ExportGroup("基础信息")]
        [Export] public string Name { get; set; } = string.Empty;          // 角色名称
        [Export] public bool IsPlayer { get; set; } = false;              // 是否为玩家角色
        [Export] public int PlayerID { get; set; }

        [Export] public E_CreatureType CreatureType { get; set; }          // 生物类型（人形/野兽/怪物等）
        [Export] public E_Faction Faction { get; set; }                    // 所属阵营（玩家/敌对/中立/野生）

        [ExportGroup("等级与成长")]
        [Export] public int Level { get; set; } = 1;                      // 当前等级
        [Export] public int ExpPerLevel { get; set; } = 100;              // 每级所需经验值
        [Export] public int Strength { get; set; } = 1;                   // 力量（影响物理攻击力/负重）
        [Export] public int Agility { get; set; } = 1;                    // 敏捷（影响攻速/闪避/暴击率）
        [Export] public int Constitution { get; set; } = 1;               // 体质（影响生命值/回复）
        [Export] public int Vitality { get; set; } = 1;                    // 活力（影响耐力值/回复）
        [Export] public int Resilience { get; set; } = 1;                 // 韧性（影响防御/暴击抵抗/减伤）

        [ExportGroup("基础属性")]
        [Export] public float Speed { get; set; } = 5.0f;                 // 移动速度
        [Export] public float Jump { get; set; } = 4.5f;                  // 跳跃力
        [Export] public float MaxHealth { get; set; } = 100f;             // 最大生命值
        [Export] public float MaxStamina { get; set; } = 50f;             // 最大耐力值
        [Export] public float MaxMana { get; set; } = 50f;                // 最大法力值

        [ExportGroup("生活技能")]
        [Export] public int SkillCooking { get; set; } = 1;               // 烹饪技能
        [Export] public int SkillForging { get; set; } = 1;               // 锻造技能
        [Export] public int SkillHandiness { get; set; } = 1;             // 巧手（开锁/陷阱/制造）
        [Export] public int SkillPersuasion { get; set; } = 1;            // 交涉（对话/交易）

        [ExportGroup("战斗")]
        [Export] public float BaseDamage { get; set; } = 5f;              // 基础攻击力
        [Export] public float CritChance { get; set; } = 5f;              // 基础暴击率(%)

        [Export] public float StaggerDamage { get; set; } = 0.2f;
        [Export] public float StaggerTime { get; set; } = 1;
        

        [ExportGroup("物品与掉落")]
        [Export] public InventoryData InventoryData { get; set; } = new(); // 背包数据
        [Export] public  Array<ItemData> EquipData { get; set; } = [];
        [Export] public Array<DropBase> DropTable { get; set; } = [];      // 死亡掉落表

        [ExportGroup("AI巡逻")]
        [Export] public float PatrolRadius { get; set; } = 10.0f;         // 巡逻半径
        [Export] public float PatrolStopTime { get; set; } = 2.0f;        // 巡逻点停留时间

        [ExportGroup("AI寻路与追击")]
        [Export] public float ChaseTargetDistance { get; set; } = 1.0f;    // 追击时与目标保持的距离
        [Export] public float RotationSpeed { get; set; } = 5.0f;          // 转身速度

        [ExportGroup("状态信息")]
        [Export] public Variant m_data { get; set; }

        public float Health { get; set; } 
        public float Stamina { get; set; }
        public float Mana { get; set; }










        public int LevelPoints => GetLevelPoints();

        public CreatureData DeepCopy() => this.DuplicateDeep() as CreatureData;

        public int GetInventoryItemCount()
        {
            int index = 0;
            foreach (var item in InventoryData.m_itemArr)
            {
                if (item == null) continue;

                index++;
            }
            return index;
        }

        private int GetLevelPoints()
        {
            int point = -1;
            point += Level;
            point -= Strength - 1;
            point -= Agility - 1;
            point -= Constitution - 1;
            point -= Vitality - 1;
            point -= Resilience - 1;
            return point;
        }

    }
}
