using 途畔归所.Dll.Base;
using 途畔归所.Dll.Manager;

namespace 途畔归所.Dll.View
{
    public partial class EscView : UIPanelBase
    {


        private void Quit()
        {


        }



        private void Save() => SaveManager.Instance.Save();



        public void ToggleUI() => this.Visible = !this.Visible;




    }
}
