using PlasticPipe.PlasticProtocol.Messages;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VED.Physics;
using VED.Utilities;

namespace VED
{
    public class SlideActor : PhysicsActor
    {
        [Serializable]
        protected class SlideActorSettings
        {
            #region Up
            [SerializeField] public bool  CanSlideUpMovingLeft    = true;
            [SerializeField] public bool  CanSlideUpMovingRight   = true;
            [SerializeField] public float MaxSlideUpDist          = 0.7f;
            #endregion

            #region Down
            [SerializeField] public bool  CanSlideDownMovingLeft  = true;
            [SerializeField] public bool  CanSlideDownMovingRight = true;
            [SerializeField] public float MaxSlideDownDist        = 0.7f;
            #endregion

            #region Left
            [SerializeField] public bool  CanSlideLeftMovingUp    = true; // whether actor can slide horizontally, when moving up
            [SerializeField] public bool  CanSlideLeftMovingDown  = true; // whether actor can slide horizontally, when moving down, useful for actors with gravity
            [SerializeField] public float MaxSlideLeftDist        = 0.7f;
            #endregion

            #region Right
            [SerializeField] public bool  CanSlideRightMovingUp   = true; // whether actor can slide horizontally, when moving up
            [SerializeField] public bool  CanSlideRightMovingDown = true; // whether actor can slide horizontally, when moving down, useful for actors with gravity
            [SerializeField] public float MaxSlideRightDist       = 0.7f;
            #endregion

            [SerializeField] public Optional<float> GradientTriangle = new Optional<float>(1.7319f, false);
            [SerializeField] public Optional<float> GradientCircle   = new Optional<float>(5.6f   , false);
        }

        [SerializeField] protected SlideActorSettings _slideSettings = new SlideActorSettings();

        public bool SlidingUp => _slidingUp;
        protected bool _slidingUp = false;

        public bool SlidingDown => _slidingDown;
        protected bool _slidingDown = false;

        public bool SlidingLeft => _slidingLeft;
        protected bool _slidingLeft = false;

        public bool SlidingRight => _slidingRight;
        protected bool _slidingRight = false;
        public bool SlidingVertically => _slidingDown || _slidingUp;
        public bool SlidingHorizontally => _slidingLeft || _slidingRight;

        #region Up
        protected bool CanSlideUp(float sign, PhysicsContact collision)
        {
            if (!_slideSettings.CanSlideUpMovingLeft  && (sign < 0))  return false;
            if (!_slideSettings.CanSlideUpMovingRight && (sign > 0))  return false;
            if (SlidingHorizontally || !MoveableVertically[1]) return false;

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

                if (slide >= 0 && (!_slideSettings.GradientTriangle.Enabled || ((edge.B.x - edge.A.x) != 0 && Mathf.Abs(edge.Gradient) < _slideSettings.GradientTriangle.Value)))
                {
                    return true;
                }
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

                Vector2 line = Vector2.Perpendicular(position - collision.RemoteCollider.Position);
                float gradient = Mathf.Abs(line.y / line.x);

                if (slide >= 0 && (!_slideSettings.GradientCircle.Enabled || gradient == Mathf.Infinity || gradient < _slideSettings.GradientCircle.Value))
                {
                    return true;
                }
                Debug.Log(gradient);
            }

            // typical case for sliding up on square collider
            slide = collision.RemoteCollider.Top - collision.LocalCollider.Bottom;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideUpDist)
            {
                return true;
            }

            return false;
        }

        protected bool CanSlideUp(float sign, List<PhysicsContact> collisions)
        {
            if (!_slideSettings.CanSlideUpMovingLeft  && (_velocity.x < 0))  return false;
            if (!_slideSettings.CanSlideUpMovingRight && (_velocity.x > 0))  return false;
            if (SlidingHorizontally || !MoveableVertically[1]) return false;

            bool canStep = true;
            foreach (PhysicsContact collision in collisions)
            {
                canStep &= CanSlideUp(sign, collision);
            }

            return canStep;
        }

        protected void PerformSlideUp()
        {
            // perform slide up
            _slidingUp = true;

            // if previously moving downward, cancel vertical movement
            _yRounded = Math.Max(_yRounded, 0f);
            _yRemainder = Math.Max(_yRemainder, 0f);
            _velocity = new Vector2(_velocity.x, Math.Max(_velocity.y, 0f));

            // convert this horizontal movement into vertical movement
            _yRounded++;
        }
        #endregion

        #region Down
        protected bool CanSlideDown(float sign, PhysicsContact collision)
        {
            if (!_slideSettings.CanSlideDownMovingLeft  && (sign < 0)) return false;
            if (!_slideSettings.CanSlideDownMovingRight && (sign > 0)) return false;
            if (SlidingHorizontally || !MoveableVertically[-1]) return false;

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

                if (slide >= 0 && (!_slideSettings.GradientTriangle.Enabled || ((edge.B.x - edge.A.x) != 0 && Mathf.Abs(edge.Gradient) < _slideSettings.GradientTriangle.Value)))
                {
                    return true;
                }
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

                Vector2 line = Vector2.Perpendicular(position - collision.RemoteCollider.Position);
                float gradient = Mathf.Abs(line.y / line.x);

                if (slide >= 0 && (!_slideSettings.GradientCircle.Enabled || gradient == Mathf.Infinity || gradient < _slideSettings.GradientCircle.Value))
                {
                    return true;
                }
                Debug.Log(gradient);
            }

            // typical case for sliding up on square collider
            slide = collision.LocalCollider.Top - collision.RemoteCollider.Bottom;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideDownDist)
            {
                return true;
            }

            return false;
        }

        protected bool CanSlideDown(float sign, List<PhysicsContact> collisions)
        {
            if (!_slideSettings.CanSlideDownMovingLeft  && (_velocity.x < 0)) return false;
            if (!_slideSettings.CanSlideDownMovingRight && (_velocity.x > 0)) return false;
            if (SlidingHorizontally || !MoveableVertically[-1]) return false;

            bool canDuck = true;
            foreach (PhysicsContact collision in collisions)
            {
                canDuck &= CanSlideDown(sign, collision);
            }

            return canDuck;
        }

        protected void PerformSlideDown()
        {
            // perform slide down
            _slidingDown = true;

            // if previously moving upward, cancel vertical movement
            _yRounded = Math.Min(_yRounded, 0f);
            _yRemainder = Math.Min(_yRemainder, 0f);
            _velocity = new Vector2(_velocity.x, Math.Min(_velocity.y, 0f));

            // convert this horizontal movement into vertical movement
            _yRounded--;
        }
        #endregion

        #region Left
        protected bool CanSlideLeft(float sign, PhysicsContact collision)
        {
            if (!_slideSettings.CanSlideLeftMovingUp   && (sign > 0))   return false;
            if (!_slideSettings.CanSlideLeftMovingDown && (sign < 0))   return false;
            if (SlidingVertically || !MoveableHorizontally[-1]) return false;

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

                if (slide >= 0 && (!_slideSettings.GradientTriangle.Enabled || ((edge.B.x - edge.A.x) != 0 && Mathf.Abs(edge.Gradient) < _slideSettings.GradientTriangle.Value)))
                {
                    return true;
                }
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

                Vector2 line = Vector2.Perpendicular(position - collision.RemoteCollider.Position);
                float gradient = Mathf.Abs(line.y / line.x);

                if (slide >= 0 && (!_slideSettings.GradientCircle.Enabled || gradient == Mathf.Infinity || gradient < _slideSettings.GradientCircle.Value))
                {
                    return true;
                }
                Debug.Log(gradient);
            }

            // typical case for sliding left on square collider
            slide = collision.LocalCollider.Right - collision.RemoteCollider.Left;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideLeftDist)
            {
                return true;
            }

            return false;
        }

        protected bool CanSlideLeft(float sign, List<PhysicsContact> collisions)
        {
            if (!_slideSettings.CanSlideLeftMovingUp   && (_velocity.y > 0))   return false;
            if (!_slideSettings.CanSlideLeftMovingDown && (_velocity.y < 0))   return false;
            if (SlidingVertically || !MoveableHorizontally[-1]) return false;

            bool canSlide = true;
            foreach (PhysicsContact collision in collisions)
            {
                canSlide &= CanSlideLeft(sign, collision);
            }

            return canSlide;
        }

        protected void PerformSlideLeft()
        {
            // perform slide left
            _slidingLeft = true;

            // if previously moving right, cancel horizontal movement
            _xRounded = Math.Min(_xRounded, 0f);
            _xRemainder = Math.Min(_xRemainder, 0f);
            _velocity = new Vector2(Math.Min(_velocity.x, 0f), _velocity.y);

            // convert this vertical movement into horizontal movement
            _xRounded--;
        }
        #endregion

        #region Right
        protected bool CanSlideRight(float sign, PhysicsContact collision)
        {
            if (!_slideSettings.CanSlideRightMovingUp   && (sign > 0))  return false;
            if (!_slideSettings.CanSlideRightMovingDown && (sign < 0))  return false;
            if (SlidingVertically || !MoveableHorizontally[1]) return false;

            // try to slide to the right of other collider
            float slide = collision.RemoteCollider.Right - collision.LocalCollider.Left;

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

                if (slide >= 0 && (!_slideSettings.GradientTriangle.Enabled || ((edge.B.x - edge.A.x) != 0 && Mathf.Abs(edge.Gradient) < _slideSettings.GradientTriangle.Value)))
                {
                    return true;
                }
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

                Vector2 line = Vector2.Perpendicular(position - collision.RemoteCollider.Position);
                float gradient = Mathf.Abs(line.y / line.x);

                if (slide >= 0 && (!_slideSettings.GradientCircle.Enabled || gradient == Mathf.Infinity || gradient < _slideSettings.GradientCircle.Value))
                {
                    return true;
                }
                Debug.Log(gradient);
            }

            // typical case for sliding right on square collider
            slide = collision.RemoteCollider.Right - collision.LocalCollider.Left;
            if (slide >= 0 && slide <= _slideSettings.MaxSlideRightDist)
            {
                return true;
            }

            return false;
        }

        protected bool CanSlideRight(float sign, List<PhysicsContact> collisions)
        {
            if (!_slideSettings.CanSlideRightMovingUp   && (_velocity.y > 0))  return false;
            if (!_slideSettings.CanSlideRightMovingDown && (_velocity.y < 0))  return false;
            if (SlidingVertically || !MoveableHorizontally[1]) return false;

            bool canSlide = true;
            foreach (PhysicsContact collision in collisions)
            {
                canSlide &= CanSlideRight(sign, collision);
            }

            return canSlide;
        }

        protected void PerformSlideRight()
        {
            // perform slide right
            _slidingRight = true;

            // if previously moving leftward, cancel horizontal movement
            _xRounded = Math.Max(_xRounded, 0);
            _xRemainder = Math.Max(_xRemainder, 0f);
            _velocity = new Vector2(Math.Max(_velocity.x, 0f), _velocity.y);

            // convert this vertical movement into horizontal movement
            _xRounded++;
        }
        #endregion

        #region UpdateMoveable
        protected override bool UpdateMoveableHorizontally(int sign)
        {
            // physics actors are non-moveable when they are attempting to move in any direction in which they collide with a solid, immoveable object, or currently non-moveable object
            // gravity actors are non-moveable when they are colliding with an object they cannot step upon or duck underneath

            List<PhysicsContact> solids = CollidingHorizontally(sign, _nearbySolids);
            foreach (PhysicsContact contact in solids)
            {
                if (CanSlideUp  (sign, contact)) continue;
                if (CanSlideDown(sign, contact)) continue;
                return false;
            }

            List<PhysicsContact> actors = CollidingHorizontally(sign, _nearbyActors);
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;
                if (actor.Immoveable || !actor.MoveableHorizontally[sign])
                {
                    if (CanSlideUp  (sign, contact)) continue;
                    if (CanSlideDown(sign, contact)) continue;
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

            List<PhysicsContact> solids = CollidingVertically(sign, _nearbySolids);
            foreach (PhysicsContact contact in solids)
            {
                if (CanSlideLeft (sign, contact)) continue;
                if (CanSlideRight(sign, contact)) continue;
                return false;
            }

            List<PhysicsContact> actors = CollidingVertically(sign, _nearbyActors);
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;
                if (actor.Immoveable || !actor.MoveableVertically[sign])
                {
                    if (CanSlideLeft (sign, contact)) continue;
                    if (CanSlideRight(sign, contact)) continue;
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
            _slidingUp = false;
            _slidingDown = false;
            _slidingLeft = false;
            _slidingRight = false;

            base.FixedTick();
        }

        protected override bool MoveHorizontally(float sign, Action<List<PhysicsContact>> CollideHorizontally)
        {
            _slidingUp = false;
            _slidingDown = false;

            List<PhysicsContact> solids = CollidingHorizontally(sign, _nearbySolids);
            if (solids.Count > 0)
            {
                if (CanSlideUp(sign, solids))
                {
                    PerformSlideUp();
                    return true;
                }

                if (CanSlideDown(sign, solids))
                {
                    PerformSlideDown();
                    return true;
                }

                CollideHorizontally?.Invoke(solids);
                return false;
            }

            List<PhysicsContact> actors = CollidingHorizontally(sign, _nearbyActors);
            if (actors.Count > 0)
            {
                if (CanSlideUp(sign, actors))
                {
                    PerformSlideUp();
                    return true;
                }

                if (CanSlideDown(sign, actors))
                {
                    PerformSlideDown();
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

            List<PhysicsContact> solids = CollidingVertically(sign, _nearbySolids);
            if (solids.Count > 0)
            {
                if (CanSlideLeft(sign, solids))
                {
                    PerformSlideLeft();
                    return true;
                }

                if (CanSlideRight(sign, solids))
                {
                    PerformSlideRight();
                    return true;
                }

                CollideVertically?.Invoke(solids);
                return false;
            }

            List<PhysicsContact> actors = CollidingVertically(sign, _nearbyActors);
            if (actors.Count > 0)
            {
                if (CanSlideLeft(sign, actors))
                {
                    PerformSlideLeft();
                    return true;
                }

                if (CanSlideRight(sign, actors))
                {
                    PerformSlideRight();
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
