using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VED.Tilemaps;

namespace VED.Physics
{
    public partial class PhysicsTilelevel : Tilelevel
    {
        public Dictionary<long, PhysicsTilelayer> PhysicsTilelayers => _physicsTilelayers;
        private Dictionary<long, PhysicsTilelayer> _physicsTilelayers = new Dictionary<long, PhysicsTilelayer>();

        public Cell[,] Cells => _cells;
        [SerializeField] protected Cell[,] _cells = null;

        public new PhysicsTilelevel Init(Level definition)
        {
            _size = new Vector2(definition.PxWid / Tilemaps.Consts.TILE_SIZE, definition.PxHei / Tilemaps.Consts.TILE_SIZE);

            InitCells();
            InitLayers(definition);

            return this;
        }
        
        public override void InitNeighbours(Level definition)
        {
            base.InitNeighbours(definition);

            // set up collision tile layer neighbours
            foreach (KeyValuePair<long, PhysicsTilelayer> collisionTilelayerKVP in _physicsTilelayers)
            {
                Dictionary<char, List<PhysicsTilelayer>> physicsTileLayerNeighbours = new Dictionary<char, List<PhysicsTilelayer>>()
                {
                    { 'n', new List<PhysicsTilelayer>() },
                    { 'e', new List<PhysicsTilelayer>() },
                    { 's', new List<PhysicsTilelayer>() },
                    { 'w', new List<PhysicsTilelayer>() }
                };
                
                foreach (KeyValuePair<char, List<Tilelevel>> neighbourTilelevelKVP in _neighbourTilelevels)
                {
                    foreach (Tilelevel neighbourTilelevel in neighbourTilelevelKVP.Value)
                    {
                        if (neighbourTilelevel is PhysicsTilelevel neighbourPhysicsTilelevel)
                        {
                            if (!neighbourPhysicsTilelevel.PhysicsTilelayers.ContainsKey(collisionTilelayerKVP.Key)) continue;

                            physicsTileLayerNeighbours[neighbourTilelevelKVP.Key].Add(neighbourPhysicsTilelevel.PhysicsTilelayers[collisionTilelayerKVP.Key]);
                        }
                    }
                }
                
                collisionTilelayerKVP.Value.InitNeighbours(physicsTileLayerNeighbours);
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

        protected override void InitLayers(Level definition)
        {
            // find all layers which are not autolayers/int layers/entity layers
            List<LayerInstance> layerDefinitions = new List<LayerInstance>();
            for (int i = 0; i < definition.LayerInstances.Count; i++)
            {
                if (definition.LayerInstances[i].Type == Tilemaps.Consts.TILELAYER_TYPE)
                {
                    layerDefinitions.Add(definition.LayerInstances[i]);
                }
            }

            _tilelayers = new Dictionary<long, Tilelayer>();
            _physicsTilelayers = new Dictionary<long, PhysicsTilelayer>();

            for (int i = 0; i < layerDefinitions.Count; i++)
            {
                GameObject gameObject = new GameObject("Tilelayer: " + layerDefinitions[i].Identifier);
                gameObject.transform.SetParent(transform);
                gameObject.transform.localPosition = Vector3.zero;

                if (layerDefinitions[i].Identifier.ToUpper().Contains(PhysicsTilelayer.KEY))
                {
                    PhysicsTilelayer collisionTilelayer = gameObject.AddComponent<PhysicsTilelayer>().Init(this, layerDefinitions[i], layerDefinitions.Count - i) as PhysicsTilelayer;
                    _physicsTilelayers.Add(layerDefinitions[i].LayerDefUid, collisionTilelayer);
                    continue;
                }

                _tilelayers.Add(layerDefinitions[i].LayerDefUid, gameObject.AddComponent<Tilelayer>().Init(layerDefinitions[i], layerDefinitions.Count - i));
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
    [CustomEditor(typeof(PhysicsTilelevel))]
    public class TilelevelEditor : Editor
    {
        private const float THICKNESS = 2f;

        public void OnSceneGUI()
        {
            // cast
            PhysicsTilelevel tilelevel = target as PhysicsTilelevel;

            // draw
            Handles.color = Color.green;

            int width  = Mathf.CeilToInt(tilelevel.Size.x / PhysicsTilelevel.Cell.Size);
            int height = Mathf.CeilToInt(tilelevel.Size.y / PhysicsTilelevel.Cell.Size);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 A = new Vector2(tilelevel.transform.position.x + x * PhysicsTilelevel.Cell.Size, tilelevel.transform.position.y - y * PhysicsTilelevel.Cell.Size);
                    Vector2 B = A + Vector2.right * PhysicsTilelevel.Cell.Size;
                    Vector2 C = B + Vector2.down * PhysicsTilelevel.Cell.Size;
                    Vector2 D = A + Vector2.down * PhysicsTilelevel.Cell.Size;

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