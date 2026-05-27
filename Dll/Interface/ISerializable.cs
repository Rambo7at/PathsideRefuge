using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 途畔归所.Dll.Interface
{
    public interface ISerializable
    {
        byte[] Serialize();
        void Deserialize(byte[] data);
    }
}
