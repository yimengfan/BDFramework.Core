using BDFramework.GameStart;

namespace Game
{
    [GameStartAtrribute(0)]
    public class LocalGameStart : IGameStart
    {
        public void Start()
        {
            BDebug.Log("主工程代码启动!","red");
        }

        public void Update()
        {
            
        }

        public void LateUpdate()
        {
            
        }
    }
}