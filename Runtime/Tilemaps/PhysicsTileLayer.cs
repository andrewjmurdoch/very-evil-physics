using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VED.Tilemaps;

namespace VED.Physics
{
    public partial class PhysicsTileLayer : TileLayer
    {
        public const string KEY = "PHYSICS";

        public Dictionary<char, List<PhysicsTileLayer>> NeighbourCollisionTilelayers => _neighbourCollisionTilelayers;
        private Dictionary<char, List<PhysicsTileLayer>> _neighbourCollisionTilelayers = new Dictionary<char, List<PhysicsTileLayer>>()
        {
            { 'n', new List<PhysicsTileLayer>() },
            { 'e', new List<PhysicsTileLayer>() },
            { 's', new List<PhysicsTileLayer>() },
            { 'w', new List<PhysicsTileLayer>() },
        };
        
        new public PhysicsTile[,] Tiles => _tiles;
        new protected PhysicsTile[,] _tiles = null;
        
        public PhysicsTileLayer Init(PhysicsTileLevel tileLevel, LayerInstance definition, int sortingOrder)
        {
            _id = definition.Iid;

            if (definition.Type == Tilemaps.Consts.TILELAYER_TYPE) return InitTilelayer(tileLevel, definition, sortingOrder);
            if (definition.Type == Tilemaps.Consts.AUTOLAYER_TYPE) return InitAutolayer(tileLevel, definition, sortingOrder);
            if (definition.Type == Tilemaps.Consts.INTLAYER_TYPE ) return InitIntlayer (tileLevel, definition, sortingOrder);
            return this;
        }
        
        public void InitAsync(PhysicsTileLevel tileLevel, LayerInstance definition, int sortingOrder, int batchSize, Action<PhysicsTileLayer> callback)
        {
            _id = definition.Iid;

                 if (definition.Type == Tilemaps.Consts.TILELAYER_TYPE) InitTilelayerAsync(tileLevel, definition, sortingOrder, batchSize, callback);
            else if (definition.Type == Tilemaps.Consts.AUTOLAYER_TYPE) InitAutolayerAsync(tileLevel, definition, sortingOrder, batchSize, callback);
            else if (definition.Type == Tilemaps.Consts.INTLAYER_TYPE ) InitIntlayerAsync (tileLevel, definition, sortingOrder, batchSize, callback);
        }

        public void InitNeighbours(Dictionary<char, List<PhysicsTileLayer>> neighbourCollisionTilelayers)
        {
            _neighbourCollisionTilelayers = neighbourCollisionTilelayers;
        }

        #region Synchronous
        protected PhysicsTileLayer InitTilelayer(PhysicsTileLevel tileLevel, LayerInstance definition, int sortingOrder)
        {
            if (!PhysicsTilesetManager.Instance.Tilesets.TryGetValue((int)definition.TilesetDefUid, out PhysicsTileset tileset))
            {
                return this;
            }

            _tiles = new PhysicsTile[definition.CWid, definition.CHei];
            for (int i = 0; i < definition.GridTiles.Count; i++)
            {
                PhysicsTileset.PhysicsTile tilesetTile = tileset.Tiles[definition.GridTiles[i].T];

                int x = (int)definition.GridTiles[i].Px[0] / TilesetManager.TileSize;
                int y = (int)definition.GridTiles[i].Px[1] / TilesetManager.TileSize;

                InitTile(tileLevel, tilesetTile, x, y, sortingOrder);
            }

            return this;
        }

        protected PhysicsTileLayer InitAutolayer(PhysicsTileLevel tileLevel, LayerInstance definition, int sortingOrder)
        {
            if (!PhysicsTilesetManager.Instance.Tilesets.TryGetValue((int)definition.TilesetDefUid, out PhysicsTileset tileset))
            {
                return this;
            }

            _tiles = new PhysicsTile[definition.CWid, definition.CHei];
            for (int i = 0; i < definition.AutoLayerTiles.Count; i++)
            {
                PhysicsTileset.PhysicsTile tilesetTile = tileset.Tiles[definition.AutoLayerTiles[i].T];

                int x = (int)definition.AutoLayerTiles[i].Px[0] / TilesetManager.TileSize;
                int y = (int)definition.AutoLayerTiles[i].Px[1] / TilesetManager.TileSize;

                InitTile(tileLevel, tilesetTile, x, y, sortingOrder);
            }

            return this;
        }

        protected PhysicsTileLayer InitIntlayer(PhysicsTileLevel tileLevel, LayerInstance definition, int sortingOrder)
        {
            if (!PhysicsTilesetManager.Instance.Tilesets.TryGetValue((int)definition.TilesetDefUid, out PhysicsTileset tileset))
            {
                return this;
            }

            _tiles = new PhysicsTile[definition.CWid, definition.CHei];
            for (int i = 0; i < definition.AutoLayerTiles.Count; i++)
            {
                PhysicsTileset.PhysicsTile tilesetTile = tileset.Tiles[definition.AutoLayerTiles[i].T];

                int x = (int)definition.AutoLayerTiles[i].Px[0] / TilesetManager.TileSize;
                int y = (int)definition.AutoLayerTiles[i].Px[1] / TilesetManager.TileSize;

                InitTile(tileLevel, tilesetTile, x, y, sortingOrder);
            }

            return this;
        }
        #endregion

        #region Asynchronous
        protected void InitTilelayerAsync(PhysicsTileLevel tileLevel, LayerInstance definition, int sortingOrder, int batchSize, Action<PhysicsTileLayer> callback)
        {
            if (!PhysicsTilesetManager.Instance.Tilesets.TryGetValue((int)definition.TilesetDefUid, out PhysicsTileset tileset))
            {
                callback?.Invoke(this);
                return;
            }

            _tiles = new PhysicsTile[definition.CWid, definition.CHei];

            int count = definition.GridTiles.Count;
            if (count <= 0)
            {
                callback?.Invoke(this);
                return;
            }
            int batches = (count / batchSize) + Math.Clamp(count % batchSize, 0, 1);

            void InstantiateTileAsync(int index)
            {
                PhysicsTileset.PhysicsTile tilesetTile = tileset.Tiles[definition.GridTiles[index].T];

                int x = (int)definition.GridTiles[index].Px[0] / TilesetManager.TileSize;
                int y = (int)definition.GridTiles[index].Px[1] / TilesetManager.TileSize;

                InitTile(tileLevel, tilesetTile, x, y, sortingOrder);
            }

            IEnumerator InstatiateTileBatchesAsync()
            {
                for (int i = 0; i < batches; i++)
                {
                    for (int j = 0; j < batchSize && (i * batchSize) + j < count; j++)
                    {
                        InstantiateTileAsync((i * batchSize) + j);
                    }
                    yield return null;
                }

                callback?.Invoke(this);
            }

            StartCoroutine(InstatiateTileBatchesAsync());
        }

        protected void InitAutolayerAsync(PhysicsTileLevel tileLevel, LayerInstance definition, int sortingOrder, int batchSize, Action<PhysicsTileLayer> callback)
        {
            if (!PhysicsTilesetManager.Instance.Tilesets.TryGetValue((int)definition.TilesetDefUid, out PhysicsTileset tileset))
            {
                callback?.Invoke(this);
                return;
            }

            _tiles = new PhysicsTile[definition.CWid, definition.CHei];

            int count = definition.AutoLayerTiles.Count;
            if (count <= 0)
            {
                callback?.Invoke(this);
                return;
            }
            int batches = (count / batchSize) + Math.Clamp(count % batchSize, 0, 1);

            void InstantiateTileAsync(int index)
            {
                PhysicsTileset.PhysicsTile tilesetTile = tileset.Tiles[definition.AutoLayerTiles[index].T];

                int x = (int)definition.AutoLayerTiles[index].Px[0] / TilesetManager.TileSize;
                int y = (int)definition.AutoLayerTiles[index].Px[1] / TilesetManager.TileSize;

                InitTile(tileLevel, tilesetTile, x, y, sortingOrder);
            }

            IEnumerator InstatiateTileBatchesAsync()
            {
                for (int i = 0; i < batches; i++)
                {
                    for (int j = 0; j < batchSize && (i * batchSize) + j < count; j++)
                    {
                        InstantiateTileAsync((i * batchSize) + j);
                    }
                    yield return null;
                }

                callback?.Invoke(this);
            }

            StartCoroutine(InstatiateTileBatchesAsync());
        }

        protected void InitIntlayerAsync(PhysicsTileLevel tileLevel, LayerInstance definition, int sortingOrder, int batchSize, Action<PhysicsTileLayer> callback)
        {
            if (!PhysicsTilesetManager.Instance.Tilesets.TryGetValue((int)definition.TilesetDefUid, out PhysicsTileset tileset))
            {
                callback?.Invoke(this);
                return;
            }

            _tiles = new PhysicsTile[definition.CWid, definition.CHei];

            int count = definition.AutoLayerTiles.Count;
            if (count <= 0)
            {
                callback?.Invoke(this);
                return;
            }
            int batches = (count / batchSize) + Math.Clamp(count % batchSize, 0, 1);

            void InstantiateTileAsync(int index)
            {
                PhysicsTileset.PhysicsTile tilesetTile = tileset.Tiles[definition.AutoLayerTiles[index].T];

                int x = (int)definition.AutoLayerTiles[index].Px[0] / TilesetManager.TileSize;
                int y = (int)definition.AutoLayerTiles[index].Px[1] / TilesetManager.TileSize;

                InitTile(tileLevel, tilesetTile, x, y, sortingOrder);
            }

            IEnumerator InstatiateTileBatchesAsync()
            {
                for (int i = 0; i < batches; i++)
                {
                    for (int j = 0; j < batchSize && (i * batchSize) + j < count; j++)
                    {
                        InstantiateTileAsync((i * batchSize) + j);
                    }
                    yield return null;
                }

                callback?.Invoke(this);
            }

            StartCoroutine(InstatiateTileBatchesAsync());
        }
        #endregion

        protected PhysicsTile InitTile(PhysicsTileLevel tileLevel, PhysicsTileset.PhysicsTile tilesetTile, int x, int y, int sortingOrder)
        {
            GameObject gameObject = new GameObject("Tile [" + x + ", " + y + "]");
            gameObject.transform.SetParent(transform);
            Vector2 offset = (Vector2.right + Vector2.down) * (1f / 2f);
            gameObject.transform.localPosition = new Vector2(x, -y) + offset;

            PhysicsTile tile = gameObject.AddComponent<PhysicsTile>().Init(tilesetTile, sortingOrder);

            int cellx = x / PhysicsTileLevel.Cell.Size;
            int celly = y / PhysicsTileLevel.Cell.Size;
            tileLevel.Cells[cellx, celly].Tiles.Add(tile);

            _tiles[x, y] = tile;
            return tile;
        }

        public List<PhysicsTile> GetTilesNearby(Vector2 position, int range = Tilemaps.Consts.NEARBY_TILE_RANGE)
        {
            return GetTilesNearby(position, range, new List<PhysicsTileLayer>());
        }
        
        public List<PhysicsTile> GetTilesNearby(Vector2 position, int range, List<PhysicsTileLayer> consideredCollisionTileLayers)
        {
            List<PhysicsTile> tiles = new List<PhysicsTile>();
            
            int x =  (int)(position.x - transform.position.x);
            int y = -(int)(position.y - transform.position.y);
            int h = _tiles.GetLength(0) - 1;
            int v = _tiles.GetLength(1) - 1;

            if (   x + range <  0
                && x - range >= h
                && y + range <  0
                && y - range >= v)
            {
                return tiles;
            } 
            
            int hMin = Math.Max(Mathf.Min(x - range, h), 0);
            int vMin = Math.Max(Mathf.Min(y - range, v), 0);
            int hMax = Math.Min(Mathf.Max(x + range, 0), h);
            int vMax = Math.Min(Mathf.Max(y + range, 0), v);
            
            for (int j = hMin; j <= hMax; j++)
            {
                for (int i = vMin; i <= vMax; i++)
                {
                    if (Tiles[j, i] == null) continue;
                    
                    tiles.Add(Tiles[j, i]);
                }
            }
            
            consideredCollisionTileLayers.Add(this);
            tiles.AddRange(GetNeighbourTilesNearby(position, range, consideredCollisionTileLayers));

            return tiles;
        }

        private List<PhysicsTile> GetNeighbourTilesNearby(Vector2 position, int range, List<PhysicsTileLayer> consideredCollisionTileLayers)
        {
            List<PhysicsTile> tiles = new List<PhysicsTile>();
            
            int x =  (int)(position.x - transform.position.x);
            int y = -(int)(position.y - transform.position.y);
            int h = _tiles.GetLength(0) - 1;
            int v = _tiles.GetLength(1) - 1;

            void GetNeighbourTiles(char direction)
            {
                foreach (PhysicsTileLayer neighbourCollisionTilelayer in _neighbourCollisionTilelayers[direction])
                {
                    if (consideredCollisionTileLayers.Contains(neighbourCollisionTilelayer)) continue;
                    tiles.AddRange(neighbourCollisionTilelayer.GetTilesNearby(position, range, consideredCollisionTileLayers));
                }
            }
            
            // range extends to right of level
            if (x + range >= h) GetNeighbourTiles('e');
            
            // range extends to left of level
            if (x - range <  0) GetNeighbourTiles('w');
            
            // range extends above level
            if (y - range <  0) GetNeighbourTiles('n');
            
            // range extends below level
            if (y + range >= v) GetNeighbourTiles('s');

            return tiles;
        }
    }
}