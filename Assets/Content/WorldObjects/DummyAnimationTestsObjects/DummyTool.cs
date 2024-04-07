using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DummyTool : MonoBehaviour
{

    [SerializeField]
    private Transform interactionPoint;

    public Transform InteractionPoint => interactionPoint;
}
