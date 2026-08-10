using Godot;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;
using static System.Net.Mime.MediaTypeNames;

namespace 途畔归所.Dll.NetWork;

[GlobalClass]
public partial class NetTransformSync : Node
{
    [Export] private float _syncInterval = 0.05f;
    [Export] private float _smoothLerpSpeed = 15.0f;

    private float _timer;
    private Node3D _node3D;
    private NetSyncBase _sync;
    private NetObject _netobj;

    private Vector3 _curPos { get => _node3D.GlobalPosition; set => _node3D.GlobalPosition = value; }
    private Vector3 _curRot { get => _node3D.GlobalRotation; set => _node3D.GlobalRotation = value; }
    private Vector3 _targetPos { get => _netobj.Position; set => _netobj.Position = value; }
    private Vector3 _targetRot { get => _netobj.Rotation; set => _netobj.Rotation = value; }

    public override void _Ready()
    {
        if (GetParent() is Node3D node3D) _node3D = node3D;

        foreach (var comp in _node3D.GetChildren())
        {
            if (comp is NetSyncBase netSyncBase)
            {
                _sync = netSyncBase;
                break;
            }
        }

        if (_sync == null)
        {
            CatLog.Err("[NetTransformSync._Ready]：未有在挂载对象中找到 NetSyncBase 组件，已销毁");
            CatUtils.StopAndExit(this);
            return;
        }

        if (_sync.NetObj == null) return;

        _netobj = _sync.NetObj;

        // 注册变换同步 RPC
        _sync.RegisterRpc<Vector3, Vector3>("NetTransformSync", RPC_NetTransformSync);
        _sync.RegisterRpc<Vector3, Vector3>("ClientTransformReport", RPC_ClientTransformReport);
    }

    public override void _Process(double delta)
    {
        if (!_sync.IsOwner) return;
        if (_targetPos == _curPos && _targetRot == _curRot) return;

        _targetPos = _curPos;
        _targetRot = _curRot;

        if (NetCore.Instance.IsHost)
        {
            _sync.CallAllRpc("NetTransformSync", _curPos, _curRot, reliable: false);
        }
        else
        {
            _sync.CallRpc("ClientTransformReport", new Godot.Collections.Array { _curPos, _curRot }, NetCore.ServerID, reliable: false);
        }
    }

    // 客户端上报给主机（由主机转发给其他客户端）
    private void RPC_ClientTransformReport(long senderId, Vector3 pos, Vector3 rot)
    {
        if (NetCore.Instance.IsClient) return;

        // 更新目标位置
        _curPos = pos;
        _curRot = rot;
        _targetPos = pos;
        _targetRot = rot;

        // 转发给其他客户端（排除发送者）
        foreach (long peerId in Multiplayer.GetPeers())
        {
            if (peerId != senderId && peerId != NetCore.ServerID)
            {
                _sync.CallRpc("NetTransformSync", new Godot.Collections.Array { pos, rot }, peerId, reliable: false);
            }
        }
    }

    // 所有客户端接收变换
    private void RPC_NetTransformSync(long senderId, Vector3 pos, Vector3 rot)
    {
        if (NetCore.Instance.IsHost) return;

        _curPos = pos;
        _curRot = rot;
        _targetPos = pos;
        _targetRot = rot;
    }
}
