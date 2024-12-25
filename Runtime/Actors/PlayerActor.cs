using System.Collections.Generic;
using UnityEngine;
using VED.Utilities;

namespace VED.Physics
{
    // todo: implement climbing mechanic
    // todo: implement ledge grab mechanic
    public class PlayerActor : GravityActor
    {
        [Space(20), Header("PlayerActor"), Space(10)]
        [SerializeField] protected PlayerActorSettings _settings = null;  
        [SerializeField] protected bool _wallplantEnabled = true;

        public bool Wallplanted => !_wallplantTimer.Complete;
        public bool JumpBanked => !_jumpBankTimer.Complete;
        protected bool CanBankJump => !Grounded && !Wallplanted && !JumpBanked;

        protected float _input = 0f;
        protected float _longJumpTime = 0f;
        protected float _wallplantSign = 1f;

        protected bool _canInitialJump = true;
        protected bool _canLongJump = true;

        protected bool _canMove = true;
        protected bool _canWallplant = false;
        protected bool _crouched = false;

        [SerializeField] protected Timer _coyoteTimer = null;
        [SerializeField] protected Timer _crouchTimer = null;
        [SerializeField] protected Timer _jumpBankTimer = null;
        [SerializeField] protected Timer _wallplantTimer = null;
        [SerializeField] protected Timer _wallplantExitTimer = null;

        #region Init
        public override void Init()
        {
            base.Init();
            InitTimers();
        }

        protected void InitTimers()
        {
            _coyoteTimer        = new Timer(_settings.COYOTE_TIME        );
            _crouchTimer        = new Timer(_settings.CROUCH_TIME        );
            _jumpBankTimer      = new Timer(_settings.JUMP_BANK_TIME     );
            _wallplantTimer     = new Timer(_settings.WALLPLANT_TIME     );
            _wallplantExitTimer = new Timer(_settings.WALLPLANT_EXIT_TIME);
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
            FixedTickWallplant();
            FixedTickVelocity(_velocityHor, _velocityVer);
        }

        public override void FixedSubTick()
        {
            FixedSubTickMove(CollideHorizontally, CollideVertically);
        }

        public override void FixedTickFriction()
        {
            // find current friction
            float friction = Friction * Time.fixedDeltaTime;

            // apply friction modification when crouching
            friction *= (_crouched && Grounded) ? _settings.CROUCH_FRICTION : 1f;

            // apply horizontal friction (velocity -= sign * clamp(friction * velocity, 0, velocity))
            // never remove more than (velocity) as this will cause the actor to move in the opposite direction
            _velocityHor -= Mathf.Sign(_velocityHor) * Mathf.Clamp(friction * _weight * Mathf.Abs(_velocityHor), 0, Mathf.Abs(_velocityHor));
            _velocityVer -= Mathf.Sign(_velocityVer) * Mathf.Clamp(friction * _weight * Mathf.Abs(_velocityVer), 0, Mathf.Abs(_velocityVer));
        }

        public override void FixedTickGrounded()
        {
            if (!_groundable) return;

            PhysicsContact GetContact()
            {
                foreach (PhysicsObject remoteObject in _nearby)
                {
                    if (_ignored.Contains(remoteObject)) continue;
                    foreach (PhysicsCollider remoteCollider in remoteObject.Colliders)
                    {
                        if (_groundCollider.CollidingVertically(-1f, remoteCollider))
                        {
                            return new PhysicsContact(this, _groundCollider, remoteObject, remoteCollider);
                        }
                    }
                }

                return null;
            }
            PhysicsContact contact = GetContact();

            if (!Grounded && contact != null && _velocityVer <= 0)
            {
                // ground actor
                Ground(contact);
            }
            else if (Grounded && contact == null && !_slidingUp && _coyoteTimer.Complete)
            {
                // unground actor
                _coyoteTimer.Set(null, () =>
                {
                    Unground();
                    _canInitialJump = false;
                    _canLongJump = false;
                });
            }
        }

        public virtual void FixedTickWallplant()
        {
            if (!Wallplanted) return;

            if (!ValidWallplant(out _wallplantSign, _nearby))
            {
                Unwallplant();
                _canInitialJump = Grounded;
            }

            if (!_wallplantTimer.Complete)
            {
                float amount = Easing.Ease(Easing.Shape.EXPO, Easing.Extent.OUT, 0f, 1f, _wallplantTimer.Elapsed);
                _velocityVer = -_settings.WALLPLANT_SLIP_SPEED * amount;
                return;
            }
        }
        #endregion

        #region Ground
        public override void Ground(PhysicsContact ground)
        {
            if (!_groundable) return;

            base.Ground(ground);

            _coyoteTimer.Reset();

            if (!_wallplantTimer.Complete)
            {
                Unwallplant();
            }

            _canInitialJump = true;
            _canWallplant = false;

            if (!_jumpBankTimer.Complete)
            {
                _jumpBankTimer.Reset();
                InitialJump();
            }
            _jumpBankTimer.Reset();
        }

        public override void Unground()
        {
            base.Unground();

            _canInitialJump = false;
            _canWallplant = true;
        }
        #endregion

        #region Move
        public void TickMove(float input)
        {
            _input = Mathf.Clamp(input, -1f, 1f);

            if (!_canMove || Mathf.Abs(_input) < _settings.MOVEMENT_THRESHOLD)
                return;

            if (!_wallplantTimer.Complete)
            {
                TickMoveWallplant();
                return;
            }

            if (Grounded && !_crouched)
            {
                TickMoveGrounded();
                return;
            }

            if (!Grounded)
            {
                TickMoveUngrounded();
                return;
            }
        }

        private void TickMoveWallplant()
        {
            if (Mathf.Abs(_input) <= _settings.WALLPLANT_EXIT_THRESHOLD || Mathf.Sign(_input) != -_wallplantSign)
            {
                // if no longer moving off of wallplant, reset wallplant exit timer
                _wallplantExitTimer.Reset();
                return;
            }

            // if timer is already set, do not reset it
            if (!_wallplantExitTimer.Complete) return;

            // if moving off of wallplant, stick for a delay period to allow aiming without falling off
            _wallplantExitTimer.Set(null, () =>
            {
                Unwallplant();
                _canInitialJump = false;
                _canLongJump = false;
            });
        }

        private void TickMoveGrounded()
        {
            float traction = Traction;
            bool turning = Mathf.Abs(_velocityHor) > 0.0f && Mathf.Sign(_velocityHor) != Mathf.Sign(_input);
            float speed = turning
                ? Mathf.Clamp(_settings.MOVEMENT_SPEED, 0, (_settings.MOVEMENT_MAX_SPEED_TURNING + Mathf.Abs(_velocityHor)))
                : Mathf.Clamp(_settings.MOVEMENT_SPEED, 0, (_settings.MOVEMENT_MAX_SPEED - Mathf.Abs(_velocityHor)));

            // move horizontally ([-1 -> 1] * clamp(speed, 0, max_velocity - velocity)) preventing addition past max velocity
            _velocityHor += _input * traction * Mathf.Clamp01(_strength / GetTotalWeight()) * speed * Time.deltaTime;
        }

        private void TickMoveUngrounded()
        {
            float traction = Traction;

            bool peak = _velocityVer > -_settings.JUMP_PEAK_THRESHOLD
                     && _velocityVer < _settings.JUMP_PEAK_THRESHOLD;
            float multiplier = peak
                ? _settings.JUMP_PEAK_MULTIPLIER
                : 1.0f;

            bool turning = Mathf.Abs(_velocityHor) > 0.0f && Mathf.Sign(_velocityHor) != Mathf.Sign(_input);
            float speed = turning
                ? Mathf.Clamp((_settings.AIR_MOVEMENT_SPEED * multiplier), 0.0f, ((_settings.AIR_MOVEMENT_MAX_SPEED_TURNING * multiplier) + Mathf.Abs(_velocityHor)))
                : Mathf.Clamp((_settings.AIR_MOVEMENT_SPEED * multiplier), 0.0f, ((_settings.AIR_MOVEMENT_MAX_SPEED * multiplier) - Mathf.Abs(_velocityHor)));

            // move horizontally with air control value & jump peak multiplier
            _velocityHor += _input * traction * Mathf.Clamp01(_strength / GetTotalWeight()) * speed * Time.deltaTime;
        }

        public void Crouch(bool input)
        {
            if (!_crouchTimer.Complete && input)
            {
                return;
            }

            // crouch
            if (!_crouched && input)
            {
                _crouched = true;
            }
            // uncrouch
            else if (_crouched && !input)
            {
                _crouched = false;
                _crouchTimer.Set();
            }
        }
        #endregion

        #region Jump
        public void InitialJump()
        {
            if (_canInitialJump)
            {
                float traction = Traction;
                Unground();

                _canInitialJump = false;
                _canLongJump = true;
                _canWallplant = true;
                _longJumpTime = 0f;

                _coyoteTimer.Reset();

                if (Wallplanted)
                {
                    // jump off of attached solid
                    Unwallplant();
                    _canWallplant = true;
                    _canLongJump = false;

                    _velocityHor = -_wallplantSign * _settings.WALLPLANT_JUMP_SPEED_HORIZONTAL * traction * Mathf.Clamp01(_strength / GetTotalWeight());
                    _velocityVer = _settings.WALLPLANT_JUMP_SPEED_VERTICAL * traction * Mathf.Clamp01(_strength / GetTotalWeight());

                    return;
                }

                _velocityHor += (_settings.JUMP_SPEED * _settings.JUMP_HORIZONTAL_FRACTION) * _input * traction * Mathf.Clamp01(_strength / GetTotalWeight());
                _velocityVer = _settings.JUMP_SPEED * traction * Mathf.Clamp01(_strength / GetTotalWeight());
                return;
            }

            if (CanBankJump)
            {
                _jumpBankTimer.Set();
                return;
            }
        }

        public void LongJump(float time)
        {
            if (!_canLongJump) return;

            // if time < long jump time, then a new jump has been triggered
            if (time < _longJumpTime || !MoveableVertically[1])
            {
                _canLongJump = false;
                return;
            }

            if (time >= _settings.LONG_JUMP_START_TIME && time <= _settings.LONG_JUMP_END_TIME)
            {
                float traction = Traction;
                _velocityVer += _settings.LONG_JUMP_SPEED * traction * Mathf.Clamp01(_strength / GetTotalWeight()) * Time.deltaTime;
            }

            // update long jump time
            _longJumpTime = time;
        }

        public bool ValidBankedJump(out PhysicsContact ground, List<PhysicsContact> contacts)
        {
            foreach (PhysicsContact contact in contacts)
            {
                if (contact.LocalCollider.CollidingVertically(-1, contact.RemoteCollider))
                {
                    ground = contact;
                    return true;
                }
            }

            ground = null;
            return false;
        }
        #endregion

        #region Wallplant
        public void Wallplant()
        {
            _canWallplant = false;
            _canLongJump = false;
            _canInitialJump = true;
            _velocityHor = 0f;
            _velocityVer = 0f;

            _wallplantTimer.Set(null, Unwallplant);
            _wallplantExitTimer.Reset();
        }

        public void Unwallplant()
        {
            _canWallplant = false;
            _canInitialJump = false;

            _wallplantTimer.Reset();
            _wallplantExitTimer.Reset();
        }

        public bool ValidWallplant(out float wallplantSign, List<PhysicsContact> contacts)
        {
            wallplantSign = 1f;
            if (!_wallplantEnabled) return false;

            foreach (PhysicsContact contact in contacts)
            {
                if (ValidWallplant(out wallplantSign, contact.RemoteObject))
                {
                    return true;
                }
            }
            return false;
        }

        public bool ValidWallplant(out float wallplantSign, List<PhysicsObject> remotes)
        {
            wallplantSign = 1f;

            foreach (PhysicsObject remote in remotes)
            {
                if (ValidWallplant(out wallplantSign, remote))
                {
                    return true;
                }
            }

            return false;
        }

        public bool ValidWallplant(out float wallplantSign, PhysicsObject remote)
        {
            float sign = Mathf.Sign((remote.transform.position - Transform.position).normalized.x);

            PhysicsContact contact = CollidingHorizontally(sign, remote);
            if (contact != null)
            {
                wallplantSign = sign;
                return true;
            }

            contact = CollidingHorizontally(-sign, remote);
            if (contact != null)
            {
                wallplantSign = -sign;
                return true;
            }

            wallplantSign = sign;
            return false;
        }
        #endregion

        #region Collision
        public override void CollideHorizontally(List<PhysicsContact> contacts)
        {
            if (_canWallplant && _velocityVer <= _settings.WALLPLANT_ENTRY_SPEED && !Grounded && !SlidingVertically)
            {
                if (ValidWallplant(out _wallplantSign, contacts))
                {
                    Wallplant();
                    return;
                }
            }

            base.CollideHorizontally(contacts);
        }

        public override void CollideVertically(List<PhysicsContact> contacts)
        {
            if (!_jumpBankTimer.Complete && ValidBankedJump(out PhysicsContact ground, contacts))
            {
                Ground(ground);
                return;
            }

            base.CollideVertically(contacts);
        }
        #endregion

        public override void ApplyImpulse(Vector2 impulse)
        {
            base.ApplyImpulse(impulse);
            _canInitialJump = false;
            _canLongJump = false;
        }
    }
}