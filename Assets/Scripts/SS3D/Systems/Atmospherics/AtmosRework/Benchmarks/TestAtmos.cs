using SS3D.Engine.AtmosphericsRework;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TestAtmos : MonoBehaviour
{
    public bool showDebug = true;

    private List<AtmosObject> atmosObjects;
    private int numberOfObjects = 2;
    public float UpdateRate = 1f;
    private float lastStep;

    void Start()
    {
        atmosObjects = new List<AtmosObject>();

        BuildGrid();
    }

    void Update()
    {
        if (Time.fixedTime >= lastStep)
        {
            int counter = RunAtmosLoop();

            if (showDebug)
                Debug.Log("Atmos loop took: " + (Time.fixedTime - lastStep) + " seconds, simulating " + counter + " active atmos objects. Fixed update rate: " + UpdateRate);
            lastStep = Time.fixedTime + UpdateRate;
        }
    }

    private void BuildGrid()
    {
        AtmosObject newAtmosObject1 = new AtmosObject();
        newAtmosObject1.Setup();
        newAtmosObject1.atmosObject.container.MakeRandom();
        // newAtmosObject1.atmosObject.container.AddCoreGas(CoreAtmosGasses.Oxygen, 300f);
        // newAtmosObject1.atmosObject.container.AddCoreGas(CoreAtmosGasses.Nitrogen, 10f);
        // newAtmosObject1.atmosObject.container.SetTemperature(500);

        AtmosObject newAtmosObject2 = new AtmosObject();
        newAtmosObject2.Setup();
        newAtmosObject2.atmosObject.container.MakeRandom();
        // newAtmosObject2.atmosObject.container.AddCoreGas(CoreAtmosGasses.Nitrogen, 100f);

        newAtmosObject1.SetNeighbour(newAtmosObject2.atmosObject, 0);
        newAtmosObject2.SetNeighbour(newAtmosObject1.atmosObject, 0);

        atmosObjects.Add(newAtmosObject1);
        atmosObjects.Add(newAtmosObject2);
    }

    private int RunAtmosLoop()
    {
        if (showDebug)
        {
            for (int i = 0; i < atmosObjects.Count; i++)
            {
                Debug.Log(atmosObjects[i].ToString());
            }
        }

        int counter = 0;

        /*
        for (int i = 0; i < atmosObjects.Count; i++)
        {
            LoadNeighbour(i);
            if (atmosObjects[i].atmosObject.state == AtmosState.Active)
            {
                
                atmosObjects[i] = AtmosCalculator.CalculateFlux(atmosObjects[i]);
                
                counter++;
            }
            SetNeighbour(i);
        }
        */

        /*
        for (int i = 0; i < atmosObjects.Count; i++)
        {
            LoadNeighbour(i);
            if (atmosObjects[i].atmosObject.state == AtmosState.Active ||
                atmosObjects[i].atmosObject.state == AtmosState.Semiactive)
            {
                atmosObjects[i] = AtmosCalculator.SimulateFlux(atmosObjects[i]);
                
            }
            SetNeighbour(i);
        }
        */

        return counter;
    }

    /// <summary>
    /// Set the internal neighbour state based on the neighbour
    /// </summary>
    /// <param name="index"></param>
    private void LoadNeighbour(int index)
    {
        if (index == 0)
        {
            AtmosObjectInfo info1 = new AtmosObjectInfo()
            {
                state = atmosObjects[1].atmosObject.state,
                container = atmosObjects[1].atmosObject.container,
            };

            AtmosObject writeObject = atmosObjects[0];
            writeObject.SetNeighbour(info1, 0);
            atmosObjects[0] = writeObject;
        }

        else if (index == 1)
        {
            AtmosObjectInfo info2 = new AtmosObjectInfo()
            {
                state = atmosObjects[0].atmosObject.state,
                container = atmosObjects[0].atmosObject.container,
            };

            AtmosObject writeObject = atmosObjects[1];
            writeObject.SetNeighbour(info2, 0);
            atmosObjects[1] = writeObject;
        }
    }

    /// <summary>
    /// Modify the neighbour based on the internal update
    /// </summary>
    /// <param name="index"></param>
    private void SetNeighbour(int index)
    {
        if (index == 0)
        {
            // AtmosObjectInfo info1 = atmosObjects[0].atmosObject;
            // AtmosObjectInfo neighbour1 = atmosObjects[1].GetNeighbour(0);
            // neighbour1.state = info1.state;
            // neighbour1.container = info1.container;

            AtmosObject writeObject = atmosObjects[1];
            writeObject.atmosObject = atmosObjects[0].GetNeighbour(0);
            atmosObjects[1] = writeObject;
        }
        else if (index == 1)
        {
            // AtmosObjectInfo info2 = atmosObjects[1].atmosObject;
            // AtmosObjectInfo neighbour2 = atmosObjects[0].GetNeighbour(0);
            // neighbour2.state = info2.state;
            // neighbour2.container = info2.container;

            AtmosObject writeObject = atmosObjects[0];
            writeObject.atmosObject = atmosObjects[1].GetNeighbour(0);
            atmosObjects[0] = writeObject;
        }

    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
            return;
#endif

        for (int i = 0; i < atmosObjects.Count; i++)
        {
            Color state;
            switch (atmosObjects[i].atmosObject.state)
            {
                case AtmosState.Active: state = new Color(0, 0, 0, 0); break;
                case AtmosState.Semiactive: state = new Color(0, 0, 0, 0.4f); break;
                case AtmosState.Inactive: state = new Color(0, 0, 0, 0.8f); break;
                default: state = new Color(0, 0, 0, 1); break;
            }

            Vector3 position = new Vector3(i, 0, 0);
            float pressure = atmosObjects[i].atmosObject.container.GetPressure() / 160f;

            if (pressure > 0f)
            {
                Gizmos.color = Color.white - state;
                Gizmos.DrawCube(position, new Vector3(0.8f, pressure, 0.8f));
            }
        }
    }
}
