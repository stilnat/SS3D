using Coimbra.Services.Events;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Systems.Entities;
using SS3D.Systems.Entities.Events;
using SS3D.Systems.Rounds.Events;
using SS3D.Systems.Tile;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace SS3D.Engine.AtmosphericsRework
{
    public class AtmosNetworkManager : NetworkSystem
    {
        // the granularity of values representing a concentration, the higher the more the change between two different concentrations
        // can be subtle. But keep in mind setting a higher value for this will mean having to send more data to clients.
        private const byte ConcentrationRange = 16;
        
        // The amount of moles for which the concentration will look at its maximum on client
        private const int MaxMoles = 1000;
        
        private List<Entity> _playersEntities = new();
        private AtmosManager _atmosManager;

        private byte[] _previousValues;
        
        public override void OnStartServer()
        {
            base.OnStartServer();
            AddHandle(SpawnedPlayersUpdated.AddListener(HandleSpawnedPlayersUpdated));
            _atmosManager = Subsystems.Get<AtmosManager>();
            _atmosManager.AtmosTick += HandleAtmosTick;
        }

        private void HandleAtmosTick()
        {
            foreach (Entity player in _playersEntities)
            { 
                AtmosContainer atmosTile = _atmosManager.GetAtmosContainer(player.Position, TileLayer.Turf);
                AtmosChunk[] chunks = atmosTile.Map.GetChunkAndEightNeighbours(player.Position);
                
                List<byte> chunkBytesList = new();

                foreach (AtmosChunk chunk in chunks)
                {
                    if (chunk == null)
                    {
                        chunkBytesList.AddRange(new byte[256]);
                        continue;
                    }
                    
                    chunkBytesList.AddRange(
                        chunk.GetAllTileAtmosObjects()
                            .Select(x => Convert.ToByte(math.min((x.AtmosObject.CoreGasses[0] / MaxMoles) * ConcentrationRange, ConcentrationRange))));
                }
                
                DeltaCompress(chunkBytesList.ToArray());
            }
        }

        private void DeltaCompress(byte[] toCompress)
        {
            if (_previousValues == null)
            {
                _previousValues = toCompress;
                return;
            }
            
            byte[] compressed;
            byte[] deltas = new byte[toCompress.Length];

            for (int i = 0; i < toCompress.Length; i++)
            {
                deltas[i] = (byte)(toCompress[i] - _previousValues[i] + ConcentrationRange);
            }

            using (MemoryStream memoryStream = new())
            {
                using (DeflateStream compressionStream = new(memoryStream, CompressionLevel.Optimal))
                {
                    compressionStream.Write(deltas, 0, toCompress.Length);
                }

                compressed = memoryStream.ToArray();
            }

            byte[] decompressDeltas;
            using (MemoryStream inputMemoryStream = new(compressed))
            using (MemoryStream outputMemoryStream = new())
            using (DeflateStream deflateStream = new(inputMemoryStream, CompressionMode.Decompress))
            {
                deflateStream.CopyTo(outputMemoryStream);
                decompressDeltas = outputMemoryStream.ToArray();
            }

            byte[] decompressed = new byte[toCompress.Length];
            
            for (int i = 0; i < toCompress.Length; i++)
            {
                decompressed[i] = (byte)(_previousValues[i] + decompressDeltas[i] - ConcentrationRange); 
            }

            _previousValues = toCompress;
            Stats(toCompress, decompressed, compressed, "lossless");
        }


        private void Stats(byte[] originalData, byte[] reconstitutedData, byte[] compressedData, string name)
        {
            int maxError = 0;
            float averageError = 0; 
            for (int i = 0; i < originalData.Length; i++)
            {
                maxError = math.max(math.abs(originalData[i] - reconstitutedData[i]), maxError);
                averageError += (math.abs(originalData[i] - reconstitutedData[i]) - averageError) / (i + 1);
            }
            Debug.Log($"method {name}, Original byte array size: {originalData.Length}, compressed data {compressedData.Length}");
            Debug.Log($"method {name} max error between original and reconstituted data {maxError}. Average error {averageError}");
        }

        private void HandleSpawnedPlayersUpdated(ref EventContext context, in SpawnedPlayersUpdated e)
        {
            _playersEntities = e.SpawnedPlayers;
        }
    }
}