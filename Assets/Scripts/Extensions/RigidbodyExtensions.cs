using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RigidbodyExtensions
{
    public static void SetRotationConstrains(this Rigidbody rigidbody, bool enabled)
    {
        if (enabled) rigidbody.EnableRotationConstrains();
        else rigidbody.DisableRotationConstrains();
    }

    public static void EnableRotationConstrains(this Rigidbody rigidbody)
    {
        rigidbody.constraints |= RigidbodyConstraints.FreezeRotation;
    }
    
    public static void DisableRotationConstrains(this Rigidbody rigidbody)
    {
        rigidbody.constraints &= ~RigidbodyConstraints.FreezeRotation;
    }
}
