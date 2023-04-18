using System;
using System.Collections.Generic;
using UnityEngine;
using VED.Utilities;

namespace VED.Physics
{
    public class GravityActor : SlideActor
    {
        [Range(-1, 1)]
        [SerializeField] protected float _gravity = 1f;

        [SerializeField] protected bool _groundEnabled = true;
        public bool Grounded => _groundContact != null;
#pragma warning disable CS0414
        [SerializeField, ReadOnly] private bool _grounded = false;
#pragma warning restore CS0414

        [SerializeField] protected PhysicsCollider _groundCollider = null;

        public PhysicsContact GroundContact => _groundContact;
        protected PhysicsContact _groundContact = null;

        protected float Friction
        {
            get
            {
                float friction = _physicsMaterial.Friction;
                if (_groundContact != null) friction *= _groundContact.RemoteObject.PhysicsMaterial.Friction;
                else friction *= PhysicsManager.Instance.AtmospherePhysicsMaterial.Friction;

                return friction;
            }
        }

        protected float Traction
        {
            get
            {
                float traction = _physicsMaterial.Traction;
                if (_groundContact != null) traction *= _groundContact.RemoteObject.PhysicsMaterial.Traction;
                else traction *= PhysicsManager.Instance.AtmospherePhysicsMaterial.Traction;

                return traction;
            }
        }

        protected float Elasticity
        {
            get
            {
                float elasticity = _physicsMaterial.Elasticity;
                if (_groundContact != null) elasticity *= _groundContact.RemoteObject.PhysicsMaterial.Elasticity;
                else elasticity *= PhysicsManager.Instance.AtmospherePhysicsMaterial.Elasticity;

                return elasticity;
            }
        }

        #region Init
        public override void Init()
        {
            base.Init();
            InitGroundCollider();
        }

        protected void InitGroundCollider()
        {
            if (_groundCollider != null) return;

            PhysicsCollider lowestCollider = _colliders[0];

            foreach (PhysicsCollider collider in _colliders)
            {
                if (collider.Bottom < lowestCollider.Bottom)
                {
                    lowestCollider = collider;
                }
            }

            _groundCollider = lowestCollider;
        }
        #endregion

        #region Tick
        public override void FixedTick()
        {
            base.FixedTick();
            TickGravity();
            TickGrounded();
            TickVelocity(_velocity.x, _velocity.y);
            TickFriction();
            TickInheritedMovement();
        }

        public override void FixedSubTick()
        {
            SubTickMove(CollideHorizontally, CollideVertically);
        }

        protected virtual void TickInheritedMovement()
        {
            if (_groundContact != null && _groundContact.RemoteObject is PhysicsActor actor)
            {
                if (Mathf.Abs(actor.Velocity.x) > Mathf.Abs(_velocity.x))
                {
                    float strength = Mathf.Clamp01(actor.Strength / _weight);
                    float difference = actor.Velocity.x - _velocity.x;

                    _velocity.x += strength * difference;
                }

                if (_groundContact.RemoteObject.Velocity.y > 0 && _groundContact.RemoteObject.Velocity.y > _velocity.y)
                {
                    float strength = Mathf.Clamp01(actor.Strength / _weight);
                    float difference = actor.Velocity.y - _velocity.y;

                    _velocity.y += strength * difference;
                }
            }
        }

        protected virtual void TickGravity()
        {
            // reduce y velocity by (weight x gravity)
            float applied = _slidingUp ? 0 : 1;
            _velocity.y -= applied * (_weight * _gravity * PhysicsManager.Instance.Gravity);
        }

        protected virtual void TickFriction()
        {
            float friction = Friction;

            // apply horizontal friction (velocity -= sign x clamp(weight x friction x velocity, 0, velocity))
            // never remove more than (velocity) as this will cause the actor to move in the opposite direction
            _velocity.x -= Mathf.Sign(_velocity.x) * Mathf.Clamp((GetTotalWeight() * friction * Mathf.Abs(_velocity.x)), 0, Mathf.Abs(_velocity.x));

            // apply vertical friction (velocity -= sign x clamp(weight x friction x velocity, 0, velocity))
            // never remove more than (velocity) as this will cause the actor to move in the opposite direction
            _velocity.y -= Mathf.Sign(_velocity.y) * Mathf.Clamp((_weight * friction * Mathf.Abs(_velocity.y)), 0, Mathf.Abs(_velocity.y));
        }

        protected virtual void TickGrounded()
        {
            if (!_groundEnabled) return;

            PhysicsContact contact = null;

            foreach (PhysicsObject remoteObject in _nearby)
            {
                if (_ignored.Contains(remoteObject)) continue;
                foreach (PhysicsCollider remoteCollider in remoteObject.Colliders)
                {
                    if (_groundCollider.CollidingVertically(-1f, remoteCollider))
                    {
                        contact = new PhysicsContact(this, _groundCollider, remoteObject, remoteCollider);
                    }
                }
            }

            if (!Grounded && contact != null && _velocity.y <= 0)
            {
                // ground actor
                Ground(contact);
            }
            else if (Grounded && contact == null && !_slidingUp)
            {
                // unground actor
                Unground();
            }
        }
        #endregion

        #region Ground
        protected virtual void Ground(PhysicsContact ground)
        {
            if (!_groundEnabled) return;

            // detach current ground if not null
            Unground();

            // attach ground
            ground.RemoteObject.Attach(ground.Invert());
            _groundContact = ground;
            _grounded = true;
        }

        public virtual void Unground()
        {
            // detach ground
            if (_groundContact != null)
            {
                _groundContact.RemoteObject.Detach(this);
                _groundContact = null;
            }

            _grounded = false;
        }
        #endregion

        #region Collision
        protected virtual void CollideHorizontally(List<PhysicsContact> contacts)
        {
            BounceHorizontally(contacts);
        }

        protected virtual void CollideVertically(List<PhysicsContact> contacts)
        {
            if (SlidingVertically)
            {
                _velocity = Vector2.zero;
            }

            BounceVertically(contacts);
        }

        protected void BounceHorizontally(List<PhysicsContact> collisions)
        {
            float sign = Mathf.Sign(_velocity.x);
            float elasticity = Elasticity;

            foreach (PhysicsContact collision in collisions)
            {
                if (collision != _groundContact)
                {
                    elasticity *= collision.RemoteObject.PhysicsMaterial.Elasticity;
                }
            }

            _velocity.x += -sign * Mathf.Abs(_velocity.x * elasticity);
        }

        protected void BounceVertically(List<PhysicsContact> collisions)
        {
            float sign = Mathf.Sign(_velocity.y);

            if (sign == -1)
            {
                TickGrounded();
            }

            float elasticity = Elasticity;

            foreach (PhysicsContact collision in collisions)
            {
                if (collision != _groundContact)
                {
                    elasticity *= collision.RemoteObject.PhysicsMaterial.Elasticity;
                }
            }

            _velocity.y = -sign * Mathf.Abs(_velocity.y * elasticity);
        }
        #endregion

        public override void ApplyImpulse(Vector2 impulse)
        {
            Unground();
            base.ApplyImpulse(impulse);
        }
    }
}