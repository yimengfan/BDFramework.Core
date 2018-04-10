using System;

namespace BDFramework.GameLife
{
    public class GameStartAtrribute : Attribute
    {
        //游戏序号
        public int Index;

        public GameStartAtrribute(int index)
        {
            this.Index = index;
        }
    }
}