using System;
using System.Collections.Generic;
using UnityEngine;

namespace VED.Physics
{
    [CreateAssetMenu(fileName = "PhysicsMaterialMapper", menuName = "VED/Physics/PhysicsMaterialMapper")]
    public class PhysicsMaterialMapper : ScriptableObject
    {
        [Serializable]
        private struct PhysicsMaterialData
        {
            [SerializeField] public string ID;
            [SerializeField] public PhysicsMaterial PhysicsMaterial;

            public PhysicsMaterialData(string id, PhysicsMaterial physicsMaterial)
            {
                ID = id;
                PhysicsMaterial = physicsMaterial;
            }
        }
        [SerializeField] private List<PhysicsMaterialData> _physicsMaterialData = new List<PhysicsMaterialData>();

        private Dictionary<string, PhysicsMaterial> _physicsMaterialDictionary = null;

        public PhysicsMaterial this[string ID]
        {
            get
            {
                if (_physicsMaterialDictionary == null) InitPhysicsMaterialDictionary();
                if (_physicsMaterialDictionary.TryGetValue(ID, out PhysicsMaterial physicsMaterial))
                {
                    return physicsMaterial;
                }
                return null;
            }
        }

        private void InitPhysicsMaterialDictionary()
        {
            _physicsMaterialDictionary = new Dictionary<string, PhysicsMaterial>();

            foreach (PhysicsMaterialData physicsMaterialData in _physicsMaterialData)
            {
                if (physicsMaterialData.PhysicsMaterial == null) continue;
                if (_physicsMaterialDictionary.ContainsKey(physicsMaterialData.ID)) continue;

                _physicsMaterialDictionary.Add(physicsMaterialData.ID, physicsMaterialData.PhysicsMaterial);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            bool found = false;
            for (int i = 0; i < _physicsMaterialData.Count; i++)
            {
                if (_physicsMaterialData[i].ID != Consts.DEFAULT_MATERIAL_ID) continue;
                found = true;
                break;
            }

            if (!found)
            {
                _physicsMaterialData.Add(new PhysicsMaterialData(Consts.DEFAULT_MATERIAL_ID, null));
            }
        }
#endif
    }
}