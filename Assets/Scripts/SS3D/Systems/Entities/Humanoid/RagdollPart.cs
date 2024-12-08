using FishNet.Component.Transforming;
using UnityEngine;

/// <summary>
/// Component, that identifies GameObjects that are parts of a ragdoll
/// </summary>
public class RagdollPart : MonoBehaviour
{
    protected void OnJointBreak(float breakForce)
    {
        Debug.Log("A joint has just been broken!, force: " + breakForce);
    }
}
