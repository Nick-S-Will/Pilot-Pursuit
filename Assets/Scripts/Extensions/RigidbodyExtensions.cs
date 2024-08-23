using System;
using UnityEngine;

public static class RigidbodyExtensions
{
    public static void EnableDynamics(this Rigidbody rigidbody) => SetDynamics(rigidbody, true);

    public static void DisableDynamics(this Rigidbody rigidbody) => SetDynamics(rigidbody, false);

    public static void SetDynamics(this Rigidbody rigidbody, bool enabled)
    {
        if (rigidbody == null) throw new NullReferenceException();

        if (!enabled && !rigidbody.isKinematic)
        {
            rigidbody.velocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
        rigidbody.isKinematic = !enabled;
    }

    public static void SetRotationConstrains(this Rigidbody rigidbody, bool enabled)
    {
        if (enabled) rigidbody.EnableRotationConstrains();
        else rigidbody.DisableRotationConstrains();
    }

    public static void EnableRotationConstrains(this Rigidbody rigidbody)
    {
        if (rigidbody == null) throw new NullReferenceException();

        rigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
    }
    
    public static void DisableRotationConstrains(this Rigidbody rigidbody)
    {
        if (rigidbody == null) throw new NullReferenceException();

        rigidbody.constraints &= ~RigidbodyConstraints.FreezeRotation;
    }
}