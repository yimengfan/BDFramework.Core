using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using BDFramework.DataListener;
namespace Game.UI
{
    public interface M_IView
    {
        DataListenerService Model { get; }
 
        Transform Transform { get; }

        void Show();
        void Hide();
        void BindModel();

        void Destory();
    }
}