using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VED.Physics
{
    public class PhysicsColliderCircle : PhysicsCollider
    {
        private float COLLISION_ANGLE = 90f;
        
        public float Radius = 0.5f;
        public override float Left => Position.x - Radius;
        public override float Right => Position.x + Radius;
        public override float Top => Position.y + Radius;
        public override float Bottom => Position.y - Radius;

        public PhysicsColliderCircle Init(Vector2 Centre, float Radius)
        {
            this.Centre = Centre;
            this.Radius = Radius;
            return this;
        }

        public override bool Interior(Vector2 point)
        {
            return (point - Position).magnitude <= Radius;
        }

        public override bool Colliding(PhysicsCollider other)
        {
            if (other == this) return false;

            if (other is PhysicsColliderCircle circle)
            {
                return (circle.Position - Position).magnitude <= (Radius + circle.Radius);
            }

            if (other is PhysicsColliderSquare square)
            {
                if (square.Interior(Position)) return true;
                if (square.AC.Colliding(this)) return true;
                if (square.BD.Colliding(this)) return true;
                if (square.AB.Colliding(this)) return true;
                if (square.CD.Colliding(this)) return true;
                return false;
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                if (triangle.Interior(Position)) return true;
                if (triangle.AB.Colliding(this)) return true;
                if (triangle.BC.Colliding(this)) return true;
                if (triangle.CA.Colliding(this)) return true;
                return false;
            }

            return false;
        }

        public override bool Colliding(PhysicsCollider other, out Vector2 point)
        {
            point = Position;
            if (other == this) return false;

            if (other is PhysicsColliderCircle circle)
            {
                Vector2 difference = circle.Position - Position;
                Vector2 direction = difference.normalized;
                Vector2 localClosest = Position + (Radius * direction);
                Vector2 remoteClosest = circle.Position - (circle.Radius * direction);
                point = Vector2.Lerp(localClosest, remoteClosest, 0.5f);

                return difference.magnitude <= (Radius + circle.Radius);
            }

            if (other is PhysicsColliderSquare square)
            {
                if (square.Interior(Position)) return true;
                if (square.AC.Colliding(this, out point)) return true;
                if (square.BD.Colliding(this, out point)) return true;
                if (square.AB.Colliding(this, out point)) return true;
                if (square.CD.Colliding(this, out point)) return true;
                return false;
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                if (triangle.Interior(Position)) return true;
                if (triangle.AB.Colliding(this, out point)) return true;
                if (triangle.BC.Colliding(this, out point)) return true;
                if (triangle.CA.Colliding(this, out point)) return true;
                return false;
            }

            return false;
        }

        public override bool CollidingHorizontally(float sign, PhysicsCollider other)
        {
            if (other == this) return false;

            if (other is PhysicsColliderCircle circle)
            {
                Vector2 difference = circle.Position - Position;
                float range = Radius + circle.Radius;
                float angle = Mathf.Abs(Vector2.Angle(difference, new Vector2(sign, 0f)));

                return difference.magnitude <= range && angle < COLLISION_ANGLE;
            }

            if (other is PhysicsColliderSquare square)
            {
                PhysicsEdge edge = sign > 0 ? square.AC : square.BD;
                float angle = Vector2.Angle(new Vector2(sign, 0), edge.Nearest(Position) - Position);
                
                return edge.Colliding(this) && angle < COLLISION_ANGLE;
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                Vector2 direction = Vector2.zero;
                List<PhysicsEdge> edges = null;

                if (sign > 0)
                {
                    direction = Vector2.right;
                    edges = triangle.LeftEdges;
                }
                else
                {
                    direction = Vector2.left;
                    edges = triangle.RightEdges;
                }

                foreach (PhysicsEdge edge in edges)
                {
                    int intersections = edge.Intersection(this, out Vector2 I1, out Vector2 I2);

                    if (intersections <= 0) continue;

                    if (intersections > 1 && Vector2.Angle(direction, (I2 - Position).normalized) < COLLISION_ANGLE) return true;
                 
                    if (Vector2.Angle(direction, (I1 - Position).normalized) < COLLISION_ANGLE) return true;
                }
                return false;
            }

            return false;
        }

        public override bool CollidingVertically(float sign, PhysicsCollider other)
        {
            if (other == this) return false;

            if (other is PhysicsColliderCircle circle)
            {
                Vector2 difference = circle.Position - Position;
                float range = Radius + circle.Radius;
                float angle = Mathf.Abs(Vector2.Angle(difference, new Vector2(0f, sign)));

                return difference.magnitude <= range && angle < COLLISION_ANGLE;
            }

            if (other is PhysicsColliderSquare square)
            {
                PhysicsEdge edge = sign > 0 ? square.CD : square.AB;
                float angle = Vector2.Angle(new Vector2(0, sign), edge.Nearest(Position) - Position);

                return edge.Colliding(this) && angle < COLLISION_ANGLE;
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                Vector2 direction = Vector2.zero;
                List<PhysicsEdge> edges = null;

                if (sign > 0)
                {
                    direction = Vector2.up;
                    edges = triangle.BottomEdges;
                }
                else
                {
                    direction = Vector2.down;
                    edges = triangle.TopEdges;
                }

                foreach (PhysicsEdge edge in edges)
                {
                    int intersections = edge.Intersection(this, out Vector2 I1, out Vector2 I2);

                    if (intersections <= 0) continue;

                    if (intersections > 1 && Vector2.Angle(direction, (I2 - Position).normalized) < COLLISION_ANGLE) return true;
  
                    if (Vector2.Angle(direction, (I1 - Position).normalized) < COLLISION_ANGLE) return true;
                }
                return false;
            }

            return false;
        }

        public override float OverlapHorizontally(float sign, PhysicsCollider other)
        {
            if (other == this) return 0;

            if (other is PhysicsColliderCircle circle)
            {
                Vector2 difference = (circle.Position - Position);

                if (difference.magnitude > Radius + circle.Radius) return 0;

                Vector2 direction = difference.normalized;
                Vector2 point = Position + direction * Radius;

                float perpDistance = Mathf.Abs(point.y - circle.Position.y);
                float chord = Mathf.Sqrt((circle.Radius * circle.Radius) - (perpDistance * perpDistance));
                float D = sign > 0 ? circle.Position.x - chord : circle.Position.x + chord;

                return D - point.x;
            }

            if (other is PhysicsColliderSquare square)
            {
                float vSign = Mathf.Sign((other.Position - Position).y);
                if ((vSign < 0 && square.Top > Position.y) || (vSign > 0 && square.Bottom < Position.y)) return sign > 0 ? square.Left - Right : square.Right - Left;

                float perpDistance = Mathf.Min(Mathf.Abs(Position.y - square.Top), Mathf.Abs(Position.y - square.Bottom));
                float chord = Mathf.Sqrt((Radius * Radius) - (perpDistance * perpDistance));

                return sign > 0 ? square.Left - (Position.x + chord) : square.Right - (Position.x - chord);
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                Vector2 point = sign > 0 ? triangle.LeftPoint : triangle.RightPoint;

                if (Interior(point))
                {
                    float perpDistance = Mathf.Abs(Position.y - point.y);
                    float chord = Mathf.Sqrt((Radius * Radius) - (perpDistance * perpDistance));

                    return sign > 0 ? point.x - (Position.x + chord) : point.x - (Position.x - chord);
                }

                float Overlap(PhysicsEdge edge)
                {
                    Vector2 nearest = edge.Nearest(Position);
                    float scalar = triangle.Interior(Position) ? -1 : 1;
                    Vector2 projection = Position + (nearest - Position).normalized * Radius * scalar;
                    Vector2 difference = projection - nearest;
                    Vector2 direction = (point - nearest).normalized;
                    float amount = 1f + (1f - Mathf.Abs(direction.y));
                    Vector2 reprojection = nearest + direction * Mathf.Abs(difference.y) * amount;

                    return (reprojection - projection).x;
                }

                List<PhysicsEdge> edges = sign > 0 ? triangle.LeftEdges : triangle.RightEdges;
                foreach (PhysicsEdge edge in edges)
                {
                    if (edge.Intersection(this, out Vector2 I1, out Vector2 I2) > 0)
                    {
                        return Overlap(edge);
                    }
                }

                return 0;
            }

            return 0;
        }

        public override float OverlapVertically(float sign, PhysicsCollider other)
        {
            if (other == this) return 0;

            if (other is PhysicsColliderCircle circle)
            {
                Vector2 difference = (circle.Position - Position);

                if (difference.magnitude > Radius + circle.Radius) return 0;

                Vector2 direction = difference.normalized;
                Vector2 point = Position + direction * Radius;

                float perpDistance = Mathf.Abs(point.x - circle.Position.x);
                float chord = Mathf.Sqrt((circle.Radius * circle.Radius) - (perpDistance * perpDistance));
                float D = sign > 0 ? circle.Position.y - chord : circle.Position.y + chord;

                return D - point.y;
            }

            if (other is PhysicsColliderSquare square)
            {
                float hSign = Mathf.Sign((other.Position - Position).x);
                if ((hSign < 0 && square.Right > Position.x) || (hSign > 0 && square.Left < Position.x)) return sign > 0 ? square.Bottom - Top : square.Top - Bottom;

                float perpDistance = Mathf.Min(Mathf.Abs(Position.x - square.Left), Mathf.Abs(Position.x - square.Right));
                float chord = Mathf.Sqrt((Radius * Radius) - (perpDistance * perpDistance));

                return sign > 0 ? square.Bottom - (Position.y + chord) : square.Top - (Position.y - chord);
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                Vector2 point = sign > 0 ? triangle.BottomPoint : triangle.TopPoint;

                if (Interior(point))
                {
                    float perpDistance = Mathf.Abs(Position.x - point.x);
                    float chord = Mathf.Sqrt((Radius * Radius) - (perpDistance * perpDistance));

                    return sign > 0 ? point.y - (Position.y + chord) : point.y - (Position.y - chord);
                }


                float Overlap(PhysicsEdge edge)
                {
                    Vector2 nearest = edge.Nearest(Position);
                    float scalar = triangle.Interior(Position) ? -1 : 1;
                    Vector2 projection = Position + (nearest - Position).normalized * Radius * scalar;
                    Vector2 difference = projection - nearest;
                    Vector2 direction = (point - nearest).normalized;
                    float amount = 1f + (1f - Mathf.Abs(direction.y));
                    Vector2 reprojection = nearest + direction * Mathf.Abs(difference.x) * amount;

                    return (reprojection - projection).y;
                }

                List<PhysicsEdge> edges = sign > 0 ? triangle.BottomEdges : triangle.TopEdges;
                foreach (PhysicsEdge edge in edges)
                {
                    if (edge.Intersection(this, out Vector2 I1, out Vector2 I2) > 0)
                    {
                        return Overlap(edge);
                    }
                }

                return 0;
            }

            return 0;
        }

        public int Intersection(PhysicsColliderCircle other, out Vector2 intersectionOne, out Vector2 intersectionTwo)
        {
            Vector2 difference = Position - other.Position;
            float distance = Mathf.Sqrt(difference.x * difference.x + difference.y * difference.y);

            if (distance > Radius + other.Radius)
            {
                // no solutions
                intersectionOne = new Vector2(float.NaN, float.NaN);
                intersectionTwo = new Vector2(float.NaN, float.NaN);
                return 0;
            }
            else if (distance < Mathf.Abs(Radius - other.Radius))
            {
                // no solutions, one circle contains the other
                intersectionOne = new Vector2(float.NaN, float.NaN);
                intersectionTwo = new Vector2(float.NaN, float.NaN);
                return 0;
            }
            else if ((distance == 0) && (Radius == other.Radius))
            {
                // no solutions, the circles coincide
                intersectionOne = new Vector2(float.NaN, float.NaN);
                intersectionTwo = new Vector2(float.NaN, float.NaN);
                return 0;
            }
            else
            {
                // find A, H, C
                float A = (Radius * Radius - other.Radius * other.Radius + distance * distance) / (2f * distance);
                float H = Mathf.Sqrt(Radius * Radius - A * A);
                Vector2 C = Position + A * (other.Position - Position) / distance;

                intersectionOne = new Vector2(
                    (float)(C.x + H * (other.Position.y - Position.y) / distance),
                    (float)(C.y - H * (other.Position.x - Position.x) / distance));

                intersectionTwo = new Vector2(
                    (float)(C.x - H * (other.Position.y - Position.y) / distance),
                    (float)(C.y + H * (other.Position.x - Position.x) / distance));

                // see if we have 1 or 2 solutions.
                if (distance == Radius + other.Radius) return 1;
                return 2;
            }
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PhysicsColliderCircle))]
    public class PhysicsColliderCircleEditor : Editor
    {
        private Vector2 _direction = Vector2.left;

        private const float SCALE = 0.1f;
        private const float THICKNESS = 2f;

        public void OnSceneGUI()
        {
            // cast
            PhysicsColliderCircle c = target as PhysicsColliderCircle;

            // draw
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(c.Position, Vector3.back, c.Radius, THICKNESS);

            // update radius
            EditorGUI.BeginChangeCheck();
            Vector2 radiusPosition = Handles.FreeMoveHandle(c.Position + _direction * c.Radius, Quaternion.identity, SCALE, Vector2.zero, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(c, "Changed Radius");

                c.Radius = (radiusPosition - c.Position).magnitude;
                _direction = (radiusPosition - c.Position).normalized;
            }

            // update centre
            EditorGUI.BeginChangeCheck();
            Vector2 centrePosition = Handles.FreeMoveHandle(c.Position, Quaternion.identity, SCALE, Vector2.zero, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(c, "Changed Centre");

                c.Centre = centrePosition - (Vector2)c.transform.position;
            }
        }
    }
#endif
}