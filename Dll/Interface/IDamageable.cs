using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using 途畔归所.Dll.Base;

namespace 途畔归所.Dll.Interface
{
    public interface IDamageable
    {

        /// <summary>
        /// 接收伤害
        /// </summary>
        /// <param name="amount">伤害量</param>
        /// <param name="source">伤害来源（可为空）</param>
        void TakeDamage(float amount, Player source = null);


    }
}
