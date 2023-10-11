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
            #region Up
            [SerializeField] public bool CanSlideUpMovingLeft            = true; // whether actor can slide up, when moving left
            [SerializeField] public bool CanSlideUpMovingRight           = true; // whether actor can slide up, when moving right
            [SerializeField] public float MaxSlideUpDist                 = 0.2f;
            [SerializeField] public Vector2 GradientSlideUpMovingLeft    = new Vector2(-2.00f, 0.00f);
            [SerializeField] public Vector2 GradientSlideUpMovingRight   = new Vector2( 0.00f, 2.00f);
            #endregion

            #region Down
            [SerializeField] public bool CanSlideDownMovingLeft          = true; // whether actor can slide down, when moving left
            [SerializeField] public bool CanSlideDownMovingRight         = true; // whether actor can slide down, when moving right
            [SerializeField] public float MaxSlideDownDist               = 0.2f;
            [SerializeField] public Vector2 GradientSlideDownMovingLeft  = new Vector2( 0.00f, 2.00f);
            [SerializeField] public Vector2 GradientSlideDownMovingRight = new Vector2(-2.00f, 0.00f);
            #endregion

            #region Left
            [SerializeField] public bool CanSlideLeftMovingUp            = true; // whether actor can slide left, when moving up
            [SerializeField] public bool CanSlideLeftMovingDown          = true; // whether actor can slide left, when moving down, useful for actors with gravity
            [SerializeField] public float MaxSlideLeftDist               = 0.2f;
            [SerializeField] public Vector2 GradientSlideLeftMovingUp    = new Vector2(-2.00f, -0.50f);
            [SerializeField] public Vector2 GradientSlideLeftMovingDown  = new Vector2( 1.50f,  float.MaxValue);
            #endregion

            #region Right
            [SerializeField] public bool CanSlideRightMovingUp           = true; // whether actor can slide right, when moving up
            [SerializeField] public bool CanSlideRightMovingDown         = true; // whether actor can slide right, when moving down, useful for actors with gravity
            [SerializeField] public float MaxSlideRightDist              = 0.2f;
            [SerializeField] public Vector2 GradientSlideRightMovingUp   = new Vector2( 0.50f,  2.00f);
            [SerializeField] public Vector2 GradientSlideRightMovingDown = new Vector2(float.MinValue, -1.50f);
            #endregion
        }

        [SerializeField] protected SlideActorSettings _slideSettings = new SlideActorSettings();

        private const float MAX_CONVERSION_THRESHOLD = 6.0f; // the max value to consider when converting momentum between directions
        private const float MIN_CONVERSION_VALUE     = 0.5f; // the min conversion rate between directions during a slide

        public bool SlidingUp => _slidingUp;
        [SerializeField, ReadOnly] protected bool _slidingUp = false;

        public bool SlidingDown => _slidingDown;
        [SerializeField, ReadOnly] protected bool _slidingDown = false;

        public bool SlidingLeft => _slidingLeft;
        [SerializeField, ReadOnly] protected bool _slidingLeft = false;

        public bool SlidingRight => _slidingRight;
        [SerializeField, ReadOnly] protected bool _slidingRight = false;

        public bool SlidingVertically => _slidingDown || _slidingUp;
        public bool SlidingHorizontally => _slidingLeft || _slidingRight;

        #region Up
        protected bool CanSlideUp(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideUpMovingLeft  && (sign < 0))  return false;
            if (!_slideSettings.CanSlideUpMovingRight && (sign > 0))  return false;
            if (!MoveableVertically[1]) return false;

            // try to slide up on other collider
            float slide;

            // special case for sliding up on triangle collider
            if (collision.RemoteCollider is PhysicsColliderTriangle triangle)
            {
                PhysicsEdge edge = new PhysicsEdge(sign > 0 ? triangle.LeftPoint : triangle.RightPoint, triangle.TopPoint);
                slide = collision.LocalCollider.Bottom - edge.A.y;

                // account for local collider being a circle
                if (collision.LocalCollider is PhysicsColliderCircle localCircle)
                {
                    Vector2 direction = (edge.A - localCircle.Position).normalized;
                    Vector2 position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
                    slide = position.y - edge.A.y;
                }

                float gradient = edge.Inverse().Gradient;
                bool gradientMovingLeft  = sign < 0 && gradient > _slideSettings.GradientSlideUpMovingLeft.x  && gradient < _slideSettings.GradientSlideUpMovingLeft.y;
                bool gradientMovingRight = sign > 0 && gradient > _slideSettings.GradientSlideUpMovingRight.x && gradient < _slideSettings.GradientSlideUpMovingRight.y;

                if (slide >= 0 && (gradientMovingLeft || gradientMovingRight))
                {
                    if (sign < 0)
                    {
                        float min = Mathf.Min(_slideSettings.GradientSlideUpMovingLeft.x, -MAX_CONVERSION_THRESHOLD);
                        float max = Mathf.Max(_slideSettings.GradientSlideUpMovingLeft.y,  MAX_CONVERSION_THRESHOLD);
                        amount = Mathf.Lerp(MIN_CONVERSION_VALUE, 1f, Mathf.InverseLerp(min, max, gradient));
                    }
                    else
                    {
                        float min = Mathf.Min(_slideSettings.GradientSlideUpMovingRight.x, -MAX_CONVERSION_THRESHOLD);
                        float max = Mathf.Max(_slideSettings.GradientSlideUpMovingRight.y,  MAX_CONVERSION_THRESHOLD);
                        amount = 1f - Mathf.Lerp(0f, MIN_CONVERSION_VALUE, Mathf.InverseLerp(min, max, gradient));
                    }
                    return true;
                }

                return false;
            }

            // special case for sliding up on circle collider
            if (collision.RemoteCollider is PhysicsColliderCircle circle)
            {
                Vector2 position = new Vector2(sign > 0 ? collision.LocalCollider.Right : collision.LocalCollider.Left, collision.LocalCollider.Bottom);

                // account for local collider being a circle
                if (collision.LocalCollider is PhysicsColliderCircle localCircle)
                {
                    Vector2 direction = (circle.Position - localCircle.Position).normalized;
                    position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
                }

                slide = position.y - circle.Position.y;

                if (slide >= 0)
                {
                    return true;
                }
            }

            // typical case for sliding up on square collider
            slide = collision.RemoteCollider.Top - collision.LocalCollider.Bottom;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideUpDist)
            {
                return true;
            }

            return false;
        }

        protected bool CanSlideUp(float sign, List<PhysicsContact> collisions, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideUpMovingLeft  && (_velocity.x < 0))  return false;
            if (!_slideSettings.CanSlideUpMovingRight && (_velocity.x > 0))  return false;
            if (!MoveableVertically[1]) return false;

            bool canSlide = true;

            foreach (PhysicsContact collision in collisions)
            {
                canSlide &= CanSlideUp(sign, collision, out float newAmount);
                amount = Mathf.Min(Mathf.Clamp01(newAmount), amount);
            }

            return canSlide;
        }

        protected void PerformSlideUp(float amount = 1)
        {
            // perform slide up
            _slidingUp = true;

            // if previously moving downward, cancel vertical movement
            _yRounded = Math.Max(_yRounded, 0f);
            _yRemainder = Math.Max(_yRemainder, 0f);
            _velocity = new Vector2(_velocity.x, Math.Max(_velocity.y, 0f));

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

            // try to slide underneath other collider
            float slide;

            // special case for sliding under triangle collider
            if (collision.RemoteCollider is PhysicsColliderTriangle triangle)
            {
                PhysicsEdge edge = new PhysicsEdge(sign > 0 ? triangle.LeftPoint : triangle.RightPoint, triangle.BottomPoint);
                slide = edge.A.y - collision.LocalCollider.Top;

                // account for local collider being a circle
                if (collision.LocalCollider is PhysicsColliderCircle localCircle)
                {
                    Vector2 direction = (edge.A - localCircle.Position).normalized;
                    Vector2 position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
                    slide = edge.A.y - position.y;
                }

                float gradient = edge.Inverse().Gradient; 
                bool gradientMovingLeft  = sign < 0 && gradient > _slideSettings.GradientSlideDownMovingLeft.x  && gradient < _slideSettings.GradientSlideDownMovingLeft.y;
                bool gradientMovingRight = sign > 0 && gradient > _slideSettings.GradientSlideDownMovingRight.x && gradient < _slideSettings.GradientSlideDownMovingRight.y;

                if (slide >= 0 && (gradientMovingLeft || gradientMovingRight))
                {
                    if (sign < 0)
                    {
                        float min = Mathf.Min(_slideSettings.GradientSlideDownMovingLeft.x, -MAX_CONVERSION_THRESHOLD);
                        float max = Mathf.Max(_slideSettings.GradientSlideDownMovingLeft.y,  MAX_CONVERSION_THRESHOLD);
                        amount = 1f - Mathf.Lerp(0f, MIN_CONVERSION_VALUE, Mathf.InverseLerp(min, max, gradient));
                    }
                    else
                    {
                        float min = Mathf.Min(_slideSettings.GradientSlideDownMovingRight.x, -MAX_CONVERSION_THRESHOLD);
                        float max = Mathf.Max(_slideSettings.GradientSlideDownMovingRight.y,  MAX_CONVERSION_THRESHOLD);
                        amount = Mathf.Lerp(MIN_CONVERSION_VALUE, 1f, Mathf.InverseLerp(min, max, gradient));
                    }
                    return true;
                }

                return false;
            }

            // special case for sliding under circle collider
            if (collision.RemoteCollider is PhysicsColliderCircle circle)
            {
                Vector2 position = new Vector2(sign > 0 ? collision.LocalCollider.Right : collision.LocalCollider.Left, collision.LocalCollider.Top);

                // account for local collider being a circle
                if (collision.LocalCollider is PhysicsColliderCircle localCircle)
                {
                    Vector2 direction = (circle.Position - localCircle.Position).normalized;
                    position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
                }

                slide = circle.Position.y - position.y;

                if (slide >= 0)
                {
                    return true;
                }
            }

            // typical case for sliding up on square collider
            slide = collision.LocalCollider.Top - collision.RemoteCollider.Bottom;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideDownDist)
            {
                return true;
            }

            return false;
        }

        protected bool CanSlideDown(float sign, List<PhysicsContact> collisions, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideDownMovingLeft  && (_velocity.x < 0)) return false;
            if (!_slideSettings.CanSlideDownMovingRight && (_velocity.x > 0)) return false;
            if (!MoveableVertically[-1]) return false;

            bool canSlide = true;
            foreach (PhysicsContact collision in collisions)
            {
                canSlide &= CanSlideDown(sign, collision, out float newAmount);
                amount = Mathf.Min(Mathf.Clamp01(newAmount), amount);
            }

            return canSlide;
        }

        protected void PerformSlideDown(float amount = 1)
        {
            // perform slide down
            _slidingDown = true;

            // if previously moving upward, cancel vertical movement
            _yRounded = Math.Min(_yRounded, 0f);
            _yRemainder = Math.Min(_yRemainder, 0f);
            _velocity = new Vector2(_velocity.x, Math.Min(_velocity.y, 0f));

            // convert this horizontal movement into vertical movement
            _yRemainder -= amount;
        }
        #endregion

        #region Left
        protected bool CanSlideLeft(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideLeftMovingUp   && (sign > 0))   return false;
            if (!_slideSettings.CanSlideLeftMovingDown && (sign < 0))   return false;
            if (!MoveableHorizontally[-1]) return false;

            // try to slide to the left of other collider
            float slide;

            // special case for sliding on triangle collider
            if (collision.RemoteCollider is PhysicsColliderTriangle triangle)
            {
                PhysicsEdge edge = new PhysicsEdge(triangle.LeftPoint, sign > 0 ? triangle.BottomPoint : triangle.TopPoint);
                slide = edge.B.x - collision.LocalCollider.Right;

                // account for local collider being a circle
                if (collision.LocalCollider is PhysicsColliderCircle localCircle)
                {
                    Vector2 direction = (edge.B - localCircle.Position).normalized;
                    Vector2 position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
                    slide = edge.B.x - position.x;
                }

                float gradient = edge.Gradient;
                bool gradientMovingUp    = sign > 0 && gradient > _slideSettings.GradientSlideLeftMovingUp.x   && gradient < _slideSettings.GradientSlideLeftMovingUp.y;
                bool gradientMovingDown  = sign < 0 && gradient > _slideSettings.GradientSlideLeftMovingDown.x && gradient < _slideSettings.GradientSlideLeftMovingDown.y;

                if (slide >= 0 && (gradientMovingUp || gradientMovingDown))
                {
                    if (sign > 0)
                    {
                        float min = Mathf.Min(_slideSettings.GradientSlideLeftMovingUp.x, -MAX_CONVERSION_THRESHOLD);
                        float max = Mathf.Max(_slideSettings.GradientSlideLeftMovingUp.y,  MAX_CONVERSION_THRESHOLD);
                        amount = 1f - Mathf.Lerp(0f, MIN_CONVERSION_VALUE, Mathf.InverseLerp(min, max, gradient));
                    }
                    else
                    {
                        float min = Mathf.Min(_slideSettings.GradientSlideLeftMovingDown.x, -MAX_CONVERSION_THRESHOLD);
                        float max = Mathf.Max(_slideSettings.GradientSlideLeftMovingDown.y,  MAX_CONVERSION_THRESHOLD);
                        amount = Mathf.Lerp(MIN_CONVERSION_VALUE, 1f, Mathf.InverseLerp(min, max, gradient));
                    }
                    return true;
                }

                return false;
            }

            // special case for sliding on circle collider
            if (collision.RemoteCollider is PhysicsColliderCircle circle)
            {
                Vector2 position = new Vector2(collision.LocalCollider.Right, sign > 0 ? collision.LocalCollider.Top : collision.LocalCollider.Bottom);

                // account for local collider being a circle
                if (collision.LocalCollider is PhysicsColliderCircle localCircle)
                {
                    Vector2 direction = (circle.Position - localCircle.Position).normalized;
                    position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
                }

                slide = circle.Position.x - position.x;

                if (slide >= 0)
                {
                    return true;
                }
            }

            // typical case for sliding left on square collider
            slide = collision.LocalCollider.Right - collision.RemoteCollider.Left;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideLeftDist)
            {
                return true;
            }

            return false;
        }

        protected bool CanSlideLeft(float sign, List<PhysicsContact> collisions, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideLeftMovingUp   && (_velocity.y > 0))   return false;
            if (!_slideSettings.CanSlideLeftMovingDown && (_velocity.y < 0))   return false;
            if (!MoveableHorizontally[-1]) return false;

            bool canSlide = true;
            foreach (PhysicsContact collision in collisions)
            {
                canSlide &= CanSlideLeft(sign, collision, out float newAmount);
                amount = Mathf.Min(Mathf.Clamp01(newAmount), amount);
            }

            return canSlide;
        }

        protected void PerformSlideLeft(float amount = 1)
        {
            // perform slide left
            _slidingLeft = true;

            // if previously moving right, cancel horizontal movement
            _xRounded = Math.Min(_xRounded, 0f);
            _xRemainder = Math.Min(_xRemainder, 0f);
            _velocity = new Vector2(Math.Min(_velocity.x, 0f), _velocity.y);

            // convert this vertical movement into horizontal movement
            _xRemainder -= amount;
        }
        #endregion

        #region Right
        protected bool CanSlideRight(float sign, PhysicsContact collision, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideRightMovingUp   && (sign > 0))  return false;
            if (!_slideSettings.CanSlideRightMovingDown && (sign < 0))  return false;
            if (!MoveableHorizontally[1]) return false;

            // try to slide to the right of other collider
            float slide;

            // special case for sliding on triangle collider
            if (collision.RemoteCollider is PhysicsColliderTriangle triangle)
            {
                PhysicsEdge edge = new PhysicsEdge(triangle.RightPoint, sign > 0 ? triangle.BottomPoint : triangle.TopPoint);
                slide = collision.LocalCollider.Left - edge.B.x;

                // account for local collider being a circle
                if (collision.LocalCollider is PhysicsColliderCircle localCircle)
                {
                    Vector2 direction = (edge.B - localCircle.Position).normalized;
                    Vector2 position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
                    slide = position.x - edge.B.x;
                }

                float gradient = edge.Gradient;
                bool gradientMovingUp   = sign > 0 && gradient > _slideSettings.GradientSlideRightMovingUp.x   && gradient < _slideSettings.GradientSlideRightMovingUp.y;
                bool gradientMovingDown = sign < 0 && gradient > _slideSettings.GradientSlideRightMovingDown.x && gradient < _slideSettings.GradientSlideRightMovingDown.y;

                if (slide >= 0 && (gradientMovingUp || gradientMovingDown))
                {
                    if (sign > 0)
                    {
                        float min = Mathf.Min(_slideSettings.GradientSlideRightMovingUp.x, -MAX_CONVERSION_THRESHOLD);
                        float max = Mathf.Max(_slideSettings.GradientSlideRightMovingUp.y,  MAX_CONVERSION_THRESHOLD);
                        amount = Mathf.Lerp(MIN_CONVERSION_VALUE, 1f, Mathf.InverseLerp(min, max, gradient));
                    }
                    else
                    {
                        float min = Mathf.Min(_slideSettings.GradientSlideRightMovingDown.x, -MAX_CONVERSION_THRESHOLD);
                        float max = Mathf.Max(_slideSettings.GradientSlideRightMovingDown.y,  MAX_CONVERSION_THRESHOLD);
                        amount = 1f - Mathf.Lerp(0f, MIN_CONVERSION_VALUE, Mathf.InverseLerp(min, max, gradient));
                    }
                    return true;
                }

                return false;
            }

            // special case for sliding on circle collider
            if (collision.RemoteCollider is PhysicsColliderCircle circle)
            {
                Vector2 position = new Vector2(collision.LocalCollider.Left, sign > 0 ? collision.LocalCollider.Top : collision.LocalCollider.Bottom);
                
                // account for local collider being a circle
                if (collision.LocalCollider is PhysicsColliderCircle localCircle)
                {
                    Vector2 direction = (circle.Position - localCircle.Position).normalized;
                    position = (localCircle.Position + direction * localCircle.Radius) - (direction * COLLISION_ERROR_MARGIN);
                }

                slide = position.x - circle.Position.x;

                if (slide >= 0)
                {
                    return true;
                }
            }

            // typical case for sliding right on square collider
            slide = collision.RemoteCollider.Right - collision.LocalCollider.Left;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideRightDist)
            {
                return true;
            }

            return false;
        }

        protected bool CanSlideRight(float sign, List<PhysicsContact> collisions, out float amount)
        {
            amount = 1;
            if (!_slideSettings.CanSlideRightMovingUp   && (_velocity.y > 0))  return false;
            if (!_slideSettings.CanSlideRightMovingDown && (_velocity.y < 0))  return false;
            if (!MoveableHorizontally[1]) return false;

            bool canSlide = true;
            foreach (PhysicsContact collision in collisions)
            {
                canSlide &= CanSlideRight(sign, collision, out float newAmount);
                amount = Mathf.Min(Mathf.Clamp01(newAmount), amount);
            }

            return canSlide;
        }

        protected void PerformSlideRight(float amount = 1)
        {
            // perform slide right
            _slidingRight = true;

            // if previously moving leftward, cancel horizontal movement
            _xRounded = Math.Max(_xRounded, 0);
            _xRemainder = Math.Max(_xRemainder, 0f);
            _velocity = new Vector2(Math.Max(_velocity.x, 0f), _velocity.y);

            // convert this vertical movement into horizontal movement
            _xRemainder += amount;
        }
        #endregion

        #region UpdateMoveable
        protected override bool UpdateMoveableHorizontally(int sign)
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

        protected override bool UpdateMoveableVertically(int sign)
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

        #region Move
        public override void FixedTick()
        {
            TickNearby();
            TickMoveable();
            TickSliding();
            TickVelocity(_velocity.x, _velocity.y);
        }

        protected virtual void TickSliding()
        {
            _slidingUp = false;
            _slidingDown = false;
            _slidingLeft = false;
            _slidingRight = false;
        }

        protected override bool MoveHorizontally(float sign, Action<List<PhysicsContact>> CollideHorizontally)
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

        protected override bool MoveVertically(float sign, Action<List<PhysicsContact>> CollideVertically)
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
