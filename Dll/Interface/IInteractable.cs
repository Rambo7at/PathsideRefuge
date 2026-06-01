using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Base;

namespace 维修公司.Dll.Interface
{
    public interface IInteractable
    {
        /// <summary>执行互动</summary>
        void PlayerInteract(bool InputE, bool InputF, Player player);
    }
}

