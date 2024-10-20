﻿using Coimbra;
using JetBrains.Annotations;
using Serilog;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using SS3D.CodeGeneration;
using SS3D.CodeGeneration.Creators;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor.AddressableAssets.Settings;
#endif

namespace SS3D.Data.AssetDatabases
{
    /// <summary>
    /// An AssetDatabase is a ScriptableObject used to hold an Asset list and to create an Enum based on this list.
    /// It is used to find assets using IDs in a very convenient manner throughout the project.
    /// </summary>
    [CreateAssetMenu(menuName = "SS3D/AssetDatabase", fileName = "AssetDatabase", order = 0)]
    public sealed class AssetDatabase : ScriptableObject
    {
        /// <summary>
        /// The path that the enum will be generated to.
        /// </summary>
        public const string DatabaseAssetPath = @"\Scripts\SS3D\Data\Generated";

        /// <summary>
        ///  The namespace that will be included on the generated Enum.
        /// </summary>
        public const string DatabaseAssetNamespaceName = "SS3D.Data.Generated";

        /// <summary>
        ///  The name that the generated enum will have;
        /// </summary>
        public string DatabaseName;

#if UNITY_EDITOR
        /// <summary>
        /// The asset group that constitutes this AssetDatabase, the system gets every asset from it and adds to an asset list.
        /// </summary>
        public AddressableAssetGroup AssetGroup;

        /// <summary>
        /// All loaded assets that will be included in the built game.
        /// </summary>
#endif
        public SerializableDictionary<string, Object> Assets;

#if UNITY_EDITOR
        /// <summary>
        /// Loads all the assets from the asset group to the Assets list.
        /// </summary>
        public void LoadAssetsFromAssetGroup()
        {
            Assets ??= new SerializableDictionary<string, Object>();

            foreach (AddressableAssetEntry entry in AssetGroup.entries)
            {
                Assets.TryAdd(entry.MainAsset.name, entry.MainAsset);
            }

            EditorUtility.SetDirty(this);
        }
#endif

        /// <summary>
        /// Gets an asset based on its ID (index).
        /// </summary>
        /// <param name="id">Uses the ID of the asset cast into a int to get the asset from a list position.</param>
        /// <typeparam name="T">The type of asset to get.</typeparam>
        /// <returns></returns>
        [CanBeNull]
        public T Get<T>([NotNull] string id)
            where T : Object
        {
            if (!Assets.TryGetValue(id, out Object asset))
            {
                Log.Error($"{nameof(AssetDatabase)} Asset of {id} is not found on the {DatabaseName} database.");
                return null;
            }

            if (typeof(T) != typeof(GameObject) && asset is GameObject gameObject)
            {
                return gameObject.GetComponent<T>();
            }

            return asset as T;
        }

        public bool TryGet<T>([NotNull] string index, [CanBeNull] out T asset)
            where T : Object
        {
            bool hasValue = Assets.TryGetValue(index, out Object foundValue);

            asset = foundValue as T;

            return hasValue;
        }

        /// <summary>
        /// Adds abd asset to the asset database. Should be used only for additional content or runtime stuff.
        /// </summary>
        /// <param name="asset"></param>
        /// <typeparam name="TAsset"></typeparam>
        public void Add<TAsset>(Object asset)
            where TAsset : Object
        {
            Assets.Add(asset.name, asset as TAsset);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Initializes all asset databases in the project and adds to the databases list.
        /// </summary>
        public static List<AssetDatabase> FindAllAssetDatabases()
        {
            string[] assets = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(AssetDatabase)}");

            List<AssetDatabase> databases = new();

            for (int index = 0; index < assets.Length; index++)
            {
                string database = assets[index];
                string assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(database);
                AssetDatabase assetDatabase = UnityEditor.AssetDatabase.LoadAssetAtPath<AssetDatabase>(assetPath);

                databases.Add(assetDatabase);
            }

            return databases;
        }

        /// <summary>
        /// Generates a script with the data of this database for easy access.
        /// </summary>
        public void GenerateDatabaseCode()
        {
            if (AssetDatabaseSettings.SkipCodeGeneration)
            {
                return;
            }

            DatabaseAssetCreator.CreateAtPath(DatabaseAssetPath, typeof(DatabaseAsset), DatabaseName, Assets.Values, DatabaseAssetNamespaceName);
        }
#endif
    }
}