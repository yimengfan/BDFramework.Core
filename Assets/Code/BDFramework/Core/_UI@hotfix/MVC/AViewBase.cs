using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using BDFramework;
using LitJson;

namespace BDFramework.UI
{
    public class AViewBase :AIEnumeratorTaskMgr,IView
    {
        public DataListenerService Model { get; private set; }
        public Transform Transform { get; private set; }
       virtual public void Show()
        {
            Transform.gameObject.SetActive(true);
        }

      virtual  public void Hide()
        {
            Transform.gameObject.SetActive(false);
        }

        public AViewBase(Transform t ,DataListenerService service)
        {
            this.Model = service;
            this.Transform = t;
        }
        //
        virtual public void BindModel()
        {
            
        }

        //
       virtual public void Destory()
        {
            //throw new System.NotImplementedException();
        }


        
        
    }
}