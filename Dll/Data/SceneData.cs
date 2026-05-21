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


        [Export] public string m_sceneName;

        [Export] public int  m_sceneHash;

        [Export] public SceneType m_sceneType = SceneType.ViewScene;

        [Export] Array<NetObject> m_NetObject;

    }
}
