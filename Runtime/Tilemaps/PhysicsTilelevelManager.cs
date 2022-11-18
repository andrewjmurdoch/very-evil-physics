using System.Collections.Generic;
using UnityEngine;
using VED.Tilemaps;
using VED.Utilities;

namespace VED.Physics
{
    public class PhysicsTilelevelManager : Singleton<PhysicsTilelevelManager>
    {
        public Dictionary<string, PhysicsTilelevel> Tilelevels => _tilelevels;
        private Dictionary<string, PhysicsTilelevel> _tilelevels = new Dictionary<string, PhysicsTilelevel>();

        public PhysicsTilelevelManager Init(List<Level> definitions)
        {
            _tilelevels = new Dictionary<string, PhysicsTilelevel>();

            for (int i = 0; i < definitions.Count; i++)
            {
                GameObject gameObject = new GameObject("Tilelevel: " + definitions[i].Identifier);
                gameObject.transform.localPosition = new Vector2(definitions[i].WorldX / Tilemaps.Consts.TILE_SIZE, -definitions[i].WorldY / Tilemaps.Consts.TILE_SIZE);

                _tilelevels.Add(definitions[i].Iid, gameObject.AddComponent<PhysicsTilelevel>().Init(definitions[i]));
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                _tilelevels[definitions[i].Iid].InitNeighbours(definitions[i]);
            }

            return this;
        }

        public PhysicsTilelevel GetTilelevel(Vector2 position)
        {
            foreach (PhysicsTilelevel physicsTilelevel in _tilelevels.Values)
            {
                if (   (position.x >= physicsTilelevel.transform.position.x)
                    && (position.x <  physicsTilelevel.transform.position.x + physicsTilelevel.Size.x)
                    && (position.y <= physicsTilelevel.transform.position.y)
                    && (position.y >  physicsTilelevel.transform.position.y - physicsTilelevel.Size.y)
                   )
                {
                    return physicsTilelevel;
                }
            }
            return null;
        }
    }
}