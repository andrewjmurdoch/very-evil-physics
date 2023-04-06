using UnityEngine;

namespace VED.Physics
{
    [CreateAssetMenu(fileName = "PhysicsManagerSettings", menuName = "VED/Physics/PhysicsManagerSettings", order = 0)]
    public class PhysicsManagerSettings : ScriptableObject
    {
        // physics step size decides the size of 1 unit of movement, set to pixel size for pixel perfect
        // set by default to 1px/1u in 24ppu (1/24)
        public float PhysicsStepSize => _physicsStepSize;
        [SerializeField] private float _physicsStepSize = 0.041666666664f;

        // the current level's atmosphere material - acquired when a player is ungrounded or unattached to any solids
        public PhysicsMaterial AtmospherePhysicsMaterial => _atmospherePhysicsMaterial;
        [SerializeField] private PhysicsMaterial _atmospherePhysicsMaterial = null;

        public PhysicsMaterial DefaultPhysicsMaterial => _defaultPhysicsMaterial;
        [SerializeField] private PhysicsMaterial _defaultPhysicsMaterial;

        public float Gravity => _gravity;
        [SerializeField] private float _gravity = 9.8f;
    }
}