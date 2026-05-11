using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace 途畔归所.Dll.NetWork
{

    /// <summary>
    /// 注：网络 Transform 同步组件，依赖 NetSyncBase。
    /// Owner 端写入 NetObj 并递增版本号；非 Owner 端从 NetObj 读取并插值平滑跟随。
    /// </summary>
    public partial class NetTransformSync : Node
    {

    }
}
