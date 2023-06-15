using UnityEditor;
using UnityEngine;

namespace VED.Physics
{
    public class PhysicsColliderSquare : PhysicsCollider
    {
        public Vector2 Size = Vector2.one;

        private const float COLLISION_ERROR_MARGIN_UP = 0.1f;
        private const float COLLISION_ERROR_MARGIN_DOWN = 0.01f;
        private const float COLLISION_ERROR_MARGIN_HORIZONTAL = 0.01f;

        // vertices
        // Z configuration [A: top left] [B: top right] [C: bottom left] [D: bottom right]
        public Vector2 A => Position + new Vector2(-Size.x / 2f,  Size.y / 2f);
        public Vector2 B => Position + new Vector2( Size.x / 2f,  Size.y / 2f);
        public Vector2 C => Position + new Vector2(-Size.x / 2f, -Size.y / 2f);
        public Vector2 D => Position + new Vector2( Size.x / 2f, -Size.y / 2f);

        public PhysicsEdge AB => new PhysicsEdge(A, B);
        public PhysicsEdge AC => new PhysicsEdge(A, C);
        public PhysicsEdge BD => new PhysicsEdge(B, D);
        public PhysicsEdge CD => new PhysicsEdge(C, D);

        public override float Left   => Position.x - (Size.x / 2f);
        public override float Right  => Position.x + (Size.x / 2f);
        public override float Top    => Position.y + (Size.y / 2f);
        public override float Bottom => Position.y - (Size.y / 2f);

        public PhysicsColliderSquare Init(Vector2 Centre, Vector2 Size)
        {
            this.Centre = Centre;
            this.Size = Size;
            return this;
        }

        public override bool Interior(Vector2 point)
        {
            float ABAM = Vector2.Dot(B - A, point - A);
            float ABAB = Vector2.Dot(B - A, B - A);
            float BCBM = Vector2.Dot(C - B, point - B);
            float BCBC = Vector2.Dot(C - B, C - B);

            return 0 <= ABAM && ABAM <= ABAB && 0 <= BCBM && BCBM <= BCBC;
        }

        public override bool Colliding(PhysicsCollider other)
        {
            if (other == this) return false;

            if (other is PhysicsColliderSquare square)
            {
                return Left < square.Right && Right > square.Left && Top > square.Bottom && Bottom < square.Top;
            }

            if (Interior(other.Centre)) return true;
            if (AC.Colliding(other)) return true;
            if (BD.Colliding(other)) return true;
            if (AB.Colliding(other)) return true;
            if (CD.Colliding(other)) return true;
            return false;
        }

        public override bool Colliding(PhysicsCollider other, out Vector2 point)
        {
            point = Position;
            if (other == this) return false;

            if (other is PhysicsColliderSquare square)
            {
                Vector2 difference = square.Position - Position;
                Vector2 direction = difference.normalized;

                Vector2 localClosest = Position;
                localClosest += Vector2.right * Mathf.Sign(difference.x) * (Size.x / 2f);
                localClosest += Vector2.up * Mathf.Sign(difference.y) * (Size.y / 2f);

                Vector2 remoteClosest = square.Position;
                remoteClosest += Vector2.right * -Mathf.Sign(difference.x) * (square.Size.x / 2f);
                remoteClosest += Vector2.up * -Mathf.Sign(difference.y) * (square.Size.y / 2f);

                point = Vector2.Lerp(localClosest, remoteClosest, 0.5f);

                return Left < square.Right && Right > square.Left && Top > square.Bottom && Bottom < square.Top;
            }

            if (Interior(other.Centre)) return true;
            if (AC.Colliding(other, out point)) return true;
            if (BD.Colliding(other, out point)) return true;
            if (AB.Colliding(other, out point)) return true;
            if (CD.Colliding(other, out point)) return true;
            return false;
        }

        public override bool CollidingHorizontally(float sign, PhysicsCollider other)
        {
            if (other == this) return false;

            PhysicsEdge edge = sign > 0 ? BD : AC;

            if (other is PhysicsColliderSquare square)
            {
                // AABB collision detection
                return edge.Left   < square.Right
                    && edge.Right  > square.Left 
                    && edge.Top    > square.Bottom + COLLISION_ERROR_MARGIN_HORIZONTAL
                    && edge.Bottom < square.Top    - COLLISION_ERROR_MARGIN_HORIZONTAL;
            }

            return edge.Colliding(other);
        }

        public override bool CollidingVertically(float sign, PhysicsCollider other)
        {
            if (other == this) return false;

            if (sign > 0)
            {
                if (other is PhysicsColliderSquare square)
                {
                    // AABB collision detection
                    return AB.Left   < square.Right  - COLLISION_ERROR_MARGIN_UP
                        && AB.Right  > square.Left   + COLLISION_ERROR_MARGIN_UP
                        && AB.Top    > square.Bottom
                        && AB.Bottom < square.Top;
                }

                return AB.Colliding(other);
            }
            else
            {
                if (other is PhysicsColliderSquare square)
                {
                    // AABB collision detection
                    return CD.Left   < square.Right  - COLLISION_ERROR_MARGIN_DOWN
                        && CD.Right  > square.Left   + COLLISION_ERROR_MARGIN_DOWN
                        && CD.Top    > square.Bottom
                        && CD.Bottom < square.Top;
                }

                return CD.Colliding(other);
            }
        }

        public override float OverlapHorizontally(float sign, PhysicsCollider other)
        {
            if (other == this) return 0;

            if (other is PhysicsColliderCircle circle)
            {
                float vSign = Mathf.Sign((other.Position - Position).y);
                if ((vSign > 0 && Top > circle.Position.y) || (vSign < 0 && Bottom < circle.Position.y)) return sign > 0 ? circle.Left - Right : circle.Right - Left;

                float perpDistance = Mathf.Min(Mathf.Abs(circle.Position.y - Top), Mathf.Abs(circle.Position.y - Bottom));
                float chord = Mathf.Sqrt((circle.Radius * circle.Radius) - (perpDistance * perpDistance));

                return sign > 0 ? (circle.Position.x - chord) - Right : (circle.Position.x + chord) - Left;
            }

            if (other is PhysicsColliderSquare square)
            {
                return (sign > 0) ? Right - square.Left : Left - square.Right;
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                return triangle.OverlapHorizontally(-sign, this);
            }

            return 0;
        }

        public override float OverlapVertically(float sign, PhysicsCollider other)
        {
            if (other == this) return 0;

            if (other is PhysicsColliderCircle circle)
            {
                float hSign = Mathf.Sign((other.Position - Position).x);
                if ((hSign > 0 && Right > circle.Position.x) || (hSign < 0 && Left < circle.Position.x)) return sign > 0 ? circle.Bottom - Top : circle.Top - Bottom;

                float perpDistance = Mathf.Min(Mathf.Abs(circle.Position.x - Left), Mathf.Abs(circle.Position.x - Right));
                float chord = Mathf.Sqrt((circle.Radius * circle.Radius) - (perpDistance * perpDistance));

                return sign > 0 ? (circle.Position.y - chord) - Top : (circle.Position.y + chord) - Bottom;
            }

            if (other is PhysicsColliderSquare square)
            {
                return (sign > 0) ? Top - square.Bottom : Bottom - square.Top;
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                return triangle.OverlapVertically(-sign, this);
            }

            return 0;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PhysicsColliderSquare))]
    public class PhysicsColliderSquareEditor : Editor
    {
        private float _vDirection = -1;
        private float _hDirection = -1;

        private const float SCALE = 0.1f;
        private const float THICKNESS = 2f;

        public void OnSceneGUI()
        {
            // cast
            PhysicsColliderSquare s = target as PhysicsColliderSquare;

            // draw
            Handles.color = Color.cyan;
            Handles.DrawLine(s.A, s.B, THICKNESS);
            Handles.DrawLine(s.B, s.D, THICKNESS);
            Handles.DrawLine(s.C, s.D, THICKNESS);
            Handles.DrawLine(s.A, s.C, THICKNESS);

            // update size
            EditorGUI.BeginChangeCheck();
            Vector2 h = Handles.FreeMoveHandle(s.Position + (Vector2.right * (_hDirection * (s.Size.x / 2f))), Quaternion.identity, SCALE, Vector2.zero, Handles.CubeHandleCap);
            Vector2 v = Handles.FreeMoveHandle(s.Position + (Vector2.up * (_vDirection * (s.Size.y / 2f))), Quaternion.identity, SCALE, Vector2.zero, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(s, "Changed Size");

                s.Size = new Vector2((s.Position - h).magnitude * 2f, (s.Position - v).magnitude * 2f);
                _hDirection = Mathf.Sign((h - s.Position).x);
                _vDirection = Mathf.Sign((v - s.Position).y);
            }

            // update centre
            EditorGUI.BeginChangeCheck();
            Vector2 centrePosition = Handles.FreeMoveHandle(s.Position, Quaternion.identity, SCALE, Vector2.zero, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(s, "Changed Centre");

                s.Centre = centrePosition - (Vector2)s.transform.position;
            }
        }
    }
#endif
}