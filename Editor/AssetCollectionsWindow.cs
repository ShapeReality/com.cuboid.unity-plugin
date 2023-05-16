using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.UIElements;

namespace Cuboid.UnityPlugin
{
    public class AssetCollectionsWindow : EditorWindow
    {
        private List<RealityAssetCollection> _collections = new();
        private VisualElement _collectionView;
        private Texture2D _emptyTexture;

        [MenuItem("Cuboid/Asset Collections")]
        public static void ShowMyEditor()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<AssetCollectionsWindow>();
            wnd.titleContent = new GUIContent("Asset Collections");
        }

        private void Awake()
        {
            _emptyTexture = new Texture2D(256, 256);
            LoadAssetCollectionsInProject();
        }

        private void CreateGUI()
        {
            TwoPaneSplitView splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);

            // collections
            ListView collectionsView = new ListView();
            splitView.Add(collectionsView);

            _collectionView = new VisualElement();
            splitView.Add(_collectionView);

            collectionsView.makeItem = () => new Label();
            collectionsView.bindItem = (item, index) => { (item as Label).text = _collections[index].name; };
            collectionsView.itemsSource = _collections;
            collectionsView.onSelectionChange += OnCollectionSelectionChange;
        }

        private void OnCollectionSelectionChange(IEnumerable<object> selectedItems)
        {
            _collectionView.Clear();

            RealityAssetCollection selectedCollection = selectedItems.First() as RealityAssetCollection;
            if (selectedCollection == null) { return; }

            Image image = new Image()
            {
                scaleMode = ScaleMode.ScaleToFit,
                sprite = CreateSprite(GetCollectionPreviewTexture(selectedCollection))
            };
        }

        private Sprite CreateSprite(Texture2D texture)
        {
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            return sprite;
        }

        private Texture2D GetAssetPreviewTexture(GameObject asset)
        {
            if (asset == null) { return _emptyTexture; }
            return AssetPreview.GetAssetPreview(asset);
        }

        private Texture2D GetCollectionPreviewTexture(RealityAssetCollection collection)
        {
            Texture2D preview = null;
            if (collection.Assets.Count > 0)
            {
                // get the preview image of the first asset in the collection
                GameObject asset = collection.Assets[0];
                preview = GetAssetPreviewTexture(asset);
            }
            else
            {
                preview = _emptyTexture;
            }
            return preview;
        }

        private void LoadAssetCollectionsInProject()
        {
            string[] guids = AssetDatabase.FindAssets("t:RealityAssetCollection");

            _collections.Clear();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                RealityAssetCollection asset = AssetDatabase.LoadAssetAtPath<RealityAssetCollection>(path);

                _collections.Add(asset);
            }
        }
    }
}
