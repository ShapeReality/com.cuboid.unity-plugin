using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cuboid.UnityPlugin.Editor
{
    /// <summary>
    /// Controls the select collections, to make sure the EditorWindow subclass doesn't have too many responsibilities
    /// </summary>
    public class AssetCollectionsController
    {
        public AssetCollectionsController()
        {
        }

        public Action UpdateCollectionsList;
        public Action<List<RealityAssetCollection>> UpdateSelectedCollections;

        private List<RealityAssetCollection> _collections = null;

        /// <summary>
        /// All collections that exist in the project's Assets folder
        /// </summary>
        public List<RealityAssetCollection> Collections
        {
            get
            {
                if (_collections == null)
                {
                    _collections = GetCollectionsInProject();
                }
                return _collections;
            }
            private set
            {
                _collections = value;
                OnCollectionsChanged();
            }
        }

        private void OnCollectionsChanged()
        {

        }

        private void ReloadCollections()
        {
            Collections = GetCollectionsInProject();
        }

        private static List<RealityAssetCollection> GetCollectionsInProject()
        {
            List<RealityAssetCollection> collections = new List<RealityAssetCollection>();
            string[] guids = AssetDatabase.FindAssets("t:RealityAssetCollection");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RealityAssetCollection collection = AssetDatabase.LoadAssetAtPath<RealityAssetCollection>(path);
                if (collection != null) { collections.Add(collection); }
            }

            return collections;
        }

        private const string k_SelectedCollectionsKey = "selected-collections";
        private List<RealityAssetCollection> _selectedCollections;

        /// <summary>
        /// All collections that are currently selected by the user inside the Asset Collections editor window
        /// </summary>
        public List<RealityAssetCollection> SelectedCollections
        {
            get
            {
                if (_selectedCollections == null)
                {
                    _selectedCollections = new List<RealityAssetCollection>();
                    string json = EditorPrefs.GetString(k_SelectedCollectionsKey);
                    if (json != "") { _selectedCollections = json.FromJson<string>().FromPaths<RealityAssetCollection>(); }
                }
                return _selectedCollections;
            }
            private set
            {
                if (_selectedCollections.ContainsSameItemsAs(value)) { return; }
                _selectedCollections = value;
                OnSelectedCollectionsChanged();
            }
        }

        private void OnSelectedCollectionsChanged()
        {
            EditorPrefs.SetString(k_SelectedCollectionsKey, _selectedCollections.ToPaths().ToJson());

            // set the current selection
            Object[] objects = SelectedCollections.ToArray<Object>();
            Selection.objects = objects;

            SelectedAssets = null;

            // make sure the asset collection view gets rerendered
            UpdateSelectedCollections?.Invoke(SelectedCollections);
        }

        /// <summary>
        /// Get the indices of the currently selected collection. 
        /// </summary>
        public List<int> GetSelectedIndices()
        {
            List<int> indices = new List<int>();

            foreach (RealityAssetCollection collection in SelectedCollections)
            {
                int index = _collections.IndexOf(collection);
                if (index != -1) { indices.Add(index); }
            }

            return indices;
        }

        /// <summary>
        /// Gets called when the selected collections change in the list view.
        /// </summary>
        public void OnCollectionSelectedIndicesChange(IEnumerable<int> indices)
        {
            List<RealityAssetCollection> objects = new List<RealityAssetCollection>();
            foreach (int index in indices)
            {
                objects.Add(Collections[index]);
            }
            SelectedCollections = objects;
        }

        /// <summary>
        /// Called when the <see cref="Selection.objects"/> changes
        /// </summary>
        public void OnSelectionChange()
        {
            List<Object> objects = Selection.objects.ToList();

            // first make sure to get the object types, cases:

            // 1. Only RealityAssetCollections
            // 2. Only GameObjects -> determine if they exist inside the current collection (only if a single one is selected)
            // 3. Mixed.

            // first get all the RealityAssetCollections inside the selection
            List<RealityAssetCollection> newCollections = Utils.Filter<RealityAssetCollection>(objects);
            SelectedCollections = newCollections;

            if (SelectedCollections.Count != 1 || SelectedCollections[0] == null) { return; }

            List<GameObject> newAssets = Utils.GetPrefabsInObjects(objects);
            SelectedAssets = newAssets;
        }

        private const string k_SelectedAssetsKey = "selected-assets";
        private string SelectedAssetsKey
        {
            get
            {
                RealityAssetCollection collection = SelectedCollection;
                return string.Join('_', k_SelectedAssetsKey, collection.name);
            }
        }

        private List<GameObject> _selectedAssets = null;

        /// <summary>
        /// The currently selected assets in the currently selected collection
        /// (only if one is selected, otherwise the list is empty)
        /// </summary>
        public List<GameObject> SelectedAssets
        {
            get
            {
                if (_selectedAssets == null)
                {
                    _selectedAssets = new List<GameObject>();
                    string json = EditorPrefs.GetString(SelectedAssetsKey);
                    if (json != "") { _selectedAssets = json.FromJson<string>().FromPaths<GameObject>(); }
                }
                return _selectedAssets;
            }
            set
            {
                if (_selectedAssets.ContainsSameItemsAs(value)) { return; }
                _selectedAssets = value;
                OnSelectedAssetsChanged();
            }
        }

        private void OnSelectedAssetsChanged()
        {
            if (_selectedAssets == null) { return; } // invalid, so don't try to store
            Debug.Log(SelectedAssets.Count);
            EditorPrefs.SetString(SelectedAssetsKey, SelectedAssets.ToPaths().ToJson());
        }

        /// <summary>
        /// Gets called when the selected assets change in the list view.
        /// </summary>
        public void OnAssetsSelectedIndicesChange(IEnumerable<int> indices)
        {
            RealityAssetCollection collection = SelectedCollection;
            List<GameObject> objects = new List<GameObject>();
            foreach (int index in indices)
            {
                objects.Add(collection.Assets[index]);
            }
            SelectedAssets = objects;
        }

        /// <summary>
        /// Get the indices of the currently selected collection. 
        /// </summary>
        public List<int> GetSelectedAssetsIndices()
        {
            RealityAssetCollection collection = SelectedCollection;
            List<int> indices = new List<int>();
            foreach (GameObject asset in SelectedAssets)
            {
                int index = collection.Assets.IndexOf(asset);
                if (index != -1) { indices.Add(index); }
            }
            return indices;
        }

        /// <summary>
        /// Called on <see cref="ListView.itemsAdded"/>, <see cref="ListView.itemsRemoved"/>
        /// and <see cref="ListView.itemIndexChanged"/>. Will save the collection to disk
        /// </summary>
        public void OnAssetsListChanged()
        {
            AssetDatabase.SaveAssetIfDirty(SelectedCollection);
        }

        /// <summary>
        /// Gets the singularly selected collection, WARNING: throws errors if more or less
        /// collections are selected than 1, and if the selected collection is invalid (null or Assets is null). 
        /// </summary>
        private RealityAssetCollection SelectedCollection
        {
            get
            {
                if (SelectedCollections == null)
                {
                    throw new Exception($"{nameof(SelectedCollections)} is null");
                }
                else if (SelectedCollections.Count != 1)
                {
                    throw new Exception($"{nameof(SelectedCollections.Count)} is not 1, do not call this method.");
                }
                else if (SelectedCollections[0] == null || SelectedCollections[0].Assets == null)
                {
                    throw new Exception($"Selected Asset Collection is not valid");
                }
                return SelectedCollections[0];
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnProjectChange()
        {
            Collections = GetCollectionsInProject();
            UpdateCollectionsList?.Invoke();
        }

        private const string k_ThumbnailSizeKey = "thumbnail-size";
        private ThumbnailSize _thumbnailSize = ThumbnailSize.NotInitialized;
        public ThumbnailSize ThumbnailSize
        {
            get
            {
                if (_thumbnailSize == ThumbnailSize.NotInitialized)
                {
                    _thumbnailSize = (ThumbnailSize)EditorPrefs.GetInt(k_ThumbnailSizeKey, (int)ThumbnailSize.Small);
                }
                return _thumbnailSize;
            }
            set
            {
                if (_thumbnailSize == value) { return; }
                _thumbnailSize = value;
                EditorPrefs.SetInt(k_ThumbnailSizeKey, (int)_thumbnailSize);
                OnThumbnailSizeChanged();
            }
        }

        private void OnThumbnailSizeChanged()
        {
        }

        /// <summary>
        /// Creates new asset collection
        /// </summary>
        public void CreateNewAssetCollection()
        {
            string fileName = RealityAssetCollection.DefaultFileName;
            string directory;
            if (SelectedCollections != null && SelectedCollections.Count > 0 && SelectedCollections[0] != null)
            {
                string path = AssetDatabase.GetAssetPath(SelectedCollections[0]);
                directory = Path.GetDirectoryName(path);
            }
            else
            {
                directory = "Assets/";
            }
            string targetPath = Path.Combine(directory, fileName + Constants.k_AssetExtension);
            RealityAssetCollection collection = Utils.CreateAssetCollection(new List<GameObject>(), targetPath);
            Selection.activeObject = collection;
        }
    }
}
