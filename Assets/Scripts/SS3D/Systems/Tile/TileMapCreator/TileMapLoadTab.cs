﻿using Coimbra;
using SS3D.Core;
using SS3D.Core.Behaviours;
using SS3D.Data.Management;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SS3D.Systems.Tile.TileMapCreator
{
    public class TileMapLoadTab : NetworkActor, ITileMenuTab
    {
        [SerializeField]
        private TileMapMenu _menu;

        [SerializeField]
        private GameObject _mapNameSlotPrefab;


        [SerializeField]
        private GameObject _loadMapContentRoot;

        [SerializeField]
        private AssetGrid _assetGrid;

        public void Clear()
        {
            for (int i = 0; i < _loadMapContentRoot.transform.childCount; i++)
            {
                _loadMapContentRoot.transform.GetChild(i).gameObject.Dispose(true);
            }
        }

        public void Display()
        {
            var MapNames = SaveSystem.GetAllObjectsNameInFolder(Subsystems.Get<TileSystem>().SavePath);

            foreach (string mapName in MapNames)
            {
                string mapNameWithNoExtension = mapName.Substring(0, mapName.IndexOf("."));
                GameObject slot = Instantiate(_mapNameSlotPrefab, _loadMapContentRoot.transform, true);
                slot.GetComponentInChildren<TextMeshProUGUI>().text = mapNameWithNoExtension;
                MapNameSlot mapNameSlot = slot.GetComponent<MapNameSlot>();
                mapNameSlot.MapNameButton.onClick.AddListener(() => LoadMap(mapNameWithNoExtension));
                mapNameSlot.DeleteButton.onClick.AddListener(() => DeleteMap(mapNameWithNoExtension));
            }
        }

        public void Refresh()
        {
            throw new System.NotImplementedException();
        }

        private void DeleteMap(string mapName)
        {
            if (IsServer)
            {
                SaveSystem.DeleteFile(Subsystems.Get<TileSystem>().SavePath + "/" + mapName);
            }
            else
            {
                Log.Information(this, "Cannot load the map on a client");
            }
        }

        private void LoadMap(string mapName)
        {
            if (IsServer)
            {
                Subsystems.Get<TileSystem>().Load(Subsystems.Get<TileSystem>().SavePath + "/" + mapName);
            }
            else
            {
                Log.Information(this, "Cannot load the map on a client");
            }
        }
    }
}
