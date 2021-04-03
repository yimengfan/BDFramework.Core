using System;
using UnityEngine;

namespace BDFramework.GameStart
{
    [Obsolete("Use IHotfixGameStart on hotfix logic")]
    public interface IGameStart
    {
        void Start();
        void Update();
        void LateUpdate();
        
    }
}



