using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using 维修公司.Dll;

public partial class ConsoleManager : Node
{
    public static ConsoleManager Instance;

    public override void _Ready()
    {
        // 单例加固：防止重复初始化
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;
        GD.Print("[ConsoleManager] 初始化完成");
    }

    public override void _Process(double delta)
    {
    }

    public void Spawn(string prefabName, Vector3 spawnPos)
    {
        if (ItemManager.Instance == null)
        {
            GD.PrintErr("[ConsoleManager.Spawn] ItemManager实例为空，无法生成物品");
            return;
        }

        RigidBody3D itemDrop = ItemManager.Instance.GetItemDrop(prefabName);
        if (itemDrop == null)
        {
            GD.PrintErr($"[ConsoleManager.Spawn] 物品[{prefabName}]生成失败（GetItemDrop返回空）");
            return;
        }

        // 核心修复1：先将物品加入场景树（必须放在设置位置前面）
        if (!itemDrop.IsInsideTree())
        {
            GetTree().CurrentScene.AddChild(itemDrop);
        }

        // 核心：计算玩家前方1.5单位的位置（保留你的偏移）
        Vector3 offset = new Vector3(0, 2, -1.5f);
        Vector3 finalPos = spawnPos + offset;

        // 核心修复2：加入场景树后，再设置全局位置
        itemDrop.GlobalPosition = finalPos;

        GD.Print($"[ConsoleManager] 成功生成物品[{prefabName}]到玩家前方位置：{finalPos}");
    }

}