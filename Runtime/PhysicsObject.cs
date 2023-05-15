using System.Collections.Generic;
using UnityEngine;
using VED.Utilities;

namespace VED.Physics
{
    public abstract class PhysicsObject : MonoBehaviour
    {
        // the physics object's default physics material
        public PhysicsMaterial PhysicsMaterial => _physicsMaterial;
        [SerializeField] protected PhysicsMaterial _physicsMaterial = null;

        // the physics object's current physics material
        public PhysicsMaterial CurrentPhysicsMaterial { get => _currentPhysicsMaterial; set => _currentPhysicsMaterial = value; }
        protected PhysicsMaterial _currentPhysicsMaterial = null;

        public Vector2 Velocity { get => _velocity; set => _velocity = value; }
        protected Vector2 _velocity = new Vector2(0, 0);

        // this physics object's PhysicsColliders
        public List<PhysicsCollider> Colliders => _colliders;
        [SerializeField] protected List<PhysicsCollider> _colliders = new List<PhysicsCollider>();

        // physics objects attached to this physics object
        public List<PhysicsContact> Attachments => _attachments;
        [SerializeField] protected List<PhysicsContact> _attachments = new List<PhysicsContact>();

        // physics objects ignored by this physics object
        public List<PhysicsObject> Ignored => _ignored;
        protected List<PhysicsObject> _ignored = new List<PhysicsObject>();
        protected Dictionary<PhysicsObject, Timer> _ignoredTimers = new Dictionary<PhysicsObject, Timer>();

        // physics objects nearby this physics object - relevant for collision detection
        public List<PhysicsObject> Nearby => _nearby;
        protected List<PhysicsObject> _nearby = new List<PhysicsObject>();

        // the margin of error allowed in collision detection
        protected const float COLLISION_ERROR_MARGIN = 0.0035f;
        protected const float NEARBY_DISTANCE = 2f;

        protected double _xRemainder = 0; // remaining fraction of rounded x movement
        protected double _yRemainder = 0; // remaining fraction of rounded y movement
        public int X => (int)_xRounded;
        protected double _xRounded = 0; // rounded int of x movement
        public int Y => (int)_yRounded;
        protected double _yRounded = 0; // rounded int of y movement
        
        public Transform Transform => _transform ??= transform;
        private Transform _transform = null;

        public virtual void Init()
        {
            _physicsMaterial = _physicsMaterial == null ? PhysicsManager.Instance.DefaultPhysicsMaterial : _physicsMaterial;
            _currentPhysicsMaterial = _physicsMaterial;
        }

        public virtual void Deinit()
        {
            for (int i = _attachments.Count - 1; i >= 0; i--)
            {
                Detach(_attachments[i]);
            }
            _attachments.Clear();
        }

        public void Attach(PhysicsContact attachment)
        {
            if (attachment.LocalObject != this)
            {
                attachment = attachment.Invert();
            }

            foreach (PhysicsContact contact in attachment.RemoteObject.Attachments)
            {
                if (contact.RemoteObject == this)
                {
                    return;
                }
            }

            foreach (PhysicsContact contact in _attachments)
            {
                if (contact.RemoteObject == attachment.RemoteObject)
                {
                    return;
                }
            }
            
            _attachments.Add(attachment);
        }

        public void Detach(PhysicsContact attachment)
        {
            if (attachment == null) return;

            if (attachment.RemoteObject == this)
            {
                attachment = attachment.Invert();
            }

            attachment.RemoteObject.Detach(this);
            Detach(attachment.RemoteObject);
        }

        public void Detach(PhysicsObject attachment)
        {
            _attachments.RemoveAll(a => a.RemoteObject == attachment);
        }

        public void Ignore(PhysicsObject physicsObject, float time = -1f)
        {
            Timer timer;

            if (time <= 0f)
            {
                if (_ignored.Contains(physicsObject) && _ignoredTimers.TryGetValue(physicsObject, out timer))
                {
                    timer.Reset();
                    _ignoredTimers.Remove(physicsObject);
                    return;
                }

                _ignored.Add(physicsObject);
                return;
            }

            if (_ignored.Contains(physicsObject))
            {
                _ignoredTimers[physicsObject].Set(callback: () => { Unignore(physicsObject); }, duration: time);
                return;
            }

            _ignored.Add(physicsObject);
            timer = new Timer(time);
            timer.Set(callback: () => { Unignore(physicsObject); });
            _ignoredTimers.Add(physicsObject, timer);
        }

        public void Ignore(List<PhysicsObject> physicsObjects, float time = 0)
        {
            foreach (PhysicsObject physicsObject in physicsObjects)
            {
                Ignore(physicsObject, time);
            }
        }

        public void Unignore(PhysicsObject physicsObject)
        {
            _ignored.Remove(physicsObject);

            if (!_ignoredTimers.TryGetValue(physicsObject, out Timer timer)) return;
            _ignoredTimers.Remove(physicsObject);
            timer.Reset();
        }

        public void Unignore(List<PhysicsObject> physicsObjects)
        {
            foreach (PhysicsObject physicsObject in physicsObjects)
            {
                Unignore(physicsObject);
            }
        }

        public List<PhysicsContact> CollidingHorizontally(float sign, List<PhysicsObject> physicsObjects)
        {
            List<PhysicsContact> contacts = new List<PhysicsContact>();

            foreach (PhysicsObject remoteObject in physicsObjects)
            {
                if (remoteObject == this) continue;

                PhysicsContact contact = CollidingHorizontally(sign, remoteObject);

                if (contact != null)
                {
                    contacts.Add(contact);
                }
            }

            return contacts;
        }

        public PhysicsContact CollidingHorizontally(float sign, PhysicsObject remoteObject)
        {
            if (remoteObject == this || _ignored.Contains(remoteObject)) return null;

            foreach (PhysicsCollider localCollider in _colliders)
            {
                foreach (PhysicsCollider remoteCollider in remoteObject.Colliders)
                {
                    if (localCollider.CollidingHorizontally(sign, remoteCollider))
                    {
                        return new PhysicsContact(this, localCollider, remoteObject, remoteCollider);
                    }
                }
            }

            return null;
        }

        public List<PhysicsContact> CollidingVertically(float sign, List<PhysicsObject> physicsObjects)
        {
            List<PhysicsContact> contacts = new List<PhysicsContact>();

            foreach (PhysicsObject remoteObject in physicsObjects)
            {
                if (remoteObject == this) continue;

                PhysicsContact contact = CollidingVertically(sign, remoteObject);

                if (contact != null)
                {
                    contacts.Add(contact);
                }
            }

            return contacts;
        }

        public PhysicsContact CollidingVertically(float sign, PhysicsObject remoteObject)
        {
            if (remoteObject == this || _ignored.Contains(remoteObject)) return null;

            foreach (PhysicsCollider localCollider in _colliders)
            {
                foreach (PhysicsCollider remoteCollider in remoteObject.Colliders)
                {
                    if (localCollider.CollidingVertically(sign, remoteCollider))
                    {
                        return new PhysicsContact(this, localCollider, remoteObject, remoteCollider);
                    }
                }
            }

            return null;
        }

        public List<PhysicsObject> FindNearby(List<PhysicsObject> remote, float distance = NEARBY_DISTANCE)
        {
            List<PhysicsObject> nearby = new List<PhysicsObject>();

            foreach (PhysicsObject physicsObject in remote)
            {
                if ((physicsObject.transform.position - Transform.position).magnitude <= distance)
                {
                    nearby.Add(physicsObject);
                }
            }

            return nearby;
        }
    }
}