using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.UFluxTest004
{
    public class SubWindow_Demo004 : AWindow
    {
        public SubWindow_Demo004(string path) : base(path)
        {
        }

        public SubWindow_Demo004(Transform transform) : base(transform)
        {
        }

        [TransformPath("Content")]
        private Text textContent;

        [UIMessageListener]
        private void TestMessage(Window_SimpleDemo004.UIMsg_Test msg)
        {
            textContent.text = msg.MsgContent;
        }
    }
}