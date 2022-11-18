using System;
using System.Collections.Generic;
using UnityEngine;
using VED.Tilemaps;

namespace VED.Physics
{
    public partial class PhysicsTilelevel : Tilelevel
    {
        [Serializable]
        public class Cell
        {
            public static int Size = 3;

            [SerializeField] public List<PhysicsTilelayer.PhysicsTile> Tiles = new List<PhysicsTilelayer.PhysicsTile>();
            [SerializeField] public List<PhysicsActor> Actors = new List<PhysicsActor>();
            [SerializeField] public List<PhysicsSolid> Solids = new List<PhysicsSolid>();
            [NonSerialized] public List<Cell> Neighbours = new List<Cell>();
        }

    }
}
