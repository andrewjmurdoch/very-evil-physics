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

            public string PhysicsMaterial => _physicsMaterialType;
            private string _physicsMaterialType = "DEFAULT";

            public PhysicsTile Init(Sprite sprite, Enums.PhysicsColliderType physicsColliderType, string physicsMaterialType)
            {
                _sprite = sprite;
                _physicsColliderType = physicsColliderType;
                _physicsMaterialType = physicsMaterialType;

                return this;
            }
        }
    }
}
