
using SS3D.Core;
using SS3D.Systems.Tile;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;

namespace SS3D.Engine.AtmosphericsRework
{
    [ExecuteAlways]
    public class AtmosManager : MonoBehaviour
    {
        public bool ShowUpdate = true;
        public float UpdateRate = 0.5f;
       
        private TileSystem tileManager;
        private List<AtmosMap> atmosMaps;
        private List<AtmosJob> atmosJobs;
        private List<JobHandle> jobHandles;
        private float lastStep;
        private bool initCompleted = false;

        // Performance markers
        static ProfilerMarker s_PreparePerfMarker = new ProfilerMarker("Atmospherics.Initialize");
        static ProfilerMarker s_StepPerfMarker = new ProfilerMarker("Atmospherics.Step");

        /// <summary>
        /// Singleton instance
        /// </summary>
        private static AtmosManager _instance;
        public static AtmosManager Instance { get { return _instance; } }

        private void Start()
        {
            tileManager = Subsystems.Get<TileSystem>();
            atmosMaps = new List<AtmosMap>();
            atmosJobs = new List<AtmosJob>();
            jobHandles = new List<JobHandle>();

            // Initialization is invoked by the tile manager
            tileManager.TileSystemLoaded += Initialize;
        }

        private void Awake()
        {
            initCompleted = false;

            if (_instance != null && _instance != this)
            {
                Debug.LogWarning("Duplicate AtmosManager found. Deleting the last instance");
                //EditorAndRuntime.Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }
        }

        private void OnDestroy()
        {
            if (atmosMaps == null || atmosJobs == null)
                return;

            foreach (AtmosMap map in atmosMaps)
            {
                map.Clear();
            }

            atmosMaps.Clear();

            foreach (AtmosJob job in atmosJobs)
            {
                job.Destroy();
            }

            atmosJobs.Clear();
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
                return;
#endif

            if (!initCompleted)
                return;

            if (Time.fixedTime >= lastStep + UpdateRate)
            {
                float dt = Time.fixedTime - lastStep;
                int tileCounter = StepAtmos(dt);

                if (ShowUpdate)
                    Debug.Log("Atmos loop took: " + (dt - UpdateRate) + " seconds, simulating " + tileCounter + " active atmos objects. Fixed update rate: " + UpdateRate);
                lastStep = Time.fixedTime;
            }
        }

        private void CreateAtmosMaps()
        {
            int chunkCounter = 0;
            atmosMaps.Clear();

            // Create identical atmos chunks for each tile chunk
            TileMap map = tileManager.CurrentMap;

            AtmosMap atmosMap = new AtmosMap(map, map.Name);

            List<TileChunk> tileChunks = map.Chunks;
            
            foreach (TileChunk chunk in tileChunks)
            {
                atmosMap.CreateChunkFromTileChunk(chunk.Key, chunk.Origin);
                chunkCounter++;
            }

            atmosMaps.Add(atmosMap);
        

            Debug.Log("AtmosManager: recreated " + chunkCounter + " chunks from the tilemap");
        }

        private void Initialize()
        {
            if (tileManager == null || tileManager.CurrentMap == null)
            {
                Debug.LogError("AtmosManager couldn't find the tilemanager or map.");
                return;
            }

            if (atmosJobs != null)
            {
                atmosJobs.ForEach(job => job.Destroy());
                atmosJobs.Clear();
            }

            s_PreparePerfMarker.Begin();

            CreateAtmosMaps();

            if (ShowUpdate)
                Debug.Log("AtmosManager: Initializing tiles");


            int initCounter = 0;
            foreach (AtmosMap map in atmosMaps)
            {
                List<TileAtmosObject> tiles = new List<TileAtmosObject>();
                List<IAtmosLoop> devices = new List<IAtmosLoop>();

                foreach (AtmosChunk chunk in map.GetAtmosChunks())
                {
                    var tileAtmosObjects = chunk.GetAllTileAtmosObjects();

                    // Initialize the atmos tiles. Cannot be done in the tilemap as it may still be creating tiles.
                    tileAtmosObjects.ForEach(tile => tile.Initialize(map.GetLinkedTileMap()));
                    tiles.AddRange(tileAtmosObjects);
                }

                devices.AddRange(tileManager.GetComponentsInChildren<IAtmosLoop>());

                AtmosJob atmosJob = new AtmosJob(map, tiles, devices);
                atmosJobs.Add(atmosJob);

                initCounter += tiles.Count; 
            }

            if (ShowUpdate)
                Debug.Log($"AtmosManager: Finished initializing {initCounter} tiles");

            initCompleted = true;
        }

        public void AddGas(Vector3 worldPosition, CoreAtmosGasses gas, float amount)
        {

            var tile = GetAtmosTile(worldPosition);
            if (tile != null)
            {
                // Update the gas amount. Keep in mind that this is a value type.
                var atmosObject = tile.GetAtmosObject();
                atmosObject.AddGas(gas, amount);
                tile.SetAtmosObject(atmosObject);

                // Indicate a refresh for the AtmosJob. TODO: Optimize to not loop everything
                atmosJobs.ForEach(job => job.Refresh());
            }
        }

        public void RemoveGas(Vector3 worldPosition, CoreAtmosGasses gas, float amount)
        {
            var tile = GetAtmosTile(worldPosition);
            if (tile != null)
            {
                // Update the gas amount. Keep in mind that this is a value type.
                var atmosObject = tile.GetAtmosObject();
                atmosObject.RemoveGas(gas, amount);
                tile.SetAtmosObject(atmosObject);

                // Indicate a refresh for the AtmosJob. TODO: Optimize to not loop everything
                atmosJobs.ForEach(job => job.Refresh());
            }
        }

        public void AddHeat(Vector3 worldPosition, float amount)
        {
            var tile = GetAtmosTile(worldPosition);
            if (tile != null)
            {
                // Update the gas amount. Keep in mind that this is a value type.
                var atmosObject = tile.GetAtmosObject();
                atmosObject.AddHeat(amount);
                tile.SetAtmosObject(atmosObject);

                // Indicate a refresh for the AtmosJob. TODO: Optimize to not loop everything
                atmosJobs.ForEach(job => job.Refresh());
            }
        }

        public void RemoveHeat(Vector3 worldPosition, float amount)
        {
            var tile = GetAtmosTile(worldPosition);
            if (tile != null)
            {
                // Update the gas amount. Keep in mind that this is a value type.
                var atmosObject = tile.GetAtmosObject();
                atmosObject.RemoveHeat(amount);
                tile.SetAtmosObject(atmosObject);

                // Indicate a refresh for the AtmosJob. TODO: Optimize to not loop everything
                atmosJobs.ForEach(job => job.Refresh());
            }
        }

        private int StepAtmos(float deltaTime)
        {
            s_StepPerfMarker.Begin();
            int counter = 0;

            // Step 0: Loop through every map
            jobHandles.Clear();
            foreach (AtmosJob atmosJob in atmosJobs)
            {
                // atmosJob.AddGasTest();


                // Step 1: Simulate tiles
                SimulateFluxJob simulateTilesJob = new SimulateFluxJob()
                {
                    buffer = atmosJob.nativeAtmosTiles,
                    dt = deltaTime
                };

                // Step 2: Simulate atmos devices and pipes
                SimulateFluxJob simulateDevicesJob = new SimulateFluxJob()
                {
                    buffer = atmosJob.nativeAtmosDevices,
                    dt = deltaTime
        };

                counter += atmosJob.CountActive();

                JobHandle simulateTilesHandle = simulateTilesJob.Schedule();
                JobHandle simulateDevicesHandle = simulateDevicesJob.Schedule();

                jobHandles.Add(simulateTilesHandle);
                jobHandles.Add(simulateDevicesHandle);
            }

            // Step 3: Complete the work
            foreach (JobHandle handle in jobHandles)
            {
                handle.Complete();
            }

            // Step 4: Write back the results
            foreach (AtmosJob job in atmosJobs)
            {
                job.WriteResultsToList();
            }

            s_StepPerfMarker.End();

            return counter;
        }

        public TileAtmosObject GetAtmosTile(Vector3 worldPosition)
        {
            foreach (var map in atmosMaps)
            {
                var atmosTile = map.GetTileAtmosObject(worldPosition);
                if (atmosTile != null)
                    return atmosTile;
            }

            return null;
        }

        public List<AtmosJob> GetAtmosJobs()
        {
            return atmosJobs;
        }

        /*
        private void OnDrawGizmos()
        {
            float gizmoSize = 0.2f;

            //#if UNITY_EDITOR
            //            if (!EditorApplication.isPlaying)
            //                return;
            //#endif

            if (atmosJobs == null)
                return;

            foreach (AtmosJob job in atmosJobs)
            {

                for (int i = 0; i < job.atmosTiles.Count; i++)
                {
                    Color state;
                    AtmosState tileState = job.atmosTiles[i].GetAtmosObject().atmosObject.state;
                    switch (tileState)
                    {
                        case AtmosState.Active: state = new Color(0, 0, 0, 0); break;
                        case AtmosState.Semiactive: state = new Color(0, 1, 0, 0.4f); break;
                        case AtmosState.Inactive: state = new Color(0, 0, 0, 0.8f); break;
                        default: state = new Color(0, 0, 0, 1); break;
                    }

                    Vector3 position = job.atmosTiles[i].GetWorldPosition();
                    float pressure = job.nativeAtmosTiles[i].atmosObject.container.GetPressure() / 160f;

                    if (tileState == AtmosState.Active || tileState == AtmosState.Semiactive || tileState == AtmosState.Inactive)
                    {
                        Gizmos.color = Color.white - state;
                        Gizmos.DrawWireCube(position + new Vector3(0, pressure, 0), new Vector3(gizmoSize, pressure * 2, gizmoSize));
                    }
                    else if (tileState == AtmosState.Blocked)
                    {
                        Gizmos.color = Color.black;
                        Gizmos.DrawWireCube(position + new Vector3(0, 2.5f / 2f, 0), new Vector3(gizmoSize, 2.5f, gizmoSize));
                    }
                    else if (tileState == AtmosState.Vacuum)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireCube(position, new Vector3(1, pressure, 1));
                    }
                }
            }
        }
        */
    }
}