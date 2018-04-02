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

        public override void Open()
        {
            base.Open();           
            this.Transform.gameObject.SetActive(true);
        }
    }
}