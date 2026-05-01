using Godot;
using 维修公司.Dll.data;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Data;
using static Godot.Control;

public partial class SlotComp : UIPanelBase
{
    [Export] public Button m_button;
    [Export] public TextureRect m_icon;
    [Export] public Label m_text;

    private Player m_Player;

    public int m_SlotID { get => m_SlotData.m_SlotIndex; set => m_SlotData.m_SlotIndex = value; }
    public SlotData m_SlotData;

    private bool m_IsDragging = false;
    private TextureRect m_DragIcon;

    public override void _Ready() => m_SlotData = (m_SlotData == null) ? m_SlotData = new SlotData() : m_SlotData;

    /// <summary>注：处理格子上的 GUI 输入事件（鼠标按下/移动）。</summary>
    /// <param name="mInput">输入事件对象。</param>
    public void OnGuiInput(InputEvent mInput)
    {
        if (mInput is InputEventMouseButton btn)
        {
            OnClickUp(btn);
            return;
        }

        if (mInput is InputEventMouseMotion motion)
        {
            DoDrag(motion);
        }
    }

    /// <summary>注：执行物品拖动逻辑。</summary>
    /// <param name="motion">鼠标移动事件。</param>
    private void DoDrag(InputEventMouseMotion motion)
    {
        if (m_IsDragging && m_DragIcon != null)
        {
            m_DragIcon.GlobalPosition = motion.GlobalPosition;
            return;
        }

        if (motion.ButtonMask != MouseButtonMask.Left) return;

        if (m_SlotData == null || m_SlotData.m_ItemData == null) return;

        if (m_DragIcon != null) return;

        m_IsDragging = true;
        m_icon.Visible = false;

        m_DragIcon = new TextureRect();
        m_DragIcon.ExpandMode = m_icon.ExpandMode;
        m_DragIcon.Size = m_icon.Size;
        m_DragIcon.Texture = m_icon.Texture;
        m_DragIcon.ZIndex = 1000;
        m_DragIcon.TopLevel = true;
        m_DragIcon.MouseFilter = MouseFilterEnum.Ignore;
        m_Player.m_CanvasLayer.AddChild(m_DragIcon);
    }

    /// <summary>注：鼠标左键抬起时停止拖动并执行交换或丢弃。</summary>
    /// <param name="btn">鼠标按钮事件。</param>
    private void OnClickUp(InputEventMouseButton btn)
    {
        if (btn.ButtonIndex == MouseButton.Left && !btn.Pressed)
        {
            m_DragIcon?.QueueFree();
            m_DragIcon = null;
            m_IsDragging = false;
            m_icon.Visible = true;
            SwapSlot();
        }
    }

    /// <summary>注：交换当前格子与目标格子的物品数据。</summary>
    private void SwapSlot()
    {
        var comp = GetMouseSlot();
        if (comp == null)
        {
            DropItem();
            return;
        }

        if (comp.m_SlotID == m_SlotID) return;

        if (comp.m_SlotData.m_ItemData == null)
        {
            comp.m_SlotData.m_ItemData = m_SlotData.m_ItemData.CopyData();
            m_SlotData.m_ItemData = null;
        }
        else
        {
            var data = comp.m_SlotData.m_ItemData.CopyData();
            comp.m_SlotData.m_ItemData = m_SlotData.m_ItemData.CopyData();
            m_SlotData.m_ItemData = data;
        }
        comp.Refresh();
        Refresh();
    }

    /// <summary>注：在当前鼠标没有指向任何格子时丢弃物品到世界。</summary>
    private void DropItem()
    {
        if (m_SlotData == null || m_SlotData.IsSlotNull) return;

        ItemData dropData = m_SlotData.m_ItemData.CopyData();

        RigidBody3D drop = dropData.DataToDrop();
        if (drop == null) return;

        SceneTree tree = GetTree();
        if (tree?.CurrentScene == null)
        {
            drop.QueueFree();
            return;
        }
        tree.CurrentScene.AddChild(drop);

        drop.GlobalPosition = m_Player.m_eye.GlobalPosition + m_Player.m_eye.GlobalBasis.Z * -1.0f;

        m_SlotData.m_ItemData = null;
        Refresh();

        m_Player.m_InventoryComp?.RefSlot();
    }

    /// <summary>注：获取当前鼠标悬停位置所在的格子组件。</summary>
    /// <returns>鼠标下的 SlotComp，若没有则返回 null。</returns>
    private SlotComp GetMouseSlot()
    {
        Control control = GetViewport().GuiGetHoveredControl();
        if (control == null) return null;
        var comp = control.GetParent();
        if (comp == null) return null;
        return comp as SlotComp;
    }

    /// <summary>注：判断当前格子是否为空。</summary>
    /// <returns>如果格子无物品返回 true。</returns>
    public bool IsSlotNull() => m_SlotData.IsSlotNull;

    /// <summary>注：尝试将传入的物品数据堆叠到当前格子上。</summary>
    /// <param name="itemData">待堆叠的物品数据。</param>
    public void TryStack(ItemData itemData)
    {
        if (m_SlotData == null || m_SlotData.m_ItemData == null) return;

        if (m_SlotData.m_ItemData.m_ID == itemData.m_ID && m_SlotData.m_ItemData.IsStack())
        {
            m_SlotData.m_ItemData.TryStack(itemData);
        }
    }

    /// <summary>注：将物品数据直接填入当前空格子。</summary>
    /// <param name="itemData">要放入的物品数据。</param>
    public void ApplyData(ItemData itemData)
    {
        if (IsSlotNull()) m_SlotData.m_ItemData = itemData;
    }

    /// <summary>注：刷新格子的图标与数量显示。</summary>
    public void Refresh()
    {
        if (IsSlotNull())
        {
            m_text.Text = string.Empty;
            m_icon.Texture = null;
            m_icon.Visible = true;
        }
        else
        {
            m_text.Text = $"{m_SlotData.m_ItemData.m_Name} x{m_SlotData.m_ItemData.m_Stack}";
            m_icon.Texture = m_SlotData.m_ItemData.m_Icon;
        }
    }

    /// <summary>注：绑定所属玩家实例。</summary>
    public void BindPlayer(Player player) => m_Player = player;
}