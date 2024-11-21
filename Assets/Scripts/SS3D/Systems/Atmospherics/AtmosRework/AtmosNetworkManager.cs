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
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Mathematics;

namespace SS3D.Engine.AtmosphericsRework
{

    public class AtmosNetworkManager : NetworkSystem
    {
        // Import FreeImage functions
        [DllImport("FreeImage")]
        private static extern IntPtr FreeImage_Allocate(int width, int height, int bpp);

        [DllImport("FreeImage")]
        private static extern bool FreeImage_SaveToMemory(int format, IntPtr dib, IntPtr stream, int flags);

        [DllImport("FreeImage")]
        private static extern void FreeImage_Unload(IntPtr dib);

        [DllImport("FreeImage")]
        private static extern IntPtr FreeImage_OpenMemory(byte[] data, uint size);

        [DllImport("FreeImage")]
        private static extern uint FreeImage_CloseMemory(IntPtr stream);

        [DllImport("FreeImage")]
        private static extern void FreeImage_AcquireMemory(IntPtr stream, out IntPtr data, out uint size);

        [DllImport("FreeImage")]
        private static extern IntPtr FreeImage_GetBits(IntPtr dib);

        [DllImport("FreeImage")]
        private static extern int FreeImage_GetWidth(IntPtr dib);

        [DllImport("FreeImage")]
        private static extern int FreeImage_GetHeight(IntPtr dib);

        [DllImport("FreeImage")]
        private static extern int FreeImage_GetLine(IntPtr dib);

        [DllImport("FreeImage")]
        private static extern IntPtr FreeImage_LoadFromMemory(int format, IntPtr stream, int flags);

        [DllImport("FreeImage")]
        private static extern int FreeImage_GetPitch(IntPtr dib);

        private const int FIF_JPEG = 2; // Format identifier for JPEG

        private List<Entity> _playersEntities = new();
        private AtmosManager _atmosManager;
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
                ComputeTexture(player);
            }
        }

        private void ComputeTexture(Entity player)
        {
            AtmosContainer atmosTile = _atmosManager.GetAtmosContainer(player.Position, TileLayer.Turf);
            List<AtmosContainer> chunkTiles = atmosTile.Chunk.GetAllTileAtmosObjects();
            Texture2D texture = new(48, 48, TextureFormat.R8, false);

            // todo : that's arbitrary, its the mole value from which the gas doesn't get more visibly concentrated on a single tile.  
            float maxAmount = 1500;

            byte[] chunkBytes = chunkTiles.Select(x =>  Convert.ToByte(math.min((x.AtmosObject.CoreGasses[0] / maxAmount) * 255, 255))).ToArray();
            byte[] expanded = new byte[chunkBytes.Length * 9];    
            // Copy the original array into the expanded array multiple times
            for (int i = 0; i < 9; i++)
            {
                Array.Copy(chunkBytes, 0, expanded, i * chunkBytes.Length, chunkBytes.Length);
            }


            byte[] jpegBytes = EncodeToJpg(expanded);
            byte[] reconstitutedBytes = DecodeFromJpg(jpegBytes);

            JpgStats(expanded, reconstitutedBytes, jpegBytes);
        }

        private void JpgStats(byte[] originalData, byte[] reconstitutedData, byte[] compressedData)
        {
            int maxError = 0;
            float averageError = 0; 
            for (int i = 0; i < originalData.Length; i++)
            {
                maxError = math.max(math.abs(originalData[i] - reconstitutedData[i]), maxError);
                averageError += (math.abs(originalData[i] - reconstitutedData[i]) - averageError) / (i + 1);
            }
            Debug.Log($"Original byte array size: {originalData.Length}, compressed data {compressedData.Length}");
            Debug.Log($"max error between original and reconstituted data {maxError}. Average error {averageError}");
        }

        private byte[] DecodeFromJpg(byte[] data)
        {
            IntPtr memoryStream = FreeImage_OpenMemory(data, (uint)data.Length);
            if (memoryStream == IntPtr.Zero)
            {
                Console.WriteLine("Failed to open FreeImage memory stream.");
                return Array.Empty<byte>();
            }

            // Step 2: Load the JPEG image from the memory stream
            IntPtr dib = FreeImage_LoadFromMemory(FIF_JPEG, memoryStream, 0);
            if (dib == IntPtr.Zero)
            {
                Console.WriteLine("Failed to load JPEG from memory.");
                FreeImage_CloseMemory(memoryStream);
                return Array.Empty<byte>();
            }

            // Step 3: Get image dimensions and pixel data
            int width = FreeImage_GetWidth(dib);
            int height = FreeImage_GetHeight(dib);
            int pitch = FreeImage_GetLine(dib); // Bytes per line (may include padding)

            IntPtr bitsPtr = FreeImage_GetBits(dib);

            // Step 4: Extract pixel data into a managed byte array
            int totalBytes = height * pitch;
            byte[] decodedBytes = new byte[totalBytes];
            Marshal.Copy(bitsPtr, decodedBytes, 0, totalBytes);

            // Free resources
            FreeImage_Unload(dib);
            FreeImage_CloseMemory(memoryStream);

            // Step 5: Truncate padding if necessary
            // For grayscale (R8), remove padding if pitch > width:
            /*if (pitch > width)
            {
                byte[] finalBytes = new byte[width * height];
                for (int y = 0; y < height; y++)
                {
                    Array.Copy(decodedBytes, y * pitch, finalBytes, y * width, width);
                }
                decodedBytes = finalBytes;
            } */

            return decodedBytes;
        }

        private byte[] EncodeToJpg(byte[] data)
        {
            // Step 1: Create a FreeImage bitmap from the byte array
            IntPtr dib = FreeImage_Allocate(48, 48, 8); // 8 bpp for grayscale
            if (dib == IntPtr.Zero)
            {
                Debug.Log("Failed to allocate FreeImage bitmap.");
                return Array.Empty<byte>();
            }

            IntPtr bitsPtr = FreeImage_GetBits(dib);
            int pitch = FreeImage_GetPitch(dib); // Bytes per line (may include padding)
            for (int y = 0; y < 48; y++)
            {
                Marshal.Copy(data, y * 48, bitsPtr + y * pitch, 48);
            }

            // Step 2: Open a FreeImage memory stream
            IntPtr memoryStream = FreeImage_OpenMemory(null, 0);
            if (memoryStream == IntPtr.Zero)
            {
                Debug.Log("Failed to open FreeImage memory stream.");
                FreeImage_Unload(dib);
                return Array.Empty<byte>();
            }

            // Step 3: Save the bitmap as a JPEG to the memory stream
            if (!FreeImage_SaveToMemory(FIF_JPEG, dib, memoryStream, 50))
            {
                Debug.Log("Failed to save image to memory as JPEG.");
                FreeImage_CloseMemory(memoryStream);
                FreeImage_Unload(dib);
                return Array.Empty<byte>();
            }

            // Step 4: Extract the byte array from the memory stream
            FreeImage_AcquireMemory(memoryStream, out IntPtr jpegData, out uint jpegSize);

            // Copy the JPEG data into a managed byte array
            byte[] jpegBytes = new byte[jpegSize];
            Marshal.Copy(jpegData, jpegBytes, 0, (int)jpegSize);

            // Clean up
            FreeImage_CloseMemory(memoryStream);
            FreeImage_Unload(dib);

            return jpegBytes;

        }

        private void HandleSpawnedPlayersUpdated(ref EventContext context, in SpawnedPlayersUpdated e)
        {
            _playersEntities = e.SpawnedPlayers;
        }
    }
}