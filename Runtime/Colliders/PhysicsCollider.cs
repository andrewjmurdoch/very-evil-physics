using UnityEngine;

namespace VED.Physics
{
    public abstract class PhysicsCollider : MonoBehaviour
    {
        public const float COLLISION_ERROR_MARGIN = 0.035f;

        protected Transform Transform
        {
            get
            {
                if (_transform == null) _transform = transform;
                return _transform;
            }
        }
        private Transform _transform = null;
        
        // the collider's local position
        public Vector2 Centre = Vector2.zero;

        // the collider's world position
        public Vector2 Position => (Vector2)Transform.position + Centre;

        public abstract float Left { get; }
        public abstract float Right { get; }
        public abstract float Top { get; }
        public abstract float Bottom { get; }
        public abstract bool Interior(Vector2 point);
        public abstract bool Colliding(PhysicsCollider other);
        public abstract bool CollidingHorizontally(float sign, PhysicsCollider other);
        public abstract bool CollidingVertically(float sign, PhysicsCollider other);
        public abstract float OverlapHorizontally(float sign, PhysicsCollider other);
        public abstract float OverlapVertically(float sign, PhysicsCollider other);
    }
}