﻿using System;
using System.Collections.Generic;
using UnityEngine;
using VED.Utilities;

namespace VED.Physics
{
    public class PhysicsActor : PhysicsObject
    {
        public static Action<PhysicsActor> Spawned;
        public static Action<PhysicsActor> Despawned;

        public float Weight => _weight;
        [SerializeField] protected float _weight = 1f;

        public float Strength => _strength;
        [SerializeField] protected float _strength = 1f;

        [SerializeField] protected bool _canPush = true;

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

        public bool Immoveable => _immoveable;
        protected bool _immoveable = false;

        protected PhysicsTilelevel.Cell _cell = null;
        protected List<PhysicsObject> _nearbyActors = new List<PhysicsObject>();
        protected List<PhysicsObject> _nearbySolids = new List<PhysicsObject>();

        private const float MAX_MOVEMENT_ITERATIONS = 1000;

        public virtual void OnEnable()
        {
            Init();
        }

        protected void OnDisable()
        {
            Deinit();
        }

        public override void Init()
        {
            base.Init();
            Spawned?.Invoke(this);
            UpdateNearby();
        }

        public virtual void Deinit()
        {
            _cell?.Actors.Remove(this);
            Despawned?.Invoke(this);
        }

        public virtual void FixedTick()
        {
            UpdateMoveable();
        }

        public virtual void Move(double x = 0, double y = 0, Action<List<PhysicsContact>> CollideHorizontally = null, Action<List<PhysicsContact>> CollideVertically = null)
        {
            if (_immoveable) return;

            _xRemainder += x;
            _yRemainder += y;
            _xRounded    = Math.Sign(_xRemainder) * Math.Floor(Math.Abs(_xRemainder));
            _yRounded    = Math.Sign(_yRemainder) * Math.Floor(Math.Abs(_yRemainder));
            _xRemainder -= _xRounded;
            _yRemainder -= _yRounded;

            _immoveable = true;

            float iterations = 0;
            while (iterations < MAX_MOVEMENT_ITERATIONS && (Math.Abs(_xRounded) > 0 || Math.Abs(_yRounded) > 0))
            {
                UpdateNearby();

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

        protected void UpdateNearby()
        {
            _nearby.Clear();

            void RemoveFromCell()
            {
                _cell?.Actors.Remove(this);
            }

            // find the level this actor is in
            PhysicsTilelevel physicsTilelevel = PhysicsTilelevelManager.Instance.GetTilelevel(Transform.position);
            if (physicsTilelevel == null)
            {
                RemoveFromCell();
                return;
            }

            // find the cell this actor is in
            PhysicsTilelevel.Cell cell = physicsTilelevel.GetCell(Transform.position);
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
            void AddCellObjectsToNearby(PhysicsTilelevel.Cell cell)
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
            foreach (PhysicsTilelayer physicsTilelayer in physicsTilelevel.PhysicsTilelayers.Values)
            {
                _nearbySolids.AddRange(physicsTilelayer.GetTilesNearby(Transform.position));
            }

            _nearby.AddRange(_nearbyActors);
            _nearby.AddRange(_nearbySolids);
        }

        protected void UpdateMoveable()
        {
            UpdateNearby();

            MoveableHorizontally[ 1] = UpdateMoveableHorizontally( 1);
            MoveableHorizontally[-1] = UpdateMoveableHorizontally(-1);
            MoveableVertically  [ 1] = UpdateMoveableVertically  ( 1);
            MoveableVertically  [-1] = UpdateMoveableVertically  (-1);

            _moveableRight = MoveableHorizontally[ 1];
            _moveableLeft  = MoveableHorizontally[-1];
            _moveableUp    = MoveableVertically  [ 1];
            _moveableDown  = MoveableVertically  [-1];
        }

        protected virtual bool UpdateMoveableHorizontally(int sign)
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

        protected virtual bool UpdateMoveableVertically(int sign)
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

        protected virtual bool MoveHorizontally(float sign, Action<List<PhysicsContact>> CollideHorizontally)
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

        protected virtual bool MoveVertically(float sign, Action<List<PhysicsContact>> CollideVertically)
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

        protected bool PushActorsHorizontally(List<PhysicsContact> actors, float sign, Action<List<PhysicsContact>> CollideHorizontally)
        {
            // push actors
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;

                // if actor is not moveable, collide
                if (!actor.MoveableHorizontally[sign])
                {
                    CollideHorizontally?.Invoke(actors);
                    return false;
                }

                // transfer velocity
                float action    = Mathf.Clamp01(_strength / actor.GetTotalWeight()) * PhysicsManager.Instance.PhysicsStepSize;
                actor.Velocity += sign * new Vector2(Mathf.Clamp(action, 0f, Mathf.Abs(Velocity.x)), 0f);

                float reaction  = Mathf.Clamp01(actor.GetTotalWeight() / _strength) * PhysicsManager.Instance.PhysicsStepSize;
                Velocity       -= sign * new Vector2(Mathf.Clamp(reaction, 0f, Mathf.Abs(Velocity.x)), 0f);
            }

            return true;
        }

        protected bool PushActorsVertically(List<PhysicsContact> actors, float sign, Action<List<PhysicsContact>> CollideVertically)
        {
            // push actors
            foreach (PhysicsContact contact in actors)
            {
                PhysicsActor actor = contact.RemoteObject as PhysicsActor;

                // if actor is not moveable, collide
                if (!actor.MoveableVertically[sign])
                {
                    CollideVertically?.Invoke(actors);
                    return false;
                }

                // transfer velocity
                float action = (_strength / actor.GetTotalWeight()) * PhysicsManager.Instance.PhysicsStepSize;
                actor.Velocity += sign * new Vector2(0f, Mathf.Clamp(action, 0f, Mathf.Abs(Velocity.y)));

                float reaction = (actor.GetTotalWeight() / _strength) * PhysicsManager.Instance.PhysicsStepSize;
                Velocity -= sign * new Vector2(0f, Mathf.Clamp(reaction, 0f, Mathf.Abs(Velocity.y)));
            }

            return true;
        }

        public virtual void Squish(List<PhysicsContact> collisions)
        {
            Debug.Log("Squish");
        }

        public virtual void ApplyImpulse(Vector2 impulse)
        {
            _velocity += impulse;
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