using System.Collections.Generic;
using VED.Tilemaps;
using VED.Utilities;

namespace VED.Physics
{
    public class PhysicsTilesetManager : Singleton<PhysicsTilesetManager>
    {
        public TilesetMapper TilesetMapper => _tilesetMapper;
        private TilesetMapper _tilesetMapper = null;

        public Dictionary<long, PhysicsTileset> Tilesets => _tilesets;
        private Dictionary<long, PhysicsTileset> _tilesets = new Dictionary<long, PhysicsTileset>();

        public void Init(TilesetManagerSettings tilesetManagerSettings, TilesetMapper tilesetMapper, List<TilesetDefinition> definitions)
        {
            TilesetManager.Instance.Init(tilesetManagerSettings, tilesetMapper, definitions);

            _tilesetMapper = tilesetMapper;

            _tilesets = new Dictionary<long, PhysicsTileset>();
            foreach (TilesetDefinition tilesetDefinition in definitions)
            {
                if (tilesetDefinition.Tags.Contains(PhysicsTileLayer.KEY))
                {
                    PhysicsTileset tileset = new PhysicsTileset().Init(tilesetDefinition);
                    if (tileset == null) continue;

                    _tilesets.Add(tilesetDefinition.Uid, tileset);
                }
            }
        }
    }
}