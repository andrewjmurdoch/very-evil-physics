using UnityEngine;
using VED.Tilemaps;

namespace VED.Physics
{
    public partial class PhysicsTilelayer : Tilelayer
    {
        public class PhysicsTile : PhysicsSolid
        {
            private SpriteRenderer _spriteRenderer = null;

            public PhysicsTile Init(PhysicsTileset.PhysicsTile definition, int sortingOrder)
            {
                if (_spriteRenderer == null) _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                _spriteRenderer.sprite = definition.Sprite;
                _spriteRenderer.sortingOrder = sortingOrder;

                Colliders.Add(InitCollider(definition.PhysicsCollider));

                _physicsMaterial = PhysicsManager.Instance.PhysicsMaterialMapper[definition.PhysicsMaterial];

                return this;
            }

            private PhysicsCollider InitCollider(Enums.PhysicsColliderType physicsColliderType)
            {
                if (physicsColliderType == Enums.PhysicsColliderType.NONE) return null;

                #region Square
                if (physicsColliderType == Enums.PhysicsColliderType.SQUARE)
                {
                    PhysicsColliderSquare square = gameObject.AddComponent<PhysicsColliderSquare>();
                    square.Init(Vector2.zero, new Vector2(1f, 1f));
                    return square;
                }
                #endregion

                #region Circle
                PhysicsColliderCircle GenerateCircleCollider(float x, float y)
                {
                    PhysicsColliderCircle circle = gameObject.AddComponent<PhysicsColliderCircle>();
                    circle.Init(new Vector2(x, y) * (1f / 2f), 1f);
                    return circle;
                }

                switch (physicsColliderType)
                {
                    case Enums.PhysicsColliderType.CIRCLE_BL: return GenerateCircleCollider(Vector2.left.x, Vector2.down.y);
                    case Enums.PhysicsColliderType.CIRCLE_BR: return GenerateCircleCollider(Vector2.right.x, Vector2.down.y);
                    case Enums.PhysicsColliderType.CIRCLE_TL: return GenerateCircleCollider(Vector2.left.x, Vector2.up.y);
                    case Enums.PhysicsColliderType.CIRCLE_TR: return GenerateCircleCollider(Vector2.right.x, Vector2.up.y);
                    default: break;
                }
                #endregion

                #region Triangle
                PhysicsColliderTriangle GenerateTriangleCollider(Vector2 a, Vector2 b, Vector2 c)
                {
                    PhysicsColliderTriangle triangle = gameObject.AddComponent<PhysicsColliderTriangle>();
                    triangle.Init(a * (1f / 2f), b * (1f / 2f), c * (1f / 2f));
                    return triangle;
                }

                switch (physicsColliderType)
                {
                    case Enums.PhysicsColliderType.TRIANGLE_BL:
                    {
                        Vector2 a = Vector2.left  + Vector2.up;
                        Vector2 b = Vector2.left  + Vector2.down;
                        Vector2 c = Vector2.right + Vector2.down;
                        return GenerateTriangleCollider(a, b, c);
                    }
                    case Enums.PhysicsColliderType.TRIANGLE_BR:
                    {
                        Vector2 a = Vector2.right + Vector2.up;
                        Vector2 b = Vector2.right + Vector2.down;
                        Vector2 c = Vector2.left  + Vector2.down;
                        return GenerateTriangleCollider(a, b, c);
                    }
                    case Enums.PhysicsColliderType.TRIANGLE_TL:
                    {
                        Vector2 a = Vector2.left  + Vector2.down;
                        Vector2 b = Vector2.left  + Vector2.up;
                        Vector2 c = Vector2.right + Vector2.up;
                        return GenerateTriangleCollider(a, b, c);
                    }
                    case Enums.PhysicsColliderType.TRIANGLE_TR:
                    {
                        Vector2 a = Vector2.right + Vector2.down;
                        Vector2 b = Vector2.right + Vector2.up;
                        Vector2 c = Vector2.left  + Vector2.up;
                        return GenerateTriangleCollider(a, b, c);
                    }
                    default: break;
                }
                #endregion

                return null;
            }
        }
    }
}
