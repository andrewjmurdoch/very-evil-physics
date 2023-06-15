using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VED.Physics
{
    public class PhysicsColliderTriangle : PhysicsCollider
    {
        [HideInInspector] public Vector2 mA = new Vector2( 0.00f,  0.50f);
        [HideInInspector] public Vector2 mB = new Vector2(-0.40f, -0.30f);
        [HideInInspector] public Vector2 mC = new Vector2( 0.40f, -0.30f);

        public Vector2 A => Position + mA;
        public Vector2 B => Position + mB;
        public Vector2 C => Position + mC;

        public Vector2[] Points => new Vector2[] { A, B, C };

        public PhysicsEdge AB => new PhysicsEdge(A, B);
        public PhysicsEdge BC => new PhysicsEdge(B, C);
        public PhysicsEdge CA => new PhysicsEdge(C, A);

        public PhysicsEdge OA => new PhysicsEdge(Centre, A);
        public PhysicsEdge OB => new PhysicsEdge(Centre, B);
        public PhysicsEdge OC => new PhysicsEdge(Centre, C);

        public PhysicsEdge[] Edges => new PhysicsEdge[] { AB, BC, CA };

        public PhysicsEdge EdgeWithA(Vector2 A)
        {
            if (AB.OA == A) return AB;
            if (BC.OA == A) return BC;
            if (CA.OA == A) return CA;
            return new PhysicsEdge(Vector2.zero, Vector2.zero);
        }

        public PhysicsEdge EdgeWithB(Vector2 B)
        {
            if (AB.OB == B) return AB;
            if (BC.OB == B) return BC;
            if (CA.OB == B) return CA;
            return new PhysicsEdge(Vector2.zero, Vector2.zero);
        }

        public override float Left => Mathf.Min(A.x, Mathf.Min(B.x, C.x));
        public override float Right => Mathf.Max(A.x, Mathf.Max(B.x, C.x));
        public override float Top => Mathf.Max(A.y, Mathf.Max(B.y, C.y));
        public override float Bottom => Mathf.Min(A.y, Mathf.Min(B.y, C.y));

        public Vector2 LeftPoint
        {
            get
            {
                Vector2 min = Vector2.right * Mathf.Infinity;
                for (int i = 0; i < 3; i++)
                {
                    min = Edges[i].OLeft < min.x ? Edges[i].OLeftPoint : min;
                }

                return min;
            }
        }

        public Vector2 RightPoint
        {
            get
            {
                Vector2 max = Vector2.right * Mathf.NegativeInfinity;
                for (int i = 0; i < 3; i++)
                {
                    max = Edges[i].ORight > max.x ? Edges[i].ORightPoint : max;
                }

                return max;
            }
        }

        public Vector2 TopPoint
        {
            get
            {
                Vector2 max = Vector2.up * Mathf.NegativeInfinity;
                for (int i = 0; i < 3; i++)
                {
                    max = Edges[i].OTop > max.y ? Edges[i].OTopPoint : max;
                }

                return max;
            }
        }

        public Vector2 BottomPoint
        {
            get
            {
                Vector2 min = Vector2.up * Mathf.Infinity;
                for (int i = 0; i < 3; i++)
                {
                    min = Edges[i].OBottom < min.y ? Edges[i].OBottomPoint : min;
                }

                return min;
            }
        }

        public List<PhysicsEdge> RightEdges  => GetEdges(Vector2.right);
        public List<PhysicsEdge> LeftEdges   => GetEdges(Vector2.left);
        public List<PhysicsEdge> TopEdges    => GetEdges(Vector2.up);
        public List<PhysicsEdge> BottomEdges => GetEdges(Vector2.down);

        public PhysicsColliderTriangle Init(Vector2 A, Vector2 B, Vector2 C)
        {
            Centre = ((A + B + C) / 3f);
            mA = A - Centre;
            mB = B - Centre;
            mC = C - Centre;
            return this;
        }

        public override bool Interior(Vector2 point)
        {
            float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

            float XD = Cross(A, B) + Cross(B, C) + Cross(C, A);

            if (Mathf.Abs(XD) > 0f)
            {
                float XA = Cross(B, C) + Cross(point, B - C);
                float XB = Cross(C, A) + Cross(point, C - A);
                float XC = Cross(A, B) + Cross(point, A - B);

                float WA = XA / XD;
                float WB = XB / XD;
                float WC = XC / XD;

                return (WA > 0 && WA < 1) && (WB > 0 && WB < 1) && (WC > 0 && WC < 1);
            }

            return false;
        }

        public override bool Colliding(PhysicsCollider other)
        {
            if (other == this) return false;

            if (Interior(other.Centre)) return true;
            if (AB.Colliding(other)) return true;
            if (BC.Colliding(other)) return true;
            if (CA.Colliding(other)) return true;
            return false;
        }

        public override bool Colliding(PhysicsCollider other, out Vector2 point)
        {
            point = Position;
            if (other == this) return false;

            if (Interior(other.Centre)) return true;
            if (AB.Colliding(other, out point)) return true;
            if (BC.Colliding(other, out point)) return true;
            if (CA.Colliding(other, out point)) return true;
            return false;
        }

        public override bool CollidingHorizontally(float sign, PhysicsCollider other)
        {
            if (other == this) return false;

            List<PhysicsEdge> edges = sign > 0 ? RightEdges : LeftEdges;
            foreach (PhysicsEdge edge in edges)
            {
                if (edge.Colliding(other)) return true;
            }
            return false;
        }

        public override bool CollidingVertically(float sign, PhysicsCollider other)
        {
            if (other == this) return false;

            List<PhysicsEdge> edges = sign > 0 ? TopEdges : BottomEdges;
            foreach (PhysicsEdge edge in edges)
            {
                if (edge.Colliding(other)) return true;
            }
            return false;
        }

        public override float OverlapHorizontally(float sign, PhysicsCollider other)
        {
            if (other == this) return 0;

            if (other is PhysicsColliderCircle circle)
            {
                return circle.OverlapHorizontally(-sign, this);
            }

            if (other is PhysicsColliderSquare square)
            {
                Vector2 point = sign > 0 ? RightPoint : LeftPoint;
                if (point.y > square.Bottom && point.y < square.Top) return sign > 0 ? square.Left - Right : square.Right - Left;

                List<PhysicsEdge> triangleEdges = sign > 0 ? RightEdges : LeftEdges;

                List<(bool intersecting, Vector2 point)> intersections = new List<(bool, Vector2)>();

                bool intersecting = false;
                foreach (PhysicsEdge triangleEdge in triangleEdges)
                {
                    bool ABI = square.AB.Intersection(triangleEdge, out Vector2 AB);
                    intersections.Add((ABI, AB));

                    bool CDI = square.CD.Intersection(triangleEdge, out Vector2 CD);
                    intersections.Add((CDI, CD));

                    if (ABI || CDI) intersecting = true;
                }
                if (!intersecting) return 0;

                if (sign > 0)
                {
                    float max = Mathf.NegativeInfinity;
                    foreach ((bool intersecting, Vector2 point) intersection in intersections)
                    {
                        if (intersection.intersecting) max = Mathf.Max(max, intersection.point.x);
                    }

                    return square.Left - max;
                }

                float min = Mathf.Infinity;
                foreach ((bool intersecting, Vector2 point) intersection in intersections)
                {
                    if (intersection.intersecting) min = Mathf.Min(min, intersection.point.x);
                }

                return square.Right - min;
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                List<PhysicsEdge> localEdges = sign > 0 ? RightEdges : LeftEdges;
                List<PhysicsEdge> remoteEdges = sign > 0 ? triangle.LeftEdges : triangle.RightEdges;

                float LocalInterior()
                {
                    List<(bool intersecting, Vector2 point)> intersections = new List<(bool intersecting, Vector2 point)>();
                    foreach (PhysicsEdge localEdge in localEdges)
                    {
                        foreach (PhysicsEdge remoteEdge in remoteEdges)
                        {
                            bool intersection = localEdge.Intersection(remoteEdge, out Vector2 point);
                            intersections.Add((intersection, point));
                        }
                    }

                    if (sign > 0)
                    {
                        float min = Mathf.Infinity;
                        foreach ((bool intersecting, Vector2 point) intersection in intersections)
                        {
                            if (intersection.intersecting) min = Mathf.Min(min, intersection.point.x);
                        }

                        return min - RightPoint.x;
                    }

                    float max = Mathf.NegativeInfinity;
                    foreach ((bool intersecting, Vector2 point) intersection in intersections)
                    {
                        if (intersection.intersecting) max = Mathf.Max(max, intersection.point.x);
                    }

                    return max - LeftPoint.x;
                }

                float NoInterior()
                {
                    Vector2 I = Vector2.zero, P = Vector2.zero;
                    
                    List<(bool intersecting, Vector2 point, PhysicsEdge localEdge)> intersections = new List<(bool intersecting, Vector2 point, PhysicsEdge localEdge)>();
                    bool intersecting = false;
                    foreach (PhysicsEdge localEdge in localEdges)
                    {
                        foreach (PhysicsEdge remoteEdge in remoteEdges)
                        {
                            bool intersection = localEdge.Intersection(remoteEdge, out Vector2 point);
                            intersections.Add((intersection, point, localEdge));

                            if (intersection) intersecting = true;
                        }
                    }
                    if (!intersecting) return 0;

                    if (sign < 0)
                    {
                        float max = Mathf.NegativeInfinity;
                        foreach ((bool intersecting, Vector2 point, PhysicsEdge localEdge) intersection in intersections)
                        {
                            if (intersection.intersecting && intersection.point.x > max) 
                            {
                                max = intersection.point.x; 
                                I = intersection.point; 
                                P = intersection.localEdge.OLeftPoint; 
                            }
                        }

                        return (I - P).x;
                    }

                    float min = Mathf.Infinity;
                    foreach ((bool intersecting, Vector2 point, PhysicsEdge localEdge) intersection in intersections)
                    {
                        if (intersection.intersecting && intersection.point.x < min)
                        {
                            min = intersection.point.x;
                            I = intersection.point;
                            P = intersection.localEdge.ORightPoint;
                        }
                    }

                    return (I - P).x;
                }

                bool localInterior = triangle.Interior(A) || triangle.Interior(B) || triangle.Interior(C);
                bool remoteInterior = Interior(triangle.A) || Interior(triangle.B) || Interior(triangle.C);

                if (localInterior)
                {
                    return LocalInterior();
                }

                if (remoteInterior)
                {
                    return triangle.OverlapHorizontally(-sign, this);
                }

                return NoInterior();
            }

            return 0;
        }

        public override float OverlapVertically(float sign, PhysicsCollider other)
        {
            if (other == this) return 0;

            if (other is PhysicsColliderCircle circle)
            {
                return circle.OverlapVertically(-sign, this);
            }

            if (other is PhysicsColliderSquare square)
            {
                Vector2 point = sign > 0 ? TopPoint : BottomPoint;
                if (point.x > square.Left && point.x < square.Right) return sign > 0 ? square.Bottom - Top : square.Top - Bottom;

                List<PhysicsEdge> triangleEdges = sign > 0 ? TopEdges : BottomEdges;

                List<(bool intersecting, Vector2 point)> intersections = new List<(bool, Vector2)>();

                bool intersecting = false;
                foreach (PhysicsEdge triangleEdge in triangleEdges)
                {
                    bool BDI = square.BD.Intersection(triangleEdge, out Vector2 BD);
                    intersections.Add((BDI, BD));

                    bool ACI = square.AC.Intersection(triangleEdge, out Vector2 AC);
                    intersections.Add((ACI, AC));

                    if (BDI || ACI) intersecting = true;
                }
                if (!intersecting) return 0;

                if (sign > 0)
                {
                    float max = Mathf.NegativeInfinity;
                    foreach ((bool intersecting, Vector2 point) intersection in intersections)
                    {
                        if (intersection.intersecting) max = Mathf.Max(max, intersection.point.y);
                    }   

                    return square.Bottom - max;
                }

                float min = Mathf.Infinity;
                foreach ((bool intersecting, Vector2 point) intersection in intersections)
                {
                    if (intersection.intersecting) min = Mathf.Min(min, intersection.point.y);
                }

                return square.Top - min;
            }

            if (other is PhysicsColliderTriangle triangle)
            {
                List<PhysicsEdge> localEdges = sign > 0 ? TopEdges : BottomEdges;
                List<PhysicsEdge> remoteEdges = sign > 0 ? triangle.BottomEdges : triangle.TopEdges;

                float LocalInterior()
                {
                    List<(bool intersecting, Vector2 point)> intersections = new List<(bool intersecting, Vector2 point)>();
                    foreach (PhysicsEdge localEdge in localEdges)
                    {
                        foreach (PhysicsEdge remoteEdge in remoteEdges)
                        {
                            bool intersection = localEdge.Intersection(remoteEdge, out Vector2 point);
                            intersections.Add((intersection, point));
                        }
                    }

                    if (sign > 0)
                    {
                        float min = Mathf.Infinity;
                        foreach ((bool intersecting, Vector2 point) intersection in intersections)
                        {
                            if (intersection.intersecting) min = Mathf.Min(min, intersection.point.y);
                        }

                        return min - TopPoint.y;
                    }

                    float max = Mathf.NegativeInfinity;
                    foreach ((bool intersecting, Vector2 point) intersection in intersections)
                    {
                        if (intersection.intersecting) max = Mathf.Max(max, intersection.point.y);
                    }

                    return max - BottomPoint.y;
                }

                float NoInterior()
                {
                    Vector2 I = Vector2.zero, P = Vector2.zero;

                    List<(bool intersecting, Vector2 point, PhysicsEdge localEdge)> intersections = new List<(bool intersecting, Vector2 point, PhysicsEdge localEdge)>();
                    bool intersecting = false;
                    foreach (PhysicsEdge localEdge in localEdges)
                    {
                        foreach (PhysicsEdge remoteEdge in remoteEdges)
                        {
                            bool intersection = localEdge.Intersection(remoteEdge, out Vector2 point);
                            intersections.Add((intersection, point, localEdge));

                            if (intersection) intersecting = true;
                        }
                    }
                    if (!intersecting) return 0;

                    if (sign < 0)
                    {
                        float max = Mathf.NegativeInfinity;
                        foreach ((bool intersecting, Vector2 point, PhysicsEdge localEdge) intersection in intersections)
                        {
                            if (intersection.intersecting && intersection.point.y > max)
                            {
                                max = intersection.point.y;
                                I = intersection.point;
                                P = intersection.localEdge.OBottomPoint;
                            }
                        }

                        return (I - P).y;
                    }

                    float min = Mathf.Infinity;
                    foreach ((bool intersecting, Vector2 point, PhysicsEdge localEdge) intersection in intersections)
                    {
                        if (intersection.intersecting && intersection.point.y < min)
                        {
                            min = intersection.point.y;
                            I = intersection.point;
                            P = intersection.localEdge.OTopPoint;
                        }
                    }

                    return (I - P).y;
                }

                bool localInterior = triangle.Interior(A) || triangle.Interior(B) || triangle.Interior(C);
                bool remoteInterior = Interior(triangle.A) || Interior(triangle.B) || Interior(triangle.C);

                if (localInterior)
                {
                    return LocalInterior();
                }

                if (remoteInterior)
                {
                    return triangle.OverlapHorizontally(-sign, this);
                }

                return NoInterior();
            }

            return 0;
        }

        private List<PhysicsEdge> GetEdges(Vector2 direction)
        {
            List<PhysicsEdge> edges = new List<PhysicsEdge>();

            Vector2 ABNormal = (AB.MidPoint - Position).normalized;
            if (Vector2.Angle(direction, ABNormal) < 90f)
            {
                edges.Add(AB);
            }

            Vector2 BCNormal = (BC.MidPoint - Position).normalized;
            if (Vector2.Angle(direction, BCNormal) < 90f)
            {
                edges.Add(BC);
            }

            Vector2 CANormal = (CA.MidPoint - Position).normalized;
            if (Vector2.Angle(direction, CANormal) < 90f)
            {
                edges.Add(CA);
            }

            return edges;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PhysicsColliderTriangle))]
    public class PhysicsColliderTriangleEditor : Editor
    {
        private const float SCALE = 0.1f;
        private const float THICKNESS = 2f;

        public void OnSceneGUI()
        {
            // cast
            PhysicsColliderTriangle t = target as PhysicsColliderTriangle;

            // draw
            Handles.color = Color.cyan;
            Handles.DrawLine(t.A, t.B, THICKNESS);
            Handles.DrawLine(t.B, t.C, THICKNESS);
            Handles.DrawLine(t.C, t.A, THICKNESS);

            // update centre
            EditorGUI.BeginChangeCheck();
            Vector2 centrePosition = Handles.FreeMoveHandle(t.Position, Quaternion.identity, SCALE, Vector2.zero, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Changed Centre");

                t.Centre = centrePosition - (Vector2)t.transform.position;
            }

            // update points A, B, C
            EditorGUI.BeginChangeCheck();
            Vector2 A = Handles.FreeMoveHandle(t.A, Quaternion.identity, SCALE, Vector2.zero, Handles.CubeHandleCap);
            Vector2 B = Handles.FreeMoveHandle(t.B, Quaternion.identity, SCALE, Vector2.zero, Handles.CubeHandleCap);
            Vector2 C = Handles.FreeMoveHandle(t.C, Quaternion.identity, SCALE, Vector2.zero, Handles.CubeHandleCap);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(t, "Changed Shape");

                t.Centre = ((A + B + C) / 3f) - (Vector2)t.transform.position;
                t.mA = A - t.Position;
                t.mB = B - t.Position;
                t.mC = C - t.Position;
            }
        }
    }
#endif
}