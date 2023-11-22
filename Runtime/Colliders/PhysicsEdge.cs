using UnityEngine;
using VED.Utilities;

namespace VED.Physics
{
    public class PhysicsEdge
    {
        public PhysicsEdge(Vector2 A, Vector2 B)
        {
            this.OA = A;
            this.OB = B;
            this.A = OA + (OB - OA).normalized * PhysicsCollider.COLLISION_ERROR_MARGIN;
            this.B = OB + (OA - OB).normalized * PhysicsCollider.COLLISION_ERROR_MARGIN;
        }

        public Vector2 OA = Vector2.zero;
        public Vector2 OB = Vector2.zero;
        public Vector2 A  = Vector2.zero;
        public Vector2 B  = Vector2.zero;

        public Vector2 Vector => B - A;
        public Vector2 OVector => OB - OA;

        public Optional<float> Gradient => (B.x - A.x) == 0 ? new Optional<float>(0f, false) : new Optional<float>((B.y - A.y) / (B.x - A.x), true);

        // the left/right/top/bottom most of the original points
        public float OLeft   => Mathf.Min(OA.x, OB.x);
        public float ORight  => Mathf.Max(OA.x, OB.x);
        public float OTop    => Mathf.Max(OA.y, OB.y);
        public float OBottom => Mathf.Min(OA.y, OB.y);

        // the left/right/top/bottom most of the points adjusted for collision detection
        public float Left    => Mathf.Min(A.x, B.x);
        public float Right   => Mathf.Max(A.x, B.x);
        public float Top     => Mathf.Max(A.y, B.y);
        public float Bottom  => Mathf.Min(A.y, B.y);

        public Vector2 OLeftPoint   => OA.x < OB.x ? OA : OB;
        public Vector2 ORightPoint  => OA.x > OB.x ? OA : OB;
        public Vector2 OTopPoint    => OA.y > OB.y ? OA : OB;
        public Vector2 OBottomPoint => OA.y < OB.y ? OA : OB;

        public Vector2 LeftPoint    => A.x < B.x ? A : B;
        public Vector2 RightPoint   => A.x > B.x ? A : B;
        public Vector2 TopPoint     => A.y > B.y ? A : B;
        public Vector2 BottomPoint  => A.y < B.y ? A : B;
        public Vector2 MidPoint     => Vector2.Lerp(A, B, 0.5f);

        public bool Is(PhysicsEdge other)
        {
            return other.OA == OA && other.OB == OB;
        }

        public bool Colliding(PhysicsCollider other)
        {
            if (other is PhysicsColliderCircle circle)
            {
                float projection = Mathf.Clamp(Vector2.Dot(circle.Position - A, Vector.normalized), 0f, Vector.magnitude);
                Vector2 closest = A + Vector.normalized * projection;

                return (closest - circle.Position).magnitude <= circle.Radius + PhysicsCollider.COLLISION_ERROR_MARGIN;
            }

            if (other is PhysicsColliderSquare square)
            {
                if (Colliding(square.AC)) return true;
                if (Colliding(square.BD)) return true;
                if (Colliding(square.AB)) return true;
                if (Colliding(square.CD)) return true;
                return false;
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                if (Colliding(triangle.AB)) return true;
                if (Colliding(triangle.BC)) return true;
                if (Colliding(triangle.CA)) return true;
                return false;
            }

            return false;
        }

        public bool Colliding(PhysicsCollider other, out Vector2 point)
        {
            point = MidPoint;

            if (other is PhysicsColliderCircle circle)
            {
                float projection = Mathf.Clamp(Vector2.Dot(circle.Position - A, Vector.normalized), 0f, Vector.magnitude);
                Vector2 closest = A + Vector.normalized * projection;
                point = closest;

                return (closest - circle.Position).magnitude <= circle.Radius + PhysicsCollider.COLLISION_ERROR_MARGIN;
            }

            if (other is PhysicsColliderSquare square)
            {
                if (Colliding(square.AC, out point)) return true;
                if (Colliding(square.BD, out point)) return true;
                if (Colliding(square.AB, out point)) return true;
                if (Colliding(square.CD, out point)) return true;
                return false;
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                if (Colliding(triangle.AB, out point)) return true;
                if (Colliding(triangle.BC, out point)) return true;
                if (Colliding(triangle.CA, out point)) return true;
                return false;
            }

            return false;
        }

        public bool Colliding(PhysicsEdge other)
        {
            return Intersection(other, out Vector2 point);
        }

        public bool Colliding(PhysicsEdge other, out Vector2 point)
        {
            return Intersection(other, out point);
        }

        public Vector2 Nearest(Vector2 point)
        {
            float projection = Mathf.Clamp(Vector2.Dot(point - A, Vector.normalized), 0f, Vector.magnitude);
            return A + Vector.normalized * projection;
        }

        public bool Intersection(PhysicsEdge other, out Vector2 intersection)
        {
            intersection = new Vector2(float.NaN, float.NaN);

            float X1 = B.x - A.x;
            float Y1 = B.y - A.y;
            float X2 = other.B.x - other.A.x;
            float Y2 = other.B.y - other.A.y;

            float D = (Y1 * X2 - X1 * Y2);

            if (D == 0)
            {
                if ((Nearest(other.A) - other.A).magnitude < PhysicsCollider.COLLISION_ERROR_MARGIN) return true;
                if ((Nearest(other.B) - other.B).magnitude < PhysicsCollider.COLLISION_ERROR_MARGIN) return true;
                if ((other.Nearest(A) - A).magnitude < PhysicsCollider.COLLISION_ERROR_MARGIN) return true;
                if ((other.Nearest(B) - B).magnitude < PhysicsCollider.COLLISION_ERROR_MARGIN) return true;
                return false;
            }

            float S = ((A.x - other.A.x) * Y2 + (other.A.y - A.y) * X2) / D;
            float T = ((other.A.x - A.x) * Y1 + (A.y - other.A.y) * X1) / -D;

            if ((S >= 0) && (S <= 1) && (T >= 0) && (T <= 1))
            {
                intersection = new Vector2(A.x + X1 * S, A.y + Y1 * S);
                return true;
            }
            return false;
        }

        public int Intersection(PhysicsColliderCircle circle, out Vector2 intersectionOne, out Vector2 intersectionTwo)
        {
            intersectionOne = Vector2.zero;
            intersectionTwo = Vector2.zero;
            float T;
            float U = Vector.x * Vector.x + Vector.y * Vector.y;
            float V = 2f * (Vector.x * (A.x - circle.Position.x) + Vector.y * (A.y - circle.Position.y));
            float W = (A.x - circle.Position.x) * (A.x - circle.Position.x) + (A.y - circle.Position.y) * (A.y - circle.Position.y) - circle.Radius * circle.Radius;
            float D = V * V - 4f * U * W;

            if ((U <= 0.0000001) || (D < 0f))
            {
                // no real solutions
                intersectionOne = new Vector2(float.NaN, float.NaN);
                intersectionTwo = new Vector2(float.NaN, float.NaN);
                return 0;
            }

            if (D == 0f)
            {
                // one solution
                T = -V / (2f * U);
                intersectionOne = new Vector2(A.x + T * Vector.x, A.y + T * Vector.y);
                intersectionTwo = new Vector2(float.NaN, float.NaN);
                return 1;
            }

            int count = 0;

            // two solutions
            T = (float)((-V + Mathf.Sqrt(D)) / (2f * U));
            if (T > 0f && T < 1f)
            {
                count++;
                intersectionOne = new Vector2(A.x + T * Vector.x, A.y + T * Vector.y);
            }

            T = (float)((-V - Mathf.Sqrt(D)) / (2f * U));
            if (T > 0f && T < 1f)
            {
                count++;
                if (count > 1)
                {
                    intersectionTwo = new Vector2(A.x + T * Vector.x, A.y + T * Vector.y);
                }
                else
                {
                    intersectionOne = new Vector2(A.x + T * Vector.x, A.y + T * Vector.y);
                }
            }

            return count;
        }

        public PhysicsEdge Inverse()
        {
            return new PhysicsEdge(OB, OA);
        }
    }
}