using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

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

        private string k_SelectedCollectionsKey = "selected-collections";
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

            // make sure the asset collection view gets rerendered
            UpdateSelectedCollections?.Invoke(SelectedCollections);
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

            //List<GameObject> newPrefabs = Utils.GetPrefabsInObjects(objects);

            //if (!newPrefabs.ContainsSameItemsAs(SelectedCollections[0].Assets))
            //{

            //}
        }

        /// <summary>
        /// 
        /// </summary>
        public void OnProjectChange()
        {
            Collections = GetCollectionsInProject();
            UpdateCollectionsList?.Invoke();
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
