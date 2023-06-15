using System;
using System.Collections.Generic;
using UnityEngine;
using VED.Tilemaps;
using VED.Utilities;

namespace VED.Physics
{
    public class PhysicsTileLevelManager : Singleton<PhysicsTileLevelManager>
    {
        public Dictionary<string, PhysicsTileLevel> TileLevels => _tileLevels;
        private Dictionary<string, PhysicsTileLevel> _tileLevels = new Dictionary<string, PhysicsTileLevel>();

        private const int ASYNC_BATCH_SIZE_TILE = 100;
        private const int ASYNC_BATCH_SIZE_ENTITY = 10;

        public PhysicsTileLevelManager Init(List<Level> definitions)
        {
            _tileLevels = new Dictionary<string, PhysicsTileLevel>();

            for (int i = 0; i < definitions.Count; i++)
            {
                GameObject gameObject = new GameObject("TileLevel: " + definitions[i].Identifier);
                gameObject.transform.localPosition = new Vector2(definitions[i].WorldX / Consts.TILE_SIZE, -definitions[i].WorldY / Consts.TILE_SIZE);

                _tileLevels.Add(definitions[i].Iid, gameObject.AddComponent<PhysicsTileLevel>().Init(definitions[i]));
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                _tileLevels[definitions[i].Iid].InitNeighbours(definitions[i]);
            }

            return this;
        }

        public void InitAsync(List<Level> definitions, Action callback, int tileBatchSize = ASYNC_BATCH_SIZE_TILE, int entityBatchSize = ASYNC_BATCH_SIZE_ENTITY)
        {
            _tileLevels = new Dictionary<string, PhysicsTileLevel>();

            int count = definitions.Count;
            if (count <= 0)
            {
                callback?.Invoke();
                return;
            }

            for (int i = 0; i < definitions.Count; i++)
            {
                int index = i;

                GameObject gameObject = new GameObject("TileLevel: " + definitions[i].Identifier);
                gameObject.transform.localPosition = new Vector2(definitions[i].WorldX / Consts.TILE_SIZE, -definitions[i].WorldY / Consts.TILE_SIZE);

                gameObject.AddComponent<PhysicsTileLevel>().InitAsync(definitions[i], tileBatchSize, entityBatchSize, (PhysicsTileLevel tileLevel) =>
                {
                    _tileLevels.Add(definitions[index].Iid, tileLevel);
                    Join();
                });
            }

            void Join()
            {
                if (count <= 0) return;
                count--;
                if (count > 0) return;

                for (int i = 0; i < definitions.Count; i++)
                {
                    _tileLevels[definitions[i].Iid].InitNeighbours(definitions[i]);
                }

                callback?.Invoke();
            }
        }

        public PhysicsTileLevel GetTileLevel(Vector2 position)
        {
            foreach (PhysicsTileLevel physicsTilelevel in _tileLevels.Values)
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