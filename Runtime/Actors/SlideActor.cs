using System;
using System.Collections.Generic;
using UnityEngine;
using VED.Utilities;

namespace VED.Physics
{
    public class SlideActor : PhysicsActor
    {
        [Serializable]
        protected class SlideActorSettings
        {
            public static AnimationCurve DefaultCurve => new AnimationCurve(new Keyframe[2] { new Keyframe(0f, 1f, 0f, -0.45f), new Keyframe(6f, 0f, 0f, 0f) });

            [Serializable]
            public class ConversionCurves
            {
                [SerializeField] public AnimationCurve Positive = DefaultCurve;
                [SerializeField] public AnimationCurve Negative = DefaultCurve;

                public AnimationCurve this[float sign] => sign > 0 ? Positive : Negative;
            }

            #region Up
            [SerializeField] public bool CanSlideUpMovingLeft = true; // whether actor can slide up, when moving left
            [SerializeField] public bool CanSlideUpMovingRight = true; // whether actor can slide up, when moving right
            [SerializeField] public float MaxSlideUpDist = 0.2f;
            [SerializeField] public ConversionCurves SlideUpConversionCurve;
            #endregion

            #region Down
            [SerializeField] public bool CanSlideDownMovingLeft = true; // whether actor can slide down, when moving left
            [SerializeField] public bool CanSlideDownMovingRight = true; // whether actor can slide down, when moving right
            [SerializeField] public float MaxSlideDownDist = 0.2f;
            [SerializeField] public ConversionCurves SlideDownConversionCurve;
            #endregion

            #region Left
            [SerializeField] public bool CanSlideLeftMovingUp = true; // whether actor can slide left, when moving up
            [SerializeField] public bool CanSlideLeftMovingDown = true; // whether actor can slide left, when moving down, useful for actors with gravity
            [SerializeField] public float MaxSlideLeftDist = 0.2f;
            [SerializeField] public ConversionCurves SlideLeftConversionCurve;

            #endregion

            #region Right
            [SerializeField] public bool CanSlideRightMovingUp = true; // whether actor can slide right, when moving up
            [SerializeField] public bool CanSlideRightMovingDown = true; // whether actor can slide right, when moving down, useful for actors with gravity
            [SerializeField] public float MaxSlideRightDist = 0.2f;
            [SerializeField] public ConversionCurves SlideRightConversionCurve;
            #endregion
        }

        [SerializeField] protected SlideActorSettings _slideSettings = new SlideActorSettings();

        public bool SlidingUp
        {
            get => _slidingUp;
            set => _slidingUp = value;
        }
        [SerializeField, ReadOnly] protected bool _slidingUp = false;

        public bool SlidingDown
        {
            get => _slidingDown;
            set => _slidingDown = value;
        }
        [SerializeField, ReadOnly] protected bool _slidingDown = false;

        public bool SlidingLeft
        {
            get => _slidingLeft;
            set => _slidingLeft = value;
        }
        [SerializeField, ReadOnly] protected bool _slidingLeft = false;

        public bool SlidingRight
        {
            get => _slidingRight;
            set => _slidingRight = value;
        }
        [SerializeField, ReadOnly] protected bool _slidingRight = false;

        public bool SlidingVertically   => _slidingDown || _slidingUp;
        public bool SlidingHorizontally => _slidingLeft || _slidingRight;

        private const float MIN_AMOUNT_THRESHOLD = 0.1f;

        #region Up
        protected bool CanSlideUp(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideUpMovingLeft  && (sign < 0)) return false;
            if (!_slideSettings.CanSlideUpMovingRight && (sign > 0)) return false;
            if (!MoveableVertically[1]) return false;

            // special case for sliding up on triangle collider
            if (collision.RemoteCollider is PhysicsColliderTriangle)
            {
                return CanSlideUpTriangle(sign, collision, out amount) && amount > MIN_AMOUNT_THRESHOLD;
            }

            // special case for sliding up on circle collider
            if (collision.RemoteCollider is PhysicsColliderCircle)
            {
                return CanSlideUpCircle(sign, collision, out amount) && amount > MIN_AMOUNT_THRESHOLD;
            }

            // typical case for sliding up on square collider
            float slide = collision.RemoteCollider.Top - collision.LocalCollider.Bottom;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideUpDist)
            {
                return amount > MIN_AMOUNT_THRESHOLD;
            }

            return false;
        }

        protected bool CanSlideUpTriangle(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1;
            PhysicsColliderTriangle triangle = collision.RemoteCollider as PhysicsColliderTriangle;
            Optional<float> gradient;

            List<PhysicsEdge> GetEdges()
            {
                List<PhysicsEdge> edges = new List<PhysicsEdge>();
                List<PhysicsEdge> edgesVer = triangle.TopEdges;
                List<PhysicsEdge> edgesHor = sign > 0 ? triangle.LeftEdges : triangle.RightEdges;

                for (int i = 0; i < edgesVer.Count; i++)
                {
                    for (int j = 0; j < edgesHor.Count; j++)
                    {
                        if (edgesVer[i].Is(edgesHor[j]))
                            edges.Add(edgesVer[i]);
                    }
                }

                return edges;
            }
            List<PhysicsEdge> edges = GetEdges();

            bool OneEdge(out float amount)
            {
                gradient = edges[0].Gradient;
                amount = !gradient.Enabled ? 1f : _slideSettings.SlideUpConversionCurve[sign].Evaluate(Mathf.Abs(gradient.Value));

                if (collision.LocalCollider.Bottom < edges[0].Bottom)
                    return collision.LocalCollider.Bottom - edges[0].Bottom <= _slideSettings.MaxSlideUpDist;

                return true;
            }

            bool TwoEdge(out float amount)
            {
                PhysicsEdge edgeBottom, edgeTop;

                if (edges[0].Bottom < edges[1].Bottom)
                {
                    edgeBottom = edges[0];
                    edgeTop = edges[1];
                }
                else
                {
                    edgeBottom = edges[1];
                    edgeTop = edges[0];
                }

                gradient = (collision.LocalCollider.Bottom < edgeTop.Bottom) ? edgeBottom.Gradient : edgeTop.Gradient;
                amount = !gradient.Enabled ? 1f : _slideSettings.SlideUpConversionCurve[sign].Evaluate(Mathf.Abs(gradient.Value));

                if (collision.LocalCollider.Bottom < edgeBottom.Bottom)
                    return collision.LocalCollider.Bottom - edgeBottom.Bottom <= _slideSettings.MaxSlideUpDist;

                return true;
            }

            if (edges.Count == 1) return OneEdge(out amount);
            if (edges.Count == 2) return TwoEdge(out amount);

            return Mathf.Abs(collision.LocalCollider.Bottom - collision.RemoteCollider.Top) <= _slideSettings.MaxSlideUpDist;
        }

        protected bool CanSlideUpCircle(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1f;
            PhysicsColliderCircle circle = collision.RemoteCollider as PhysicsColliderCircle;
            Vector2 position = new Vector2(sign > 0 ? collision.LocalCollider.Right : collision.LocalCollider.Left, collision.LocalCollider.Bottom);

            // account for local collider being a circle
            if (collision.LocalCollider is PhysicsColliderCircle localCircle)
            {
                Vector2 direction = (circle.Position - localCircle.Position).normalized;
                position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
            }

            return (position.y - circle.Position.y) >= 0;
        }

        protected bool CanSlideUp(float sign, List<PhysicsContact> collisions, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideUpMovingLeft && (_velocityHor < 0)) return false;
            if (!_slideSettings.CanSlideUpMovingRight && (_velocityHor > 0)) return false;
            if (!MoveableVertically[1]) return false;

            bool canSlide = true;

            foreach (PhysicsContact collision in collisions)
            {
                canSlide &= CanSlideUp(sign, collision, out float newAmount);
                amount = Mathf.Min(newAmount, amount);
            }

            return canSlide;
        }

        protected void PerformSlideUp(float amount = 1)
        {
            // perform slide up
            _slidingUp = amount > MIN_AMOUNT_THRESHOLD;

            // if previously moving downward, cancel vertical movement
            _yRounded = Math.Max(_yRounded, 0f);
            _yRemainder = Math.Max(_yRemainder, 0f);
            _velocityVer = Math.Max(_velocityVer, 0f);

            // convert this horizontal movement into vertical movement
            _yRemainder += amount;
        }
        #endregion

        #region Down
        protected bool CanSlideDown(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideDownMovingLeft  && (sign < 0)) return false;
            if (!_slideSettings.CanSlideDownMovingRight && (sign > 0)) return false;
            if (!MoveableVertically[-1]) return false;

            // special case for sliding under triangle collider
            if (collision.RemoteCollider is PhysicsColliderTriangle)
            {
                return CanSlideDownTriangle(sign, collision, out amount) && amount > MIN_AMOUNT_THRESHOLD;
            }

            // special case for sliding under circle collider
            if (collision.RemoteCollider is PhysicsColliderCircle)
            {
                return CanSlideDownCircle(sign, collision, out amount) && amount > MIN_AMOUNT_THRESHOLD;
            }

            // typical case for sliding down on square collider
            float slide = collision.LocalCollider.Top - collision.RemoteCollider.Bottom;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideDownDist)
            {
                return amount > MIN_AMOUNT_THRESHOLD;
            }

            return false;
        }

        protected bool CanSlideDownTriangle(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1;
            PhysicsColliderTriangle triangle = collision.RemoteCollider as PhysicsColliderTriangle;
            Optional<float> gradient;

            List<PhysicsEdge> GetEdges()
            {
                List<PhysicsEdge> edges = new List<PhysicsEdge>();
                List<PhysicsEdge> edgesVer = triangle.BottomEdges;
                List<PhysicsEdge> edgesHor = sign > 0 ? triangle.LeftEdges : triangle.RightEdges;

                for (int i = 0; i < edgesVer.Count; i++)
                {
                    for (int j = 0; j < edgesHor.Count; j++)
                    {
                        if (edgesVer[i].Is(edgesHor[j]))
                            edges.Add(edgesVer[i]);
                    }
                }

                return edges;
            }
            List<PhysicsEdge> edges = GetEdges();

            bool OneEdge(out float amount)
            {
                gradient = edges[0].Gradient;
                amount = !gradient.Enabled ? 1f : _slideSettings.SlideDownConversionCurve[sign].Evaluate(Mathf.Abs(gradient.Value));

                if (collision.LocalCollider.Top > edges[0].Top)
                    return collision.LocalCollider.Top - edges[0].Top <= _slideSettings.MaxSlideDownDist;

                return true;
            }

            bool TwoEdge(out float amount)
            {
                PhysicsEdge edgeBottom, edgeTop;

                if (edges[0].Bottom < edges[1].Bottom)
                {
                    edgeBottom = edges[0];
                    edgeTop = edges[1];
                }
                else
                {
                    edgeBottom = edges[1];
                    edgeTop = edges[0];
                }

                gradient = (collision.LocalCollider.Top > edgeBottom.Top) ? edgeTop.Gradient : edgeBottom.Gradient;
                amount = !gradient.Enabled ? 1f : _slideSettings.SlideDownConversionCurve[sign].Evaluate(Mathf.Abs(gradient.Value));

                if (collision.LocalCollider.Top > edgeTop.Top)
                    return collision.LocalCollider.Top - edgeTop.Top <= _slideSettings.MaxSlideDownDist;

                return true;
            }

            if (edges.Count == 1) return OneEdge(out amount);
            if (edges.Count == 2) return TwoEdge(out amount);

            return Mathf.Abs(collision.LocalCollider.Top - collision.RemoteCollider.Bottom) <= _slideSettings.MaxSlideDownDist;
        }

        protected bool CanSlideDownCircle(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1f;
            PhysicsColliderCircle circle = collision.RemoteCollider as PhysicsColliderCircle;
            Vector2 position = new Vector2(sign > 0 ? collision.LocalCollider.Right : collision.LocalCollider.Left, collision.LocalCollider.Top);

            // account for local collider being a circle
            if (collision.LocalCollider is PhysicsColliderCircle localCircle)
            {
                Vector2 direction = (circle.Position - localCircle.Position).normalized;
                position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
            }

            return (circle.Position.y - position.y) >= 0;
        }

        protected bool CanSlideDown(float sign, List<PhysicsContact> collisions, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideDownMovingLeft  && (_velocityHor < 0)) return false;
            if (!_slideSettings.CanSlideDownMovingRight && (_velocityHor > 0)) return false;
            if (!MoveableVertically[-1]) return false;

            bool canSlide = true;
            foreach (PhysicsContact collision in collisions)
            {
                canSlide &= CanSlideDown(sign, collision, out float newAmount);
                amount = Mathf.Min(newAmount, amount);
            }

            return canSlide;
        }

        protected void PerformSlideDown(float amount = 1)
        {
            // perform slide down
            _slidingDown = amount > MIN_AMOUNT_THRESHOLD;

            // if previously moving upward, cancel vertical movement
            _yRounded = Math.Min(_yRounded, 0f);
            _yRemainder = Math.Min(_yRemainder, 0f);
            _velocityVer = Math.Min(_velocityVer, 0f);

            // convert this horizontal movement into vertical movement
            _yRemainder -= amount;
        }
        #endregion

        #region Left
        protected bool CanSlideLeft(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideLeftMovingUp && (sign > 0)) return false;
            if (!_slideSettings.CanSlideLeftMovingDown && (sign < 0)) return false;
            if (!MoveableHorizontally[-1]) return false;

            // special case for sliding on triangle collider
            if (collision.RemoteCollider is PhysicsColliderTriangle)
            {
                return CanSlideLeftTriangle(sign, collision, out amount) && amount > MIN_AMOUNT_THRESHOLD;
            }

            // special case for sliding on circle collider
            if (collision.RemoteCollider is PhysicsColliderCircle circle)
            {
                return CanSlideLeftCircle(sign, collision, out amount) && amount > MIN_AMOUNT_THRESHOLD;
            }

            // typical case for sliding left on square collider
            float slide = collision.LocalCollider.Right - collision.RemoteCollider.Left;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideLeftDist)
            {
                return amount > MIN_AMOUNT_THRESHOLD;
            }

            return false;
        }

        protected bool CanSlideLeftTriangle(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1;
            PhysicsColliderTriangle triangle = collision.RemoteCollider as PhysicsColliderTriangle;
            Optional<float> gradient;

            List<PhysicsEdge> GetEdges()
            {
                List<PhysicsEdge> edges = new List<PhysicsEdge>();
                List<PhysicsEdge> edgesHor = triangle.LeftEdges;
                List<PhysicsEdge> edgesVer = sign > 0 ? triangle.BottomEdges : triangle.TopEdges;

                for (int i = 0; i < edgesHor.Count; i++)
                {
                    for (int j = 0; j < edgesVer.Count; j++)
                    {
                        if (edgesHor[i].Is(edgesVer[j]))
                            edges.Add(edgesHor[i]);
                    }
                }

                return edges;
            }
            List<PhysicsEdge> edges = GetEdges();

            bool OneEdge(out float amount)
            {
                gradient = edges[0].Gradient;
                amount = !gradient.Enabled ? 1f : _slideSettings.SlideLeftConversionCurve[sign].Evaluate(Mathf.Abs(gradient.Value));

                if (collision.LocalCollider.Right > edges[0].Right)
                    return collision.LocalCollider.Right - edges[0].Right <= _slideSettings.MaxSlideLeftDist;

                return true;
            }

            bool TwoEdge(out float amount)
            {
                PhysicsEdge edgeLeft, edgeRight;

                if (edges[0].Left < edges[1].Left)
                {
                    edgeLeft = edges[0];
                    edgeRight = edges[1];
                }
                else
                {
                    edgeLeft = edges[1];
                    edgeRight = edges[0];
                }

                gradient = (collision.LocalCollider.Right > edgeLeft.Right) ? edgeRight.Gradient : edgeLeft.Gradient;
                amount = !gradient.Enabled ? 1f : _slideSettings.SlideLeftConversionCurve[sign].Evaluate(Mathf.Abs(gradient.Value));

                if (collision.LocalCollider.Right > edgeRight.Right)
                    return collision.LocalCollider.Right - edgeRight.Right <= _slideSettings.MaxSlideLeftDist;

                return true;
            }

            if (edges.Count == 1) return OneEdge(out amount);
            if (edges.Count == 2) return TwoEdge(out amount);

            return Mathf.Abs(collision.LocalCollider.Right - collision.RemoteCollider.Left) <= _slideSettings.MaxSlideLeftDist;
        }

        protected bool CanSlideLeftCircle(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1f;
            PhysicsColliderCircle circle = collision.RemoteCollider as PhysicsColliderCircle;
            Vector2 position = new Vector2(collision.LocalCollider.Right, sign > 0 ? collision.LocalCollider.Top : collision.LocalCollider.Bottom);

            // account for local collider being a circle
            if (collision.LocalCollider is PhysicsColliderCircle localCircle)
            {
                Vector2 direction = (circle.Position - localCircle.Position).normalized;
                position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
            }

            return (circle.Position.x - position.x) >= 0;
        }

        protected bool CanSlideLeft(float sign, List<PhysicsContact> collisions, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideLeftMovingUp && (_velocityVer > 0)) return false;
            if (!_slideSettings.CanSlideLeftMovingDown && (_velocityVer < 0)) return false;
            if (!MoveableHorizontally[-1]) return false;

            bool canSlide = true;
            foreach (PhysicsContact collision in collisions)
            {
                canSlide &= CanSlideLeft(sign, collision, out float newAmount);
                amount = Mathf.Min(newAmount, amount);
            }

            return canSlide;
        }

        protected void PerformSlideLeft(float amount = 1)
        {
            // perform slide left
            _slidingLeft = amount > MIN_AMOUNT_THRESHOLD;

            // if previously moving right, cancel horizontal movement
            _xRounded = Math.Min(_xRounded, 0f);
            _xRemainder = Math.Min(_xRemainder, 0f);
            _velocityHor = Math.Min(_velocityHor, 0f);

            // convert this vertical movement into horizontal movement
            _xRemainder -= amount;
        }
        #endregion

        #region Right
        protected bool CanSlideRight(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideRightMovingUp   && (sign > 0)) return false;
            if (!_slideSettings.CanSlideRightMovingDown && (sign < 0)) return false;
            if (!MoveableHorizontally[1]) return false;

            // special case for sliding on triangle collider
            if (collision.RemoteCollider is PhysicsColliderTriangle)
            {
                return CanSlideRightTriangle(sign, collision, out amount) && amount > MIN_AMOUNT_THRESHOLD;
            }

            // special case for sliding on circle collider
            if (collision.RemoteCollider is PhysicsColliderCircle circle)
            {
                return CanSlideRightCircle(sign, collision, out amount) && amount > MIN_AMOUNT_THRESHOLD;
            }

            // typical case for sliding right on square collider
            float slide = collision.RemoteCollider.Right - collision.LocalCollider.Left;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideRightDist)
            {
                return amount > MIN_AMOUNT_THRESHOLD;
            }

            return false;
        }

        protected bool CanSlideRightTriangle(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1;
            PhysicsColliderTriangle triangle = collision.RemoteCollider as PhysicsColliderTriangle;
            Optional<float> gradient;

            List<PhysicsEdge> GetEdges()
            {
                List<PhysicsEdge> edges = new List<PhysicsEdge>();
                List<PhysicsEdge> edgesHor = triangle.RightEdges;
                List<PhysicsEdge> edgesVer = sign > 0 ? triangle.BottomEdges : triangle.TopEdges;

                for (int i = 0; i < edgesHor.Count; i++)
                {
                    for (int j = 0; j < edgesVer.Count; j++)
                    {
                        if (edgesHor[i].Is(edgesVer[j]))
                            edges.Add(edgesHor[i]);
                    }
                }

                return edges;
            }
            List<PhysicsEdge> edges = GetEdges();

            bool OneEdge(out float amount)
            {
                gradient = edges[0].Gradient;
                amount = !gradient.Enabled ? 1f : _slideSettings.SlideRightConversionCurve[sign].Evaluate(Mathf.Abs(gradient.Value));

                if (collision.LocalCollider.Left < edges[0].Left)
                    return collision.LocalCollider.Left - edges[0].Left <= _slideSettings.MaxSlideRightDist;

                return true;
            }

            bool TwoEdge(out float amount)
            {
                PhysicsEdge edgeLeft, edgeRight;

                if (edges[0].Left < edges[1].Left)
                {
                    edgeLeft = edges[0];
                    edgeRight = edges[1];
                }
                else
                {
                    edgeLeft = edges[1];
                    edgeRight = edges[0];
                }

                gradient = (collision.LocalCollider.Left < edgeRight.Left) ? edgeLeft.Gradient : edgeRight.Gradient;
                amount = !gradient.Enabled ? 1f : _slideSettings.SlideRightConversionCurve[sign].Evaluate(Mathf.Abs(gradient.Value));

                if (collision.LocalCollider.Left < edgeLeft.Left)
                    return collision.LocalCollider.Left - edgeLeft.Left <= _slideSettings.MaxSlideRightDist;

                return true;
            }

            if (edges.Count == 1) return OneEdge(out amount);
            if (edges.Count == 2) return TwoEdge(out amount);

            return Mathf.Abs(collision.LocalCollider.Left - collision.RemoteCollider.Right) <= _slideSettings.MaxSlideRightDist;
        }

        protected bool CanSlideRightCircle(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1f;
            PhysicsColliderCircle circle = collision.RemoteCollider as PhysicsColliderCircle;
            Vector2 position = new Vector2(collision.LocalCollider.Left, sign > 0 ? collision.LocalCollider.Top : collision.LocalCollider.Bottom);

            // account for local collider being a circle
            if (collision.LocalCollider is PhysicsColliderCircle localCircle)
            {
                Vector2 direction = (circle.Position - localCircle.Position).normalized;
                position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
            }

            return (position.x - circle.Position.x) >= 0;
        }

        protected bool CanSlideRight(float sign, List<PhysicsContact> collisions, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideRightMovingUp   && (_velocityVer > 0)) return false;
            if (!_slideSettings.CanSlideRightMovingDown && (_velocityVer < 0)) return false;
            if (!MoveableHorizontally[1]) return false;

            bool canSlide = true;
            foreach (PhysicsContact collision in collisions)
            {
                canSlide &= CanSlideRight(sign, collision, out float newAmount);
                amount = Mathf.Min(newAmount, amount);
            }

            return canSlide;
        }

        protected void PerformSlideRight(float amount = 1)
        {
            // perform slide right
            _slidingRight = amount > MIN_AMOUNT_THRESHOLD;

            // if previously moving leftward, cancel horizontal movement
            _xRounded = Math.Max(_xRounded, 0);
            _xRemainder = Math.Max(_xRemainder, 0f);
            _velocityHor = Math.Max(_velocityHor, 0f);

            // convert this vertical movement into horizontal movement
            _xRemainder += amount;
        }
        #endregion

        #region UpdateMoveable
        public override bool UpdateMoveableHorizontally(int sign)
        {
            // physics actors are non-moveable when they are attempting to move in any direction in which they collide with a solid, immoveable object, or currently non-moveable object
            // gravity actors are non-moveable when they are colliding with an object they cannot step upon or duck underneath

            float amount = 1;

            List<PhysicsContact> solids = CollidingHorizontally(sign, _nearbySolids);
            foreach (PhysicsContact contact in solids)
            {
                if (CanSlideUp  (sign, contact, out amount)) continue;
                if (CanSlideDown(sign, contact, out amount)) continue;
                return false;
            }

            List<PhysicsContact> actors = CollidingHorizontally(sign, _nearbyActors);
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;
                if (actor.Immoveable || !actor.MoveableHorizontally[sign])
                {
                    if (CanSlideUp  (sign, contact, out amount)) continue;
                    if (CanSlideDown(sign, contact, out amount)) continue;
                    return false;
                }
            }

            // no collisions found
            return true;
        }

        public override bool UpdateMoveableVertically(int sign)
        {
            // physics actors are non-moveable when they are attempting to move in any direction in which they collide with a solid, immoveable object, or currently non-moveable object
            // gravity actors are non-moveable when they are colliding with an object they cannot slide against

            float amount = 1;

            List<PhysicsContact> solids = CollidingVertically(sign, _nearbySolids);
            foreach (PhysicsContact contact in solids)
            {
                if (CanSlideLeft (sign, contact, out amount)) continue;
                if (CanSlideRight(sign, contact, out amount)) continue;
                return false;
            }

            List<PhysicsContact> actors = CollidingVertically(sign, _nearbyActors);
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;
                if (actor.Immoveable || !actor.MoveableVertically[sign])
                {
                    if (CanSlideLeft (sign, contact, out amount)) continue;
                    if (CanSlideRight(sign, contact, out amount)) continue;
                    return false;
                }
            }

            // no collisions found
            return true;
        }
        #endregion

        #region Tick
        public override void FixedTick()
        {
            FixedTickNearby();
            FixedTickMoveable();
            FixedTickSliding();
            FixedTickVelocity(_velocityHor, _velocityVer);
        }

        public virtual void FixedTickSliding()
        {
            _slidingUp    = false;
            _slidingDown  = false;
            _slidingLeft  = false;
            _slidingRight = false;
        }

        public override bool MoveHorizontally(float sign, Action<List<PhysicsContact>> CollideHorizontally)
        {
            _slidingUp = false;
            _slidingDown = false;

            float amount = 1;

            List<PhysicsContact> solids = CollidingHorizontally(sign, _nearbySolids);
            if (solids.Count > 0)
            {
                if (CanSlideUp(sign, solids, out amount))
                {
                    PerformSlideUp(amount);
                    return true;
                }

                if (CanSlideDown(sign, solids, out amount))
                {
                    PerformSlideDown(amount);
                    return true;
                }

                CollideHorizontally?.Invoke(solids);
                return false;
            }

            List<PhysicsContact> actors = CollidingHorizontally(sign, _nearbyActors);
            if (actors.Count > 0)
            {
                if (CanSlideUp(sign, actors, out amount))
                {
                    PerformSlideUp(amount);
                    return true;
                }

                if (CanSlideDown(sign, actors, out amount))
                {
                    PerformSlideDown(amount);
                    return true;
                }

                if (!_canPush)
                {
                    CollideHorizontally?.Invoke(actors);
                    return false;
                }

                return PushActorsHorizontally(actors, sign, CollideHorizontally);
            }

            Transform.position += new Vector3(sign * PhysicsManager.Instance.PhysicsStepSize, 0);
            return true;
        }

        public override bool MoveVertically(float sign, Action<List<PhysicsContact>> CollideVertically)
        {
            _slidingLeft = false;
            _slidingRight = false;

            float amount = 1;

            List<PhysicsContact> solids = CollidingVertically(sign, _nearbySolids);
            if (solids.Count > 0)
            {
                if (CanSlideLeft(sign, solids, out amount))
                {
                    PerformSlideLeft(amount);
                    return true;
                }

                if (CanSlideRight(sign, solids, out amount))
                {
                    PerformSlideRight(amount);
                    return true;
                }

                CollideVertically?.Invoke(solids);
                return false;
            }

            List<PhysicsContact> actors = CollidingVertically(sign, _nearbyActors);
            if (actors.Count > 0)
            {
                if (CanSlideLeft(sign, actors, out amount))
                {
                    PerformSlideLeft(amount);
                    return true;
                }

                if (CanSlideRight(sign, actors, out amount))
                {
                    PerformSlideRight(amount);
                    return true;
                }

                if (!_canPush)
                {
                    CollideVertically?.Invoke(actors);
                    return false;
                }

                return PushActorsVertically(actors, sign, CollideVertically);
            }

            Transform.position += new Vector3(0, sign * PhysicsManager.Instance.PhysicsStepSize);
            return true;
        }
        #endregion
    }
}