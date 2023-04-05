using UnityEngine;

namespace VED.Physics
{
    [CreateAssetMenu(fileName = "PhysicsManagerSettings", menuName = "VED/Physics/PhysicsManagerSettings", order = 0)]
    public class PhysicsManagerSettings : ScriptableObject
    {
        // the current level's atmosphere material - acquired when a player is ungrounded or unattached to any solids
        public PhysicsMaterial AtmospherePhysicsMaterial => _atmospherePhysicsMaterial;
        [SerializeField] private PhysicsMaterial _atmospherePhysicsMaterial = null;

        public PhysicsMaterial DefaultPhysicsMaterial => _defaultPhysicsMaterial;
        [SerializeField] private PhysicsMaterial _defaultPhysicsMaterial;

        public float Gravity => _gravity;
        [SerializeField] private float _gravity = 9.8f;
    }
}