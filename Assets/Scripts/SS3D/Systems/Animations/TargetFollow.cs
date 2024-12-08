using UnityEngine;

public class TargetFollow : MonoBehaviour
{

    public Transform Followed { get; set; }

    private void Update()
    {
        if (Followed == null)
        {
            return;
        }

        transform.position = Followed.position;
        transform.rotation = Followed.rotation;
    }
}
