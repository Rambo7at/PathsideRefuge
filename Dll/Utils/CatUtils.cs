using Godot;
using System;

namespace 途畔归所.Dll.Utils
{
    /// <summary>
    /// 项目通用工具类 —— 含稳定字符串哈希等
    /// </summary>
    public static class CatUtils
    {
        /// <summary>
        /// 注：获取与运行时无关的稳定哈希码（djb2 变体，种子使用项目端口 3043）。
        /// 用于 RPC 方法名、变量名等需要网络一致的哈希。
        /// </summary>
        public static int GetStableHashCode(this string str)
        {
            unchecked
            {
                int hash = 3043;
                int hash2 = 3043;
                int length = str.Length;

                for (int i = 0; i < length; i += 2)
                {
                    hash = ((hash << 5) + hash) ^ str[i];

                    if (i + 1 < length)
                        hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash + hash2 * 686421487;
            }
        }


        /// <summary>从资源路径提取名称</summary>
        /// <param name="resourcePath">资源路径</param>
        /// <returns>返回资源名，失败时为空</returns>
        public static string GetResourceName(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                GD.PrintErr("[ToolUtils.GetResourceName] 资源路径为空");
                return null;
            }

            // 截取最后一个/后的文件名（如手电筒.tscn）
            int lastSlashIndex = resourcePath.LastIndexOf('/');
            if (lastSlashIndex == -1)
            {
                GD.PrintErr($"[ToolUtils.GetResourceName] 路径 {resourcePath} 格式异常（无/分隔符）");
                return null;
            }

            string fileNameWithExt = resourcePath.Substring(lastSlashIndex + 1);
            string resourceName = fileNameWithExt.Replace(".tscn", "");

            // 提取结果为空则返回null
            if (string.IsNullOrEmpty(resourceName))
            {
                GD.PrintErr($"[ToolUtils.GetResourceName] 路径 {resourcePath} 提取名称为空");
                return null;
            }

            return resourceName;
        }



        public static void StopAndExit(Node node)
        {
            node.SetProcess(false);
            node.SetPhysicsProcess(false);

            node.QueueFree();
        }





    }
}