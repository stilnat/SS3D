using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetFollow : MonoBehaviour
{

    public Transform Followed { get; set; }


    // Update is called once per frame
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
