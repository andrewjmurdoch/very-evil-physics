using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VED.Physics
{
    public class PhysicsRay : MonoBehaviour
    {
        private const float STEP = 1f;
        private const float HASH_STEP = 0.01f;

        private Vector2 _origin = Vector2.zero;
        private Vector2 _direction = Vector2.zero;
        private float _magnitude = 0f;

        private List<PhysicsObject> _ignored = null;
        private List<PhysicsObject> _focused = null;

        public PhysicsRay(Vector2 origin, Vector2 direction, float magnitude, List<PhysicsObject> ignored = null, List<PhysicsObject> focused = null)
        {
            _origin = origin;
            _direction = direction;
            _magnitude = magnitude;
            _ignored = ignored;
            _focused = focused;
        }

        public bool Cast(out PhysicsObject hit)
        {
            return Cast(out hit, _origin, _direction, _magnitude, _ignored, _focused);
        }

        public static bool Cast(out PhysicsObject hit, Vector2 origin, Vector2 direction, float magnitude, List<PhysicsObject> ignored = null, List<PhysicsObject> focused = null)
        {
            hit = null;

            // create ray
            PhysicsEdge physicsEdge = new PhysicsEdge(origin, origin + (direction * magnitude));

            // create sorted list of physics objects to check collision
            // sorted based on proximity to origin of ray
            // ignored specified physics objects, if necessary
            // select only specific physics objects, if necessary
            SortedList<float, PhysicsObject> physicsObjects = new SortedList<float, PhysicsObject>();
            void AddPhysicsObject(PhysicsObject physicsObject)
            {
                if (ignored != null &&  ignored.Contains(physicsObject)) return;
                if (focused != null && !focused.Contains(physicsObject)) return;

                float magnitude = ((Vector2)physicsObject.Transform.position - origin).magnitude;
                while (physicsObjects.ContainsKey(magnitude)) magnitude += HASH_STEP;

                physicsObjects.Add(magnitude, physicsObject);
            }

            List<PhysicsTilelevel.Cell> cells = new List<PhysicsTilelevel.Cell>();
            void AddCell(PhysicsTilelevel.Cell cell)
            {
                if (cells.Contains(cell)) return;
                cells.Add(cell);
            }

            // loop through world space
            // find any cells ray will intersect
            // find any neighbouring cells, of cells ray will intersect
            PhysicsTilelevel physicsTilelevel = null;
            List<PhysicsTilelayer> physicsTilelayers = new List<PhysicsTilelayer>();
            for (int i = 0; i < (int)magnitude; i += (int)STEP)
            {
                Vector2 position = origin + (direction * i);

                PhysicsTilelevel newPhysicsTilelevel = PhysicsTilelevelManager.Instance.GetTilelevel(position);
                if (newPhysicsTilelevel == null) continue;
                if (newPhysicsTilelevel != physicsTilelevel)
                {
                    // update physicsTilelayers
                    physicsTilelevel = newPhysicsTilelevel;
                    physicsTilelayers = physicsTilelevel.PhysicsTilelayers.Values.ToList();
                }

                // add nearby tiles to physics objects to check
                for (int j = 0; j < physicsTilelayers.Count; j++)
                {
                    List<PhysicsTilelayer.PhysicsTile> nearbyTiles = physicsTilelayers[j].GetTilesNearby(position, 1);
                    for (int k = 0; k < nearbyTiles.Count; k++)
                    {
                        AddPhysicsObject(nearbyTiles[k]);
                    }
                }

                // find all cells nearby ray
                PhysicsTilelevel.Cell cell = physicsTilelevel.GetCell(position);
                if (cell == null) continue;

                AddCell(cell);

                for (int j = 0; j < cell.Neighbours.Count; j++)
                {
                    AddCell(cell.Neighbours[j]);
                }
            }

            // loop through relevant cells and collate physics objects
            for (int i = 0; i < cells.Count; i++)
            {
                PhysicsTilelevel.Cell cell = cells[i];

                // add solids
                for (int j = 0; j < cell.Solids.Count; j++)
                {
                    AddPhysicsObject(cell.Solids[j]);
                }

                // add actors
                for (int j = 0; j < cell.Actors.Count; j++)
                {
                    AddPhysicsObject(cell.Actors[j]);
                }
            }

            // loop through ordered list and find first collision
            for (int i = 0; i < physicsObjects.Values.Count; i++)
            {
                PhysicsObject physicsObject = physicsObjects.Values[i];
                for (int j = 0; j < physicsObject.Colliders.Count; j++)
                {
                    if (physicsEdge.Colliding(physicsObject.Colliders[j]))
                    {
                        hit = physicsObject;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
