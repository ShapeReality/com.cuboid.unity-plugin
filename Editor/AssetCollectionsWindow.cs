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
        private AssetCollectionsController _controller = new AssetCollectionsController();

        private const string k_ShapeRealityUrl = "https://shapereality.io";
        private const string k_LicenseUrl = "https://cuboid.readthedocs.io/en/latest/about/license";
        private const string k_ManualUrl = "https://cuboid.readthedocs.io";

        private const string k_StyleSheetPath = "Styles/AssetCollectionsWindow";
        private const string k_DarkIconPath = "Icons/cuboid_dark_icon";
        private const string k_LightIconPath = "Icons/cuboid_light_icon";
        private const string k_DarkLogoPath = "Icons/cuboid_dark";
        private const string k_LightLogoPath = "Icons/cuboid_light";

        private const string k_CollectionsToolbarLogo = "collections-toolbar-logo";
        private const string k_CollectionsToolbar = "collections-toolbar";
        private const string k_CollectionsView = "collections-view";
        private const string k_CollectionView = "collection-view";

        private const string k_CollectionHeader = "collection-header";
        private const string k_TitleWithThumbnail = "title-with-thumbnail";
        private const string k_CollectionButtons = "collection-buttons";

        private const string k_CollectionItem = "collection-item";
        private const string k_Asset = "asset";
        private const string k_Title = "title";
        private const string k_AssetMetadata = "asset-metadata";
        private const string k_AssetMetadataTitle = "asset-metadata-title";
        private const string k_AssetMetadataSubscript = "asset-metadata-subscript";
        private const string k_AssetMetadataSubscript2 = "asset-metadata-subscript2";
        private const string k_AssetMetadataMiniThumbnail = "asset-metadata-mini-thumbnail";
        private const string k_AssetMetadataObjectType = "asset-metadata-object-type";

        private StyleSheet _styleSheet;
        private VisualElement _collectionView;
        private ListView _collectionsList;
        private Image _collectionViewThumbnail;
        private EditorApplication.CallbackFunction _onDelayCall;

        /// <summary>
        /// Called when the <see cref="Selection.objects"/> changes
        /// </summary>
        private void OnSelectionChange() => _controller.OnSelectionChange();

        /// <summary>
        /// Called whenever the project changes
        /// </summary>
        private void OnProjectChange() => _controller.OnProjectChange();

        private void Awake()
        {
            LoadStyleSheet();

            // HACK: There is a bug in the ListView that makes it so that it doesn't render the items
            // when changing between thumbnail sizes, so this will make sure it does.
            // TODO: Once again, implement ListView ourselves.
            _onDelayCall = () => { OnProjectChange(); };
            EditorApplication.delayCall += _onDelayCall;
        }

        private void OnEnable()
        {
            _controller.UpdateCollectionsList += UpdateCollectionsList;
            _controller.UpdateSelectedCollections += UpdateSelectedCollections;
        }

        private void OnDisable()
        {
            _controller.UpdateCollectionsList -= UpdateCollectionsList;
            _controller.UpdateSelectedCollections -= UpdateSelectedCollections;
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

        private void UpdateCollectionsList()
        {
            // important to update the itemsSource, not just RefreshItems,
            // because it's not directly bound (because Collections is a property)
            if (_collectionsList != null)
            {
                _collectionsList.itemsSource = _controller.Collections;
            }
        }

        private void CreateGUI()
        {
            // top bar
            Toolbar toolbar = new Toolbar(); toolbar.AddToClassList(k_CollectionsToolbar); rootVisualElement.Add(toolbar);

            // add menu
            ToolbarMenu addMenu = new ToolbarMenu() { text = "Add" }; toolbar.Add(addMenu);
            addMenu.menu.AppendAction("Create New Asset Collection", (_) => _controller.CreateNewAssetCollection());
            addMenu.menu.AppendAction(
                "Convert Selection to Asset Collection",
                (_) => FolderToCollection.ConvertSelectionToCollection(),
                FolderToCollection.ConvertSelectionToCollectionValidate() ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            // logo
            Image logo = new Image() { image = GetLogo() }; logo.AddToClassList(k_CollectionsToolbarLogo); toolbar.Add(logo);

            // help menu
            ToolbarMenu helpMenu = new ToolbarMenu() { text = "Help" }; toolbar.Add(helpMenu);
            helpMenu.menu.AppendAction("Cuboid Manual", (_) => Application.OpenURL(k_ManualUrl));
            helpMenu.menu.AppendAction("License", (_) => Application.OpenURL(k_LicenseUrl));
            helpMenu.menu.AppendAction("shapereality.io", (_) => Application.OpenURL(k_ShapeRealityUrl));

            TwoPaneSplitView splitView = new TwoPaneSplitView(0, 250, TwoPaneSplitViewOrientation.Horizontal);
            rootVisualElement.Add(splitView);
            rootVisualElement.styleSheets.Add(_styleSheet);
            
            splitView.Add(RenderCollectionsList());
            
            _collectionView = new VisualElement();
            splitView.Add(_collectionView);
        }

        /// <summary>
        /// Render collections UI. Returns the element so that it can be added
        /// in the <see cref="CreateGUI"/> method. 
        /// </summary> 
        private VisualElement RenderCollectionsList()
        {
            VisualElement collectionsView = new VisualElement();
            collectionsView.AddToClassList(k_CollectionsView);

            _collectionsList = new ListView()
            {
                viewDataKey = "collections-list", // required for data persistence
                fixedItemHeight = 30,
                selectionType = SelectionType.Multiple,
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
                    Image image = item.Q<Image>();
                    Label label = item.Q<Label>();

                    RealityAssetCollection collection = (index >= 0 && index < _controller.Collections.Count) ?
                        _controller.Collections[index] : null;

                    image.image = collection != null ? ThumbnailProvider.GetCollectionThumbnail(collection) : ThumbnailProvider.EmptyTexture;
                    label.text = collection != null ? collection.name : "";
                },
                itemsSource = _controller.Collections
            };
            _collectionsList.onSelectedIndicesChange += _controller.OnCollectionSelectedIndicesChange;
            collectionsView.Add(_collectionsList);
            return collectionsView;
        }

        private void OnRefreshButtonPressed()
        {
            ThumbnailProvider.EmptyCache();
        }

        private void OnBuildButtonPressed()
        {
            BuildUtils.Build(_controller.SelectedCollections);
        }

        private void OnDuplicateButtonPressed()
        {
            Utils.Duplicate(_controller.SelectedCollections);
        }

        private void OnDeleteButtonPressed()
        {
            Utils.Delete(_controller.SelectedCollections);
        }

        private VisualElement RenderHeader(List<RealityAssetCollection> selectedCollections)
        {
            Toolbar header = new Toolbar(); header.AddToClassList(k_CollectionHeader);

            // determine thumbnail and title
            bool multiple = selectedCollections.Count > 1;
            Texture2D image = multiple ? null : ThumbnailProvider.GetCollectionThumbnail(selectedCollections[0]);
            string title = multiple ? "<Multiple>" : selectedCollections[0].name;

            // title with thumbnail
            VisualElement titleWithThumbnail = new VisualElement(); titleWithThumbnail.AddToClassList(k_TitleWithThumbnail); header.Add(titleWithThumbnail);

            if (image != null)
            {
                Image thumbnail = new Image() { scaleMode = ScaleMode.ScaleToFit, image = image };
                titleWithThumbnail.Add(thumbnail);
            }
            titleWithThumbnail.Add(new Label(title));

            // buttons
            VisualElement buttons = new VisualElement(); buttons.AddToClassList(k_CollectionButtons); header.Add(buttons);

            // refresh button
            Button refreshButton = new Button(OnRefreshButtonPressed); buttons.Add(refreshButton);
            refreshButton.Add(new Image() { image = EditorGUIUtility.IconContent("Refresh").image });

            // build button
            buttons.Add(new Button(OnBuildButtonPressed) { text = "Build..." });

            // more button
            Button moreButton = new Button(() =>
            {
                GenericMenu moreMenu = new GenericMenu();
                moreMenu.AddDisabledItem(new GUIContent("Thumbnail Size"));
                moreMenu.AddItem(new GUIContent("Small"), _controller.ThumbnailSize == ThumbnailSize.Small, () => { _controller.ThumbnailSize = ThumbnailSize.Small; });
                moreMenu.AddItem(new GUIContent("Medium"), _controller.ThumbnailSize == ThumbnailSize.Medium, () => { _controller.ThumbnailSize = ThumbnailSize.Medium; });
                moreMenu.AddItem(new GUIContent("Large"), _controller.ThumbnailSize == ThumbnailSize.Large, () => { _controller.ThumbnailSize = ThumbnailSize.Large; });
                moreMenu.AddSeparator("");
                moreMenu.AddItem(new GUIContent("Duplicate"), false, OnDuplicateButtonPressed);
                moreMenu.AddSeparator("");
                moreMenu.AddItem(new GUIContent("Delete"), false, OnDeleteButtonPressed);
                moreMenu.ShowAsContext();
            });
            moreButton.Add(new Image() { image = EditorGUIUtility.IconContent("_Menu").image }); buttons.Add(moreButton);

            return header;
        }

        /// <summary>
        /// Renders the selected collections in a list using the same items as
        /// the <see cref="RenderAssetCollectionList"/>
        /// </summary>
        private ListView RenderSelectedCollectionsList(List<RealityAssetCollection> selectedCollections)
        {
            ListView selectedCollectionsList = new ListView()
            {
                selectionType = SelectionType.None,
                fixedItemHeight = (int)_controller.ThumbnailSize,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                makeItem = RenderAssetCollectionListItem,
                bindItem = (item, index) =>
                {
                    AssetCollectionListItemData data = new(item);
                    RealityAssetCollection collection = selectedCollections[index];
                    data.Thumbnail.image = ThumbnailProvider.GetCollectionThumbnail(collection);
                    data.Title.text = collection.name;
                    data.Subscript.text = AssetDatabase.GetAssetPath(collection);
                    data.MiniThumbnail.image = AssetPreview.GetMiniThumbnail(collection);
                },
                itemsSource = selectedCollections
            };
            return selectedCollectionsList;
        }

        private ListView RenderAssetCollectionList(RealityAssetCollection collection)
        {
            string name = string.Join('_', collection.name, "assetslist", _controller.ThumbnailSize.ToString());
            ListView assetsList = new ListView()
            {
                name = name,
                viewDataKey = name,
                selectionType = SelectionType.Multiple,
                headerTitle = nameof(RealityAssetCollection.Assets),
                showFoldoutHeader = true,
                showAddRemoveFooter = true,
                showBoundCollectionSize = true,
                showBorder = true,
                reorderable = true,
                reorderMode = ListViewReorderMode.Simple,
                fixedItemHeight = (int)_controller.ThumbnailSize,
                showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly,
                makeItem = RenderAssetCollectionListItem,
                bindItem = (item, index) =>
                {
                    AssetCollectionListItemData data = new AssetCollectionListItemData(item);
                    GameObject asset = collection.Assets[index];
                    data.Thumbnail.image = ThumbnailProvider.GetThumbnail(asset);
                    data.Title.text = asset != null ? asset.name : "None (Game Object)";
                    data.Subscript.text = asset != null ? AssetDatabase.GetAssetPath(asset) : null;
                    data.MiniThumbnail.image = asset != null ? AssetPreview.GetMiniThumbnail(asset) : null;
                    data.Subscript2.text = asset != null ? AssetDatabase.GetAssetPath(asset).EndsWith(".prefab") ? "Prefab" : "Imported Model" : null;

                },
                itemsSource = collection.Assets
            };
            assetsList.itemsAdded += (_) => _controller.OnAssetsListChanged();
            assetsList.itemsRemoved += (_) => _controller.OnAssetsListChanged();
            assetsList.itemIndexChanged += (_, _) => _controller.OnAssetsListChanged();
            assetsList.onSelectedIndicesChange += _controller.OnAssetsSelectedIndicesChange;
            return assetsList;
        }

        /// <summary>
        /// Clears the view and renders the currently selected collection.
        /// If no collection is selected, it will not render anything. 
        /// </summary>
        private void UpdateSelectedCollections(List<RealityAssetCollection> selectedCollections)
        {
            List<int> indices = _controller.GetSelectedIndices();
            _collectionsList.SetSelectionWithoutNotify(indices);

            if (_collectionView == null) { return; }
            _collectionView.Clear();

            if (selectedCollections == null || selectedCollections.Count == 0)
            {
                _collectionView.Add(new Label("No Asset Collection Selected"));
                return;
            }

            // header
            _collectionView.Add(RenderHeader(selectedCollections));

            // list
            if (selectedCollections.Count == 1)
            {
                // renders the list of Assets that are inside the singular selected collection
                ListView assetsList = RenderAssetCollectionList(selectedCollections[0]);
                _collectionView.Add(assetsList);

                List<int> assetsIndices = _controller.GetSelectedAssetsIndices();
                assetsList.SetSelectionWithoutNotify(assetsIndices);
            }
            else
            {
                // renders multiple selected collections to preview for operations like Build, or Delete
                _collectionView.Add(RenderSelectedCollectionsList(selectedCollections));
            }
        }

        /// <summary>
        /// Renders a singular item that is used by both
        /// <see cref="RenderAssetCollectionList"/> and
        /// <see cref="RenderSelectedCollectionsList"/>
        /// </summary>
        private VisualElement RenderAssetCollectionListItem()
        {
            VisualElement element = new VisualElement();
            element.AddToClassList(k_Asset);
            Image thumbnail = new Image()
            {
                scaleMode = ScaleMode.ScaleToFit
            };
            thumbnail.style.width = (int)_controller.ThumbnailSize;
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
        }

        /// <summary>
        /// Performs queries, so that it can be used by both
        /// <see cref="RenderAssetCollectionList"/> and
        /// <see cref="RenderSelectedCollectionsList"/>
        /// </summary>
        private class AssetCollectionListItemData
        {
            public Image Thumbnail { get; private set; }
            public Label Title { get; private set; }
            public Label Subscript { get; private set; }
            public Image MiniThumbnail { get; private set; }
            public Label Subscript2 { get; private set; }

            public AssetCollectionListItemData(VisualElement item)
            {
                Thumbnail = item.Q<Image>();
                Title = item.Q<Label>(className: k_AssetMetadataTitle);
                Subscript = item.Q<Label>(className: k_AssetMetadataSubscript);
                MiniThumbnail = item.Q<Image>(className: k_AssetMetadataMiniThumbnail);
                Subscript2 = item.Q<Label>(className: k_AssetMetadataObjectType);
            }
        }

        #region Editor Window

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

        #endregion
    }
}
