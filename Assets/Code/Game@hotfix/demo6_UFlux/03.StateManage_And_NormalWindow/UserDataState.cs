using System.Collections.Generic;
using BDFramework.UFlux.View.Props;
using UnityEngine;
using UnityEngine.UI;

namespace BDFramework.UFlux.Test
{
 
    public class UserDataState : StateBase
    {
        public class Item : StateBase
        {
            public int Id;
            public int Number;
            public int TotalNumber;
        }
        
       public List<Item> ItemList =new List<Item>();
    }
}