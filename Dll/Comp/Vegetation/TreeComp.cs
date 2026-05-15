using Godot;
using Godot.Collections;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.Comp.Vegetation
{
    public partial class TreeComp : VegetationComp, IDamageable
    {


        [Export] public Array<string> m_dropList { get; set; }

        [Export] public Node3D spawn;


        public void TakeDamage(float amount, Player source = null)
        {
            m_Health -= amount;
            CatLog.Debug($"[TreeComp] 被命中 剩余 {m_Health}");
        }

        public override void _Ready()
        {

            InitEntityBase();




        }

        public override void _ExitTree()
        {

        }

        public override void _PhysicsProcess(double delta)
        {

            Die(this);
        }

        public override void Die(Node node)
        {
            if (m_Health <= 0)
            {

                foreach (var item in m_dropList)
                {
                    var DROP = ItemManager.Instance.GetItemDrop(item);
                    GetTree().Root.AddChild(DROP);
                    DROP.GlobalPosition = spawn.GlobalPosition;

                }
                CatUtils.StopAndExit(node);
            }
        }

    }
}
