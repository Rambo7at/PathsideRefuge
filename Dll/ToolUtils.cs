using Godot;
using System;
using System.Collections.Generic;

namespace 维修公司.Utils
{
    /// <summary>全局工具类</summary>
    public static class ToolUtils
    {
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



        /// <summary>获取节点自身挂载的脚本</summary>
        /// <typeparam name="T">脚本类型</typeparam>
        /// <param name="node">目标节点</param>
        /// <returns>脚本实例，失败返回null</returns>
        public static T GetNodeScript<T>(Node node) where T : Node
        {
            if (node == null)
            {
                GD.PrintErr("[ToolUtils.GetNodeScript] 节点为空");
                return null;
            }
            var script = node.GetNodeOrNull<T>(".");
            if (script == null)
            {
                GD.PrintErr($"[ToolUtils.GetNodeScript] 节点名{node.Name}脚本获取失败");
                return null;
            }

            return script;
        }


        /// <summary>安全获取子节点（避免空引用）</summary>
        /// <typeparam name="T">节点类型</typeparam>
        /// <param name="parent">父节点</param>
        /// <param name="nodePath">子节点路径</param>
        /// <returns>目标节点，失败返回null</returns>
        public static T GetChildSafe<T>(Node parent, string nodePath) where T : Node
        {
            if (parent == null || string.IsNullOrEmpty(nodePath))
            {
                GD.PrintErr("[ToolUtils] 父节点/节点路径为空");
                return null;
            }

            Node node = parent.GetNode(nodePath);
            if (node == null)
            {
                GD.PrintErr($"[ToolUtils] 未找到节点 {nodePath}");
                return null;
            }

            T targetNode = node as T;
            if (targetNode == null)
            {
                GD.PrintErr($"[ToolUtils] 节点 {nodePath} 类型不符");
                return null;
            }

            return targetNode;
        }



        /// <summary>格式化游戏时间为 00:00 格式</summary>
        /// <param name="hour">小时</param>
        /// <param name="minute">分钟</param>
        /// <returns>格式化后的时间字符串</returns>
        public static string FormatGameTime(int hour, int minute)
        {
            return $"{hour:D2}:{minute:D2}";
        }
    }
}