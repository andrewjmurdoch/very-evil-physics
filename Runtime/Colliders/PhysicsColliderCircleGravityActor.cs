using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VED.Physics
{
    public class PhysicsColliderCircleGravityActor : PhysicsColliderCircle
    {
        private float COLLISION_ANGLE_VERTICAL = 60f;
        private float COLLISION_ANGLE_HORIZONTAL = 85f;
        
        public override bool CollidingHorizontally(float sign, PhysicsCollider other)
        {
            if (other == this) return false;
            if (!( Left   < other.Right
                && Right  > other.Left
                && Top    > other.Bottom
                && Bottom < other.Top)) return false;

            if (other is PhysicsColliderCircle circle)
            {
                Vector2 difference = circle.Position - Position;
                float range = Radius + circle.Radius;
                float angle = Mathf.Abs(Vector2.Angle(difference, new Vector2(sign, 0f)));

                return difference.magnitude <= range && angle < COLLISION_ANGLE_HORIZONTAL;
            }

            if (other is PhysicsColliderSquare square)
            {
                PhysicsEdge edge = sign > 0 ? square.AC : square.BD;
                float angle = Vector2.Angle(new Vector2(sign, 0), edge.Nearest(Position) - Position);
                
                return edge.Colliding(this) && angle < COLLISION_ANGLE_HORIZONTAL;
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

                    if (intersections > 1 && Vector2.Angle(direction, (I2 - Position).normalized) < COLLISION_ANGLE_HORIZONTAL) return true;
                 
                    if (Vector2.Angle(direction, (I1 - Position).normalized) < COLLISION_ANGLE_HORIZONTAL) return true;
                }
                return false;
            }

            return false;
        }
        
        public override bool CollidingVertically(float sign, PhysicsCollider other)
        {
            if (other == this) return false;
            if (!( Left   < other.Right
                && Right  > other.Left
                && Top    > other.Bottom
                && Bottom < other.Top)) return false;

            if (other is PhysicsColliderCircle circle)
            {
                Vector2 difference = circle.Position - Position;
                float range = Radius + circle.Radius;
                float angle = Mathf.Abs(Vector2.Angle(difference, new Vector2(0f, sign)));

                if (sign < 0) return difference.magnitude <= range && angle < COLLISION_ANGLE_VERTICAL;
                
                return difference.magnitude <= range && angle < COLLISION_ANGLE_VERTICAL;
            }

            if (other is PhysicsColliderSquare square)
            {
                PhysicsEdge edge = sign > 0 ? square.CD : square.AB;
                float angle = Vector2.Angle(new Vector2(0, sign), edge.Nearest(Position) - Position);

                if (sign < 0) return edge.Colliding(this) && angle < COLLISION_ANGLE_VERTICAL;
                
                return edge.Colliding(this) && angle < COLLISION_ANGLE_VERTICAL;
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

                    if (sign < 0)
                    {
                        if (intersections > 1 && Vector2.Angle(direction, (I2 - Position).normalized) < COLLISION_ANGLE_VERTICAL) return true;
  
                        if (Vector2.Angle(direction, (I1 - Position).normalized) < COLLISION_ANGLE_VERTICAL) return true;
                        
                        return false;
                    }
                    
                    if (intersections > 1 && Vector2.Angle(direction, (I2 - Position).normalized) < COLLISION_ANGLE_VERTICAL) return true;
  
                    if (Vector2.Angle(direction, (I1 - Position).normalized) < COLLISION_ANGLE_VERTICAL) return true;
                }
                return false;
            }

            return false;
        }

    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(PhysicsColliderCircleGravityActor))]
    public class PhysicsColliderCircleGravityActorEditor : Editor
    {
        private Vector2 _direction = Vector2.left;

        private const float SCALE = 0.1f;
        private const float THICKNESS = 2f;

        public void OnSceneGUI()
        {
            // cast
            PhysicsColliderCircleGravityActor c = target as PhysicsColliderCircleGravityActor;

            // draw
            Handles.color = Color.cyan;
            Handles.DrawWireDisc(c.Position, Vector3.back, c.Radius, THICKNESS);

            // update radius
            EditorGUI.BeginChangeCheck();
            Vector2 radiusPosition = Handles.FreeMoveHandle(c.Position + _direction * c.Radius, SCALE, Vector2.zero, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(c, "Changed Radius");

                c.Radius = (radiusPosition - c.Position).magnitude;
                _direction = (radiusPosition - c.Position).normalized;
            }

            // update centre
            EditorGUI.BeginChangeCheck();
            Vector2 centrePosition = Handles.FreeMoveHandle(c.Position, SCALE, Vector2.zero, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(c, "Changed Centre");

                c.Centre = centrePosition - (Vector2)c.transform.position;
            }
        }
    }
#endif
}
