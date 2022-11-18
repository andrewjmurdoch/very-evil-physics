using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VED.Utilities;

namespace VED.Physics
{
    public class PhysicsManager : Singleton<PhysicsManager>
    {
        public PhysicsManagerSettings PhysicsManagerSettings => _physicsManagerSettings;
        private PhysicsManagerSettings _physicsManagerSettings;

        public PhysicsMaterialMapper PhysicsMaterialMapper => _physicsMaterialMapper;
        private PhysicsMaterialMapper _physicsMaterialMapper;

        // physics step size decides the size of 1 unit of movement, set to pixel size for pixel perfect
        // set by default to 1px/1u in 24ppu (1/24)
        public float PhysicsStepSize => _physicsStepSize;
        private float _physicsStepSize = 0.041666666664f;

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
            _actors.Add(actor);
        }

        private void RemoveActor(PhysicsActor actor)
        {
            _actors.Remove(actor);
        }

        private void AddSolid(PhysicsSolid solid)
        {
            _solids.Add(solid);
        }

        private void RemoveSolid(PhysicsSolid solid)
        {
            _solids.Remove(solid);
        }

        public void FixedTick()
        {
            for (int i = 0; i < _actors.Count; i++) _actors[i].FixedTick();
        }
    }
}