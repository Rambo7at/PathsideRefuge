using Godot;
using Godot.Collections;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Data;
using 途畔归所.Dll.Interface;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.NetWork;
using 途畔归所.Dll.Utils;
using static 途畔归所.Dll.Data.VegetationData;

namespace 途畔归所.Dll.Base;

public partial class VegetationBase : StaticBody3D, IDamageable
{
    [Export] public Node3D m_dropPot;
    [Export] private VegetationData m_VegetationData { get; set; }

    protected NetSyncBase m_netSyncBase;

    public string ID => m_VegetationData.m_ID;
    public string ObjectName => m_VegetationData.m_name;
    public VegetationType Type => m_VegetationData.m_type;
    public float Health { get => m_VegetationData.m_health; protected set => m_VegetationData.m_health = value; }
    public bool IsDead => Health <= 0;
    public Vector3 DropPos => m_dropPot?.GlobalPosition ?? GlobalPosition;
    public Array<DropBase> DropTable => m_VegetationData.m_dropTable;

    public override void _EnterTree()
    {
        if (m_VegetationData == null)
        {
            CatLog.Err($"[VegetationBase] {Name} 缺少 VegetationData");
            CatUtils.StopAndExit(this);
            return;
        }
        if (CatUtils.FindChildNode<NetSyncBase>(this) is not NetSyncBase sync)
        {
            CatLog.Err($"[VegetationBase] {Name} 缺少 NetSyncBase");
            CatUtils.StopAndExit(this);
            return;
        }
        m_netSyncBase = sync;
        m_netSyncBase.RegisterRpc<float>("RPC_RequestDamage", RPC_RequestDamage);
        m_netSyncBase.RegisterRpc<float>("RPC_SyncHealth", RPC_SyncHealth);
        m_netSyncBase.RegisterRpc("RPC_RequestHealth", RPC_RequestHealth);
    }

    public override void _Ready()
    {
        if (NetCore.Instance.IsClient && m_netSyncBase.IsInit)
        {

            m_netSyncBase.SendRpcToHost("RPC_RequestHealth");
        }
    }

    /// <summary>实际伤害结算（仅主机调用）</summary>
    protected virtual void ApplyDamage(float amount)
    {
        Health -= amount;

        // 主机广播血量给所有客户端
        m_netSyncBase.SendRpcBroadcast("RPC_SyncHealth", Health);
        CatLog.Debug($"{ObjectName}被命中 {Health} 剩余 ");
        if (IsDead) OnDeath();

    }

    /// <summary>客户端请求主机结算伤害</summary>
    protected virtual void RPC_RequestDamage(long senderId, float amount)
    {
        if (NetCore.Instance.IsClient) return;
        ApplyDamage(amount);
    }

    /// <summary>客户端接收主机广播的血量同步</summary>
    protected virtual void RPC_SyncHealth(long senderId, float newHealth)
    {
        if (NetCore.Instance.IsHost) return;
        Health = newHealth;
        if (IsDead) OnDeath();
    }

    /// <summary>主机向请求者单独发送当前血量</summary>
    protected virtual void RPC_RequestHealth(long senderId)
    {
        if (NetCore.Instance.IsClient) return;
        m_netSyncBase.SendRpcToPeer("RPC_SyncHealth", senderId, Health);
    }


    /// <summary>死亡钩子，子类重写以生成掉落物等</summary>
    protected virtual void OnDeath()
    {
        foreach (var drop in DropTable)
        {
            if (drop == null) continue;
            foreach (var item in drop.GetItemDrop())
            {
                NetObjectManager.Instance.SpawnObject(item, DropPos, Vector3.Zero);
            }
        }
        CatUtils.StopAndExit(this);
    }


    /// <summary>外部调用入口，区分主机/客户端</summary>
    public virtual void TakeDamage(float amount, Node node = null)
    {
        if (IsDead) return;

        if (NetCore.Instance.IsHost)
        {
            ApplyDamage(amount);
        }
        else
        {
            m_netSyncBase.SendRpcToHost("RPC_RequestDamage", amount);
        }
    }
}

