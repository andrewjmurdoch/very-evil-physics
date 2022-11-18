using System;

namespace VED.Physics
{
    [Serializable]
    public class PhysicsContact
    {
        public PhysicsObject   LocalObject    = null;
        public PhysicsCollider LocalCollider  = null;
        public PhysicsObject   RemoteObject   = null;
        public PhysicsCollider RemoteCollider = null;

        public PhysicsContact(PhysicsObject localObject, PhysicsCollider localCollider, PhysicsObject remoteObject, PhysicsCollider remoteCollider)
        {
            LocalObject    = localObject;
            LocalCollider  = localCollider;
            RemoteObject   = remoteObject;
            RemoteCollider = remoteCollider;
        }

        public PhysicsContact Invert()
        {
            return new PhysicsContact(RemoteObject, RemoteCollider, LocalObject, LocalCollider);
        }
    }
}