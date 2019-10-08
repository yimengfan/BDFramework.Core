using UnityEngine;

namespace Game.UI
{
    public class M_SubWindow : M_AWindow
    {
        public M_SubWindow(Transform transform) : base(transform)
        {
           
        }

        public override void Close()
        {
            base.Close();
            this.Transform.gameObject.SetActive(false);
        }

        public override void Open(M_WindowData data = null)
        {
            base.Open();           
            this.Transform.gameObject.SetActive(true);
        }

    }
}