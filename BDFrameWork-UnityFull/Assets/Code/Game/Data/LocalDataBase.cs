using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SQLite4Unity3d;

namespace Game.Data
{
    abstract public class LocalDataBase
    {
        [PrimaryKey]
        public int Id { get; set; }
    }
}

