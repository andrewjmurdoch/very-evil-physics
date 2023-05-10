using System;
using System.Collections.Generic;
using UnityEngine;

namespace VED.Physics
{
    public class PhysicsSolid : PhysicsObject
    {
        public static Action<PhysicsSolid> Spawned;
        public static Action<PhysicsSolid> Despawned;

        protected PhysicsTileLevel.Cell _cell = null;

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

        public override void Deinit()
        {
            base.Deinit();

            _cell?.Solids.Remove(this);
            Despawned?.Invoke(this);
        }

        protected void UpdateNearby()
        {
            _nearby.Clear();

            void RemoveFromCell()
            {
                if (_cell != null)
                {
                    _cell.Solids.Remove(this);
                }
            }

            // find the level this solid is in
            PhysicsTileLevel tileLevel = PhysicsTileLevelManager.Instance.GetTileLevel(Transform.position);
            if (tileLevel == null)
            {
                RemoveFromCell();
                return;
            }

            // find cell this solid is in
            PhysicsTileLevel.Cell cell = tileLevel.GetCell(Transform.position);
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
                _cell.Solids.Add(this);
            }

            // find nearby actors based on cell + neighbour cells
            void AddCellActorsToNearby(PhysicsTileLevel.Cell cell)
            {
                foreach (PhysicsActor physicsActor in cell.Actors)
                {
                    if (!_nearby.Contains(physicsActor))
                    {
                        _nearby.Add(physicsActor);
                    }
                }
            }

            AddCellActorsToNearby(_cell);
            for (int i = 0; i < _cell.Neighbours.Count; i++)
            {
                AddCellActorsToNearby(_cell.Neighbours[i]);
            }

            // remove all ignored objects
            for (int i = _nearby.Count - 1; i >= 0; i--)
            {
                if (_ignored.Contains(_nearby[i]))
                {
                    _nearby.RemoveAt(i);
                }
            }
        }

        public void Move(double x = 0, double y = 0)
        {
            if (Math.Abs(x) > 0 || Math.Abs(y) > 0) UpdateNearby();

            _xRemainder += x;
            _yRemainder += y;
            _xRounded = Math.Round(_xRemainder);
            _yRounded = Math.Round(_yRemainder);
            _xRemainder -= _xRounded;
            _yRemainder -= _yRounded;

            MoveHorizontally(_xRounded);
            MoveVertically(_yRounded);
        }

        public void MoveHorizontally(double amount = 0)
        {
            // move horizontally
            Transform.position += new Vector3((float)amount * PhysicsManager.Instance.PhysicsStepSize, 0);

            if (Math.Abs(amount) > 0)
            {
                float sign = Math.Sign(amount);
                List<PhysicsContact> collisions = CollidingHorizontally(sign, _nearby);

                // push overlapping actors
                foreach (PhysicsContact collision in collisions)
                {
                    PhysicsActor actor = collision.RemoteObject as PhysicsActor;

                    if (actor != null)
                    {
                        // find overlapping area and move by that amount
                        actor.Move(collision.LocalCollider.OverlapHorizontally(sign, collision.RemoteCollider) / PhysicsManager.Instance.PhysicsStepSize, 0, CollideHorizontally: actor.Squish);
                    }
                }

                // if not already pushed, carry attached actors
                foreach (PhysicsContact attachment in _attachments)
                {
                    PhysicsActor actor = attachment.RemoteObject as PhysicsActor;

                    if (actor != null && !collisions.Contains(attachment))
                    {
                        actor.Move(amount, 0);
                    }
                }
            }
        }

        public void MoveVertically(double amount = 0)
        {
            // move vertically
            Transform.position += new Vector3(0, (float)amount * PhysicsManager.Instance.PhysicsStepSize);

            if (Math.Abs(amount) > 0)
            {
                float sign = Math.Sign(amount);
                List<PhysicsContact> collisions = CollidingVertically(sign, _nearby);

                // push overlapping actors
                foreach (PhysicsContact collision in collisions)
                {
                    PhysicsActor actor = collision.RemoteObject as PhysicsActor;

                    if (actor != null)
                    {
                        // find overlapping area and move by that amount
                        actor.Move(0, collision.LocalCollider.OverlapVertically(sign, collision.RemoteCollider) / PhysicsManager.Instance.PhysicsStepSize, CollideVertically: actor.Squish);
                    }
                }

                // if not already pushed, carry attached actors
                foreach (PhysicsContact attachment in _attachments)
                {
                    PhysicsActor actor = attachment.RemoteObject as PhysicsActor;

                    if (actor != null && !collisions.Contains(attachment))
                    {
                        actor.Move(0, amount);
                    }
                }
            }
        }
    }
}