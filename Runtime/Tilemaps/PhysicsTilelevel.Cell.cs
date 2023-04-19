using System;
using System.Collections.Generic;
using UnityEngine;
using VED.Tilemaps;

namespace VED.Physics
{
    public partial class PhysicsTileLevel : TileLevel
    {
        [Serializable]
        public class Cell
        {
            public static int Size = 3;

            [SerializeField] public List<PhysicsTileLayer.PhysicsTile> Tiles = new List<PhysicsTileLayer.PhysicsTile>();
            [SerializeField] public List<PhysicsActor> Actors = new List<PhysicsActor>();
            [SerializeField] public List<PhysicsSolid> Solids = new List<PhysicsSolid>();
            [NonSerialized] public List<Cell> Neighbours = new List<Cell>();
        }

    }
}
