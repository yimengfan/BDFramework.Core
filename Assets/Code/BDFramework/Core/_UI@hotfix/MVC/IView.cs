using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using  BDFramework.DataListener;
namespace BDFramework.UI
{
    public interface IView
    {
        DataListenerService Model { get; }
 
        Transform Transform { get; }

        void Show();
        void Hide();
        void BindModel();

        void Destory();
    }
}