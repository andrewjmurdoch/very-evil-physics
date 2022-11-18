using System.Collections.Generic;
using UnityEngine;
using VED.Utilities;

namespace VED.Physics
{
    // todo: implement climbing mechanic
    // todo: implement ledge grab mechanic
    public class PlayerActor : GravityActor
    {
        [SerializeField] private new PhysicsCollider _groundCollider = null;
        [SerializeField] private PlayerActorSettings _settings = null;

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

        protected Timer _coyoteTimer = null;
        protected Timer _crouchTimer = null;
        protected Timer _jumpBankTimer = null;
        protected Timer _wallplantTimer = null;
        protected Timer _wallplantExitTimer = null;

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

        protected override void InitGroundContact()
        {
            if (_groundCollider != null) return;
            base.InitGroundContact();
        }
        #endregion

        #region Tick
        public override void FixedTick()
        {
            base.FixedTick();
            TickWallplant();
        }

        protected override void UpdateFriction()
        {
            // find current friction
            float friction = Friction;
            // apply friction modification when crouching
            friction *= (_crouched && Grounded) ? _settings.CROUCH_FRICTION : 1f;

            // apply horizontal friction (velocity -= sign * clamp(friction * velocity, 0, velocity))
            // never remove more than (velocity) as this will cause the actor to move in the opposite direction
            _velocity.x -= Mathf.Sign(_velocity.x) * Mathf.Clamp(friction * Mathf.Abs(_velocity.x), 0, Mathf.Abs(_velocity.x));
            _velocity.y -= Mathf.Sign(_velocity.y) * Mathf.Clamp(friction * Mathf.Abs(_velocity.y), 0, Mathf.Abs(_velocity.y));
        }

        protected override void UpdateGrounded()
        {
            PhysicsContact GetContact()
            {
                foreach (PhysicsObject remoteObject in _nearby)
                {
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

            if (!Grounded && contact != null && _velocity.y <= 0)
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

        protected virtual void TickWallplant()
        {
            if (!Wallplanted) return;

            if (!ValidWallplant(out _wallplantSign, _nearby))
            {
                Unwallplant();
                _canInitialJump = Grounded;
            }

            if (!_wallplantTimer.Complete)
            {
                float amount = Easing.Ease(Easing.LerpType.EXPO, Easing.EaseType.OUT, 0f, 1f, _wallplantTimer.Elapsed);
                _velocity.y = -_settings.WALLPLANT_SLIP_SPEED * amount;
                return;
            }
        }
        #endregion

        #region Ground
        protected override void Ground(PhysicsContact ground)
        {
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
        public void Move(float input)
        {
            _input = Mathf.Clamp(input, -1f, 1f);

            if (!_canMove || Mathf.Abs(_input) < _settings.MOVEMENT_THRESHOLD)
            {
                return;
            }

            if (!_wallplantTimer.Complete)
            {
                if (Mathf.Abs(_input) > _settings.WALLPLANT_EXIT_THRESHOLD && Mathf.Sign(_input) == -_wallplantSign)
                {
                    // if moving off of wallplant, stick for a delay period to allow aiming without falling off
                    if (_wallplantExitTimer.Complete)
                    {
                        _wallplantExitTimer.Set(null, () =>
                        {
                            Unwallplant();
                            _canInitialJump = false;
                            _canLongJump = false;
                        });
                    }
                }
                else
                {
                    // if no longer moving off of wallplant, reset timer
                    _wallplantExitTimer.Reset();
                }

                return;
            }

            float traction = Traction;

            if (Grounded && !_crouched)
            {
                // move horizontally ([-1 -> 1] * clamp(speed, 0, max_velocity - velocity)) preventing addition past max velocity

                float speed = _settings.MOVEMENT_SPEED;
                if (Mathf.Sign(_velocity.x) == Mathf.Sign(_input))
                {
                    speed = Mathf.Clamp(_settings.MOVEMENT_SPEED, 0, (_settings.MOVEMENT_MAX_SPEED - Mathf.Abs(_velocity.x)));
                }

                _velocity.x += _input * traction * Mathf.Clamp01(_strength / GetTotalWeight()) * speed * TimeManager.DeltaTime;
                return;
            }

            if (!Grounded)
            {
                float speed = _settings.AIR_MOVEMENT_SPEED;
                float maxSpeed = _settings.AIR_MOVEMENT_MAX_SPEED;

                if (_velocity.y > -_settings.JUMP_PEAK_THRESHOLD && _velocity.y < _settings.JUMP_PEAK_THRESHOLD)
                {
                    speed = _settings.JUMP_PEAK_MOVEMENT_SPEED;
                    maxSpeed = _settings.JUMP_PEAK_MOVEMENT_MAX_SPEED;
                }

                if (Mathf.Sign(_velocity.x) == Mathf.Sign(_input))
                {
                    speed = Mathf.Clamp(speed, 0, (maxSpeed - Mathf.Abs(_velocity.x)));
                }

                // move horizontally with air control value & jump peak multiplier
                _velocity.x += _input * traction * Mathf.Clamp01(_strength / GetTotalWeight()) * speed * TimeManager.DeltaTime;
                return;
            }
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

                    _velocity.x = -_wallplantSign * _settings.WALLPLANT_JUMP_SPEED_HORIZONTAL * traction * Mathf.Clamp01(_strength / GetTotalWeight());
                    _velocity.y = _settings.WALLPLANT_JUMP_SPEED_VERTICAL * traction * Mathf.Clamp01(_strength / GetTotalWeight());

                    return;
                }

                _velocity.x += (_settings.JUMP_SPEED * _settings.JUMP_HORIZONTAL_FRACTION) * _input * traction * Mathf.Clamp01(_strength / GetTotalWeight());
                _velocity.y = _settings.JUMP_SPEED * traction * Mathf.Clamp01(_strength / GetTotalWeight());
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
                _velocity.y += _settings.LONG_JUMP_SPEED * traction * Mathf.Clamp01(_strength / GetTotalWeight()) * TimeManager.DeltaTime;
            }

            // update long jump time
            _longJumpTime = time;
        }

        protected bool ValidBankedJump(out PhysicsContact ground, List<PhysicsContact> contacts)
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
        protected void Wallplant()
        {
            _canWallplant = false;
            _canLongJump = false;
            _canInitialJump = true;
            _velocity = Vector2.zero;

            _wallplantTimer.Set(null, Unwallplant);
            _wallplantExitTimer.Reset();
        }

        protected void Unwallplant()
        {
            _canWallplant = false;
            _canInitialJump = false;

            _wallplantTimer.Reset();
            _wallplantExitTimer.Reset();
        }

        protected bool ValidWallplant(out float wallplantSign, List<PhysicsContact> contacts)
        {
            foreach (PhysicsContact contact in contacts)
            {
                if (ValidWallplant(contact.RemoteObject, out wallplantSign))
                {
                    return true;
                }
            }

            wallplantSign = 1f;
            return false;
        }

        protected bool ValidWallplant(out float wallplantSign, List<PhysicsObject> remotes)
        {
            foreach (PhysicsObject remote in remotes)
            {
                if (ValidWallplant(remote, out wallplantSign))
                {
                    return true;
                }
            }

            wallplantSign = 1f;
            return false;
        }

        protected bool ValidWallplant(PhysicsObject remote, out float wallplantSign)
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
        protected override void CollideHorizontally(List<PhysicsContact> contacts)
        {
            if (_canWallplant && _velocity.y <= _settings.WALLPLANT_ENTRY_SPEED && !Grounded && !SlidingVertically)
            {
                if (ValidWallplant(out _wallplantSign, contacts))
                {
                    Wallplant();
                    return;
                }
            }

            base.CollideHorizontally(contacts);
        }

        protected override void CollideVertically(List<PhysicsContact> contacts)
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