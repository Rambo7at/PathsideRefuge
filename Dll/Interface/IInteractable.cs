using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Godot;

namespace 维修公司.Dll.Interface
{
    public interface IInteractable
    {
        /// <summary>执行互动</summary>
        void PlayerInteract(bool InputE, bool InputF,PlayerController player);
    }
}

