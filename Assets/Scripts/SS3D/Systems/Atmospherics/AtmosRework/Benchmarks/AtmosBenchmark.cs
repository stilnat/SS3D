using SS3D.Engine.AtmosphericsRework;
using SS3D.Systems.Tile;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;

public class AtmosBenchmark : MonoBehaviour
{
    public bool showUpdate = true;
    public float UpdateRate = 0.5f;
    public int gridSize = 16;
    public bool showGizmos = true;

    private float lastStep;
    private NativeArray<AtmosObject> atmosObjects;

    void Start()
    {
        Initialize(gridSize);
    }

    private void OnDestroy()
    {
        atmosObjects.Dispose();
    }

    private void Initialize(int gridSize)
    {
        int arraySize = gridSize * gridSize;

        atmosObjects = new NativeArray<AtmosObject>(arraySize, Allocator.Persistent);

        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                AtmosObject atmos = new AtmosObject();

                atmos.MakeRandom();

                atmosObjects[y * gridSize + x] = atmos;
            }
        }
    }



    // Update is called once per frame
    void Update()
    {
        if (Time.fixedTime >= lastStep)
        {
            int counter = RunAtmosJob();

            if (showUpdate)
                Debug.Log("Atmos loop took: " + (Time.fixedTime - lastStep) + " seconds, simulating " + counter + " active atmos objects. Fixed update rate: " + UpdateRate);
            lastStep = Time.fixedTime + UpdateRate;
        }
    }

    private Vector3 GetPositionFromIndex(int index)
    {
        int x = index % gridSize;
        int y = Mathf.FloorToInt(index / gridSize);

        return new Vector3(x, 0, y);
    }

    private int RunAtmosJob()
    {
        var counterArray = new NativeArray<int>(1, Allocator.TempJob);

        // Step 1: Simulate
        SimulateFluxJob simulateJob = new SimulateFluxJob()
        {
            counterArray = counterArray,
            buffer = atmosObjects,
        };

        JobHandle simulateHandle = simulateJob.Schedule();
        simulateHandle.Complete();

        int counter = counterArray[0];
        counterArray.Dispose();

        return counter;
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        if (!EditorApplication.isPlaying)
            return;
#endif

        if (!showGizmos)
            return;

        for (int i = 0; i < atmosObjects.Length; i++)
        {
            Color state;
            switch (atmosObjects[i].State)
            {
                case AtmosState.Active: state = new Color(0, 0, 0, 0); break;
                case AtmosState.Semiactive: state = new Color(0, 0, 0, 0.4f); break;
                case AtmosState.Inactive: state = new Color(0, 0, 0, 0.8f); break;
                default: state = new Color(0, 0, 0, 1); break;
            }
            Vector3 position = GetPositionFromIndex(i);
            float pressure =  atmosObjects[i].Pressure / 160f;;

            if (pressure > 0f)
            {
                Gizmos.color = Color.white - state;
                Gizmos.DrawCube(position, new Vector3(0.8f, pressure, 0.8f));
            }
        }
    }


    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Standard)]
    private struct SimulateFluxJob : IJob
    {
        public NativeArray<int> counterArray;
        public NativeArray<AtmosObject> buffer;

        public void Execute()
        {
            /*
            for (int index = 0; index < buffer.Length; index++)
            {
                if (buffer[index].atmosObject.state == AtmosState.Active || buffer[index].atmosObject.state == AtmosState.Semiactive)
                {
                    int counter = counterArray[0];
                    counter++;
                    counterArray[0] = counter;

                    // Load neighbour
                    for (int i = 0; i < 4; i++)
                    {
                        int neighbourIndex = buffer[index].GetNeighbourIndex(i);

                        if (neighbourIndex != -1)
                        {
                            LoadNeighbour(index, neighbourIndex, i);
                        }
                    }

                    // Do actual work
                    buffer[index] = AtmosCalculator.SimulateFlux(buffer[index]);

                    // Set neighbour
                    for (int i = 0; i < 4; i++)
                    {
                        int neighbourIndex = buffer[index].GetNeighbourIndex(i);
                        if (neighbourIndex != -1)
                        {
                            SetNeighbour(index, neighbourIndex, i);
                        }
                    }
                }
            }
            */
        }
    }
}