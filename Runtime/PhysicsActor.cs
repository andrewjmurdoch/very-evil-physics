using System;
using System.Collections.Generic;
using UnityEngine;
using VED.Utilities;

namespace VED.Physics
{
    public class PhysicsActor : PhysicsObject
    {

        public static Action<PhysicsActor> Spawned;
        public static Action<PhysicsActor> Despawned;

        public int Index { get; set; } = 0;

        public float Weight
        {
            get => _weight;
            set => _weight = value;
        }
        [Space(20), Header("PhysicsActor"), Space(10)]
        [SerializeField] protected float _weight = 1f;

        public float Strength
        {
            get => _strength;
            set => _strength = value;
        }
        [SerializeField] protected float _strength = 1f;
        public float StrengthByWeight => _strength / _weight;
        public float StrengthByTotalWeight => _strength / GetTotalWeight();

        public bool Pushable
        {
            get => _pushable;
            set => _pushable = value;
        }
        [SerializeField] protected bool _pushable = true;

        public bool CanPush
        {
            get => _canPush;
            set => _canPush = value;
        }
        [SerializeField] protected bool _canPush = true;

        public bool Immoveable
        {
            get => _immoveable;
            set => _immoveable = value;
        }
        [SerializeField] protected bool _immoveable = false;

        public Dictionary<float, bool> MoveableHorizontally = new Dictionary<float, bool>()
        {
            {-1, true},
            { 1, true}
        };

        // serialized fields for observing in inspector
        [SerializeField, ReadOnly] bool _moveableLeft  = true;
        [SerializeField, ReadOnly] bool _moveableRight = true;

        public Dictionary<float, bool> MoveableVertically = new Dictionary<float, bool>()
        {
            {-1, true},
            { 1, true}
        };

        // serialized fields for observing in inspector
        [SerializeField, ReadOnly] bool _moveableUp   = true;
        [SerializeField, ReadOnly] bool _moveableDown = true;

        protected PhysicsTileLevel.Cell _cell = null;
        protected List<PhysicsObject> _nearbyActors = new List<PhysicsObject>();
        protected List<PhysicsObject> _nearbySolids = new List<PhysicsObject>();

        private const float MAX_MOVEMENT_ITERATIONS = 1000;

        public virtual void OnEnable()
        {
            Init();
        }

        public virtual void OnDisable()
        {
            Deinit();
        }

        public override void Init()
        {
            base.Init();

            Spawned?.Invoke(this);
            FixedTickNearby();
        }

        public override void Deinit()
        {
            base.Deinit();

            _cell?.Actors.Remove(this);
            _cell = null;
            Despawned?.Invoke(this);
        }

        public virtual void FixedTick()
        {
            FixedTickNearby();
            FixedTickMoveable();
            FixedTickVelocity(_velocityHor, _velocityVer);
        }

        public virtual void FixedSubTick()
        {
            FixedSubTickMove();
        }

        public virtual void FixedSubTickMove(Action<List<PhysicsContact>> CollideHorizontally = null, Action<List<PhysicsContact>> CollideVertically = null)
        {
            double x = _xRounded + _xRemainder;
            double y = _yRounded + _yRemainder;
            _xRounded = Math.Sign(x) * Math.Floor(Math.Abs(x));
            _yRounded = Math.Sign(y) * Math.Floor(Math.Abs(y));
            _xRemainder = x - _xRounded;
            _yRemainder = y - _yRounded;

            if (Math.Abs(_xRounded) <= 0 && Math.Abs(_yRounded) <= 0) return;
            
            if (_immoveable) return;
            _immoveable = true;

            FixedTickNearby();

            if (Math.Abs(_xRounded) > 0)
            {
                float sign = Math.Sign(_xRounded);
                _xRounded -= sign;

                MoveableHorizontally[sign] = MoveHorizontally(sign, CollideHorizontally);
                if (!MoveableHorizontally[sign]) _xRounded = 0;
            }

            if (Math.Abs(_yRounded) > 0)
            {
                float sign = Math.Sign(_yRounded);
                _yRounded -= sign;

                MoveableVertically[sign] = MoveVertically(sign, CollideVertically);
                if (!MoveableVertically[sign]) _yRounded = 0;
            }

            _immoveable = false;
        }

        public virtual void Move(double x, double y, Action<List<PhysicsContact>> CollideHorizontally = null, Action<List<PhysicsContact>> CollideVertically = null)
        {
            if (_immoveable) return;

            FixedTickVelocity(x, y);

            _immoveable = true;

            float iterations = 0;
            while (iterations < MAX_MOVEMENT_ITERATIONS && (Math.Abs(_xRounded) > 0 || Math.Abs(_yRounded) > 0))
            {
                FixedTickNearby();

                if (Math.Abs(_xRounded) > 0)
                {
                    float sign = Math.Sign(_xRounded);
                    _xRounded -= sign;

                    MoveableHorizontally[sign] = MoveHorizontally(sign, CollideHorizontally);
                    if (!MoveableHorizontally[sign]) _xRounded = 0;
                }

                if (Math.Abs(_yRounded) > 0)
                {
                    float sign = Math.Sign(_yRounded);
                    _yRounded -= sign;

                    MoveableVertically[sign] = MoveVertically(sign, CollideVertically);
                    if (!MoveableVertically[sign]) _yRounded = 0;
                }

                iterations++;
            }

            _immoveable = false;

            _xRounded = 0;
            _yRounded = 0;
        }

        public void FixedTickNearby()
        {
            _nearby.Clear();

            void RemoveFromCell()
            {
                _cell?.Actors.Remove(this);
            }

            // find the level this actor is in
            PhysicsTileLevel physicsTilelevel = PhysicsTileLevelManager.Instance.GetTileLevel(Transform.position);
            if (physicsTilelevel == null)
            {
                RemoveFromCell();
                return;
            }

            // find the cell this actor is in
            PhysicsTileLevel.Cell cell = physicsTilelevel.GetCell(Transform.position);
            if (cell == null)
            {
                RemoveFromCell();
                return;
            }

            // upon entering a new cell
            if (cell != _cell)
            {
                RemoveFromCell();
                _cell = cell;
                _cell.Actors.Add(this);
            }

            // find nearby actors & solids based on cell + neighbour cells
            void AddCellObjectsToNearby(PhysicsTileLevel.Cell cell)
            {
                foreach (PhysicsActor physicsActor in cell.Actors)
                {
                    if (!_nearbyActors.Contains(physicsActor) && physicsActor != this)
                    {
                        _nearbyActors.Add(physicsActor);
                    }
                }

                foreach (PhysicsSolid physicsSolid in cell.Solids)
                {
                    if (!_nearbySolids.Contains(physicsSolid))
                    {
                        _nearbySolids.Add(physicsSolid);
                    }
                }
            }

            _nearbyActors = new List<PhysicsObject>();
            _nearbySolids = new List<PhysicsObject>();

            AddCellObjectsToNearby(_cell);
            for (int i = 0; i < _cell.Neighbours.Count; i++)
            {
                AddCellObjectsToNearby(_cell.Neighbours[i]);
            }

            // find nearby tiles for each physics layer in level
            foreach (PhysicsTileLayer physicsTilelayer in physicsTilelevel.PhysicsTileLayers.Values)
            {
                _nearbySolids.AddRange(physicsTilelayer.GetTilesNearby(Transform.position));
            }

            _nearby.AddRange(_nearbyActors);
            _nearby.AddRange(_nearbySolids);

            // remove all ignored objects
            for (int i = _nearby.Count - 1; i >= 0; i--)
            {
                if (_ignored.Contains(_nearby[i]))
                {
                    _nearby.RemoveAt(i);
                }
            }
        }

        public void FixedTickMoveable()
        {
            MoveableHorizontally[ 1] = UpdateMoveableHorizontally( 1);
            MoveableHorizontally[-1] = UpdateMoveableHorizontally(-1);
            MoveableVertically  [ 1] = UpdateMoveableVertically  ( 1);
            MoveableVertically  [-1] = UpdateMoveableVertically  (-1);

            _moveableRight = MoveableHorizontally[ 1];
            _moveableLeft  = MoveableHorizontally[-1];
            _moveableUp    = MoveableVertically  [ 1];
            _moveableDown  = MoveableVertically  [-1];
        }

        public void FixedTickVelocity(double x, double y)
        {
            _xRemainder += x;
            _yRemainder += y;
            _xRounded = Math.Sign(_xRemainder) * Math.Floor(Math.Abs(_xRemainder));
            _yRounded = Math.Sign(_yRemainder) * Math.Floor(Math.Abs(_yRemainder));
            _xRemainder -= _xRounded;
            _yRemainder -= _yRounded;
        }

        public virtual bool UpdateMoveableHorizontally(int sign)
        {
            // physics actors are non-moveable when they are attempting to move in any direction in which they collide with a solid, immoveable object, or currently non-moveable object

            List<PhysicsContact> solids = CollidingHorizontally(sign, _nearbySolids);
            if (solids.Count > 0)
            {
                return false;
            }

            List<PhysicsContact> actors = CollidingHorizontally(sign, _nearbyActors);
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;
                if (actor.Immoveable || !actor.MoveableHorizontally[sign])
                {
                    return false;
                }
            }

            // no collisions found
            return true;
        }

        public virtual bool UpdateMoveableVertically(int sign)
        {
            // physics actors are non-moveable when they are attempting to move in any direction in which they collide with a solid, immoveable object, or currently non-moveable object

            List<PhysicsContact> solids = CollidingVertically(sign, _nearbySolids);
            if (solids.Count > 0)
            {
                return false;
            }

            List<PhysicsContact> actors = CollidingVertically(sign, _nearbyActors);
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;
                if (actor.Immoveable || !actor.MoveableVertically[sign])
                {
                    return false;
                }
            }

            // no collisions found
            return true;
        }

        public virtual bool MoveHorizontally(float sign, Action<List<PhysicsContact>> CollideHorizontally)
        {
            List<PhysicsContact> solids = CollidingHorizontally(sign, _nearbySolids);
            if (solids.Count > 0)
            {
                CollideHorizontally(solids);
                return false;
            }

            List<PhysicsContact> actors = CollidingHorizontally(sign, _nearbyActors);
            if (actors.Count > 0)
            {
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

        public virtual bool MoveVertically(float sign, Action<List<PhysicsContact>> CollideVertically)
        {
            List<PhysicsContact> solids = CollidingVertically(sign, _nearbySolids);
            if (solids.Count > 0)
            {
                CollideVertically?.Invoke(solids);
                return false;
            }

            List<PhysicsContact> actors = CollidingVertically(sign, _nearbyActors);
            if (actors.Count > 0)
            {
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

        public bool PushActorsHorizontally(List<PhysicsContact> actors, float sign, Action<List<PhysicsContact>> CollideHorizontally)
        {
            // validate
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;

                // if actor is not moveable, collide
                if (!actor.MoveableHorizontally[sign] || !actor.Pushable)
                {
                    CollideHorizontally?.Invoke(actors);
                    return false;
                }
            }

            // push actors
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;

                // transfer velocity
                float weight = actor.GetTotalWeight();
                float action = Mathf.Clamp01(_strength / weight);
                actor.Push(x: sign * action);

                float reaction = Mathf.Clamp01(weight / _strength);
                _xRemainder -= sign * reaction;
            }

            return true;
        }

        public bool PushActorsVertically(List<PhysicsContact> actors, float sign, Action<List<PhysicsContact>> CollideVertically)
        {
            // validate
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;

                // if actor is not moveable, collide
                if (!actor.MoveableVertically[sign] || !actor.Pushable)
                {
                    CollideVertically?.Invoke(actors);
                    return false;
                }
            }

            // push actors
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;

                // transfer velocity
                float weight = actor.GetTotalWeight();
                float action = Mathf.Clamp01(_strength / weight);
                actor.Push(y: sign * action);

                float reaction = Mathf.Clamp01(weight / _strength);
                _yRemainder -= sign * reaction;
            }

            return true;
        }

        public void Push(double x = 0, double y = 0)
        {
            _xRemainder += x;
            _yRemainder += y;
        }

        public virtual void Squish(List<PhysicsContact> collisions)
        {
            Debug.Log("Squish");
        }

        public virtual void ApplyImpulse(Vector2 impulse)
        {
            _velocityHor += impulse.x;
            _velocityVer += impulse.y;
        }
        
        public float GetTotalWeight(List<PhysicsActor> counted = null)
        {
            counted ??= new List<PhysicsActor>();
            counted.Add(this);
            
            float weight = _weight;
            foreach (PhysicsContact contact in _attachments)
            {
                if (contact.RemoteObject is not PhysicsActor actor) continue;
                if (counted.Contains(actor)) continue;
                
                weight += actor.GetTotalWeight(counted);
            }
            return weight;
        }
    }
}