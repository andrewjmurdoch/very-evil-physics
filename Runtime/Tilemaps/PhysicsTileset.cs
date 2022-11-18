using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using VED.Tilemaps;

namespace VED.Physics
{
    public partial class PhysicsTileset : Tileset
    {
        public new PhysicsTile[] Tiles => _tiles;
        private new PhysicsTile[] _tiles = null;

        public new PhysicsTileset Init(TilesetDefinition definition)
        {
            // get size of tileset
            int size = (int)((definition.PxWid / Consts.TILE_SIZE) * (definition.PxHei / Consts.TILE_SIZE));
            _tiles = new PhysicsTile[size];

            // get tileset
            int start = definition.RelPath.LastIndexOf('/') + 1;
            int end = definition.RelPath.LastIndexOf('.');
            string name = definition.RelPath.Substring(start, end - start);
            SpriteAtlas spriteAtlas = PhysicsTilesetManager.Instance.TilesetMapper[name];

            // distinguish between collider enums + material enums
            List<EnumTagValue> colliderEnumTagValues = new List<EnumTagValue>();
            List<EnumTagValue> materialEnumTagValues = new List<EnumTagValue>();
            for (int i = 0; i < definition.EnumTags.Count; i++)
            {
                if (definition.EnumTags[i].EnumValueId.Contains("COL_"))
                {
                    colliderEnumTagValues.Add(definition.EnumTags[i]);
                    continue;
                }

                if (definition.EnumTags[i].EnumValueId.Contains("MAT_"))
                {
                    materialEnumTagValues.Add(definition.EnumTags[i]);
                }
            }

            // create tiles
            for (int i = 0; i < size; i++)
            {
                Sprite sprite = spriteAtlas.GetSprite(name + '_' + i.ToString());
                Enums.PhysicsColliderType physicsCollider = GetTileCollider(colliderEnumTagValues, i);
                Enums.PhysicsMaterialType physicsMaterial = GetTileMaterial(materialEnumTagValues, i);

                _tiles[i] = new PhysicsTile().Init(sprite, physicsCollider, physicsMaterial);
            }

            return this;
        }

        private Enums.PhysicsColliderType GetTileCollider(List<EnumTagValue> enumTagValues, int ID)
        {
            foreach (EnumTagValue enumTagValue in enumTagValues)
            {
                string name = enumTagValue.EnumValueId.Substring(enumTagValue.EnumValueId.IndexOf('_') + 1);
                Enums.PhysicsColliderType physicsColliderType = Enum.Parse<Enums.PhysicsColliderType>(name);

                for (int i = 0; i < enumTagValue.TileIds.Count; i++)
                {
                    if (enumTagValue.TileIds[i] == ID)
                    {
                        return physicsColliderType;
                    }
                }
            }

            return Enums.PhysicsColliderType.NONE;
        }

        private Enums.PhysicsMaterialType GetTileMaterial(List<EnumTagValue> enumTagValues, int ID)
        {
            foreach (EnumTagValue enumTagValue in enumTagValues)
            {
                string name = enumTagValue.EnumValueId.Substring(enumTagValue.EnumValueId.IndexOf('_') + 1);
                Enums.PhysicsMaterialType physicsMaterialType = Enum.Parse<Enums.PhysicsMaterialType>(name);

                for (int i = 0; i < enumTagValue.TileIds.Count; i++)
                {
                    if (enumTagValue.TileIds[i] == ID)
                    {
                        return physicsMaterialType;
                    }
                }
            }

            return Enums.PhysicsMaterialType.DEFAULT;
        }
    }
}