using System;
using System.Collections.Generic;
using UnityEngine;
using VED.Utilities;

namespace VED.Physics
{
    public class GravityActor : SlideActor
    {
        public float Gravity
        {
            get => _gravity;
            set => _gravity = value;
        }
        [Range(-1, 1), SerializeField] protected float _gravity = 1f;

        public bool Groundable
        {
            get => _groundable;
            set => _groundable = value;
        }
        [SerializeField] protected bool _groundable = true;

        public bool Grounded => _groundContact != null;
#pragma warning disable CS0414
        [SerializeField, ReadOnly] protected bool _grounded = false;
#pragma warning restore CS0414

        [SerializeField] protected PhysicsCollider _groundCollider = null;

        public PhysicsContact GroundContact
        {
            get => _groundContact;
            set => _groundContact = value;
        }
        protected PhysicsContact _groundContact = null;

        public float Friction
        {
            get
            {
                return _groundContact != null ? _groundContact.RemoteObject.PhysicsMaterial.Friction : PhysicsManager.Instance.AtmospherePhysicsMaterial.Friction;
            }
        }

        public float Traction
        {
            get
            {
                return _groundContact != null ? _groundContact.RemoteObject.PhysicsMaterial.Traction : PhysicsManager.Instance.AtmospherePhysicsMaterial.Traction;
            }
        }

        public float Elasticity
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

        public override void Deinit()
        {
            Unground();
            base.Deinit();
        }
        #endregion

        #region Tick
        public override void FixedTick()
        {
            FixedTickNearby();
            FixedTickMoveable();
            FixedTickSliding();
            FixedTickGrounded();
            FixedTickGravity();
            FixedTickFriction();
            FixedTickInheritedMovement();
            FixedTickVelocity(_velocityHor, _velocityVer);
        }

        public override void FixedSubTick()
        {
            FixedSubTickMove(CollideHorizontally, CollideVertically);
        }

        public virtual void FixedTickInheritedMovement()
        {
            if (_groundContact != null && _groundContact.RemoteObject is PhysicsActor actor)
            {
                if (Mathf.Abs(actor.Velocity.x) > Mathf.Abs(_velocityHor))
                {
                    float strength = Mathf.Clamp01(actor.Strength / _weight);
                    float difference = actor.Velocity.x - _velocityHor;

                    _velocityHor += strength * difference;
                }

                if (_groundContact.RemoteObject.Velocity.y > 0 && _groundContact.RemoteObject.Velocity.y > _velocityVer)
                {
                    float strength = Mathf.Clamp01(actor.Strength / _weight);
                    float difference = actor.Velocity.y - _velocityVer;

                    _velocityVer += strength * difference;
                }
            }
        }

        public virtual void FixedTickGravity()
        {
            // reduce y velocity by (weight x gravity)
            float applied = _slidingUp ? 0 : 1;
            _velocityVer -= applied * (_weight * _gravity * PhysicsManager.Instance.Gravity);
        }

        public virtual void FixedTickFriction()
        {
            float friction = Friction * Time.fixedDeltaTime;

            // apply horizontal friction (velocity -= sign x clamp(weight x friction x velocity, 0, velocity))
            // never remove more than (velocity) as this will cause the actor to move in the opposite direction
            _velocityHor -= Mathf.Sign(_velocityHor) * Mathf.Clamp((GetTotalWeight() * friction * Mathf.Abs(_velocityHor)), 0, Mathf.Abs(_velocityHor));

            // apply vertical friction (velocity -= sign x clamp(weight x friction x velocity, 0, velocity))
            // never remove more than (velocity) as this will cause the actor to move in the opposite direction
            _velocityVer -= Mathf.Sign(_velocityVer) * Mathf.Clamp((_weight * friction * Mathf.Abs(_velocityVer)), 0, Mathf.Abs(_velocityVer));
        }

        public virtual void FixedTickGrounded()
        {
            if (!_groundable) return;

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

            if (!Grounded && contact != null && _velocityVer <= 0)
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
        public virtual void Ground(PhysicsContact ground)
        {
            if (!_groundable) return;

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
        public virtual void CollideHorizontally(List<PhysicsContact> contacts)
        {
            BounceHorizontally(contacts);
        }

        public virtual void CollideVertically(List<PhysicsContact> contacts)
        {
            if (SlidingVertically)
            {
                _velocityHor = 0f;
                _velocityVer = 0f;
            }

            BounceVertically(contacts);
        }

        public void BounceHorizontally(List<PhysicsContact> collisions)
        {
            float sign = Mathf.Sign(_velocityHor);
            float elasticity = Elasticity;

            foreach (PhysicsContact collision in collisions)
            {
                if (collision != _groundContact)
                {
                    elasticity *= collision.RemoteObject.PhysicsMaterial.Elasticity;
                }
            }

            _velocityHor += -sign * Mathf.Abs(_velocityHor * elasticity);
        }

        public void BounceVertically(List<PhysicsContact> collisions)
        {
            float sign = Mathf.Sign(_velocityVer);

            if (sign == -1)
            {
                FixedTickGrounded();
            }

            float elasticity = Elasticity;

            foreach (PhysicsContact collision in collisions)
            {
                if (collision != _groundContact)
                {
                    elasticity *= collision.RemoteObject.PhysicsMaterial.Elasticity;
                }
            }

            _velocityVer = -sign * Mathf.Abs(_velocityVer * elasticity);
        }
        #endregion

        public override void ApplyImpulse(Vector2 impulse)
        {
            Unground();
            base.ApplyImpulse(impulse);
        }
    }
}