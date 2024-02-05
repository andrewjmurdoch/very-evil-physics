using System.Collections.Generic;
using UnityEngine;
using VED.Tilemaps;

namespace VED.Physics
{
    public partial class PhysicsTileset : Tileset
    {
        public class PhysicsTile : Tileset.Tile
        {
            public List<Enums.PhysicsColliderType> PhysicsColliderTypes => _physicsColliderTypes;
            private List<Enums.PhysicsColliderType> _physicsColliderTypes = new List<Enums.PhysicsColliderType>() { Enums.PhysicsColliderType.NONE };

            public string PhysicsMaterial => _physicsMaterialType;
            private string _physicsMaterialType = Consts.DEFAULT_MATERIAL_ID;

            public PhysicsTile Init(Sprite sprite, List<Enums.PhysicsColliderType> physicsColliderTypes, string physicsMaterialType)
            {
                _sprite = sprite;
                _physicsColliderTypes = physicsColliderTypes;
                _physicsMaterialType = physicsMaterialType;

                return this;
            }
        }
    }
}
