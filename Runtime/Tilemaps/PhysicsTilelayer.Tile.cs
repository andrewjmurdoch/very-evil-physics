using UnityEngine;
using VED.Tilemaps;
using static VED.Physics.Enums;

namespace VED.Physics
{
    public partial class PhysicsTileLayer : TileLayer
    {
        public class PhysicsTile : PhysicsSolid
        {
            const float HALF = 0.5000000f;
            const float THIR = 0.3333333f;
            const float QUAR = 0.2500000f;

            private SpriteRenderer _spriteRenderer = null;

            public PhysicsTile Init(PhysicsTileset.PhysicsTile definition, int sortingOrder)
            {
                if (_spriteRenderer == null) _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                _spriteRenderer.sprite = definition.Sprite;
                _spriteRenderer.sortingOrder = sortingOrder;

                InitCollider(definition.PhysicsCollider);

                _physicsMaterial = PhysicsManager.Instance.PhysicsMaterialMapper[definition.PhysicsMaterial];

                return this;
            }

            private void InitCollider(PhysicsColliderType physicsColliderType)
            {
                if (physicsColliderType == PhysicsColliderType.NONE) return;

                switch (physicsColliderType)
                {
                    case PhysicsColliderType.S       : InitColliderSquare           (); break;
                    case PhysicsColliderType.S_HL    : InitColliderSquareHL         (); break;
                    case PhysicsColliderType.S_HR    : InitColliderSquareHR         (); break;
                    case PhysicsColliderType.S_HT    : InitColliderSquareHT         (); break;
                    case PhysicsColliderType.S_HB    : InitColliderSquareHB         (); break;
                    case PhysicsColliderType.S_HMH   : InitColliderSquareHMH        (); break;
                    case PhysicsColliderType.S_HMV   : InitColliderSquareHMV        (); break;
                    case PhysicsColliderType.S_1TL   : InitColliderSquare1TL        (); break;
                    case PhysicsColliderType.S_1TR   : InitColliderSquare1TR        (); break;
                    case PhysicsColliderType.S_1TT   : InitColliderSquare1TT        (); break;
                    case PhysicsColliderType.S_1TB   : InitColliderSquare1TB        (); break;
                    case PhysicsColliderType.S_1TMH  : InitColliderSquare1TMH       (); break;
                    case PhysicsColliderType.S_1TMV  : InitColliderSquare1TMV       (); break;
                    case PhysicsColliderType.S_2TL   : InitColliderSquare2TL        (); break;
                    case PhysicsColliderType.S_2TR   : InitColliderSquare2TR        (); break;
                    case PhysicsColliderType.S_2TT   : InitColliderSquare2TT        (); break;
                    case PhysicsColliderType.S_2TB   : InitColliderSquare2TB        (); break;
                    case PhysicsColliderType.S_2TMH  : InitColliderSquare2TMH       (); break;
                    case PhysicsColliderType.S_2TMV  : InitColliderSquare2TMV       (); break;
                    case PhysicsColliderType.C_BL    : InitColliderCircleBL         (); break;
                    case PhysicsColliderType.C_BR    : InitColliderCircleBR         (); break;
                    case PhysicsColliderType.C_TL    : InitColliderCircleTL         (); break;
                    case PhysicsColliderType.C_TR    : InitColliderCircleTR         (); break;
                    case PhysicsColliderType.T_BL    : InitColliderTriangleBL       (); break;
                    case PhysicsColliderType.T_BR    : InitColliderTriangleBR       (); break;
                    case PhysicsColliderType.T_TL    : InitColliderTriangleTL       (); break;
                    case PhysicsColliderType.T_TR    : InitColliderTriangleTR       (); break;
                    case PhysicsColliderType.T_HBL1  : InitColliderTriangleHBL1     (); break;
                    case PhysicsColliderType.T_HBL2  : InitColliderTriangleHBL2     (); break;
                    case PhysicsColliderType.T_HBL3  : InitColliderTriangleHBL3     (); break;
                    case PhysicsColliderType.T_HBR1  : InitColliderTriangleHBR1     (); break;
                    case PhysicsColliderType.T_HBR2  : InitColliderTriangleHBR2     (); break;
                    case PhysicsColliderType.T_HBR3  : InitColliderTriangleHBR3     (); break;
                    case PhysicsColliderType.T_VBL1  : InitColliderTriangleVBL1     (); break;
                    case PhysicsColliderType.T_VBL2  : InitColliderTriangleVBL2     (); break;
                    case PhysicsColliderType.T_VBL3  : InitColliderTriangleVBL3     (); break;
                    case PhysicsColliderType.T_VBR1  : InitColliderTriangleVBR1     (); break;
                    case PhysicsColliderType.T_VBR2  : InitColliderTriangleVBR2     (); break;
                    case PhysicsColliderType.T_VBR3  : InitColliderTriangleVBR3     (); break;
                    case PhysicsColliderType.T_RBL1  : InitColliderTriangleRBL1     (); break;
                    case PhysicsColliderType.T_RBL2  : InitColliderTriangleRBL2     (); break;
                    case PhysicsColliderType.T_RBR1  : InitColliderTriangleRBR1     (); break;
                    case PhysicsColliderType.T_RBR2  : InitColliderTriangleRBR2     (); break;
                    case PhysicsColliderType.T_RTL1  : InitColliderTriangleRTL1     (); break;
                    case PhysicsColliderType.T_RTL2  : InitColliderTriangleRTL2     (); break;
                    case PhysicsColliderType.T_RTR1  : InitColliderTriangleRTR1     (); break;
                    case PhysicsColliderType.T_RTR2  : InitColliderTriangleRTR2     (); break;
                    case PhysicsColliderType.TS_HBL  : InitColliderTriangleSquareHBL(); break;
                    case PhysicsColliderType.TS_HBR  : InitColliderTriangleSquareHBR(); break;
                    case PhysicsColliderType.TS_HTL  : InitColliderTriangleSquareHTL(); break;
                    case PhysicsColliderType.TS_HTR  : InitColliderTriangleSquareHTR(); break;
                    case PhysicsColliderType.TS_VBL  : InitColliderTriangleSquareVBL(); break;
                    case PhysicsColliderType.TS_VBR  : InitColliderTriangleSquareVBR(); break;
                    case PhysicsColliderType.TS_VTL  : InitColliderTriangleSquareVTL(); break;
                    case PhysicsColliderType.TS_VTR  : InitColliderTriangleSquareVTR(); break;

                }
            }

            #region Square
            private void GenerateColliderSquare(Vector2 centre, Vector2 size)
            {
                PhysicsColliderSquare square = gameObject.AddComponent<PhysicsColliderSquare>();
                square.Init(centre, size);
                Colliders.Add(square);
            }

            private void InitColliderSquare     () => GenerateColliderSquare(Vector2.zero, Vector2.one);
            private void InitColliderSquareHL   () => GenerateColliderSquare(Vector2.left  * QUAR, new Vector2(HALF, 1f));
            private void InitColliderSquareHR   () => GenerateColliderSquare(Vector2.right * QUAR, new Vector2(HALF, 1f));
            private void InitColliderSquareHT   () => GenerateColliderSquare(Vector2.up    * QUAR, new Vector2(1f, HALF));
            private void InitColliderSquareHB   () => GenerateColliderSquare(Vector2.down  * QUAR, new Vector2(1f, HALF));
            private void InitColliderSquareHMH  () => GenerateColliderSquare(Vector2.zero, new Vector2(1f, HALF));
            private void InitColliderSquareHMV  () => GenerateColliderSquare(Vector2.zero, new Vector2(HALF, 1f));
            private void InitColliderSquare1TL  () => GenerateColliderSquare(new Vector2(-THIR, 0f), new Vector2(THIR, 1f));
            private void InitColliderSquare1TR  () => GenerateColliderSquare(new Vector2( THIR, 0f), new Vector2(THIR, 1f));
            private void InitColliderSquare1TT  () => GenerateColliderSquare(new Vector2(0f,  THIR), new Vector2(1f, THIR));
            private void InitColliderSquare1TB  () => GenerateColliderSquare(new Vector2(0f, -THIR), new Vector2(1f, THIR));
            private void InitColliderSquare1TMH () => GenerateColliderSquare(Vector2.zero, new Vector2(1f, THIR));
            private void InitColliderSquare1TMV () => GenerateColliderSquare(Vector2.zero, new Vector2(THIR, 1f));
            private void InitColliderSquare2TL  () => GenerateColliderSquare(new Vector2(-THIR / 2f, 0f), new Vector2(2f * THIR, 1f));
            private void InitColliderSquare2TR  () => GenerateColliderSquare(new Vector2( THIR / 2f, 0f), new Vector2(2f * THIR, 1f));
            private void InitColliderSquare2TT  () => GenerateColliderSquare(new Vector2(0f,  THIR / 2f), new Vector2(1f, 2f * THIR));
            private void InitColliderSquare2TB  () => GenerateColliderSquare(new Vector2(0f, -THIR / 2f), new Vector2(1f, 2f * THIR));
            private void InitColliderSquare2TMH () => GenerateColliderSquare(Vector2.zero, new Vector2(1f, 2f * THIR));
            private void InitColliderSquare2TMV () => GenerateColliderSquare(Vector2.zero, new Vector2(2f * THIR, 1f));
            #endregion

            #region Circle
            private void GenerateColliderCircle(float x, float y)
            {
                PhysicsColliderCircle circle = gameObject.AddComponent<PhysicsColliderCircle>();
                circle.Init(new Vector2(x, y), 1f);
                Colliders.Add(circle);
            }

            private void InitColliderCircleBL() => GenerateColliderCircle(HALF * Vector2.left .x, HALF * Vector2.down.y);
            private void InitColliderCircleBR() => GenerateColliderCircle(HALF * Vector2.right.x, HALF * Vector2.down.y);
            private void InitColliderCircleTL() => GenerateColliderCircle(HALF * Vector2.left .x, HALF * Vector2.up.y);
            private void InitColliderCircleTR() => GenerateColliderCircle(HALF * Vector2.right.x, HALF * Vector2.up.y);
            #endregion

            #region Triangle
            private void GenerateColliderTriangle(Vector2 a, Vector2 b, Vector2 c)
            {
                PhysicsColliderTriangle triangle = gameObject.AddComponent<PhysicsColliderTriangle>();
                triangle.Init(a, b, c);
                Colliders.Add(triangle);
            }

            private void InitColliderTriangleBL()
            {
                Vector2 a = HALF * Vector2.left + HALF * Vector2.up;
                Vector2 b = HALF * Vector2.left + HALF * Vector2.down;
                Vector2 c = HALF * Vector2.right + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleBR()
            {
                Vector2 a = HALF * Vector2.right + HALF * Vector2.up;
                Vector2 b = HALF * Vector2.right + HALF * Vector2.down;
                Vector2 c = HALF * Vector2.left  + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleTL()
            {
                Vector2 a = HALF * Vector2.left  + HALF * Vector2.down;
                Vector2 b = HALF * Vector2.left  + HALF * Vector2.up;
                Vector2 c = HALF * Vector2.right + HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleTR()
            {
                Vector2 a = HALF * Vector2.right + HALF * Vector2.down;
                Vector2 b = HALF * Vector2.right + HALF * Vector2.up;
                Vector2 c = HALF * Vector2.left  + HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleHBL1()
            {
                Vector2 a = HALF * Vector2.left;
                Vector2 b = HALF * Vector2.left  + HALF * Vector2.down;
                Vector2 c = HALF * Vector2.right + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleHBL2()
            {
                Vector2 a, b, c;

                a = HALF * Vector2.left;
                b = HALF * Vector2.left + HALF * Vector2.up;
                c = HALF * Vector2.right;
                GenerateColliderTriangle(a, b, c);

                a = HALF * Vector2.left;
                b = HALF * Vector2.right;
                c = HALF * Vector2.right + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleHBL3()
            {
                Vector2 a = HALF * Vector2.left  + HALF * Vector2.up;
                Vector2 b = HALF * Vector2.right + HALF * Vector2.up;
                Vector2 c = HALF * Vector2.right;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleHBR1()
            {
                Vector2 a = HALF * Vector2.left  + HALF * Vector2.down;
                Vector2 b = HALF * Vector2.right + HALF * Vector2.down;
                Vector2 c = HALF * Vector2.right;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleHBR2()
            {
                Vector2 a, b, c;

                a = HALF * Vector2.left;
                b = HALF * Vector2.right;
                c = HALF * Vector2.right + HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);

                a = HALF * Vector2.right;
                b = HALF * Vector2.left;
                c = HALF * Vector2.left + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleHBR3()
            {
                Vector2 a = HALF * Vector2.left  + HALF * Vector2.up;
                Vector2 b = HALF * Vector2.right + HALF * Vector2.up;
                Vector2 c = HALF * Vector2.left;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleVBL1()
            {
                Vector2 a = HALF * Vector2.left + HALF * Vector2.up;
                Vector2 b = HALF * Vector2.left + HALF * Vector2.down;
                Vector2 c = HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleVBL2()
            {
                Vector2 a, b, c;

                a = HALF * Vector2.left + HALF * Vector2.up;
                b = HALF * Vector2.up;
                c = HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);

                a = HALF * Vector2.up;
                b = HALF * Vector2.down;
                c = HALF * Vector2.right + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleVBL3()
            {
                Vector2 a = HALF * Vector2.up;
                Vector2 b = HALF * Vector2.right + HALF * Vector2.up;
                Vector2 c = HALF * Vector2.right + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleVBR1()
            {
                Vector2 a = HALF * Vector2.down;
                Vector2 b = HALF * Vector2.right + HALF * Vector2.down;
                Vector2 c = HALF * Vector2.right + HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleVBR2()
            {
                Vector2 a, b, c;

                a = HALF * Vector2.left + HALF * Vector2.down;
                b = HALF * Vector2.down;
                c = HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);

                a = HALF * Vector2.down;
                b = HALF * Vector2.up;
                c = HALF * Vector2.right + HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleVBR3()
            {
                Vector2 a = HALF * Vector2.left + HALF * Vector2.down;
                Vector2 b = HALF * Vector2.left + HALF * Vector2.up;
                Vector2 c = HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleRBL1()
            {
                Vector2 a = HALF * Vector2.down;
                Vector2 b = HALF * Vector2.left + HALF * Vector2.down;
                Vector2 c = HALF * Vector2.left;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleRBL2()
            {
                Vector2 a, b, c;

                a = HALF * Vector2.right + HALF * Vector2.down;
                b = HALF * Vector2.down;
                c = HALF * Vector2.left  + HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);

                a = HALF * Vector2.down;
                b = HALF * Vector2.left;
                c = HALF * Vector2.left + HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleRBR1()
            {
                Vector2 a = HALF * Vector2.down;
                Vector2 b = HALF * Vector2.right + HALF * Vector2.down;
                Vector2 c = HALF * Vector2.right;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleRBR2()
            {
                Vector2 a, b, c;

                a = HALF * Vector2.left + HALF * Vector2.down;
                b = HALF * Vector2.down;
                c = HALF * Vector2.right + HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);

                a = HALF * Vector2.down;
                b = HALF * Vector2.right;
                c = HALF * Vector2.right + HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleRTL1()
            {
                Vector2 a = HALF * Vector2.up;
                Vector2 b = HALF * Vector2.left + HALF * Vector2.up;
                Vector2 c = HALF * Vector2.left;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleRTL2()
            {
                Vector2 a, b, c;

                a = HALF * Vector2.right + HALF * Vector2.up;
                b = HALF * Vector2.up;
                c = HALF * Vector2.left + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);

                a = HALF * Vector2.up;
                b = HALF * Vector2.left;
                c = HALF * Vector2.left + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleRTR1()
            {
                Vector2 a = HALF * Vector2.up;
                Vector2 b = HALF * Vector2.right + HALF * Vector2.up;
                Vector2 c = HALF * Vector2.right;
                GenerateColliderTriangle(a, b, c);
            }

            private void InitColliderTriangleRTR2()
            {
                Vector2 a, b, c;

                a = HALF * Vector2.left + HALF * Vector2.up;
                b = HALF * Vector2.up;
                c = HALF * Vector2.right + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);

                a = HALF * Vector2.up;
                b = HALF * Vector2.right;
                c = HALF * Vector2.right + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);
            }
            #endregion

            #region Triangle & Square
            private void InitColliderTriangleSquareHBL()
            {
                Vector2 a = HALF * Vector2.right;
                Vector2 b = HALF * Vector2.left;
                Vector2 c = HALF * Vector2.left + HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);

                GenerateColliderSquare(new Vector2(0f, -QUAR), new Vector2(1f, HALF));
            }

            private void InitColliderTriangleSquareHBR()
            {
                Vector2 a = HALF * Vector2.left;
                Vector2 b = HALF * Vector2.right;
                Vector2 c = HALF * Vector2.right + HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);

                GenerateColliderSquare(new Vector2(0f, -QUAR), new Vector2(1f, HALF));
            }

            private void InitColliderTriangleSquareHTL()
            {
                Vector2 a = HALF * Vector2.right;
                Vector2 b = HALF * Vector2.left;
                Vector2 c = HALF * Vector2.left + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);

                GenerateColliderSquare(new Vector2(0f, QUAR), new Vector2(1f, HALF));
            }

            private void InitColliderTriangleSquareHTR()
            {
                Vector2 a = HALF * Vector2.left;
                Vector2 b = HALF * Vector2.right;
                Vector2 c = HALF * Vector2.right + HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);

                GenerateColliderSquare(new Vector2(0f, QUAR), new Vector2(1f, HALF));
            }

            private void InitColliderTriangleSquareVBL()
            {
                Vector2 a = HALF * Vector2.right + HALF * Vector2.down;
                Vector2 b = HALF * Vector2.down;
                Vector2 c = HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);

                GenerateColliderSquare(new Vector2(-QUAR, 0f), new Vector2(HALF, 1f));
            }

            private void InitColliderTriangleSquareVBR()
            {
                Vector2 a = HALF * Vector2.left + HALF * Vector2.down;
                Vector2 b = HALF * Vector2.down;
                Vector2 c = HALF * Vector2.up;
                GenerateColliderTriangle(a, b, c);

                GenerateColliderSquare(new Vector2(QUAR, 0f), new Vector2(HALF, 1f));
            }

            private void InitColliderTriangleSquareVTL()
            {
                Vector2 a = HALF * Vector2.right + HALF * Vector2.up;
                Vector2 b = HALF * Vector2.up;
                Vector2 c = HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);

                GenerateColliderSquare(new Vector2(-QUAR, 0f), new Vector2(HALF, 1f));
            }

            private void InitColliderTriangleSquareVTR()
            {
                Vector2 a = HALF * Vector2.left + HALF * Vector2.up;
                Vector2 b = HALF * Vector2.up;
                Vector2 c = HALF * Vector2.down;
                GenerateColliderTriangle(a, b, c);

                GenerateColliderSquare(new Vector2(QUAR, 0f), new Vector2(HALF, 1f));
            }
            #endregion
        }
    }
}