using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using 维修公司.Dll;
using 途畔归所.Dll.Core;

public partial class ConsoleManager : Node
{
    public static ConsoleManager Instance { get;  set; }

    Dictionary<string, Func<ConsoleComp,bool>> m_CommandMap = [];


    public override void _Ready()
    {
        Instance = this;
        init();
        GD.Print("[ConsoleManager] 初始化完成");
    }
    public override void _Process(double delta)
    {
    }
    /// <summary>注：初始化 </summary>
    private void init()
    {
        m_CommandMap.Clear();
        m_CommandMap.Add("clear", Clear);
        m_CommandMap.Add("help", Help);
        m_CommandMap.Add("spawn", Spawn);
    }


    /// <summary>注：解析命令 </summary>
    public bool ParseCommand(ConsoleComp consoleComp)
    {
        if (!m_CommandMap.ContainsKey(consoleComp.m_Command[0])) return false;
        m_CommandMap[consoleComp.m_Command[0]](consoleComp);  // 执行委托内容
        return true;
    }

    #region 命令处理
    private bool Clear(ConsoleComp consoleComp)
    {
        consoleComp.m_RichTextLabel.Text = "";
        consoleComp.m_RichTextLabel.Text += "[成功] 控制台已清空\n";
        return true;
    }
    private bool Help(ConsoleComp consoleComp)
    {
        consoleComp.m_RichTextLabel.Text += "===== 控制台命令 =====\n";
        consoleComp.m_RichTextLabel.Text += "spawn 物品名 - 生成指定物品\n";
        consoleComp.m_RichTextLabel.Text += "clear - 清空控制台\n";
        return true;
    }
    /// <summary>注：生成功能  </summary>
    private bool Spawn(ConsoleComp consoleComp)
    {
        RigidBody3D itemDrop = ItemManager.Instance.GetItemDrop(consoleComp.m_Command[1]);
        if (itemDrop == null)
        {
            GD.PrintErr($"[ConsoleManager.Spawn] 物品[{consoleComp.m_Command[1]}]生成失败（GetItemDrop返回空）");
            return false;
        }

        // 先将物品加入场景树（必须放在设置位置前面）
        if (!itemDrop.IsInsideTree())GetTree().CurrentScene.AddChild(itemDrop);

        // 核心：计算玩家前方1.5单位的位置（保留你的偏移）
        Vector3 offset = new Vector3(0, 2, -1.5f);
        Vector3 finalPos = consoleComp.m_player.GlobalPosition + offset;

        // 核心修复2：加入场景树后，再设置全局位置
        itemDrop.GlobalPosition = finalPos;
        return true;
    }

    #endregion
}