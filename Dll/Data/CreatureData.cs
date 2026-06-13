using Godot;
using Godot.Collections;
using System;
using System.Runtime.InteropServices.JavaScript;
using 维修公司.Dll.data;

namespace 途畔归所.Dll.Data
{
    [GlobalClass]
    public partial class CreatureData : Resource
    {
        public enum BioType
        {
            Player,     // 玩家
            Monster,    // 怪物
            Npc,        // 中立NPC
            Animal,     // 野生动物
            Pet,        // 玩家宠物
        }

        public enum Race
        {
            Human,      // 人类
        }

        [ExportGroup("基础信息")]
        [Export] public string m_name = string.Empty;      // 名字
        [Export] public bool m_isPlayer = false;
        [Export] public PlayerData m_playerData;
        [Export] public BioType m_bioType;                 // 生物类型
        [Export] public Race m_race = Race.Human;          // 种族

        [ExportGroup("等级与成长")]
        [Export] public int m_level = 1;                   // 当前等级
        [Export] public int m_expPerLevel = 100;           // 每次升级所需基础经验值

        [ExportGroup("核心属性")]
        [Export] public int m_strength = 1;                // 力量：影响物理攻击力、负重
        [Export] public int m_agility = 1;                 // 敏捷：影响攻击速度、闪避率、暴击率
        [Export] public int m_constitution = 1;            // 体能：影响最大生命值、生命回复
        [Export] public int m_stamina = 1;                 // 耐力：影响最大耐力值、耐力回复
        [Export] public int m_resilience = 1;              // 韧性：影响防御力、暴击抵抗、减伤

        [ExportGroup("基础属性")]
        [Export] public float m_speed = 5.0f;              // 移动速度
        [Export] public float m_jump = 4.5f;               // 跳跃力
        [Export] public float m_maxHealth = 100f;          // 最大生命值
        [Export] public float m_maxStamina = 50f;          // 最大耐力值
        [Export] public float m_maxMana = 50f;             // 最大法力值
        [Export] public float m_baseAttack = 5f;           // 基础攻击力
        [Export] public float m_critChance = 5f;           // 暴击率(%)

        [ExportGroup("生活技能")]
        [Export] public int m_skillCooking = 1;            // 料理：影响食物制作效果与品质
        [Export] public int m_skillForging = 1;            // 锻造：影响武器/防具制作效果与品质
        [Export] public int m_skillHandiness = 1;          // 巧手：影响开锁、陷阱解除、精细制作
        [Export] public int m_skillPersuasion = 1;         // 交涉：影响NPC对话选项、交易价格

        [ExportGroup("物品与掉落")]
        [Export] public InventoryData m_inventoryData = new(); // 背包数据
        [Export] public string[] m_dropTable = [];         // 掉落表(物品ID数组)

        [ExportGroup("AI巡逻")]
        [Export] public float m_patrolRadius = 10.0f;      // 巡逻半径
        [Export] public float m_patrolStopTime = 2.0f;     // 巡逻点停留时间

        [ExportGroup("AI寻路与追击")]
        [Export] public float m_chaseTargetDistance = 1.0f; // 追击时与目标保持的距离
        [Export] public float m_rotationSpeed = 1.0f;

        public CreatureData DeepCopy() => this.DuplicateDeep() as CreatureData;

        public int GetInventoryItemCount()
        {
            int index = 0;
            foreach (var item in m_inventoryData.m_itemArr)
            {
                if (item == null) continue;

                index++;
            }
            return index;
        }
    }
}
