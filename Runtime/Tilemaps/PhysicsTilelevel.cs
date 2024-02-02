using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VED.Tilemaps;

namespace VED.Physics
{
    public partial class PhysicsTileLevel : TileLevel
    {
        public Dictionary<long, PhysicsTileLayer> PhysicsTileLayers => _physicsTileLayers;
        private Dictionary<long, PhysicsTileLayer> _physicsTileLayers = new Dictionary<long, PhysicsTileLayer>();

        new public Dictionary<char, List<PhysicsTileLevel>> NeighbourTileLevels => _neighbourTileLevels;
        new protected Dictionary<char, List<PhysicsTileLevel>> _neighbourTileLevels = new Dictionary<char, List<PhysicsTileLevel>>()
        {
            { 'n', new List<PhysicsTileLevel>() },
            { 'e', new List<PhysicsTileLevel>() },
            { 's', new List<PhysicsTileLevel>() },
            { 'w', new List<PhysicsTileLevel>() }
        };


        public Cell[,] Cells => _cells;
        [SerializeField] protected Cell[,] _cells = null;

        public new PhysicsTileLevel Init(Level definition)
        {
            _id = definition.Iid;
            _size = new Vector2(definition.PxWid / TilesetManager.TileSize, definition.PxHei / TilesetManager.TileSize);

            InitCells();
            InitTileLayers(definition);
            InitEntityLayers(definition);

            return this;
        }
        
        public void InitAsync(Level definition, int tileBatchSize, int entityBatchSize, Action<PhysicsTileLevel> callback)
        {
            _id = definition.Iid;
            _size = new Vector2(definition.PxWid / TilesetManager.TileSize, definition.PxHei / TilesetManager.TileSize);

            InitCells();
            InitTileLayersAsync(definition, tileBatchSize, () =>
            {
                InitEntityLayersAsync(definition, entityBatchSize, () =>
                {
                    callback?.Invoke(this);
                });
            });
        }

        public override void InitNeighbours(Level definition)
        {
            PhysicsTileLevelManager physicsTilelevelManager = PhysicsTileLevelManager.Instance;

            // set up neighbours
            _neighbourTileLevels = new Dictionary<char, List<PhysicsTileLevel>>()
            {
                { 'n', new List<PhysicsTileLevel>() },
                { 'e', new List<PhysicsTileLevel>() },
                { 's', new List<PhysicsTileLevel>() },
                { 'w', new List<PhysicsTileLevel>() }
            };
            foreach (NeighbourLevel neighbourLevel in definition.Neighbours)
            {
                if (!physicsTilelevelManager.TileLevels.ContainsKey(neighbourLevel.LevelIid)) continue;

                if (physicsTilelevelManager.TileLevels[neighbourLevel.LevelIid] is PhysicsTileLevel physicsTilelevel)
                    _neighbourTileLevels[neighbourLevel.Dir[0]].Add(physicsTilelevel);
            }

            // set up collision tile layer neighbours
            foreach (KeyValuePair<long, PhysicsTileLayer> collisionTileLayerKVP in _physicsTileLayers)
            {
                Dictionary<char, List<PhysicsTileLayer>> physicsTileLayerNeighbours = new Dictionary<char, List<PhysicsTileLayer>>()
                {
                    { 'n', new List<PhysicsTileLayer>() },
                    { 'e', new List<PhysicsTileLayer>() },
                    { 's', new List<PhysicsTileLayer>() },
                    { 'w', new List<PhysicsTileLayer>() }
                };
                
                foreach (KeyValuePair<char, List<PhysicsTileLevel>> neighbourTileLevelKVP in _neighbourTileLevels)
                {
                    foreach (PhysicsTileLevel neighbourTilelevel in neighbourTileLevelKVP.Value)
                    {
                        if (neighbourTilelevel is PhysicsTileLevel neighbourPhysicsTilelevel)
                        {
                            if (!neighbourPhysicsTilelevel.PhysicsTileLayers.ContainsKey(collisionTileLayerKVP.Key)) continue;

                            physicsTileLayerNeighbours[neighbourTileLevelKVP.Key].Add(neighbourPhysicsTilelevel.PhysicsTileLayers[collisionTileLayerKVP.Key]);
                        }
                    }
                }
                
                collisionTileLayerKVP.Value.InitNeighbours(physicsTileLayerNeighbours);
            }

        }
        
        private void InitCells()
        {
            // set up cells
            int width  = Mathf.CeilToInt(_size.x / Cell.Size);
            int height = Mathf.CeilToInt(_size.y / Cell.Size);

            _cells = new Cell[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    _cells[x, y] = new Cell();
                }
            }

            // assign neighbours
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int dx = -1; dx < 2; dx++)
                    {
                        for (int dy = -1; dy < 2; dy++)
                        {
                            if ((dx == 0 && dy == 0)
                            || (x + dx < 0 || x + dx >= width)
                            || (y + dy < 0 || y + dy >= height))
                            {
                                continue;
                            }

                            _cells[x, y].Neighbours.Add(_cells[x + dx, y + dy]);
                        }
                    }
                }
            }
        }

        protected override void InitTileLayers(Level definition)
        {
            _tileLayers = new Dictionary<string, TileLayer>();
            _physicsTileLayers = new Dictionary<long, PhysicsTileLayer>();

            // find all layers which are not entity layers
            List<LayerInstance> layerDefinitions = new List<LayerInstance>();
            for (int i = 0; i < definition.LayerInstances.Count; i++)
            {
                if (definition.LayerInstances[i].Type != Tilemaps.Consts.ENTITYLAYER_TYPE)
                {
                    layerDefinitions.Add(definition.LayerInstances[i]);
                }
            }

            for (int i = 0; i < layerDefinitions.Count; i++)
            {
                GameObject gameObject = new GameObject("TileLayer: " + layerDefinitions[i].Identifier);
                gameObject.transform.SetParent(transform);
                gameObject.transform.localPosition = Vector3.zero;

                if (layerDefinitions[i].Identifier.ToUpper().Contains(PhysicsTileLayer.KEY))
                {
                    PhysicsTileLayer physicsTilelayer = gameObject.AddComponent<PhysicsTileLayer>().Init(this, layerDefinitions[i], -i);
                    _physicsTileLayers.Add(layerDefinitions[i].LayerDefUid, physicsTilelayer);
                    continue;
                }

                _tileLayers.Add(layerDefinitions[i].Iid, gameObject.AddComponent<TileLayer>().Init(layerDefinitions[i], -i));
            }
        }

        protected override void InitTileLayersAsync(Level definition, int batchSize, Action callback)
        {
            _tileLayers = new Dictionary<string, TileLayer>();
            _physicsTileLayers = new Dictionary<long, PhysicsTileLayer>();

            // find all layers which are not entity layers
            List<LayerInstance> layerDefinitions = new List<LayerInstance>();
            for (int i = 0; i < definition.LayerInstances.Count; i++)
            {
                if (definition.LayerInstances[i].Type != Tilemaps.Consts.ENTITYLAYER_TYPE)
                {
                    layerDefinitions.Add(definition.LayerInstances[i]);
                }
            }

            int count = layerDefinitions.Count;
            if (count <= 0)
            {
                callback?.Invoke();
                return;
            }

            for (int i = 0; i < layerDefinitions.Count; i++)
            {
                int index = i;

                GameObject gameObject = new GameObject("TileLayer: " + layerDefinitions[i].Identifier);
                gameObject.transform.SetParent(transform);
                gameObject.transform.localPosition = Vector3.zero;

                if (layerDefinitions[i].Identifier.ToUpper().Contains(PhysicsTileLayer.KEY))
                {
                    gameObject.AddComponent<PhysicsTileLayer>().InitAsync(this, layerDefinitions[i], -index, batchSize, (PhysicsTileLayer physicsTileLayer) =>
                    {
                        _physicsTileLayers.Add(layerDefinitions[index].LayerDefUid, physicsTileLayer);
                        Join();
                    });
                    continue;
                }

                gameObject.AddComponent<TileLayer>().InitAsync(layerDefinitions[i], -index, batchSize, (TileLayer tileLayer) =>
                {
                    _tileLayers.Add(layerDefinitions[index].Iid, tileLayer);
                    Join();
                });
            }

            void Join()
            {
                if (count <= 0) return;
                count--;
                if (count > 0) return;

                callback?.Invoke();
            }
        }

        public Cell GetCell(Vector2 position)
        {
            Transform transform = this.transform;
            int x = (int)(position.x - transform.position.x);
            int y = -(int)(position.y - transform.position.y);

            int cellx = Mathf.Clamp(x / Cell.Size, 0, (int)_size.x);
            int celly = Mathf.Clamp(y / Cell.Size, 0, (int)_size.y);

            return _cells[cellx, celly];
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(PhysicsTileLevel))]
    public class TilelevelEditor : Editor
    {
        private const float THICKNESS = 2f;

        public void OnSceneGUI()
        {
            // cast
            PhysicsTileLevel tileLevel = target as PhysicsTileLevel;

            // draw
            Handles.color = Color.green;

            int width  = Mathf.CeilToInt(tileLevel.Size.x / PhysicsTileLevel.Cell.Size);
            int height = Mathf.CeilToInt(tileLevel.Size.y / PhysicsTileLevel.Cell.Size);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 A = new Vector2(tileLevel.transform.position.x + x * PhysicsTileLevel.Cell.Size, tileLevel.transform.position.y - y * PhysicsTileLevel.Cell.Size);
                    Vector2 B = A + Vector2.right * PhysicsTileLevel.Cell.Size;
                    Vector2 C = B + Vector2.down * PhysicsTileLevel.Cell.Size;
                    Vector2 D = A + Vector2.down * PhysicsTileLevel.Cell.Size;

                    Handles.DrawLine(A, B, THICKNESS);
                    Handles.DrawLine(B, C, THICKNESS);
                    Handles.DrawLine(C, D, THICKNESS);
                    Handles.DrawLine(D, A, THICKNESS);
                }
            }
        }
    }
#endif
}