using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Reflection;
using Object = UnityEngine.Object;

namespace Cuboid.UnityPlugin.Editor
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
        private EditorApplication.CallbackFunction _onDelayCall;

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

            // HACK: There is a bug in the ListView that makes it so that it doesn't render the items
            // when changing between thumbnail sizes, so this will make sure it does.
            // TODO: Once again, implement ListView ourselves.
            _onDelayCall = () => { OnProjectChange(); };
            EditorApplication.delayCall += _onDelayCall;
        }

        private void OnDestroy()
        {
            EditorApplication.delayCall -= _onDelayCall;
        }

        private void LoadStyleSheet()
        {
            _styleSheet = Resources.Load<StyleSheet>(k_StyleSheetPath);
            if (_styleSheet == null)
            {
                Debug.LogWarning($"Could not find style sheet at {k_StyleSheetPath}");
            }
        }

        private bool _lastSelectedNullGameObject = false;

        private void OnSelectionChange()
        {
            RealityAssetCollection selectedCollection = Selection.activeObject as RealityAssetCollection;
            if (selectedCollection != null && !_lastSelectedNullGameObject)
            {
                // if the current active object is of type RealityAssetCollection, set the selection to it. 
                _selectedCollection = selectedCollection;
                UpdateCollectionsListSelectedIndex();
            }
            _lastSelectedNullGameObject = false;
        }

        // called when the user performs an action inside the Unity editor
        private void OnProjectChange()
        {
            LoadAssetCollectionsInProject();
            if (_collectionsList != null) { _collectionsList.RefreshItems(); }
            UpdateCollectionsListSelectedIndex();
            RenderSelectedCollectionUI();
        }

        private void UpdateCollectionsListSelectedIndex()
        {
            if (_selectedCollection != null &&
                _collectionsList != null &&
                _collections.Count > 0)
            {
                int index = _collections.IndexOf(_selectedCollection);
                _collectionsList.selectedIndex = index != -1 ? index : 0;
            }
        }

        private void OnCollectionSelectionChange(IEnumerable<object> selectedItems)
        {
            _selectedCollection = selectedItems.First() as RealityAssetCollection;
            OnSelectedCollectionChanged();
        }

        private void OnSelectedCollectionChanged()
        {
            if (_selectedCollection != null)
            {
                Selection.activeObject = _selectedCollection;
            }

            EditorPrefs.SetString(k_SelectedCollectionKey, _selectedCollection != null ? _selectedCollection.name : "");
            RenderSelectedCollectionUI();
        }

        private void CreateNewAssetCollection()
        {
            string fileName = RealityAssetCollection.DefaultFileName;
            string directory;
            if (_selectedCollection != null)
            {
                string path = AssetDatabase.GetAssetPath(_selectedCollection);
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

        private void DuplicateAssetCollection()
        {
            if (_selectedCollection != null)
            {
                string path = AssetDatabase.GetAssetPath(_selectedCollection);
                string newPath = AssetDatabase.GenerateUniqueAssetPath(path);
                bool success = AssetDatabase.CopyAsset(path, newPath);
                if (!success)
                {
                    throw new Exception($"Failed to duplicate selected Asset Collection from {path} to {newPath}");
                }
                _selectedCollection = AssetDatabase.LoadAssetAtPath<RealityAssetCollection>(newPath);
                OnSelectedCollectionChanged();
            }
        }

        private void DeleteAssetCollection()
        {
            if (_selectedCollection != null)
            {
                string path = AssetDatabase.GetAssetPath(_selectedCollection);
                AssetDatabase.DeleteAsset(path);
                _selectedCollection = null;
                AssetDatabase.Refresh();
                OnProjectChange();
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

            if (selection.Length > 0 && selection[selection.Length - 1] != null)
            {
                // set the selection
                Selection.objects = selection;
            }
            else
            {
                _lastSelectedNullGameObject = true; // a bit hacky, but makes sure the list view
                // stays focused, instead of focusing the _selectedCollection. 
                Selection.objects = new Object[] { _selectedCollection };
            }

            // store the selection
            EditorPrefs.SetString(k_SelectedAssetsKey + _selectedCollection.name, indicesList.ToJson());
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
            addMenu.menu.AppendAction("Create New Asset Collection", (_) => { CreateNewAssetCollection(); });
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

            DragAndDropManipulator dragAndDropManipulator = new DragAndDropManipulator(rootVisualElement, (gameObjects) =>
            {
                if (_selectedCollection != null)
                {
                    _selectedCollection.Assets.AddRange(gameObjects);
                    SaveSelectedCollection();
                    if (_assetsList != null) { _assetsList.RefreshItems(); }
                }
            });
        }

        private void SaveSelectedCollection()
        {
            if (_selectedCollection != null)
            {
                EditorUtility.SetDirty(_selectedCollection);
                AssetDatabase.SaveAssets();
                // update the icon
                _collectionsList.RefreshItem(_collections.IndexOf(_selectedCollection));
                _collectionViewThumbnail.image = GetCollectionThumbnail(_selectedCollection);
            }
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

        private Image _collectionViewThumbnail;

        /// <summary>
        /// Clears the view and renders the currently selected collection.
        /// If no collection is selected, it will not render anything. 
        /// </summary>
        private void RenderSelectedCollectionUI()
        {
            if (_collectionView == null)
            {
                return;
            }
            _collectionView.Clear();

            if (_selectedCollection == null) { return; }

            Toolbar header = new Toolbar() { name = "CollectionHeader" };
            _collectionView.Add(header);

            VisualElement titleWithThumbnail = new VisualElement() { name = "TitleWithThumbnail"};

            _collectionViewThumbnail = new Image()
            {
                scaleMode = ScaleMode.ScaleToFit,
                image = GetCollectionThumbnail(_selectedCollection)
            };
            titleWithThumbnail.Add(_collectionViewThumbnail);
            titleWithThumbnail.Add(new Label(_selectedCollection.name)
            {
            });
            header.Add(titleWithThumbnail);

            VisualElement buttons = new VisualElement() { name = "CollectionButtons" };
            header.Add(buttons);

            Button refreshButton = new Button(() => { ThumbnailProvider.EmptyCache(); OnProjectChange(); });
            refreshButton.Add(new Image()
            {
                image = EditorGUIUtility.IconContent("Refresh").image
            });
            buttons.Add(refreshButton);
            buttons.Add(new Button(() =>
            {
                BuildUtils.Build(_selectedCollection);
            })
            {
                text = "Build..."
            });
            Button moreButton = new Button(() =>
            {
                GenericMenu moreMenu = new GenericMenu();
                
                moreMenu.AddDisabledItem(new GUIContent("Thumbnail Size"));
                moreMenu.AddItem(new GUIContent("Small"), CurrentThumbnailSize == ThumbnailSize.Small, () => { CurrentThumbnailSize = ThumbnailSize.Small; });
                moreMenu.AddItem(new GUIContent("Medium"), CurrentThumbnailSize == ThumbnailSize.Medium, () => { CurrentThumbnailSize = ThumbnailSize.Medium; });
                moreMenu.AddItem(new GUIContent("Large"), CurrentThumbnailSize == ThumbnailSize.Large, () => { CurrentThumbnailSize = ThumbnailSize.Large; });
                moreMenu.AddSeparator("");
                moreMenu.AddItem(new GUIContent("Duplicate"), false, () => DuplicateAssetCollection());
                moreMenu.AddSeparator("");
                moreMenu.AddItem(new GUIContent("Delete"), false, () => DeleteAssetCollection());

                moreMenu.ShowAsContext();
            });
            moreButton.Add(new Image()
            {
                image = EditorGUIUtility.IconContent("_Menu").image
            });
            buttons.Add(moreButton);

            string name = _selectedCollection.name + "_assetsList_" + CurrentThumbnailSize.ToString();
            _assetsList = new ListView()
            {
                name = name,
                viewDataKey = name,
                selectionType = SelectionType.Multiple,
                headerTitle = "Assets",
                showFoldoutHeader = true,
                showAddRemoveFooter = true,
                showBoundCollectionSize = true,
                showBorder = true,
                reorderable = true,
                reorderMode = ListViewReorderMode.Simple, // animated doesn't support reording multiple items at once
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
                    image.image = ThumbnailProvider.GetThumbnail(asset);
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
            _assetsList.itemsAdded += (_) => { SaveSelectedCollection(); };
            _assetsList.itemsRemoved += (_) => { SaveSelectedCollection(); };
            _assetsList.itemIndexChanged += (_, _) => { SaveSelectedCollection(); };
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

        private Texture GetCollectionThumbnail(RealityAssetCollection collection)
        {
            return ThumbnailProvider.GetThumbnail(collection.Assets.Count > 0 ? collection.Assets[0] : null);
        }
    }
}
