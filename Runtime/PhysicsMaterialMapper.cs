using System;
using System.Collections.Generic;
using UnityEngine;

namespace VED.Physics
{
    [CreateAssetMenu(fileName = "PhysicsMaterialMapper", menuName = "VED/Physics/PhysicsMaterialMapper")]
    public class PhysicsMaterialMapper : ScriptableObject
    {
        [Serializable]
        private struct PhysicsMaterialTypePair
        {
            [SerializeField] public PhysicsMaterial PhysicsMaterial;
            [SerializeField] public Enums.PhysicsMaterialType PhysicsMaterialType; 
        }
        [SerializeField] private List<PhysicsMaterialTypePair> _physicsMaterialTypePairs = new List<PhysicsMaterialTypePair>();

        private Dictionary<Enums.PhysicsMaterialType, PhysicsMaterial> _physicsMaterialDictionary = new Dictionary<Enums.PhysicsMaterialType, PhysicsMaterial>();

        public PhysicsMaterial this[Enums.PhysicsMaterialType physicsMaterialType]
        {
            get
            {
                if (!_physicsMaterialDictionary.ContainsKey(physicsMaterialType)) return null;
                return _physicsMaterialDictionary[physicsMaterialType];
            }
        }

#if UNITY_EDITOR
        public void OnValidate()
        {
            _physicsMaterialDictionary.Clear();
            foreach (PhysicsMaterialTypePair physicsMaterialTypePair in _physicsMaterialTypePairs)
            {
                if (physicsMaterialTypePair.PhysicsMaterial == null) continue;
                if (_physicsMaterialDictionary.ContainsKey(physicsMaterialTypePair.PhysicsMaterialType)) continue;

                _physicsMaterialDictionary.Add(physicsMaterialTypePair.PhysicsMaterialType, physicsMaterialTypePair.PhysicsMaterial);
            }
        }
#endif
    }
}