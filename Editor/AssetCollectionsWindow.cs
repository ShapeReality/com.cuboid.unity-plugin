using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;

namespace Cuboid.UnityPlugin
{
    public class AssetCollectionsWindow : EditorWindow
    {
        private enum ThumbnailSize
        {
            NotInitialized = 0,
            Small = 64,
            Medium = 128,
            Large = 256
        }

        private const string k_ShapeRealityUrl = "https://shapereality.io";
        private const string k_LicenseUrl = "https://cuboid.readthedocs.io/en/latest/about/license";
        private const string k_ManualUrl = "https://cuboid.readthedocs.io";
        
        private const string k_StyleSheetPath = "Styles/AssetCollectionsWindow";
        private const string k_DarkIconPath = "Icons/cuboid_dark_icon";
        private const string k_LightIconPath = "Icons/cuboid_light_icon";
        private const string k_DarkLogoPath = "Icons/cuboid_dark";
        private const string k_LightLogoPath = "Icons/cuboid_light";

        private const string k_CollectionsView = "collections-view";
        private const string k_CollectionView = "collection-view";
        private const string k_CollectionItem = "collection-item";
        private const string k_Asset = "asset";
        private const string k_Title = "title";
        private const string k_AssetMetadata = "asset-metadata";
        private const string k_AssetMetadataTitle = "asset-metadata-title";
        private const string k_AssetMetadataSubscript = "asset-metadata-subscript";
        private const string k_AssetMetadataSubscript2 = "asset-metadata-subscript2";
        private const string k_AssetMetadataMiniThumbnail = "asset-metadata-mini-thumbnail";
        private const string k_AssetMetadataObjectType = "asset-metadata-object-type";

        private const string k_SelectedCollectionKey = "selected-collection";
        private const string k_SelectedAssetsKey = "selected-assets_";

        private List<RealityAssetCollection> _collections = new();

        private StyleSheet _styleSheet;
        private VisualElement _collectionView;
        private ListView _collectionsList;
        private ListView _assetsList;
        private Texture2D _emptyTexture;

        private const string k_ThumbnailSizeKey = "thumbnail-size";
        private ThumbnailSize _currentThumbnailSize = ThumbnailSize.NotInitialized;
        private ThumbnailSize CurrentThumbnailSize
        {
            get
            {
                if (_currentThumbnailSize == ThumbnailSize.NotInitialized)
                {
                    _currentThumbnailSize = (ThumbnailSize)EditorPrefs.GetInt(k_ThumbnailSizeKey, (int)ThumbnailSize.Small);
                }
                return _currentThumbnailSize;
            }
            set
            {
                _currentThumbnailSize = value;
                EditorPrefs.SetInt(k_ThumbnailSizeKey, (int)_currentThumbnailSize);
                RenderSelectedCollectionUI();
            }
        }

        private RealityAssetCollection _selectedCollection;

        [MenuItem("Cuboid/Asset Collections")]
        public static void ShowWindow()
        {
            // This method is called when the user selects the menu item in the Editor
            EditorWindow wnd = GetWindow<AssetCollectionsWindow>();

            wnd.minSize = new Vector2(400f, 200f);
            wnd.Show();

            UpdateTitleContent(wnd);
        }

        private static Texture GetIcon()
        {
            string path = EditorGUIUtility.isProSkin ? k_DarkIconPath : k_LightIconPath;
            return Resources.Load<Texture>(path);
        }

        private static Texture GetLogo()
        {
            string path = EditorGUIUtility.isProSkin ? k_DarkLogoPath : k_LightLogoPath;
            return Resources.Load<Texture>(path);
        }

        private static void UpdateTitleContent(EditorWindow editorWindow)
        {
            editorWindow.titleContent = new GUIContent("Asset Collections", GetIcon());
        }

        private void Awake()
        {
            LoadAssetCollectionsInProject();

            // set selection collection based on stored name in EditorPrefs
            string selectedCollectionName = EditorPrefs.GetString(k_SelectedCollectionKey);
            _selectedCollection = _collections.Find((c) => c.name == selectedCollectionName);

            LoadStyleSheet();
            _emptyTexture = new Texture2D(256, 256);
        }

        private void LoadStyleSheet()
        {
            _styleSheet = Resources.Load<StyleSheet>(k_StyleSheetPath);
            if (_styleSheet == null)
            {
                Debug.LogWarning($"Could not find style sheet at {k_StyleSheetPath}");
            }
        }

        private void OnSelectionChange()
        {
            RealityAssetCollection selectedCollection = Selection.activeObject as RealityAssetCollection;
            if (selectedCollection != null)
            {
                // if the current active object is of type RealityAssetCollection, set the selection to it. 
                _selectedCollection = selectedCollection;
                UpdateCollectionsListSelectedIndex();
            }
        }

        // called when the user performs an action inside the Unity editor
        private void OnProjectChange()
        {
            ThumbnailProvider.EmptyCache();
            LoadAssetCollectionsInProject();
            if (_collectionsList != null) { _collectionsList.RefreshItems(); }
            if (_assetsList != null) { _assetsList.RefreshItems(); }
            UpdateCollectionsListSelectedIndex();
        }

        private void UpdateCollectionsListSelectedIndex()
        {
            if (_selectedCollection != null && _collectionsList != null)
            {
                _collectionsList.selectedIndex = _collections.IndexOf(_selectedCollection);
            }
        }

        private void CreateGUI()
        {
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
            
            splitView.Add(RenderCollectionsUI());
            
            _collectionView = new VisualElement();
            splitView.Add(_collectionView);
            RenderSelectedCollectionUI();

            UpdateCollectionsListSelectedIndex();
        }

        private void OnCollectionSelectionChange(IEnumerable<object> selectedItems)
        {
            _selectedCollection = selectedItems.First() as RealityAssetCollection;

            if (_selectedCollection != null)
            {
                Selection.activeObject = _selectedCollection;
            }

            EditorPrefs.SetString(k_SelectedCollectionKey, _selectedCollection.name);

            RenderSelectedCollectionUI();
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
            EditorPrefs.SetString(k_SelectedAssetsKey + _selectedCollection.name, indicesList.ToJson());
        }

        /// <summary>
        /// Render collections UI. Returns the element so that it can be added
        /// in the <see cref="CreateGUI"/> method. 
        /// </summary> 
        private VisualElement RenderCollectionsUI()
        {
            VisualElement collectionsView = new VisualElement();
            collectionsView.AddToClassList(k_CollectionsView);

            _collectionsList = new ListView()
            {
                viewDataKey = "collections-list", // required for data persistence
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

        /// <summary>
        /// Clears the view and renders the currently selected collection.
        /// If no collection is selected, it will not render anything. 
        /// </summary>
        private void RenderSelectedCollectionUI()
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
                moreMenu.AddItem(new GUIContent("Delete"), false, () => { });

                moreMenu.AddSeparator("");
                moreMenu.AddDisabledItem(new GUIContent("Thumbnail Size"));
                moreMenu.AddItem(new GUIContent("Small"), CurrentThumbnailSize == ThumbnailSize.Small, () => { CurrentThumbnailSize = ThumbnailSize.Small; });
                moreMenu.AddItem(new GUIContent("Medium"), CurrentThumbnailSize == ThumbnailSize.Medium, () => { CurrentThumbnailSize = ThumbnailSize.Medium; });
                moreMenu.AddItem(new GUIContent("Large"), CurrentThumbnailSize == ThumbnailSize.Large, () => { CurrentThumbnailSize = ThumbnailSize.Large; });


                moreMenu.ShowAsContext();
            });
            moreButton.Add(new Image()
            {
                image = EditorGUIUtility.IconContent("_Menu").image
            });
            buttons.Add(moreButton);

            _assetsList = new ListView()
            {
                viewDataKey = _selectedCollection.name + "_assetsList_" + (int)CurrentThumbnailSize,
                selectionType = SelectionType.Multiple,
                headerTitle = "Assets",
                showFoldoutHeader = true,
                showAddRemoveFooter = true,
                showBoundCollectionSize = true,
                showBorder = true,
                reorderable = true,
                reorderMode = ListViewReorderMode.Animated,
                fixedItemHeight = (int)CurrentThumbnailSize,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                makeItem = () =>
                {
                    VisualElement element = new VisualElement();
                    element.AddToClassList(k_Asset);
                    Image thumbnail = new Image()
                    {
                        scaleMode = ScaleMode.ScaleToFit
                    };
                    thumbnail.style.width = (int)CurrentThumbnailSize;
                    element.Add(thumbnail);

                    VisualElement metadata = new VisualElement();
                    metadata.AddToClassList(k_AssetMetadata);
                    element.Add(metadata);
                    Label title = new Label();
                    title.AddToClassList(k_AssetMetadataTitle);
                    metadata.Add(title);

                    Label subscript = new Label();
                    subscript.AddToClassList(k_AssetMetadataSubscript);
                    metadata.Add(subscript);

                    VisualElement subscript2 = new VisualElement();
                    subscript2.AddToClassList(k_AssetMetadataSubscript2);
                    metadata.Add(subscript2);

                    Image miniThumbnail = new Image()
                    {
                        scaleMode = ScaleMode.ScaleToFit
                    };
                    miniThumbnail.AddToClassList(k_AssetMetadataMiniThumbnail);
                    subscript2.Add(miniThumbnail);

                    Label objectType = new Label();
                    objectType.AddToClassList(k_AssetMetadataObjectType);
                    subscript2.Add(objectType);

                    return element;
                },
                bindItem = (item, index) =>
                {
                    GameObject asset = _selectedCollection.Assets[index];
                    Image image = item.Q<Image>();
                    image.image = GetAssetThumbnail(asset);
                    Label metadataTitle = item.Q<Label>(className: k_AssetMetadataTitle);
                    metadataTitle.text = asset != null ? asset.name : "None (Game Object)";

                    Label metadataSubscript = item.Q<Label>(className: k_AssetMetadataSubscript);
                    metadataSubscript.text = asset != null ? AssetDatabase.GetAssetPath(asset) : "";

                    Image metadataMiniThumbnail = item.Q<Image>(className: k_AssetMetadataMiniThumbnail);
                    metadataMiniThumbnail.image = asset != null ? AssetPreview.GetMiniThumbnail(asset) : null;

                    Label metadataObjectType = item.Q<Label>(className: k_AssetMetadataObjectType);
                    
                    
                    metadataObjectType.text = asset != null ? AssetDatabase.GetAssetPath(asset).EndsWith(".prefab") ? "Prefab" : "Imported Model" : null;
                },
                itemsSource = _selectedCollection.Assets
            };
            
            _collectionView.Add(_assetsList);

            // Sets the selected index
            string json = EditorPrefs.GetString(k_SelectedAssetsKey + _selectedCollection.name, "");
            if (json != "")
            {
                List<int> indices = json.ToIntList();
                _assetsList.SetSelection(indices);
            }
            
            _assetsList.onSelectedIndicesChange += OnAssetsSelectedIndicesChange;
        }

        /// <summary>
        /// Loads all asset collections of type <see cref="RealityAssetCollection"/>
        /// that exist in the Assets folder. 
        /// </summary>
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
            return ThumbnailProvider.GetThumbnail(asset);
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
