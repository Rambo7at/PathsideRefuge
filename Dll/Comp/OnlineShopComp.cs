using Godot;
using System;
using System.Runtime.InteropServices.JavaScript;
using 维修公司.Dll;
using 维修公司.Utils;
using 途畔归所.Dll.Core;

public partial class OnlineShopComp : Control
{
    /// <summary>注：商品的显示栏</summary>
    [Export] private GridContainer m_GridContainer;

    /// <summary>注：商品的搜索栏</summary>
    [Export] private LineEdit m_LineEdit;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready() => refGoods();

    public void refGoods()
    {

        foreach (var good in ShopManager.Instance.OnlineShopGoods)
        {
            var Ui = UIManager.Instance.GetUI("GoodUI");
            var Scropt = ToolUtils.GetNodeScript<GoodComp>(Ui);
            Scropt.图片栏.Texture = good.m_Good.m_Icon;
            Scropt.价格栏.Text = good.m_Price.ToString();

            m_GridContainer.AddChild(Ui);
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
