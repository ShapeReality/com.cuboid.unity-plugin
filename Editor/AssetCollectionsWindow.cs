using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace Cuboid.UnityPlugin
{
    public class AssetCollectionsWindow : EditorWindow
    {
        private const string k_ShapeRealityUrl = "https://shapereality.io";
        private const string k_LicenseUrl = "https://cuboid.readthedocs.io/en/latest/about/license";
        private const string k_ManualUrl = "https://cuboid.readthedocs.io";
        private const string k_PackagePath = "Packages/com.cuboid.unity-plugin";
        private const string k_StyleSheetPath = "Editor/AssetCollectionsWindow.uss";
        private const string k_DarkIconPath = "Editor/cuboid_dark_icon.png";
        private const string k_LightIconPath = "Editor/cuboid_light_icon.png";
        private const string k_DarkLogoPath = "Editor/cuboid_dark.png";
        private const string k_LightLogoPath = "Editor/cuboid_light.png";
        private const string k_CollectionsView = "collections-view";
        private const string k_CollectionView = "collection-view";
        private const string k_CollectionItem = "collection-item";
        private const string k_Asset = "asset";
        private const string k_Title = "title";

        private const string k_SelectedCollectionKey = "selected-collection";
        private const string k_SelectedAssetsKey = "selected-assets_";

        private List<RealityAssetCollection> _collections = new();
        private Dictionary<RealityAssetCollection, Sprite> _thumbnailsCache = new();
        private RealityAssetCollection _selectedCollection;

        private StyleSheet _styleSheet;
        private VisualElement _collectionView;
        private ListView _collectionsList;
        private ListView _assetsList;
        private Texture2D _emptyTexture;
        
        [MenuItem("Cuboid/Asset Collections")]
        public static void ShowMyEditor()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<AssetCollectionsWindow>();

            UpdateTitleContent(wnd);
        }

        private static Texture GetIcon()
        {
            string path = GetPath(EditorGUIUtility.isProSkin ? k_DarkIconPath : k_LightIconPath);
            return AssetDatabase.LoadAssetAtPath<Texture>(path);
        }

        private static Texture GetLogo()
        {
            string path = GetPath(EditorGUIUtility.isProSkin ? k_DarkLogoPath : k_LightLogoPath);
            return AssetDatabase.LoadAssetAtPath<Texture>(path);
        }

        private static string GetPath(string path)
        {
            return Path.Combine(k_PackagePath, path);
        }

        private static void UpdateTitleContent(EditorWindow editorWindow)
        {
            editorWindow.titleContent = new GUIContent("Asset Collections", GetIcon());
        }

        private void Awake()
        {
            LoadAssetCollectionsInProject();
            
            string selectedCollectionName = EditorPrefs.GetString(k_SelectedCollectionKey);
            _selectedCollection = _collections.Find((c) => c.name == selectedCollectionName);
            Debug.Log($"name: {selectedCollectionName}, collection: {_selectedCollection}");
        }

        private void InitializeUI()
        {
           if (_styleSheet == null)
            {
                string path = Path.Combine(k_PackagePath, k_StyleSheetPath);
                _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                if (_styleSheet == null)
                {
                    Debug.LogWarning($"Could not find style sheet at {path}");
                }
            }

            if (_emptyTexture == null)
            {
                _emptyTexture = new Texture2D(256, 256);
            }
        }

        private void OnSelectionChange()
        {
            RealityAssetCollection selectedCollection = Selection.activeObject as RealityAssetCollection;
            if (selectedCollection != null)
            {
                _selectedCollection = selectedCollection;
                SetSelection();
            }
        }

        private void OnProjectChange()
        {
            LoadAssetCollectionsInProject();
            _collectionsList.RefreshItems();
            if (_assetsList != null)
            {
                _assetsList.RefreshItems();
            }
            SetSelection();
        }

        private void SetSelection()
        {
            if (_selectedCollection != null)
            {
                _collectionsList.selectedIndex = _collections.IndexOf(_selectedCollection);
            }
        }

        private void CreateGUI()
        {
            InitializeUI();

            // top bar
            Toolbar toolbar = new Toolbar() { name = "AssetCollectionsToolbar"};
            rootVisualElement.Add(toolbar);

            ToolbarMenu addMenu = new ToolbarMenu()
            {
                name = "AddMenu",
                text = "Add"
            };
            addMenu.menu.AppendAction("Create New Asset Collection", (_) => { });
            toolbar.Add(addMenu);

            toolbar.Add(new Image()
            {
                name = "ToolbarLogo",
                image = GetLogo()
            });

            ToolbarMenu helpMenu = new ToolbarMenu()
            {
                text = "Help"
            };
            helpMenu.menu.AppendAction("Cuboid Manual", (_) => { Application.OpenURL(k_ManualUrl); });
            helpMenu.menu.AppendAction("License", (_) => { Application.OpenURL(k_LicenseUrl); });
            helpMenu.menu.AppendSeparator();
            helpMenu.menu.AppendAction("shapereality.io", (_) => { Application.OpenURL(k_ShapeRealityUrl); });
            toolbar.Add(helpMenu);

            TwoPaneSplitView splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);
            rootVisualElement.styleSheets.Add(_styleSheet);
            
            splitView.Add(UI_Collections());
            
            _collectionView = new VisualElement();
            splitView.Add(_collectionView);
            UI_Collection();

            SetSelection();
        }

        private void OnCollectionSelectionChange(IEnumerable<object> selectedItems)
        {
            _selectedCollection = selectedItems.First() as RealityAssetCollection;

            if (_selectedCollection != null)
            {
                Selection.activeObject = _selectedCollection;
            }

            EditorPrefs.SetString(k_SelectedCollectionKey, _selectedCollection.name);

            UI_Collection();
        }

        [System.Serializable]
        public class IntList
        {
            public List<int> Value;

            public IntList(List<int> value)
            {
                Value = value;
            }
        }

        private void OnAssetsSelectedIndicesChange(IEnumerable<int> indices)
        {
            Object[] selection = new Object[indices.Count()];

            List<int> indicesList = new List<int>();
            int i = 0;
            foreach (int index in indices)
            {
                indicesList.Add(index);
                selection[i++] = _selectedCollection.Assets[index];
            }

            // set the selection
            Selection.objects = selection;

            // store the selection
            EditorPrefs.SetString(k_SelectedAssetsKey + _selectedCollection.name, JsonUtility.ToJson(new IntList(indicesList)));
        }

        //private void OnAssetsSelectionChange(IEnumerable<object> selectedItems)
        //{
        //    // select the assets
        //    Object[] selection = new Object[selectedItems.Count()];
        //    int i = 0;
        //    foreach (object item in selectedItems)
        //    {
        //        selection[i++] = item as Object;
        //    }

        //    Selection.objects = selection;
        //}

        private VisualElement UI_Collections()
        {
            // collections
            VisualElement collectionsView = new VisualElement();
            collectionsView.AddToClassList(k_CollectionsView);

            _collectionsList = new ListView()
            {
                viewDataKey = "collectionsList",
                fixedItemHeight = 30,
                makeItem = () =>
                {
                    VisualElement element = new VisualElement();
                    element.AddToClassList(k_CollectionItem);
                    element.Add(new Image()
                    {
                        scaleMode = ScaleMode.ScaleToFit
                    });
                    element.Add(new Label());
                    return element;
                },
                bindItem = (item, index) =>
                {
                    RealityAssetCollection collection = _collections[index];
                    Image image = item.Q<Image>();
                    image.image = GetCollectionThumbnail(collection);
                    Label label = item.Q<Label>();
                    label.text = collection.name;
                },
                itemsSource = _collections
            };
            _collectionsList.onSelectionChange += OnCollectionSelectionChange;

            collectionsView.Add(_collectionsList);

            return collectionsView;
        }

        private void UI_Collection()
        {
            _collectionView.Clear();

            if (_selectedCollection == null) { return; }

            Toolbar header = new Toolbar() { name = "CollectionHeader" };
            _collectionView.Add(header);

            VisualElement titleWithThumbnail = new VisualElement() { name = "TitleWithThumbnail"};
            titleWithThumbnail.Add(new Image()
            {
                scaleMode = ScaleMode.ScaleToFit,
                image = GetCollectionThumbnail(_selectedCollection)
            });
            titleWithThumbnail.Add(new Label(_selectedCollection.name)
            {
            });
            header.Add(titleWithThumbnail);

            VisualElement buttons = new VisualElement() { name = "CollectionButtons" };
            header.Add(buttons);

            Button refreshButton = new Button(() => { OnProjectChange(); });
            refreshButton.Add(new Image()
            {
                image = EditorGUIUtility.IconContent("Refresh").image
            });
            buttons.Add(refreshButton);
            buttons.Add(new Button(() =>
            {
                _selectedCollection.Build();
            })
            {
                text = "Build..."
            });
            Button moreButton = new Button(() =>
            {
                GenericMenu moreMenu = new GenericMenu();
                moreMenu.AddItem(new GUIContent("Duplicate"), false, () => { });
                moreMenu.AddItem(new GUIContent("Delete"), false, () => { });

                moreMenu.ShowAsContext();
            });
            moreButton.Add(new Image()
            {
                image = EditorGUIUtility.IconContent("_Menu").image
            });
            buttons.Add(moreButton);

            _assetsList = new ListView()
            {
                viewDataKey = _selectedCollection.name + "_assetsList",
                selectionType = SelectionType.Multiple,
                headerTitle = "Assets",
                showFoldoutHeader = true,
                showAddRemoveFooter = true,
                showBoundCollectionSize = true,
                showBorder = true,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                fixedItemHeight = 60,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                makeItem = () =>
                {
                    VisualElement element = new VisualElement();
                    element.AddToClassList(k_Asset);
                    element.Add(new Image()
                    {
                        scaleMode = ScaleMode.ScaleToFit
                    });
                    element.Add(new Label());
                    return element;
                },
                bindItem = (item, index) =>
                {
                    GameObject asset = _selectedCollection.Assets[index];
                    Image image = item.Q<Image>();
                    image.image = GetAssetThumbnail(asset);
                    Label label = item.Q<Label>();
                    label.text = asset != null ? asset.name : "None (Game Object)";
                },
                itemsSource = _selectedCollection.Assets
            };
            
            _collectionView.Add(_assetsList);

            string json = EditorPrefs.GetString(k_SelectedAssetsKey + _selectedCollection.name, "");
            if (json != "")
            {
                List<int> indices = JsonUtility.FromJson<IntList>(json).Value;
                _assetsList.SetSelection(indices);
            }
            
            _assetsList.onSelectedIndicesChange += OnAssetsSelectedIndicesChange;
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

        #region Thumbnails

        private Texture GetAssetThumbnail(GameObject asset)
        {
            if (asset == null) { return _emptyTexture; }
            return AssetPreview.GetAssetPreview(asset);
        }

        private Texture GetCollectionThumbnail(RealityAssetCollection collection)
        {
            Texture preview = _emptyTexture;
            if (collection.Assets.Count > 0)
            {
                // get the preview image of the first asset in the collection
                GameObject asset = collection.Assets[0];
                preview = GetAssetThumbnail(asset);
            }
            return preview;
        }

        #endregion
    }
}
