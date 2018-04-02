using System.Runtime.InteropServices.ComTypes;
using UnityEngine;

namespace BDFramework.UI
{
    public interface IView
    {
        DataDrive_Service DataBinder { get; }
 
        Transform Transform { get; }

        void Show();
        void Hide();
        void BindData();

        void Destory();
    }
}