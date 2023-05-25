using System.Collections.Generic;
using UnityEngine;
using VED.Utilities;

namespace VED.Physics
{
    public class PhysicsManager : Singleton<PhysicsManager>
    {
        public PhysicsManagerSettings PhysicsManagerSettings => _physicsManagerSettings;
        private PhysicsManagerSettings _physicsManagerSettings;

        public PhysicsMaterialMapper PhysicsMaterialMapper => _physicsMaterialMapper;
        private PhysicsMaterialMapper _physicsMaterialMapper;

        public float PhysicsStepSize => _physicsManagerSettings.PhysicsStepSize;

        public PhysicsMaterial AtmospherePhysicsMaterial => _physicsManagerSettings.AtmospherePhysicsMaterial;

        public PhysicsMaterial DefaultPhysicsMaterial => _physicsManagerSettings.DefaultPhysicsMaterial;

        public float Gravity => _physicsManagerSettings.Gravity;

        public List<PhysicsSolid> Solids => _solids;
        private List<PhysicsSolid> _solids = new List<PhysicsSolid>();

        public List<PhysicsActor> Actors => _actors;
        private List<PhysicsActor> _actors = new List<PhysicsActor>();

        public List<PhysicsObject> All
        {
            get
            {
                List<PhysicsObject> all = new List<PhysicsObject>();

                all.AddRange(Actors);
                all.AddRange(Solids);

                return all;
            }
        }

        private int _iterator = 0;

        public void Init(PhysicsManagerSettings physicsManagerSettings, PhysicsMaterialMapper physicsMaterialMapper)
        {
            _physicsManagerSettings = physicsManagerSettings;
            _physicsMaterialMapper = physicsMaterialMapper;

            // listen to spawning events
            PhysicsActor.Spawned += AddActor;
            PhysicsActor.Despawned += RemoveActor;

            PhysicsSolid.Spawned += AddSolid;
            PhysicsSolid.Despawned += RemoveSolid;
        }

        private void Deinit()
        {
            // stop listening to spawning events
            PhysicsActor.Spawned -= AddActor;
            PhysicsActor.Despawned -= RemoveActor;

            PhysicsSolid.Spawned -= AddSolid;
            PhysicsSolid.Despawned -= RemoveSolid;
        }

        private void AddActor(PhysicsActor actor)
        {
            if (_actors.Contains(actor)) return;
            actor.Index = _actors.Count;
            _actors.Add(actor);
        }

        private void RemoveActor(PhysicsActor actor)
        {
            if (_iterator >= actor.Index) _iterator--;
            _actors.Remove(actor);
        }

        private void AddSolid(PhysicsSolid solid)
        {
            if (_solids.Contains(solid)) return;
            solid.Index = _solids.Count;
            _solids.Add(solid);
        }

        private void RemoveSolid(PhysicsSolid solid)
        {
            if (_iterator >= solid.Index) _iterator--;
            _solids.Remove(solid);
        }

        public void FixedTick()
        {
            int iterations = 0;
            for (_iterator = _actors.Count - 1; _iterator >= 0; _iterator--)
            {
                PhysicsActor actor = _actors[_iterator];
                actor.FixedTick();
                iterations = Mathf.Max(iterations, Mathf.Max(Mathf.Abs(actor.X), Mathf.Abs(actor.Y)));
            }

            for (int i = 0; i < iterations; i++)
            {
                for (_iterator = _actors.Count - 1; _iterator >= 0; _iterator--)
                {
                    _actors[_iterator].FixedSubTick();
                }
            }
        }
    }
}