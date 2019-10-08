using UnityEngine;

namespace BDFramework.UI
{
    public class SubWindow : AWindow
    {
        public SubWindow(Transform transform) : base(transform)
        {
           
        }

        public override void Close()
        {
            base.Close();
            this.Transform.gameObject.SetActive(false);
        }

        public override void Open(WindowData data = null)
        {
            base.Open();           
            this.Transform.gameObject.SetActive(true);
        }

    }
}