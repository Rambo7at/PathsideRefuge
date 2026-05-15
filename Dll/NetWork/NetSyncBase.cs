using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork
{

    [GlobalClass]
    public partial class NetSyncBase : Node
    {

        private Node3D _node3D;

        private int _nodeHash;

        public NetObject m_NetObj { get; set; }

        public bool IsOwner => m_NetObj != null && m_NetObj.OwnerPeerID == NetCore.Instance.LocalPeerID;



        public override void _EnterTree()
        {

            var node = GetParent();

            if (node is not Node3D node3D)
            {
                CatLog.Err("[NetSyncBase._EnterTree]：挂载的组件对象，不是Node3D类型，已删除");

                node.QueueFree();  // 目前不需要关闭循环
                return;
            }

            _node3D = node3D;
            _nodeHash = CatUtils.GetStableHashCode(node3D.Name);


            if (m_NetObj == null)
            {

                if (NetCore.Instance.IsHost)
                {
                    var ID = NetObjectRegistry.Instance.RegisterObject(_nodeHash, _node3D.GlobalPosition, _node3D.GlobalRotation);

                    var netobj = NetObjectRegistry.Instance.GetNetObject(ID);

                    m_NetObj = netobj;

                    CatLog.Warn("[NetSyncBase._Ready]：发现未注册组件，已提交注册");
                }
                else
                {
                    CatLog.Warn("[NetSyncBase._Ready]：发现未场景有，注册组件，非主机状态，已删除");
                    node.QueueFree();  // 目前不需要关闭循环
                    return;
                }

            }

        }





        public override void _Ready()
        {

        }

        public override void _Process(double delta)
        {

        }


    }
}
