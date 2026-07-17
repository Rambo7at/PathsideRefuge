using Godot;
using Godot.Collections;
using 维修公司.Dll.data;
using 途畔归所.Dll.Creature;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Base
{
    public partial class Humanoid : CreatureBase
    {
        [Export] public BoneAttachment3D m_HandL;
        [Export] public BoneAttachment3D m_HandR;
        [Export] private Array<PackedScene> m_DefaultEquip;




        public Equipment m_Equipment;


        public Array<ItemData> m_EquipData { get => m_CreatureData.EquipData; set => m_CreatureData.EquipData = value; }





        public override void _EnterTree()
        {
            // 父类初始化
            base._EnterTree();

            // 自检
            if (m_HandL == null || m_HandR == null)
            {
                string loga = m_HandL == null ? "m_HandL/" : string.Empty;
                string logb = m_HandR == null ? "m_HandR/" : string.Empty;

                CatLog.Err($"[CreatureBase._EnterTree]: {Name}-{m_Name} 缺少核心组件：{loga + logb}，请检查编译器");
                CatUtils.StopAndExit(this);
            }

            AddChild(m_Equipment ??= new Equipment());
        }


        public override void _Ready() => base._Ready();



        protected override float FinalDamage()
        {
            float damage = m_BaseDamage;

            return damage;
        }




    }
}
