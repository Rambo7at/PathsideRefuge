using Godot;
using System;
using System.Collections.Generic;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Core;
using 途畔归所.Dll.Manager;
using 途畔归所.Dll.Utils;

namespace 途畔归所.Dll.NetWork;

/// <summary>注：全局 RPC 网关，统一管理所有跨场景 RPC 调用</summary>
public partial class RpcGateway : Node
{
    private static RpcGateway _instance;
    public static RpcGateway Instance => _instance ??= new();

    public override void _Ready()
    {
        _instance = this;
        CatLog.Ok("[RpcGateway] 初始化完成");
    }
}