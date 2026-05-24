using Godot;
using Godot.Collections;
using 途畔归所.Dll.NetWork;

namespace 途畔归所.Dll.Data
{
    [GlobalClass]
    public partial class SceneData : Resource
    {
        public enum SceneType
        {
            GameScene = 0,
            ViewScene = 1,
        }

        [Export] public string m_sceneName { get; set; }

        [Export] public int  m_sceneHash { get; set; }

        [Export] public bool m_newScene { get; set; } = true;

        [Export] public SceneType m_sceneType { get; set; }

        [Export] public Array<NetObject> m_NetObjectArr { get; set; }



        public SceneData DeepCopy() => this.DuplicateDeep() as SceneData;
    }
}
