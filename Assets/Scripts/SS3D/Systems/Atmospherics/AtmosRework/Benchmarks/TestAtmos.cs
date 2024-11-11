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
        newAtmosObject1.MakeRandom();
        // newAtmosObject1.atmosObject.container.AddCoreGas(CoreAtmosGasses.Oxygen, 300f);
        // newAtmosObject1.atmosObject.container.AddCoreGas(CoreAtmosGasses.Nitrogen, 10f);
        // newAtmosObject1.atmosObject.container.SetTemperature(500);

        AtmosObject newAtmosObject2 = new AtmosObject();
        newAtmosObject2.MakeRandom();
        // newAtmosObject2.atmosObject.container.AddCoreGas(CoreAtmosGasses.Nitrogen, 100f);


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





    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
            return;
#endif

        for (int i = 0; i < atmosObjects.Count; i++)
        {
            Color state;
            switch (atmosObjects[i].State)
            {
                case AtmosState.Active: state = new Color(0, 0, 0, 0); break;
                case AtmosState.Semiactive: state = new Color(0, 0, 0, 0.4f); break;
                case AtmosState.Inactive: state = new Color(0, 0, 0, 0.8f); break;
                default: state = new Color(0, 0, 0, 1); break;
            }

            Vector3 position = new Vector3(i, 0, 0);
            float pressure = atmosObjects[i].Pressure / 160f;

            if (pressure > 0f)
            {
                Gizmos.color = Color.white - state;
                Gizmos.DrawCube(position, new Vector3(0.8f, pressure, 0.8f));
            }
        }
    }
}