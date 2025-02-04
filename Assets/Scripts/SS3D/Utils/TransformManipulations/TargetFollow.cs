using UnityEngine;

/// <summary>
/// Utility component allowing a gameobject to reproduce another game object's rotation and position without parenting.
/// </summary>
public class TargetFollow : MonoBehaviour
{
    public Vector3 Followed { get; private set; }

    private float _timing;

    public void Follow(Vector3 position)
    {
        Followed = position;
    }

    private void LateUpdate()
    {
        transform.position = Followed;
    }
}
