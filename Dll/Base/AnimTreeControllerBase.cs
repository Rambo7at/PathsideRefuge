using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace 途畔归所.Dll.Base
{
    public class AnimTreeControllerBase
    {
        /// <summary>注：存储已绑定的动画动作，键为动画名，值为触发 Travel 的委托。</summary>
        public System.Collections.Generic.Dictionary<string, Action> m_TravelActions = [];

        /// <summary>注：关联的 AnimationTree 节点引用。</summary>
        protected AnimationTree m_AnimationTree;

        /// <summary>注：已注册的状态机与其对应播放器的映射。</summary>
        protected System.Collections.Generic.Dictionary<string, AnimationNodeStateMachinePlayback> m_StateMachinePlaybacks = [];

        /// <summary>注：已注册的状态机与其内部所有动画名列表的映射。</summary>
        protected System.Collections.Generic.Dictionary<string, List<string>> m_StateMachineAnimLists = [];

        /// <summary>注：构造函数，绑定 AnimationTree 实例。</summary>
        public AnimTreeControllerBase(AnimationTree tree) => m_AnimationTree ??= tree;

        /// <summary>注：注册一个状态机：提取其所有动画状态名称，并绑定为 Travel 动作。</summary>
        /// <param name="playbackPath">播放器参数路径，如 "parameters/移动/playback"。</param>
        /// <param name="stateMachineName">状态机在 BlendTree 中的节点名称。</param>
        public void RegisterStateMachine(string playbackPath, string stateMachineName)
        {
            if (string.IsNullOrEmpty(stateMachineName))
            {
                GD.PrintErr($"[AnimTreeControllerBase] 注册失败：状态机名称为空");
                return;
            }

            if (m_StateMachinePlaybacks.ContainsKey(stateMachineName))
            {
                GD.PrintErr($"[AnimTreeControllerBase] 状态机 [{stateMachineName}] 已经注册过");
                return;
            }

            var playback = (AnimationNodeStateMachinePlayback)m_AnimationTree.Get(playbackPath);
            if (playback == null)
            {
                GD.PrintErr($"[AnimTreeControllerBase] 播放器路径无效：{playbackPath}");
                return;
            }

            var stateNames = ExtractStateNames(stateMachineName);
            if (stateNames == null || stateNames.Count == 0)
            {
                GD.PrintErr($"[AnimTreeControllerBase] 未能从状态机 [{stateMachineName}] 提取到任何动画状态");
                return;
            }

            var list = m_StateMachineAnimLists[stateMachineName] = [];
            foreach (var name in stateNames)
            {
                list.Add(name);
            }

            m_StateMachinePlaybacks[stateMachineName] = playback;
            BuildTravelActions(stateMachineName);

            GD.Print($"[AnimTreeControllerBase] 状态机 [{stateMachineName}] 注册完成，共绑定 {m_TravelActions.Count} 个动作");
        }

        /// <summary>注：根据已注册的状态机构建所有动画的 Travel 委托，存入 m_TravelActions。</summary>
        private void BuildTravelActions(string stateMachineName)
        {
            // 精简：直接获取列表（RegisterStateMachine 已确保存在且非空）
            var list = m_StateMachineAnimLists[stateMachineName];
            if (list.Count == 0)
            {
                GD.PrintErr($"[AnimTreeControllerBase] 构建动作失败：状态机 [{stateMachineName}] 没有动画列表");
                return;
            }

            foreach (var animName in list)
            {
                if (m_TravelActions.ContainsKey(animName))
                {
                    GD.Print($"[AnimTreeControllerBase] 警告：动画动作 [{animName}] 将被覆盖");
                }

                string capturedAnim = animName;
                m_TravelActions[animName] = () =>
                {
                    if (m_StateMachinePlaybacks.TryGetValue(stateMachineName, out var pb))
                        pb.Travel(capturedAnim);
                    else
                        GD.PrintErr($"[AnimTreeControllerBase] 执行动作时找不到状态机 [{stateMachineName}]");
                };
            }
        }

        /// <summary>注：从指定的状态机节点中提取所有动画状态名称（StringName 列表）。</summary>
        /// <param name="stateMachineName">状态机在 BlendTree 中的节点名称。</param>
        /// <returns>动画状态名称数组，出错时返回空数组。</returns>
        private Array<StringName> ExtractStateNames(string stateMachineName)
        {
            var blendTree = m_AnimationTree.TreeRoot as AnimationNodeBlendTree;
            if (blendTree == null)
            {
                GD.PrintErr($"[AnimTreeControllerBase] AnimationTree 的根节点不是 AnimationNodeBlendTree");
                return [];
            }

            var node = blendTree.GetNode(stateMachineName);
            if (node == null)
            {
                GD.PrintErr($"[AnimTreeControllerBase] 在 BlendTree 中未找到节点 [{stateMachineName}]");
                return [];
            }

            AnimationNodeStateMachine stateMachine = node as AnimationNodeStateMachine;
            if (stateMachine == null)
            {
                GD.PrintErr($"[AnimTreeControllerBase] 节点 [{stateMachineName}] 不是 AnimationNodeStateMachine");
                return [];
            }

            if (string.IsNullOrEmpty(stateMachine.ResourceName))
            {
                GD.PrintErr($"[AnimTreeControllerBase] 状态机 [{stateMachineName}] 未设置 Resource 名称");
                return [];
            }
            if (stateMachine.ResourceName != stateMachineName)
            {
                GD.PrintErr($"[AnimTreeControllerBase] 状态机 [{stateMachineName}] 的 Resource 名称 ({stateMachine.ResourceName}) 与传入名称不匹配");
                return [];
            }

            return stateMachine.GetNodeList();
        }
    }
}
