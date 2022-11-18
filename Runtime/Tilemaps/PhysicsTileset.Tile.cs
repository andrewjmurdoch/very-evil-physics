using UnityEngine;
using VED.Tilemaps;

namespace VED.Physics
{
    public partial class PhysicsTileset : Tileset
    {
        public class PhysicsTile : Tileset.Tile
        {
            public Enums.PhysicsColliderType PhysicsCollider => _physicsColliderType;
            private Enums.PhysicsColliderType _physicsColliderType = Enums.PhysicsColliderType.NONE;

            public Enums.PhysicsMaterialType PhysicsMaterial => _physicsMaterialType;
            private Enums.PhysicsMaterialType _physicsMaterialType = Enums.PhysicsMaterialType.DEFAULT;

            public PhysicsTile Init(Sprite sprite, Enums.PhysicsColliderType physicsColliderType, Enums.PhysicsMaterialType physicsMaterialType)
            {
                _sprite = sprite;
                _physicsColliderType = physicsColliderType;
                _physicsMaterialType = physicsMaterialType;

                return this;
            }
        }
    }
}
