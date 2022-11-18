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

        public bool Grounded => _groundContact != null;
        [SerializeField, ReadOnly] private bool _grounded = false;

        protected PhysicsCollider _groundCollider = null;

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
            InitGroundContact();
        }

        protected virtual void InitGroundContact()
        {
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
            UpdateGravity();
            UpdateGrounded();
            UpdateMovement();
            UpdateFriction();
            UpdateInheritedMovement();
        }

        protected virtual void UpdateInheritedMovement()
        {
            if (_groundContact != null && _groundContact.RemoteObject is PhysicsActor actor)
            {
                float friction = Friction;

                if (Mathf.Abs(actor.Velocity.x) > Mathf.Abs(_velocity.x))
                {
                    float strength = Mathf.Clamp01(_weight / actor.Weight);
                    float magnitude = Mathf.Min(Mathf.Abs(actor.Velocity.x - _velocity.x), Mathf.Abs(actor.Velocity.x));
                    float sign = Mathf.Sign(actor.Velocity.x);

                    _velocity.x += sign * strength * magnitude * friction;
                }

                if (_groundContact.RemoteObject.Velocity.y > 0 && _groundContact.RemoteObject.Velocity.y >= _velocity.y)
                {
                    float strength = Mathf.Clamp01(_weight / actor.Weight);
                    float magnitude = Mathf.Min(Mathf.Abs(actor.Velocity.y - _velocity.y), Mathf.Abs(actor.Velocity.y));
                    float sign = Mathf.Sign(actor.Velocity.y);

                    _velocity.y += sign * strength * magnitude;
                }
            }
        }

        protected virtual void UpdateMovement()
        {
            Move(_velocity.x, _velocity.y, CollideHorizontally, CollideVertically);
        }

        protected virtual void UpdateGravity()
        {
            // reduce y velocity by (weight x gravity)
            float applied = _slidingUp ? 0 : 1;
            _velocity.y -= applied * (_weight * _gravity * PhysicsManager.Instance.Gravity);
        }

        protected virtual void UpdateFriction()
        {
            float friction = Friction;

            // apply horizontal friction (velocity -= sign x clamp(weight x friction x velocity, 0, velocity))
            // never remove more than (velocity) as this will cause the actor to move in the opposite direction
            _velocity.x -= Mathf.Sign(_velocity.x) * Mathf.Clamp((GetTotalWeight() * friction * Mathf.Abs(_velocity.x)), 0, Mathf.Abs(_velocity.x));

            // apply vertical friction (velocity -= sign x clamp(weight x friction x velocity, 0, velocity))
            // never remove more than (velocity) as this will cause the actor to move in the opposite direction
            _velocity.y -= Mathf.Sign(_velocity.y) * Mathf.Clamp((_weight * friction * Mathf.Abs(_velocity.y)), 0, Mathf.Abs(_velocity.y));
        }

        protected virtual void UpdateGrounded()
        {
            PhysicsContact contact = null;

            foreach (PhysicsObject remoteObject in _nearby)
            {
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
                UpdateGrounded();
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