using Godot;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.View;

[GlobalClass]
public partial class EscView : CanvasLayer
{
    private const int layerValue = 900;
    public override void _Ready()
    {
        Layer = layerValue;
    }


    private void Quit()
    {


    }
    private void Save() => SaveManager.Instance.Save();
    public void ToggleUI() => this.Visible = !this.Visible;
}

