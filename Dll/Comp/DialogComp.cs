using Godot;
using 途畔归所.Dll.Base;
using 途畔归所.Dll.Utils;
using 途畔归所.Dll.View;

namespace 途畔归所.Dll.Comp
{
    public partial class DialogComp : Node
    {



        string log = "你好！";


        public override void _Ready()
        {

            if (GetParent() is not CreatureBase node)
            {
                CatUtils.StopAndExit(this);
                return;
            }
        }


        public void StartDialog(Player pl)
        {
            var view = pl.m_PlayerGUI.GetDialogView();

            if (view == null) return;

            view.SetDialogTet(log);
        }

    }
}
