using System;
using UnityEngine;

namespace VED
{
    [CreateAssetMenu(fileName = "PhysicsMaterial", menuName = "VED/Physics/PhysicsMaterial", order = 0)]
    public class PhysicsMaterial : ScriptableObject
    {
        public float Friction { get => _friction; set => _friction = value; }
        [Range(0, 2)]
        [SerializeField]
        private float _friction = 0f; // potential to slow a colliding object 

        public float Traction { get => _traction; set => _traction = value; }
        [Range(0, 2)]
        [SerializeField]
        private float _traction = 0f; // potential to allow colliding objects to propel themselves

        public float Elasticity { get => _elasticity; set => _elasticity = value; }
        [Range(0, 2)]
        [SerializeField]
        private float _elasticity = 0f; // potential to bounce colliding objects

        public static PhysicsMaterial Zero => CreateInstance<PhysicsMaterial>();

        public PhysicsMaterial(int friction, int traction, int elasticity)
        {
            _friction = friction;
            _traction = traction;
            _elasticity = elasticity;
        }

        public static PhysicsMaterial operator +(PhysicsMaterial a, PhysicsMaterial b)
        {
            PhysicsMaterial physicsMaterial = CreateInstance<PhysicsMaterial>();
            physicsMaterial.Friction = a.Friction + b.Friction;
            physicsMaterial.Traction = a.Traction + b.Traction;
            physicsMaterial.Elasticity = a.Elasticity + b.Elasticity;

            return physicsMaterial;
        }

        public static PhysicsMaterial operator -(PhysicsMaterial a, PhysicsMaterial b)
        {
            PhysicsMaterial physicsMaterial = CreateInstance<PhysicsMaterial>();
            physicsMaterial.Friction = a.Friction - b.Friction;
            physicsMaterial.Traction = a.Traction - b.Traction;
            physicsMaterial.Elasticity = a.Elasticity - b.Elasticity;

            return physicsMaterial;
        }

        public static PhysicsMaterial operator *(PhysicsMaterial a, PhysicsMaterial b)
        {
            PhysicsMaterial physicsMaterial = CreateInstance<PhysicsMaterial>();
            physicsMaterial.Friction = a.Friction * b.Friction;
            physicsMaterial.Traction = a.Traction * b.Traction;
            physicsMaterial.Elasticity = a.Elasticity * b.Elasticity;

            return physicsMaterial;
        }

        public static PhysicsMaterial operator /(PhysicsMaterial a, PhysicsMaterial b)
        {
            PhysicsMaterial physicsMaterial = CreateInstance<PhysicsMaterial>();
            physicsMaterial.Friction = a.Friction / b.Friction;
            physicsMaterial.Traction = a.Traction / b.Traction;
            physicsMaterial.Elasticity = a.Elasticity / b.Elasticity;

            return physicsMaterial;
        }

        public static PhysicsMaterial operator /(PhysicsMaterial a, float b)
        {
            PhysicsMaterial physicsMaterial = CreateInstance<PhysicsMaterial>();
            physicsMaterial.Friction = a.Friction / b;
            physicsMaterial.Traction = a.Traction / b;
            physicsMaterial.Elasticity = a.Elasticity / b;

            return physicsMaterial;
        }
    }
}