﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using log4net;
using log4net.Config;
using System.Configuration;
using System.IO;

namespace ManageWalla
{
    /// <summary>
    /// Interaction logic for MainTwo.xaml
    /// </summary>
    public partial class MainTwo : Window
    {
        private enum PaneMode
        {
            CategoryView = 0,
            CategoryEdit = 1,
            CategoryAdd = 2,
            TagView = 3,
            TagEdit = 4,
            TagAdd = 5,
            GalleryView = 6,
            GalleryEdit = 7,
            GalleryAdd = 8,
            Upload = 9,
            Account = 10
        }

        private enum FetchDirection
        {
            Begin = 0,
            Last = 1,
            Next = 2,
            Previous = 3
        }

        public enum MessageSeverity
        {
            Info = 0,
            Warning = 1,
            Error = 2
        }

        private PaneMode currentPane;
        private Tag currentTag = null;
        private Gallery currentGallery = null;
        private Category currentCategory = null;
        private MainController controller = null;
        public UploadUIState uploadUIState = null;
        public UploadImageFileList uploadFots = null;
        public UploadStatusListBind uploadStatusListBind = null;
        public ImageMainViewerList imageMainViewerList = null;
        public GlobalState state = null;
        public ImageList currentImageList = null;
        private bool tagListUploadRefreshing = false;

        private static readonly ILog logger = LogManager.GetLogger(typeof(MainTwo));

        public MainTwo()
        {
            InitializeComponent();

            uploadFots = (UploadImageFileList)FindResource("uploadImagefileListKey");
            uploadUIState = (UploadUIState)FindResource("uploadUIStateKey");
            uploadStatusListBind = (UploadStatusListBind)FindResource("uploadStatusListBindKey");
            imageMainViewerList = (ImageMainViewerList)FindResource("imageMainViewerListKey");
        }

        #region Pane Control Events
        private void radCategory_Checked(object sender, RoutedEventArgs e)
        {
            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Visible;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdGalleryRefresh.Visibility = System.Windows.Visibility.Hidden;
            lblUploadProposedImageCount.Visibility = System.Windows.Visibility.Hidden;

            RefreshOverallPanesStructure(PaneMode.CategoryView);
            RefreshPanesAllControls(PaneMode.CategoryView);
            RefreshAndDisplayCategoryList(false);
        }

        private void radUpload_Checked(object sender, RoutedEventArgs e)
        {
            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdGalleryRefresh.Visibility = System.Windows.Visibility.Hidden;
            lblUploadProposedImageCount.Visibility = System.Windows.Visibility.Visible;

            RefreshOverallPanesStructure(PaneMode.Upload);
            RefreshPanesAllControls(PaneMode.Upload);
        }

        private void radGallery_Checked(object sender, RoutedEventArgs e)
        {
            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdGalleryRefresh.Visibility = System.Windows.Visibility.Visible;
            lblUploadProposedImageCount.Visibility = System.Windows.Visibility.Hidden;

            RefreshOverallPanesStructure(PaneMode.GalleryView);
            RefreshPanesAllControls(PaneMode.GalleryView);
            RefreshAndDisplayGalleryList(false);
        }

        private void radTag_Checked(object sender, RoutedEventArgs e)
        {
            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Visible;
            cmdGalleryRefresh.Visibility = System.Windows.Visibility.Hidden;
            lblUploadProposedImageCount.Visibility = System.Windows.Visibility.Hidden;

            RefreshOverallPanesStructure(PaneMode.TagView);
            RefreshPanesAllControls(PaneMode.TagView);
            RefreshAndDisplayTagList(false);
        }

        private void cmdAccount_Click(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(PaneMode.Account);
        }

        private void cmdContract_Click(object sender, RoutedEventArgs e)
        {
            gridLeft.ColumnDefinitions[0].Width = new GridLength(40);
            gridLeft.ColumnDefinitions[1].Width = new GridLength(0);

            gridRight.RowDefinitions[0].Height = new GridLength(0);
        }

        private void cmdExpand_Click(object sender, RoutedEventArgs e)
        {
            gridLeft.ColumnDefinitions[0].Width = new GridLength(0);
            gridLeft.ColumnDefinitions[1].Width = new GridLength(300);
            gridRight.RowDefinitions[0].Height = new GridLength(40);
        }

        private void RefreshOverallPanesStructure(PaneMode mode)
        {
            //Ensure panes are all correctly setup each time a refresh is called.
            gridLeft.ColumnDefinitions[0].Width = new GridLength(0); //Sidebar
            gridLeft.ColumnDefinitions[1].Width = new GridLength(300); //Main control
            gridLeft.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star); //Image display grid
            gridRight.RowDefinitions[0].Height = new GridLength(40); //Working Pane

            cmdSort.IsChecked = false;
            cmdFilter.IsChecked = false;
            ShowHideFilterSort();

            switch (mode)
            {
                case PaneMode.CategoryView:
                case PaneMode.CategoryAdd:
                case PaneMode.CategoryEdit:
                    panCategoryUnavailable.Visibility = Visibility.Visible;
                    panTagUnavailable.Visibility = Visibility.Collapsed;
                    panGalleryUnavailable.Visibility = Visibility.Collapsed;
                    lstImageMainViewerList.Visibility = Visibility.Visible;
                    lstUploadImageFileList.Visibility = Visibility.Collapsed;
                    panUpload.Visibility = System.Windows.Visibility.Collapsed;

                    gridLeft.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[4].Height = new GridLength(0);
                    gridLeft.RowDefinitions[6].Height = new GridLength(0);
                    gridLeft.RowDefinitions[8].Height = new GridLength(0);
                    break;
                case PaneMode.TagView:
                case PaneMode.TagAdd:
                case PaneMode.TagEdit:
                    panTagUnavailable.Visibility = Visibility.Visible;
                    panCategoryUnavailable.Visibility = Visibility.Collapsed;
                    panGalleryUnavailable.Visibility = Visibility.Collapsed;
                    lstImageMainViewerList.Visibility = Visibility.Visible;
                    lstUploadImageFileList.Visibility = Visibility.Collapsed;
                    panUpload.Visibility = System.Windows.Visibility.Collapsed;

                    gridLeft.RowDefinitions[2].Height = new GridLength(0);
                    gridLeft.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[6].Height = new GridLength(0);
                    gridLeft.RowDefinitions[8].Height = new GridLength(0);
                    break;
                case PaneMode.GalleryView:
                    panGalleryUnavailable.Visibility = Visibility.Visible;
                    panCategoryUnavailable.Visibility = Visibility.Collapsed;
                    panTagUnavailable.Visibility = Visibility.Collapsed;
                    lstImageMainViewerList.Visibility = Visibility.Visible;
                    lstUploadImageFileList.Visibility = Visibility.Collapsed;
                    panUpload.Visibility = System.Windows.Visibility.Collapsed;

                    gridLeft.RowDefinitions[2].Height = new GridLength(0);
                    gridLeft.RowDefinitions[4].Height = new GridLength(0);
                    gridLeft.RowDefinitions[6].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[8].Height = new GridLength(0);

                    gridRight.RowDefinitions[0].Height = new GridLength(40);
                    gridRight.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);

                    gridGallerySelection.Visibility = Visibility.Collapsed;
                    panGridRightHeader.Visibility = Visibility.Visible;
                    break;
                case PaneMode.GalleryEdit:
                case PaneMode.GalleryAdd:
                    gridRight.RowDefinitions[0].Height = new GridLength(2, GridUnitType.Star);
                    gridRight.RowDefinitions[1].Height = new GridLength(2, GridUnitType.Star);

                    gridGallerySelection.Visibility = Visibility.Visible;
                    panGridRightHeader.Visibility = Visibility.Collapsed;
                    break;
                case PaneMode.Upload:
                    panCategoryUnavailable.Visibility = Visibility.Collapsed;
                    panTagUnavailable.Visibility = Visibility.Collapsed;
                    panGalleryUnavailable.Visibility = Visibility.Collapsed;
                    lstImageMainViewerList.Visibility = Visibility.Collapsed;
                    lstUploadImageFileList.Visibility = Visibility.Visible;
                    panUpload.Visibility = System.Windows.Visibility.Visible;

                    gridRight.ColumnDefinitions[1].Width = new GridLength(250);
                    panSort.IsEnabled = false;
                    panFilter.IsEnabled = false;

                    gridLeft.RowDefinitions[2].Height = new GridLength(0);
                    gridLeft.RowDefinitions[4].Height = new GridLength(0);
                    gridLeft.RowDefinitions[6].Height = new GridLength(0);
                    gridLeft.RowDefinitions[8].Height = new GridLength(1, GridUnitType.Star);
                    break;
                case PaneMode.Account:
                    gridLeft.ColumnDefinitions[1].Width = new GridLength(0); //Main control
                    gridRight.RowDefinitions[0].Height = new GridLength(0); //Working Pane
                    gridLeft.ColumnDefinitions[2].Width = new GridLength(0); //Image display grid
                    gridLeft.ColumnDefinitions[3].Width = new GridLength(1, GridUnitType.Star); //Account grid

                    break;
            }
        }

        private void RefreshPanesAllControls(PaneMode mode)
        {

            switch (mode)
            {
                #region Category
                case PaneMode.CategoryView:
                    gridCategory.RowDefinitions[1].MaxHeight = 34;
                    gridCategory.RowDefinitions[2].MaxHeight = 0;
                    gridCategory.RowDefinitions[3].MaxHeight = 0;
                    gridCategory.RowDefinitions[4].MaxHeight = 0;
                    
                    treeCategoryView.IsEnabled = true;
                    cmdCategoryAdd.IsEnabled = true;
                    cmdCategoryEdit.IsEnabled = true;

                    radTag.IsEnabled = true;
                    radGallery.IsEnabled = true;
                    
                    //radUpload.IsEnabled = true;
                    cmdAccount.IsEnabled = true;
                    break;
                case PaneMode.CategoryAdd:
                case PaneMode.CategoryEdit:
                    gridCategory.RowDefinitions[1].MaxHeight = 0;
                    gridCategory.RowDefinitions[2].MaxHeight = 25;
                    gridCategory.RowDefinitions[3].MaxHeight = 75;
                    gridCategory.RowDefinitions[4].MaxHeight = 34;
                    
                    cmdCategoryCancel.Visibility = Visibility.Visible;
                    
                    if (mode == PaneMode.CategoryAdd)
                    {
                        cmdCategorySave.Content = "Save New";
                        cmdCategoryDelete.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        cmdCategorySave.Content = "Save Edit";
                        cmdCategoryDelete.Visibility = Visibility.Visible;
                    }

                    radTag.IsEnabled = false;
                    radGallery.IsEnabled = false;
                    radUpload.IsEnabled = false;
                    cmdAccount.IsEnabled = false;
                    break;
                #endregion

                #region Tag
                case PaneMode.TagView:
                    gridTag.RowDefinitions[1].MaxHeight = 34;
                    gridTag.RowDefinitions[2].MaxHeight = 0;
                    gridTag.RowDefinitions[3].MaxHeight = 0;
                    gridTag.RowDefinitions[4].MaxHeight = 0;

                    cmdTagAdd.Visibility = Visibility.Visible;
                    cmdTagEdit.Visibility = Visibility.Visible;
                    wrapMyTags.IsEnabled = true;

                    radCategory.IsEnabled = true;
                    radGallery.IsEnabled = true;
                    radUpload.IsEnabled = true;
                    cmdAccount.IsEnabled = true;

                    break;
                case PaneMode.TagAdd:
                case PaneMode.TagEdit:
                    gridTag.RowDefinitions[1].MaxHeight = 0;
                    gridTag.RowDefinitions[2].MaxHeight = 25;
                    gridTag.RowDefinitions[3].MaxHeight = 75;
                    gridTag.RowDefinitions[4].MaxHeight = 34;
                    wrapMyTags.IsEnabled = false;

                    radCategory.IsEnabled = false;
                    radGallery.IsEnabled = false;
                    radUpload.IsEnabled = false;
                    cmdAccount.IsEnabled = false;

                    if (mode == PaneMode.TagAdd)
                    {
                        this.cmdTagSave.Content = "Save New";
                        cmdTagDelete.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                    {
                        this.cmdTagSave.Content = "Save Edit";
                        cmdTagDelete.Visibility = System.Windows.Visibility.Visible;
                    }
                    break;
                #endregion

                #region Gallery
                case PaneMode.GalleryView:
                    /*
                <RowDefinition Height="*" /> <!-- wrap galleries -->
                <RowDefinition Height="34" /> <!-- add edit buttons -->
                <RowDefinition Height="25" /> <!-- Name -->
                <RowDefinition Height="75" /> <!-- Desc -->
                <RowDefinition Height="25" /><!-- Access Type -->
                <RowDefinition Height="25" /> <!-- Password -->
                <RowDefinition Height="25" /> <!-- Group Fotos -->
                <RowDefinition Height="34" /> <!-- Save buttons --> 
                    */

                    gridGallery.RowDefinitions[1].MaxHeight = 34;
                    gridGallery.RowDefinitions[2].MaxHeight = 0;
                    gridGallery.RowDefinitions[3].MaxHeight = 0;
                    gridGallery.RowDefinitions[4].MaxHeight = 0;
                    gridGallery.RowDefinitions[5].MaxHeight = 0;
                    gridGallery.RowDefinitions[6].MaxHeight = 0;
                    gridGallery.RowDefinitions[7].MaxHeight = 0;

                    radCategory.IsEnabled = true;
                    radTag.IsEnabled = true;
                    radUpload.IsEnabled = true;
                    cmdAccount.IsEnabled = true;
                    wrapMyGalleries.IsEnabled = true;

                    break;
                case PaneMode.GalleryEdit:
                case PaneMode.GalleryAdd:
                    gridGallery.RowDefinitions[1].MaxHeight = 0;
                    gridGallery.RowDefinitions[2].MaxHeight = 25;
                    gridGallery.RowDefinitions[3].MaxHeight = 75;
                    gridGallery.RowDefinitions[4].MaxHeight = 25;
                    gridGallery.RowDefinitions[5].MaxHeight = 25;
                    gridGallery.RowDefinitions[6].MaxHeight = 25;
                    gridGallery.RowDefinitions[7].MaxHeight = 34;

                    radCategory.IsEnabled = false;
                    radTag.IsEnabled = false;
                    radUpload.IsEnabled = false;
                    cmdAccount.IsEnabled = false;

                    wrapMyGalleries.IsEnabled = false;

                    if (cmbGalleryAccessType.SelectedIndex != 1)
                    {
                        txtGalleryPassword.Text = "";
                        txtGalleryPassword.IsEnabled = false;
                    }
                    else
                    {
                        txtGalleryPassword.IsEnabled = true;
                    }

                    if (mode == PaneMode.GalleryAdd)
                    {
                        this.cmdGallerySave.Content = "Save New";
                        cmdGalleryDelete.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                    {
                        this.cmdGallerySave.Content = "Save Edit";
                        cmdGalleryDelete.Visibility = System.Windows.Visibility.Visible;
                    }
                    break;
                #endregion

                #region Upload
                case PaneMode.Upload:
                    if (uploadUIState.Uploading == true)
                    {
                        lstUploadImageFileList.IsEnabled = false;
                        cmdUploadImportFiles.Visibility = Visibility.Hidden;
                        cmdUploadImportFolder.Visibility = Visibility.Hidden;
                        cmdUploadClear.IsEnabled = true;
                        cmdUploadClear.Content = "Cancel Uploads";
                        break;
                    }

                    if (uploadUIState.UploadToNewCategory)
                    {
                        grdUploadSettings.RowDefinitions[2].MaxHeight = 25; //Category Name
                        grdUploadSettings.RowDefinitions[3].MaxHeight = 80; //Category Description
                    }
                    else
                    {
                        grdUploadSettings.RowDefinitions[2].MaxHeight = 0;
                        grdUploadSettings.RowDefinitions[3].MaxHeight = 0;
                    }

                    //Initialise upload controls, no state to consider.
                    if (uploadUIState.Mode == UploadUIState.UploadMode.None)
                    {
                        //Tag Setup
                        //gridTag.RowDefinitions[1].MaxHeight = 34;
                        //gridTag.RowDefinitions[2].MaxHeight = 0;
                        //gridTag.RowDefinitions[3].MaxHeight = 0;
                        //gridTag.RowDefinitions[4].MaxHeight = 0;
                        //cmdAssociateTag.Visibility = Visibility.Visible;
                        //cmdTagAdd.Visibility = Visibility.Collapsed;
                        //cmdEditTag.Visibility = Visibility.Collapsed;
                        //wrapMyTags.IsEnabled = false;
                        //cmdAssociateTag.IsEnabled = false;
                        //RefreshAndDisplayTagList(false);

                        //Upload controls setup
                        lstUploadImageFileList.IsEnabled = false;
                        cmdUploadImportFolder.Visibility = Visibility.Visible;
                        cmdUploadImportFiles.Visibility = Visibility.Visible;
                        cmdUploadClear.Visibility = Visibility.Collapsed;
                        cmdUploadAll.IsEnabled = false;
                        panUpload.IsEnabled = false;
                    }
                    else
                    {
                        //Upload has been initialised, set controls to reflect upload options.
                        lstUploadImageFileList.IsEnabled = true;
                        panUpload.IsEnabled = true;
                        //grdUploadSettings.RowDefinitions[3].MaxHeight = 25;  //Upload to new category
                        //tabUploadImageDetails.IsEnabled = true;


                        cmdUploadAll.IsEnabled = true;
                        cmdUploadClear.Content = "Clear";
                        cmdUploadClear.IsEnabled = true;
                        cmdUploadClear.Visibility = Visibility.Visible;

                        //Enable Tags
                        wrapMyTags.IsEnabled = true;
                        //cmdAssociateTag.IsEnabled = true;

                        if (uploadUIState.Mode == UploadUIState.UploadMode.Images)
                        {
                            grdUploadImageDetails.RowDefinitions[0].MaxHeight = 0; //Sub category marker
                            //grdUploadSettings.RowDefinitions[2].MaxHeight = 0; //Map to sub folders
                            //chkUploadMapToSubFolders.IsEnabled = false;

                            cmdUploadImportFolder.Visibility = Visibility.Hidden;
                        }
                        else if (uploadUIState.Mode == UploadUIState.UploadMode.Folder)
                        {

                            if (uploadUIState.GotSubFolders)
                            {
                                //grdUploadImageDetails.RowDefinitions[0].MaxHeight = 25; //Sub category marker
                                //grdUploadSettings.RowDefinitions[2].MaxHeight = 25; //Maintain sub folders.
                                //chkUploadMapToSubFolders.IsEnabled = true;
                            }
                            else
                            {
                                //grdUploadImageDetails.RowDefinitions[0].MaxHeight = 0; //Sub category marker
                                //grdUploadSettings.RowDefinitions[2].MaxHeight = 0; //Map to sub folders
                                //chkUploadMapToSubFolders.IsEnabled = false;
                            }
                            cmdUploadImportFiles.Visibility = Visibility.Hidden;
                        }
                    }
                    
                    break;
                #endregion

                case PaneMode.Account:

                    break;
            }

            currentPane = mode;
        }

        private void ShowHideFilterSort()
        {
            if (cmdFilter.IsChecked == true)
            {
                gridRight.ColumnDefinitions[1].Width = new GridLength(250);
                panFilter.Visibility = System.Windows.Visibility.Visible;
                panSort.Visibility = System.Windows.Visibility.Collapsed;
            }
            else if (cmdSort.IsChecked == true)
            {
                gridRight.ColumnDefinitions[1].Width = new GridLength(250);
                panSort.Visibility = System.Windows.Visibility.Visible;
                panFilter.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                gridRight.ColumnDefinitions[1].Width = new GridLength(0);
            }
        }
        #endregion

        private void cmdFilter_Checked(object sender, RoutedEventArgs e)
        {
            cmdSort.IsChecked = false;
            ShowHideFilterSort();
        }

        private void cmdSort_Checked(object sender, RoutedEventArgs e)
        {
            cmdFilter.IsChecked = false;
            ShowHideFilterSort();
        }

        private void cmdSort_Unchecked(object sender, RoutedEventArgs e)
        {
            ShowHideFilterSort();
        }

        private void cmdFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            ShowHideFilterSort();
        }

        #region Category UI Control
        /// <summary>
        /// Method to load and refresh the category list, based on online status and whether or not a local cache contains a previous version
        /// </summary>
        /// <param name="forceRefresh"></param>
        async private void RefreshAndDisplayCategoryList(bool forceRefresh)
        {
            //Catch first time loads, user intiated refresh and when user was offline and is now online.  But only if logged on.
            if (state.connectionState != GlobalState.ConnectionState.NoAccount &&
                (state.categoryLoadState == GlobalState.DataLoadState.No || forceRefresh || state.categoryLoadState == GlobalState.DataLoadState.LocalCache))
            {
                //TODO show pending animation.
                panCategoryUnavailable.Visibility = System.Windows.Visibility.Visible;
                gridCategory.Visibility = Visibility.Collapsed;

                string response = await controller.CategoryRefreshListAsync();
                if (response != "OK")
                {
                    DisplayMessage(response, MessageSeverity.Error, false);
                }
            }

            switch (state.categoryLoadState)
            {
                case GlobalState.DataLoadState.Loaded:
                case GlobalState.DataLoadState.LocalCache:

                    CategoryListReloadFromState();
                    panCategoryUnavailable.Visibility = System.Windows.Visibility.Collapsed;
                    gridCategory.Visibility = Visibility.Visible;
                    break;
                case GlobalState.DataLoadState.Unavailable:
                    panCategoryUnavailable.Visibility = System.Windows.Visibility.Visible;
                    gridCategory.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        /// <summary>
        /// Update controls from populated state object.
        /// </summary>
        public void CategoryListReloadFromState()
        {
            long categoryId = 0;
            //Keep a reference to the currently selected tag list.
            TreeViewItem item = (TreeViewItem)treeCategoryView.SelectedItem;
            if (item != null)
            {
                CategoryListCategoryRef category = (CategoryListCategoryRef)item.Tag;
                categoryId = category.id;
            }
            else
            {
                CategoryListCategoryRef firstCategoryWithImages = state.categoryList.CategoryRef.First<CategoryListCategoryRef>(r => r.count > 0);
                categoryId = firstCategoryWithImages.id;
            }

            CategoryListCategoryRef baseCategory = state.categoryList.CategoryRef.Single<CategoryListCategoryRef>(r => r.parentId == 0);

            treeCategoryView.Items.Clear();
            CategoryAddTreeViewLevel(baseCategory.id, null);

            CategorySelect(categoryId, (TreeViewItem)treeCategoryView.Items[0], treeCategoryView);

            UploadRefreshCategoryList();
        }

        private void CategoryAddTreeViewLevel(long parentId, TreeViewItem currentHeader)
        {
            foreach (CategoryListCategoryRef current in state.categoryList.CategoryRef.Where(r => r.parentId == parentId))
            {
                TreeViewItem newItem = new TreeViewItem();
                int totalCount = current.count;
                CategoryGetImageCountRecursive(current.id, ref totalCount);
                newItem.Header = current.name + " (" + current.count.ToString() + ")";
                newItem.ToolTip = current.desc + " (" + totalCount.ToString() + ")";
                newItem.Tag = current;
                newItem.IsExpanded = true;
                //newItem.Style = (Style)FindResource("styleRadioButton");
                //newItem.Template = (ControlTemplate)FindResource("templateRadioButton");
                newItem.AllowDrop = true;
                newItem.Drop += new DragEventHandler(CategoryDroppedImages);
                newItem.Selected += new RoutedEventHandler(FetchCategoryImagesFirstAsync);

                if (currentHeader == null)
                {
                    treeCategoryView.Items.Add(newItem);
                }
                else
                {
                    currentHeader.Items.Add(newItem);
                }

                CategoryAddTreeViewLevel(current.id, newItem);
            }
        }

        private void CategoryGetImageCountRecursive(long parentId, ref int count)
        {
            foreach (CategoryListCategoryRef current in state.categoryList.CategoryRef.Where(r => r.parentId == parentId))
            {
                count = count + current.count;
                CategoryGetImageCountRecursive(current.id, ref count);
            }
        }

        private void CategorySelect(long categoryId, TreeViewItem currentHeader, TreeView treeViewToUpdate)
        {
            foreach (TreeViewItem item in currentHeader.Items)
            {
                CategoryListCategoryRef category = (CategoryListCategoryRef)item.Tag;
                if (category.id == categoryId)
                {
                    item.IsSelected = true;
                    treeViewToUpdate.Items.MoveCurrentTo(item);
                    return;
                }
                if (item.HasItems)
                {
                    CategorySelect(categoryId, item, treeViewToUpdate);
                }
            }
        }

        async private Task CategoryPopulateMetaData()
        {
            TreeViewItem item = (TreeViewItem)treeCategoryView.SelectedItem;
            if (item == null)
            {
                return;
            }

            Category category = await controller.CategoryGetMetaAsync((CategoryListCategoryRef)item.Tag);
            txtCategoryName.Text = category.Name;
            txtCategoryDescription.Text = category.Desc;
            currentCategory = category;
        }

        //TODO - ensure that this is called when a search is applied.
        async private void FetchCategoryImagesFirstAsync(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            TreeViewItem selectedTreeViewItem = (TreeViewItem)sender;

            /* Refresh tag image state */
            if (selectedTreeViewItem != null)
            {
                CategoryListCategoryRef categoryRef = (CategoryListCategoryRef)selectedTreeViewItem.Tag;
                currentImageList = await controller.CategoryGetImagesAsync(categoryRef.id, 0, GetSearchQueryString());

                /* Populate tag image list from state */
                ImageListUpdateControls();
            }
        }

        async private Task FetchMoreImagesAsync(FetchDirection direction)
        {
            if (currentImageList == null)
                return;

            /* Update current tag image list */
            int cursor = 0;
            switch (direction)
            {
                case FetchDirection.Begin:
                    cursor = 0;
                    break;
                case FetchDirection.Next:
                    if ((currentImageList.imageCursor + state.imageFetchSize) <= currentImageList.totalImageCount)
                        cursor = currentImageList.imageCursor + state.imageFetchSize;
                    break;
                case FetchDirection.Previous:
                    cursor = Math.Max(currentImageList.imageCursor - state.imageFetchSize, 0);
                    break;
                case FetchDirection.Last:
                    cursor = Math.Abs(currentImageList.totalImageCount / state.imageFetchSize) * state.imageFetchSize;
                    break;
            }

            switch (currentPane)
            {
                case PaneMode.CategoryView:
                case PaneMode.CategoryEdit:
                case PaneMode.CategoryAdd:
                    currentImageList = await controller.CategoryGetImagesAsync(currentImageList.id, cursor, GetSearchQueryString());
                    break;
                case PaneMode.TagAdd:
                case PaneMode.TagEdit:
                case PaneMode.TagView:
                    currentImageList = await controller.TagGetImagesAsync(currentImageList.id, currentImageList.Name, cursor, GetSearchQueryString());
                    break;
                case PaneMode.GalleryView:
                    currentImageList = await controller.GalleryGetImagesAsync(currentImageList.id, currentImageList.Name, cursor, GetSearchQueryString());
                    break;
            }
            

            /* Populate tag image list from state */
            ImageListUpdateControls();
        }

        async private Task ImageListUpdateControls()
        {
            imageMainViewerList.Clear();

            if (currentImageList == null)
                return;

            if (currentImageList.Images == null)
                return;

            foreach (ImageListImageRef imageRef in currentImageList.Images)
            {
                GeneralImage newImage = new GeneralImage(controller.GetServerHelper());
                newImage.imageId = imageRef.id;
                newImage.Name = imageRef.name;
                newImage.Description = imageRef.desc;
                newImage.UploadDate = imageRef.uploadDate;
                newImage.FilePath = imageRef.localPath;

                imageMainViewerList.Add(newImage);
            }

            if (currentImageList.imageCursor == 0)
            {
                cmdImageNavigationBegin.IsEnabled = false;
                cmdImageNavigationPrevious.IsEnabled = false;
            }
            else
            {
                cmdImageNavigationBegin.IsEnabled = true;
                cmdImageNavigationPrevious.IsEnabled = true;
            }

            if ((currentImageList.imageCursor + state.imageFetchSize) > currentImageList.totalImageCount)
            {
                cmdImageNavigationLast.IsEnabled = false;
                cmdImageNavigationNext.IsEnabled = false;
            }
            else
            {
                cmdImageNavigationLast.IsEnabled = true;
                cmdImageNavigationNext.IsEnabled = true;
            }

            foreach (GeneralImage newImage in imageMainViewerList)
            {
                await newImage.LoadImage();
            }
        }

        private void cmdCategoryCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.CategoryView);
        }

        private void txtCategoryName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Filter out non-digits, except spaces input
            foreach (char c in e.Text)
                if (!Char.IsLetterOrDigit(c) && !Char.IsWhiteSpace(c))
                {
                    e.Handled = true;
                    break;
                }
        }

        private void cmdCategoryAdd_Click(object sender, RoutedEventArgs e)
        {
            txtCategoryName.Text = "";
            txtCategoryDescription.Text = "";
            RefreshPanesAllControls(PaneMode.CategoryAdd);
        }

        async private void cmdCategoryEdit_Click(object sender, RoutedEventArgs e)
        {
            await CategoryPopulateMetaData();
            RefreshPanesAllControls(PaneMode.CategoryEdit);
        }

        async private void cmdCategorySave_Click(object sender, RoutedEventArgs e)
        {
            string response = null;

            CategoryListCategoryRef currentSelectedCategory = null;
            TreeViewItem item = (TreeViewItem)treeCategoryView.SelectedItem;
            if (item != null)
            {
                currentSelectedCategory = (CategoryListCategoryRef)item.Tag;
            }

            if (currentPane == PaneMode.CategoryAdd)
            {
                Category category = new Category();
                category.Name = txtCategoryName.Text;
                category.Desc = txtCategoryDescription.Text;
                if (currentSelectedCategory == null)
                {
                    category.parentId = 0;
                }
                else
                {
                    category.parentId = currentSelectedCategory.id;
                }
                response = await controller.CategoryCreateAsync(category);
            }
            else
            {
                currentCategory.Name = txtCategoryName.Text;
                currentCategory.Desc = txtCategoryDescription.Text;
                if (currentSelectedCategory != null && currentCategory.id != currentSelectedCategory.id)
                {
                    currentCategory.parentId = currentSelectedCategory.id;
                }

                response = await controller.CategoryUpdateAsync(currentCategory);
            }

            if (response != "OK")
            {
                DisplayMessage(response, MessageSeverity.Error, false);
                return;
            }

            RefreshPanesAllControls(PaneMode.CategoryView);
            RefreshAndDisplayCategoryList(true);
        }

        async private void cmdCategoryDelete_Click(object sender, RoutedEventArgs e)
        {
            string response = await controller.CategoryDeleteAsync(currentCategory);
            if (response != "OK")
            {
                DisplayMessage(response, MessageSeverity.Error, false);
                return;
            }

            RefreshPanesAllControls(PaneMode.CategoryView);
            RefreshAndDisplayCategoryList(true);
        }

        async private void CategoryDroppedImages(object sender, DragEventArgs e)
        {
            e.Handled = true;

            TreeViewItem meTreeviewNode = sender as TreeViewItem;
            CategoryListCategoryRef meToCategory = (CategoryListCategoryRef)meTreeviewNode.Tag;

            TreeViewItem meFromTreeviewNode = (TreeViewItem)treeCategoryView.SelectedItem;
            CategoryListCategoryRef meFromCategory = (CategoryListCategoryRef)meFromTreeviewNode.Tag;

            if (meFromCategory == null || meToCategory == null)
            {
                MessageBox.Show("Unexpected error, selected category could not be established.");
                return;
            }

            int count = lstImageMainViewerList.SelectedItems.Count;
            if (count > 0)
            {
                if (MessageBox.Show("Do you want to move the " + count.ToString() + " selected images to the category: " + meToCategory.name + "?", "ManageWalla", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ImageMoveList moveList = new ImageMoveList();
                    moveList.ImageRef = new long[count];
                    for (int i = 0; i < count; i++)
                    {
                        GeneralImage image = (GeneralImage)lstImageMainViewerList.Items[i];
                        if (image.categoryId == meToCategory.id)
                        {
                            MessageBox.Show("The update cannot be done, you have selected images which are already in the category: " + meToCategory.name);
                            return;
                        }
                        moveList.ImageRef[i] = image.imageId;
                    }

                    string response = await controller.CategoryMoveImagesAsync(meToCategory.id, moveList);
                    if (response != "OK")
                    {
                        DisplayMessage(response, MessageSeverity.Error, false);
                        return;
                    }

                    RefreshAndDisplayCategoryList(true);
                }
            }










        }
        #endregion

        #region Tag UI Control
        /// <summary>
        /// Method to load and refresh the tag list, based on online status and whether or not a local cache contains a previous version
        /// </summary>
        /// <param name="forceRefresh"></param>
        async private void RefreshAndDisplayTagList(bool forceRefresh)
        {
            //Catch first time loads, user intiated refresh and when user was offline and is now online.  But only if logged on.
            if (state.connectionState != GlobalState.ConnectionState.NoAccount &&
                (state.tagLoadState == GlobalState.DataLoadState.No || forceRefresh || state.tagLoadState == GlobalState.DataLoadState.LocalCache))
            {
                //TODO show pending animation.
                panTagUnavailable.Visibility = System.Windows.Visibility.Visible;
                gridTag.Visibility = Visibility.Collapsed;

                string response = await controller.TagRefreshListAsync();
                if (response != "OK")
                {
                    DisplayMessage(response, MessageSeverity.Error, false);
                }
            }

            switch (state.tagLoadState)
            {
                case GlobalState.DataLoadState.Loaded:
                case GlobalState.DataLoadState.LocalCache:

                    TagListReloadFromState();
                    panTagUnavailable.Visibility = System.Windows.Visibility.Collapsed;
                    gridTag.Visibility = Visibility.Visible;
                    break;
                case GlobalState.DataLoadState.Unavailable:
                    panTagUnavailable.Visibility = System.Windows.Visibility.Visible;
                    gridTag.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        /// <summary>
        /// Update controls from populated state object.
        /// </summary>
        public void TagListReloadFromState()
        {
            //Keep a reference to the currently selected tag list.
            long tagId = 0;
            RadioButton checkedButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedButton != null)
            {
                
                TagListTagRef current = (TagListTagRef)checkedButton.Tag;
                tagId = current.id;
            }

            wrapMyTags.Children.Clear();

            foreach (TagListTagRef tag in state.tagList.TagRef)
            {
                RadioButton newRadioButton = new RadioButton();

                newRadioButton.Content = tag.name + " (" + tag.count + ")";
                newRadioButton.Style = (Style)FindResource("styleRadioButton");
                newRadioButton.Template = (ControlTemplate)FindResource("templateRadioButton");
                newRadioButton.GroupName = "GroupTag";
                newRadioButton.Tag = tag;
                newRadioButton.AllowDrop = true;
                newRadioButton.Checked += new RoutedEventHandler(FetchTagImagesFirstAsync);
                newRadioButton.Drop += new DragEventHandler(TagDroppedImages);
                wrapMyTags.Children.Add(newRadioButton);
            }

            //Re-check the selected checkbox, else check the first
            RadioButton recheckButton = null;
            if (tagId == 0)
            {
                recheckButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().First();
            }
            else
            {
                foreach (RadioButton currentButton in wrapMyTags.Children.OfType<RadioButton>())
                {
                    TagListTagRef current = (TagListTagRef)currentButton.Tag;
                    if (current.id == tagId)
                    {
                        recheckButton = currentButton;
                        break;
                    }
                }
            }

            if (recheckButton != null)
                recheckButton.IsChecked = true;

            UploadRefreshTagsList();
        }

        //TODO - ensure that this is called when a search is applied.
        async private void FetchTagImagesFirstAsync(object sender, RoutedEventArgs e)
        {
            RadioButton checkedButton = (RadioButton)sender;
            //RadioButton checkedButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();

            /* Refresh tag image state */
            if (checkedButton != null)
            {
                TagListTagRef tagListTagRefTemp = (TagListTagRef)checkedButton.Tag;
                currentImageList = await controller.TagGetImagesAsync(tagListTagRefTemp.id, tagListTagRefTemp.name, 0, GetSearchQueryString());

                /* Populate tag image list from state */
                await ImageListUpdateControls();
            }
        }

        /*
        async private Task FetchMoreImagesAsync(FetchDirection direction)
        {
            if (currentImageList == null)
                return;

            //Update current tag image list
            int cursor = 0;
            switch (direction)
            {
                case FetchDirection.Begin:
                    cursor = 0;
                    break;
                case FetchDirection.Next:
                    if ((currentImageList.imageCursor + state.imageFetchSize) <= currentImageList.totalImageCount)
                        cursor = currentImageList.imageCursor + state.imageFetchSize;
                    break;
                case FetchDirection.Previous:
                    cursor = Math.Max(currentImageList.imageCursor - state.imageFetchSize, 0);
                    break;
                case FetchDirection.Last:
                    cursor = Math.Abs(currentImageList.totalImageCount / state.imageFetchSize) * state.imageFetchSize;
                    break;
            }

            currentImageList = await controller.TagGetImagesAsync(currentImageList.id, currentImageList.Name, cursor, GetSearchQueryString());

            ImageListUpdateControls();
        }
*/

        //TODO add functionality + server side.
        private string GetSearchQueryString()
        {
            return null;
        }

        /*
        async private Task TagImageListUpdateControls()
        {
            imageMainViewerList.Clear();

            if (currentImageList == null)
                return;

            if (currentImageList.Images == null)
                return;

            foreach (ImageListImageRef imageRef in currentImageList.Images)
            {
                GeneralImage newImage = new GeneralImage(controller.GetServerHelper());
                newImage.imageId = imageRef.id;
                newImage.Name = imageRef.name;
                newImage.Description = imageRef.desc;
                newImage.UploadDate = imageRef.uploadDate;
                newImage.FilePath = imageRef.localPath;

                imageMainViewerList.Add(newImage);
            }

            if (currentImageList.imageCursor == 0)
            {
                cmdImageNavigationBegin.IsEnabled = false;
                cmdImageNavigationPrevious.IsEnabled = false;
            }
            else
            {
                cmdImageNavigationBegin.IsEnabled = true;
                cmdImageNavigationPrevious.IsEnabled = true;
            }

            if ((currentImageList.imageCursor + state.imageFetchSize) > currentImageList.totalImageCount)
            {
                cmdImageNavigationLast.IsEnabled = false;
                cmdImageNavigationNext.IsEnabled = false;
            }
            else
            {
                cmdImageNavigationLast.IsEnabled = true;
                cmdImageNavigationNext.IsEnabled = true;
            }

            foreach (GeneralImage newImage in imageMainViewerList)
            {
                await newImage.LoadImage();
            }
        }
        */

        async private Task PopulateTagMetaData()
        {
            RadioButton checkedButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();

            if (checkedButton != null)
            {
                TagListTagRef tagListTagRefTemp = (TagListTagRef)checkedButton.Tag;
                Tag tag = await controller.TagGetMetaAsync((TagListTagRef)checkedButton.Tag);
                txtTagName.Text = tag.Name;
                txtTagDescription.Text = tag.Desc;
                currentTag = tag;
            }
        }

        async private void TagDroppedImages(object sender, DragEventArgs e)
        {
            RadioButton meTagButton = sender as RadioButton;
            TagListTagRef meTag = (TagListTagRef)meTagButton.Tag;

            int count = lstImageMainViewerList.SelectedItems.Count;
            if (count > 0)
            {
                if (MessageBox.Show("Do you want to add the " + count.ToString() + " selected images to the tag: " + meTag.name + "?", "ManageWalla", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ImageMoveList moveList = new ImageMoveList();
                    moveList.ImageRef = new long[count];
                    for (int i = 0; i < count; i++)
                    {
                        GeneralImage image = (GeneralImage)lstImageMainViewerList.Items[i];
                        moveList.ImageRef[i] = image.imageId;
                    }

                    string response = await controller.TagAddRemoveImagesAsync(meTag.name, moveList, true);
                    if (response != "OK")
                    {
                        DisplayMessage(response, MessageSeverity.Error, false);
                        return;
                    }

                    RefreshAndDisplayTagList(true);
                }
            }
        }

        private void cmdTagRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAndDisplayTagList(true);
        }

        private void txtTagName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Filter out non-digit text input
            foreach (char c in e.Text)
                if (!Char.IsLetterOrDigit(c))
                {
                    e.Handled = true;
                    break;
                }
        }

        async private void cmdTagDelete_Click(object sender, RoutedEventArgs e)
        {
            string response = await controller.TagDeleteAsync(currentTag);
            if (response != "OK")
            {
                DisplayMessage(response, MessageSeverity.Error, false);
                return;
            }

            RefreshPanesAllControls(PaneMode.TagView);
            RefreshAndDisplayTagList(true);
        }

        private void cmdTagAdd_Click(object sender, RoutedEventArgs e)
        {
            txtTagName.Text = "";
            txtTagDescription.Text = "";
            RefreshPanesAllControls(PaneMode.TagAdd);
        }

        async private void cmdTagEdit_Click(object sender, RoutedEventArgs e)
        {
            await PopulateTagMetaData();
            RefreshPanesAllControls(PaneMode.TagEdit);
        }

        private void cmdTagCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.TagView);
        }

        async private void cmdTagSave_Click(object sender, RoutedEventArgs e)
        {
            string response = null;

            //Check tag name is unique

            if (currentPane == PaneMode.TagAdd)
            {
                Tag tag = new Tag();
                tag.Name = txtTagName.Text;
                tag.Desc = txtTagDescription.Text;

                //Add Images selected

                response = await controller.TagCreateAsync(tag);
            }
            else
            {
                string oldTagName = currentTag.Name;
                currentTag.Name = txtTagName.Text;
                currentTag.Desc = txtTagDescription.Text;

                response = await controller.TagUpdateAsync(currentTag, oldTagName);
            }

            if (response != "OK")
            {
                DisplayMessage(response, MessageSeverity.Error, false);
                return;
            }

            RefreshPanesAllControls(PaneMode.TagView);
            RefreshAndDisplayTagList(true);
        }
        #endregion

        #region Window init
        private void mainTwo_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            controller.Dispose();
        }

        private void mainTwo_Loaded(object sender, RoutedEventArgs e)
        {
            controller = new MainController(this);

            string response = controller.InitApplication();
            state = controller.GetState();

            switch (state.connectionState)
            {
                case GlobalState.ConnectionState.LoggedOn:
                    DisplayMessage("Account: " + state.userName + " has been connected with FotoWalla", MessageSeverity.Info, false);
                    radCategory.IsChecked = true;
                    //cmdCategory.RaiseEvent(new RoutedEventArgs(CheckBox.CheckedEvent));
                    break;
                case GlobalState.ConnectionState.Offline:
                    DisplayMessage("No internet connection could be established with FotoWalla", MessageSeverity.Warning, false);
                    //cmdCategory.RaiseEvent(new RoutedEventArgs(CheckBox.CheckedEvent));
                    break;
                case GlobalState.ConnectionState.NoAccount:
                    DisplayMessage("There is no account settings saved for this user, you must associate an account", MessageSeverity.Info, false);

                    cmdAccount.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                    break;
                case GlobalState.ConnectionState.FailedLogin:
                    DisplayMessage("The logon for account: " + state.userName + ", failed with the message: " + response, MessageSeverity.Warning, false);
                    cmdAccount.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
            }

            DisplayConnectionStatus();
        }

        public void DisplayMessage(string message, MessageSeverity severity, bool modal)
        {
            //TODO sort out severity.
            if (modal)
            {
                MessageBox.Show(message);
            }
            else
            {
                MessageBox.Show(message);
            }
        }

        private void DisplayConnectionStatus()
        {
            if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
            {
                this.Title = "FotoWalla - Connected";
            }
            else
            {
                this.Title = "FotoWalla - Offline";
            }
        }
        #endregion

        #region Upload UI Control
        async private void cmdUploadImportFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
            if (folderDialog.SelectedPath.Length > 0)
            {
                uploadUIState.MapToSubFolders = false; ;
                DirectoryInfo folder = new DirectoryInfo(folderDialog.SelectedPath);
                if (folder.GetDirectories().Length > 0)
                {
                    uploadUIState.GotSubFolders = true;

                    if (MessageBox.Show("Do you want to add images from the sub folders too ?", "ManageWalla", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    {
                        await controller.LoadImagesFromFolder(folder, true, uploadFots);
                        uploadUIState.MapToSubFolders = true;
                    }
                    else
                    {
                        await controller.LoadImagesFromFolder(folder, false, uploadFots);
                    }
                }
                else
                {
                    await controller.LoadImagesFromFolder(folder, false, uploadFots);
                }

                uploadUIState.Mode = UploadUIState.UploadMode.Folder;
                RefreshPanesAllControls(PaneMode.Upload);
            }
        }

        async private void cmdUploadImportFiles_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.DefaultExt = @"*.JPG;*.BMP";  //TODO add additional formats.
            openDialog.Multiselect = true;
            System.Windows.Forms.DialogResult result = openDialog.ShowDialog();
            if (openDialog.FileNames.Length > 0)
            {
                await controller.LoadImagesFromArray(openDialog.FileNames, uploadFots);
                uploadUIState.GotSubFolders = false;
                uploadUIState.Mode = UploadUIState.UploadMode.Images;
                RefreshPanesAllControls(PaneMode.Upload);
            }
        }

        private void cmdUploadClear_Click(object sender, RoutedEventArgs e)
        {
            uploadFots.Clear();
            ResetUploadState(true);
            RefreshPanesAllControls(PaneMode.Upload);
        }

        private void ResetUploadState(bool fullReset)
        {
            if (fullReset)
            {
                uploadUIState.GotSubFolders = false;
                uploadUIState.CategoryName = "";
                uploadUIState.CategoryDesc = "";
                uploadUIState.MapToSubFolders = false;
                uploadUIState.UploadToNewCategory = false;

                uploadUIState.Mode = UploadUIState.UploadMode.None;
            }

            uploadUIState.MetaUdfChar1 = null;
            uploadUIState.MetaUdfChar2 = null;
            uploadUIState.MetaUdfChar3 = null;
            uploadUIState.MetaUdfText1 = null;
            uploadUIState.MetaUdfNum1 = 0;
            uploadUIState.MetaUdfNum2 = 0;
            uploadUIState.MetaUdfNum3 = 0;
            uploadUIState.MetaUdfDate1 = new DateTime(1900, 01, 01);
            uploadUIState.MetaUdfDate2 = new DateTime(1900, 01, 01);
            uploadUIState.MetaUdfDate3 = new DateTime(1900, 01, 01);

            uploadUIState.MetaUdfChar1All = false;
            uploadUIState.MetaUdfChar2All = false;
            uploadUIState.MetaUdfChar3All = false;
            uploadUIState.MetaUdfText1All = false;
            uploadUIState.MetaUdfNum1All = false;
            uploadUIState.MetaUdfNum2All = false;
            uploadUIState.MetaUdfNum3All = false;
            uploadUIState.MetaUdfDate1All = false;
            uploadUIState.MetaUdfDate2All = false;
            uploadUIState.MetaUdfDate3All = false;
            uploadUIState.MetaTagRefAll = false;

            //TODO clear down each images meta data changes.
        }

        async private void cmdUploadResetMeta_Click(object sender, RoutedEventArgs e)
        {
            ResetUploadState(false);
            await controller.ResetMeFotsMeta(uploadFots);
            RefreshPanesAllControls(PaneMode.Upload);
        }

        private void UploadRefreshCategoryList()
        {
            
            long categoryId = 0;
            //Keep a reference to the currently selected category item.
            TreeViewItem item = (TreeViewItem)treeUploadCategoryView.SelectedItem;
            if (item != null)
            {
                CategoryListCategoryRef category = (CategoryListCategoryRef)item.Tag;
                categoryId = category.id;
            }
            else
            {
                CategoryListCategoryRef firstCategoryWithImages = state.categoryList.CategoryRef.First<CategoryListCategoryRef>(r => r.parentId != 0);
                categoryId = firstCategoryWithImages.id;
            }

            treeUploadCategoryView.Items.Clear();
            UploadAddCategoryToTreeView(0, null);

            CategorySelect(categoryId, (TreeViewItem)treeUploadCategoryView.Items[0], treeUploadCategoryView);
        }

        private void UploadAddCategoryToTreeView(long parentId, TreeViewItem currentHeader)
        {
            foreach (CategoryListCategoryRef current in state.categoryList.CategoryRef.Where(r => r.parentId == parentId))
            {
                TreeViewItem newItem = new TreeViewItem();
                newItem.Header = current.name;
                newItem.ToolTip = current.desc + " (" + current.count.ToString() + ")";
                newItem.Tag = current;
                newItem.IsExpanded = true;
                //newItem.Style = (Style)FindResource("styleRadioButton");
                //newItem.Template = (ControlTemplate)FindResource("templateRadioButton");

                if (currentHeader == null)
                {
                    treeUploadCategoryView.Items.Add(newItem);
                }
                else
                {
                    currentHeader.Items.Add(newItem);
                }

                UploadAddCategoryToTreeView(current.id, newItem);
            }
        }

        private void UploadRefreshTagsList()
        {
            lstUploadTagList.Items.Clear();
            //Load existing tags into the tag
            foreach (TagListTagRef tagRef in state.tagList.TagRef)
            {
                ListBoxItem newItem = new ListBoxItem();
                newItem.Content = tagRef.name;
                newItem.Tag = tagRef;
                lstUploadTagList.Items.Add(newItem);
            }
        }
        
        private void lstUploadImageFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UploadTagListReload();
        }

        private void UploadTagListReload()
        {
            tagListUploadRefreshing = true;

            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            if (current == null)
                return;

            //Deselect each tag.
            foreach (ListBoxItem tagItem in lstUploadTagList.Items)
            {
                tagItem.IsSelected = false;
            }

            if (uploadUIState.MetaTagRefAll)
            {
                if (uploadUIState.MetaTagRef != null)
                {
                    foreach (ImageMetaTagRef tagRef in uploadUIState.MetaTagRef)
                    {
                        foreach (ListBoxItem tagItem in lstUploadTagList.Items)
                        {
                            TagListTagRef currentTagRef = (TagListTagRef)tagItem.Tag;
                            if (currentTagRef.id == tagRef.id)
                            {
                                tagItem.IsSelected = true;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                if (current.Meta.Tags != null)
                {
                    foreach (ImageMetaTagRef tagRef in current.Meta.Tags)
                    {
                        foreach (ListBoxItem tagItem in lstUploadTagList.Items)
                        {
                            TagListTagRef currentTagRef = (TagListTagRef)tagItem.Tag;
                            if (currentTagRef.id == tagRef.id)
                            {
                                tagItem.IsSelected = true;
                                break;
                            }
                        }
                    }
                }
            }

            tagListUploadRefreshing = false;
        }

        private void lstUploadTagList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
               UpdateUploadTagCollection();
        }

        private void UpdateUploadTagCollection()
        {
            if (tagListUploadRefreshing)
                return;

            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            if (current == null)
                return;

            //Add all selected items to the tagref collection
            if (uploadUIState.MetaTagRefAll)
            {
                //Re-initialise collection
                uploadUIState.MetaTagRef = new ImageMetaTagRef[lstUploadTagList.SelectedItems.Count];
                for (int i = 0; i < lstUploadTagList.SelectedItems.Count; i++)
                {
                    ListBoxItem item = (ListBoxItem)lstUploadTagList.SelectedItems[i];
                    TagListTagRef tagListTagRef = (TagListTagRef)item.Tag;

                    ImageMetaTagRef newTagRef = new ImageMetaTagRef();
                    newTagRef.id = tagListTagRef.id;
                    newTagRef.op = "C";
                    newTagRef.name = tagListTagRef.name;
                    uploadUIState.MetaTagRef[i] = newTagRef;
                }
            }
            else
            {
                //Re-initialise collection
                current.Meta.Tags = new ImageMetaTagRef[lstUploadTagList.SelectedItems.Count];
                for (int i = 0; i < lstUploadTagList.SelectedItems.Count; i++)
                {
                    ListBoxItem item = (ListBoxItem)lstUploadTagList.SelectedItems[i];
                    TagListTagRef tagListTagRef = (TagListTagRef)item.Tag;

                    ImageMetaTagRef newTagRef = new ImageMetaTagRef();
                    newTagRef.id = tagListTagRef.id;
                    newTagRef.op = "C";
                    newTagRef.name = tagListTagRef.name;
                    current.Meta.Tags[i] = newTagRef;
                }
            }
        }

/*
        private void cmdAssociateTag_Click(object sender, RoutedEventArgs e)
        {
            if (currentPane == PaneMode.Upload)
            {
                RadioButton checkedTagButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();

                if (checkedTagButton != null)
                {
                    TagListTagRef tagListTagRefTemp = (TagListTagRef)checkedTagButton.Tag;

                    ImageMetaTagRef newTagRef = new ImageMetaTagRef();
                    newTagRef.id = tagListTagRefTemp.id;
                    newTagRef.op = "C";
                    newTagRef.name = tagListTagRefTemp.name;

                    UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
                    ImageMetaTagRef[] newTagRefArray;

                    if (chkUploadTagsAll.IsChecked == true)
                    {
                        if (uploadUIState.MetaTagRef == null)
                        {
                            newTagRefArray = new ImageMetaTagRef[1] { newTagRef };
                        }
                        else
                        {
                            newTagRefArray = new ImageMetaTagRef[uploadUIState.MetaTagRef.Length + 1];
                            uploadUIState.MetaTagRef.CopyTo(newTagRefArray, 0);
                            newTagRefArray[newTagRefArray.Length - 1] = newTagRef;
                        }
                        uploadUIState.MetaTagRef = newTagRefArray;
                    }
                    else
                    {
                        if (current.Meta.Tags == null)
                        {
                            newTagRefArray = new ImageMetaTagRef[1] { newTagRef };
                        }
                        else
                        {
                            newTagRefArray = new ImageMetaTagRef[current.Meta.Tags.Length + 1];
                            current.Meta.Tags.CopyTo(newTagRefArray, 0);
                            newTagRefArray[newTagRefArray.Length - 1] = newTagRef;
                        }
                        current.Meta.Tags = newTagRefArray;
                    }


                    UploadEnableDisableTags(current);
                    checkedTagButton.IsChecked = false;
                    BindingOperations.GetBindingExpressionBase(lstUploadTagList, ListBox.ItemsSourceProperty).UpdateTarget();
                }
            }
        }

        private void UploadEnableDisableTags(UploadImage current)
        {
            //Either check the gloabl tags collection or the image specific collection.
            ImageMetaTagRef[] tagToCheck = null;
            try
            {
                if (chkUploadTagsAll.IsChecked == true)
                {
                    tagToCheck = uploadUIState.MetaTagRef;
                }
                else
                {
                    tagToCheck = current.Meta.Tags;
                }
            }
            catch (NullReferenceException ex)
            {
                //Can be ignored, just leave the method
            }

            //Loop to enable buttons if they are not currently in the selected list.
            foreach (RadioButton button in wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsEnabled == false))
            {
                bool exists = false;
                if (tagToCheck != null)
                {
                    foreach (ImageMetaTagRef tagRef in tagToCheck)
                    {
                        TagListTagRef existingTagRef = (TagListTagRef)button.Tag;
                        if (tagRef.id == existingTagRef.id)
                        {
                            exists = true;
                        }
                    }
                }

                if (!exists)
                {
                    button.IsEnabled = true;
                }
                exists = false;
            }

            //Update tags, so ones in use are disabled.
            if (current != null)
            {
                if (tagToCheck != null)
                {
                    foreach (ImageMetaTagRef tagRef in tagToCheck)
                    {
                        foreach (RadioButton button in wrapMyTags.Children)
                        {
                            TagListTagRef existingTagRef = (TagListTagRef)button.Tag;
                            if (tagRef.id == existingTagRef.id)
                            {
                                button.IsEnabled = false;
                            }
                        }
                    }
                }
            }
        }

        private void cmdUploadRemoveTag_Click(object sender, RoutedEventArgs e)
        {
            ImageMetaTagRef currentTag = (ImageMetaTagRef)lstUploadTagList.SelectedItem;
            if (currentTag != null)
            {
                UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;

                if (chkUploadTagsAll.IsChecked == true)
                {
                    ImageMetaTagRef[] newTagListTagRef = uploadUIState.MetaTagRef.Where(r => r.id != currentTag.id).ToArray();
                    uploadUIState.MetaTagRef = newTagListTagRef;
                }
                else
                {
                    current = (UploadImage)lstUploadImageFileList.SelectedItem;
                    ImageMetaTagRef[] newTagListTagRef = current.Meta.Tags.Where(r => r.id != currentTag.id).ToArray();
                    current.Meta.Tags = newTagListTagRef;
                }

                BindingOperations.GetBindingExpressionBase(lstUploadTagList, ListBox.ItemsSourceProperty).UpdateTarget();
                UploadEnableDisableTags(current);
                lstUploadTagList.Items.Refresh();
            }
        }
        */

        private void chkUploadToNewCategory_Checked(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(currentPane);
        }

        private void chkUploadToNewCategory_Unchecked(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(currentPane);
        }

        async private void cmdUploadAll_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)treeUploadCategoryView.SelectedItem;
            if (item == null)
            {
                MessageBox.Show("You must select a Category for your uploaded images to be stored in.");
                return;
            }

            if (uploadUIState.UploadToNewCategory && uploadUIState.CategoryName.Length < 1)
            {
                MessageBox.Show("You have selected to add a new category, you must enter a name to continue.");
                return;
            }

            CategoryListCategoryRef category = (CategoryListCategoryRef)item.Tag;
            long categoryId = category.id;

            uploadUIState.Uploading = true;
            RefreshPanesAllControls(PaneMode.Upload);

            string response = await controller.DoUploadAsync(uploadFots, uploadUIState, categoryId);
            if (response != null)
            {
                MessageBox.Show("The upload encountered an error.  Message: " + response);
            }

            //TODO - Remove Images which were successfully uploaded.
            uploadUIState.Uploading = false;

            if (lstUploadImageFileList.Items.Count > 0)
            {
                lstUploadImageFileList.IsEnabled = true;
            }
            else
            {
                ResetUploadState(true);
            }

            RefreshPanesAllControls(PaneMode.Upload);
        }
        #endregion

        #region Upload Binding Remapping
        private void chkUploadTagsAll_Checked(object sender, RoutedEventArgs e)
        {
            UpdateUploadTagCollection();
            UploadTagListReload();
            //UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            //uploadUIState.MetaTagRef = current.Meta.Tags;

            //BindingOperations.ClearBinding(lstUploadTagList, ListBox.ItemsSourceProperty);
            //Binding binding = new Binding("MetaTagRef");
            //binding.Mode = BindingMode.TwoWay;
            //binding.Source = uploadUIState;
            //BindingOperations.SetBinding(lstUploadTagList, ListBox.ItemsSourceProperty, binding);
        }

        private void chkUploadTagsAll_Unchecked(object sender, RoutedEventArgs e)
        {
            //BindingOperations.ClearBinding(lstUploadTagList, ListBox.ItemsSourceProperty);
            //Binding binding = new Binding("/Meta.Tags");
            //binding.Mode = BindingMode.TwoWay;
            //BindingOperations.SetBinding(lstUploadTagList, ListBox.ItemsSourceProperty, binding);
        }

        private void chkUploadUdfChar1All_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaUdfChar1 = current.Meta.UdfChar1;

            BindingOperations.ClearBinding(txtUploadUdfChar1, TextBox.TextProperty);
            Binding binding = new Binding("MetaUdfChar1");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(txtUploadUdfChar1, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfChar1All_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(txtUploadUdfChar1, TextBox.TextProperty);
            Binding binding = new Binding("/Meta.UdfChar1");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(txtUploadUdfChar1, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfChar2All_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaUdfChar2 = current.Meta.UdfChar2;

            BindingOperations.ClearBinding(txtUploadUdfChar2, TextBox.TextProperty);
            Binding binding = new Binding("MetaUdfChar2");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(txtUploadUdfChar2, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfChar2All_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(txtUploadUdfChar2, TextBox.TextProperty);
            Binding binding = new Binding("/Meta.UdfChar2");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(txtUploadUdfChar2, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfChar3All_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaUdfChar3 = current.Meta.UdfChar3;

            BindingOperations.ClearBinding(txtUploadUdfChar3, TextBox.TextProperty);
            Binding binding = new Binding("MetaUdfChar3");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(txtUploadUdfChar3, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfChar3All_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(txtUploadUdfChar1, TextBox.TextProperty);
            Binding binding = new Binding("/Meta.UdfChar3");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(txtUploadUdfChar3, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfText1All_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaUdfText1 = current.Meta.UdfText1;

            BindingOperations.ClearBinding(txtUploadUdfText1, TextBox.TextProperty);
            Binding binding = new Binding("MetaUdfText1");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(txtUploadUdfText1, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfText1All_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(txtUploadUdfChar1, TextBox.TextProperty);
            Binding binding = new Binding("/Meta.UdfText1");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(txtUploadUdfText1, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfNum1All_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaUdfNum1 = current.Meta.UdfNum1;

            BindingOperations.ClearBinding(txtUploadUdfNum1, TextBox.TextProperty);
            Binding binding = new Binding("MetaUdfNum1");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(txtUploadUdfNum1, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfNum1All_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(txtUploadUdfNum1, TextBox.TextProperty);
            Binding binding = new Binding("/Meta.UdfNum1");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(txtUploadUdfNum1, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfNum2All_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaUdfNum2 = current.Meta.UdfNum2;

            BindingOperations.ClearBinding(txtUploadUdfNum2, TextBox.TextProperty);
            Binding binding = new Binding("MetaUdfNum2");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(txtUploadUdfNum2, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfNum2All_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(txtUploadUdfNum2, TextBox.TextProperty);
            Binding binding = new Binding("/Meta.UdfNum2");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(txtUploadUdfNum2, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfNum3All_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaUdfNum3 = current.Meta.UdfNum3;

            BindingOperations.ClearBinding(txtUploadUdfNum3, TextBox.TextProperty);
            Binding binding = new Binding("MetaUdfNum3");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(txtUploadUdfNum3, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfNum3All_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(txtUploadUdfNum1, TextBox.TextProperty);
            Binding binding = new Binding("/Meta.UdfNum3");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(txtUploadUdfNum3, TextBox.TextProperty, binding);
        }

        private void chkUploadUdfDate1All_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaUdfDate1 = current.Meta.UdfDate1;

            BindingOperations.ClearBinding(datUploadUdfDate1, DatePicker.SelectedDateProperty);
            Binding binding = new Binding("MetaUdfDate1");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(datUploadUdfDate1, DatePicker.SelectedDateProperty, binding);
        }

        private void chkUploadUdfDate1All_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(datUploadUdfDate1, DatePicker.SelectedDateProperty);
            Binding binding = new Binding("/Meta.UdfDate1");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(datUploadUdfDate1, DatePicker.SelectedDateProperty, binding);
        }

        private void chkUploadUdfDate2All_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaUdfDate2 = current.Meta.UdfDate2;

            BindingOperations.ClearBinding(datUploadUdfDate2, DatePicker.SelectedDateProperty);
            Binding binding = new Binding("MetaUdfDate2");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(datUploadUdfDate2, DatePicker.SelectedDateProperty, binding);
        }

        private void chkUploadUdfDate2All_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(datUploadUdfDate2, DatePicker.SelectedDateProperty);
            Binding binding = new Binding("/Meta.UdfDate2");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(datUploadUdfDate2, DatePicker.SelectedDateProperty, binding);
        }

        private void chkUploadUdfDate3All_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaUdfDate3 = current.Meta.UdfDate3;

            BindingOperations.ClearBinding(datUploadUdfDate3, DatePicker.SelectedDateProperty);
            Binding binding = new Binding("MetaUdfDate3");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(datUploadUdfDate3, DatePicker.SelectedDateProperty, binding);
        }

        private void chkUploadUdfDate3All_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(datUploadUdfDate3, DatePicker.SelectedDateProperty);
            Binding binding = new Binding("/Meta.UdfDate3");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(datUploadUdfDate3, DatePicker.SelectedDateProperty, binding);
        }
        #endregion

        #region Account
        async private void cmdUploadStatusRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshUploadStatusStateAsync(true);
        }

        private void RefreshUploadStatusFromStateList()
        {
            /* Clear list and add local image load errors */
            uploadStatusListBind.Clear();

            foreach (UploadImage currentUploadImage in uploadFots.Where(r => r.State == UploadImage.UploadState.Error))
            {
                UploadStatusListImageUploadRef newImageRef = new UploadStatusListImageUploadRef();
                newImageRef.imageStatus = -1;
                newImageRef.name = currentUploadImage.Meta.Name;
                newImageRef.lastUpdated = DateTime.Now;
                newImageRef.errorMessage = currentUploadImage.UploadError;

                uploadStatusListBind.Add(newImageRef);
            }

            /* Load in existing upload entries */
            if (state.uploadStatusList != null)
            {
                foreach (UploadStatusListImageUploadRef currentImageUploadRef in state.uploadStatusList.ImageUploadRef)
                {
                    uploadStatusListBind.Add(currentImageUploadRef);
                }
            }

            /* Refresh message and icon */
            datUploadStatusList.GetBindingExpression(DataGrid.ItemsSourceProperty).UpdateTarget();

            //TODO update message and icon
        }

        async private Task RefreshUploadStatusStateAsync(bool forceUpdate)
        {
            if (state.connectionState != GlobalState.ConnectionState.NoAccount &&
                (state.uploadStatusListState == GlobalState.DataLoadState.No || forceUpdate || state.tagLoadState == GlobalState.DataLoadState.LocalCache))
            {
                string response = await controller.RefreshUploadStatusListAsync();
                if (response != "OK")
                {
                    DisplayMessage(response, MessageSeverity.Error, false);
                }
            }

            switch (state.uploadStatusListState)
            {
                case GlobalState.DataLoadState.Loaded:
                case GlobalState.DataLoadState.LocalCache:
                    RefreshUploadStatusFromStateList();
                    panUploadStatusListUnavailable.Visibility = System.Windows.Visibility.Collapsed;
                    datUploadStatusList.Visibility = Visibility.Visible;
                    break;
                case GlobalState.DataLoadState.Unavailable:
                    panUploadStatusListUnavailable.Visibility = System.Windows.Visibility.Visible;
                    datUploadStatusList.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        async private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabAccount.SelectedIndex == 1)
            {
                await RefreshUploadStatusStateAsync(false);
            }
        }
        #endregion

        async private void cmdImageNavigationLast_Click(object sender, RoutedEventArgs e)
        {
            await FetchMoreImagesAsync(FetchDirection.Last);
        }

        async private void cmdImageNavigationNext_Click(object sender, RoutedEventArgs e)
        {
            await FetchMoreImagesAsync(FetchDirection.Next);
        }

        async private void cmdImageNavigationPrevious_Click(object sender, RoutedEventArgs e)
        {
            await FetchMoreImagesAsync(FetchDirection.Previous);
        }

        async private void cmdImageNavigationBegin_Click(object sender, RoutedEventArgs e)
        {
            await FetchMoreImagesAsync(FetchDirection.Begin);
        }

        private void cmdTagAddRemoveImages_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            Image meImage = sender as Image;
            if (meImage != null && e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(meImage, "dunno", DragDropEffects.Copy);
            }
        }

        private void cmdCategoryRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAndDisplayCategoryList(true);
        }


        #region Gallery
        async private void RefreshAndDisplayGalleryList(bool forceRefresh)
        {
            //Catch first time loads, user intiated refresh and when user was offline and is now online.  But only if logged on.
            if (state.connectionState != GlobalState.ConnectionState.NoAccount &&
                (state.galleryLoadState == GlobalState.DataLoadState.No || forceRefresh || state.galleryLoadState == GlobalState.DataLoadState.LocalCache))
            {
                //TODO show pending animation.
                panGalleryUnavailable.Visibility = System.Windows.Visibility.Visible;
                gridGallery.Visibility = Visibility.Collapsed;

                string response = await controller.GalleryRefreshListAsync();
                if (response != "OK")
                {
                    DisplayMessage(response, MessageSeverity.Error, false);
                }
            }

            switch (state.galleryLoadState)
            {
                case GlobalState.DataLoadState.Loaded:
                case GlobalState.DataLoadState.LocalCache:
                    GalleryListReloadFromState();
                    panGalleryUnavailable.Visibility = System.Windows.Visibility.Collapsed;
                    gridGallery.Visibility = Visibility.Visible;
                    break;
                case GlobalState.DataLoadState.Unavailable:
                    panGalleryUnavailable.Visibility = System.Windows.Visibility.Visible;
                    gridGallery.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        public void GalleryListReloadFromState()
        {
            //Keep a reference to the currently selected gallery item.
            long galleryId = 0;
            RadioButton checkedButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedButton != null)
            {

                GalleryListGalleryRef current = (GalleryListGalleryRef)checkedButton.Tag;
                galleryId = current.id;
            }

            wrapMyGalleries.Children.Clear();

            foreach (GalleryListGalleryRef gallery in state.galleryList.GalleryRef)
            {
                RadioButton newRadioButton = new RadioButton();

                newRadioButton.Content = gallery.name + " (" + gallery.count + ")";
                newRadioButton.Style = (Style)FindResource("styleRadioButton");
                newRadioButton.Template = (ControlTemplate)FindResource("templateRadioButton");
                newRadioButton.GroupName = "GroupGallery";
                newRadioButton.Tag = gallery;
                newRadioButton.Checked += new RoutedEventHandler(FetchGalleryImagesFirstAsync);
                wrapMyGalleries.Children.Add(newRadioButton);
            }

            //Re-check the selected checkbox, else check the first
            RadioButton recheckButton = null;
            if (galleryId == 0)
            {
                recheckButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().First();
            }
            else
            {
                foreach (RadioButton currentButton in wrapMyGalleries.Children.OfType<RadioButton>())
                {
                    GalleryListGalleryRef current = (GalleryListGalleryRef)currentButton.Tag;
                    if (current.id == galleryId)
                    {
                        recheckButton = currentButton;
                        break;
                    }
                }
            }

            if (recheckButton != null)
                recheckButton.IsChecked = true;
        }

        async private void FetchGalleryImagesFirstAsync(object sender, RoutedEventArgs e)
        {
            RadioButton checkedButton = (RadioButton)sender;

            /* Refresh tag image state */
            if (checkedButton != null)
            {
                GalleryListGalleryRef galleryListRefTemp = (GalleryListGalleryRef)checkedButton.Tag;
                currentImageList = await controller.GalleryGetImagesAsync(galleryListRefTemp.id, galleryListRefTemp.name, 0, GetSearchQueryString());

                /* Populate tag image list from state */
                await ImageListUpdateControls();
            }

        }

        async private Task<bool> PopulateGalleryMetaData()
        {
            RadioButton checkedButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();

            if (checkedButton != null)
            {
                GalleryListGalleryRef galleryListGalleryRef = (GalleryListGalleryRef)checkedButton.Tag;
                Gallery gallery = await controller.GalleryGetMetaAsync(galleryListGalleryRef);
                txtGalleryName.Text = gallery.Name;
                txtGalleryDescription.Text = gallery.Desc;
                txtGalleryPassword.Text = gallery.Password;
                cmbGalleryAccessType.SelectedIndex = gallery.AccessType;
                cmbGalleryGroupingType.SelectedIndex = gallery.GroupingType;

                //TODO select radio buttons for tags.
                //lstGalleryTagListInclude.SelectedItems
                foreach (GalleryTagRef tagRef in gallery.Tags)
                {
                    if (tagRef.exclude)
                    {
                        foreach (ListBoxItem current in lstGalleryTagListExclude.Items)
                        {
                            TagListTagRef tagRefInList = (TagListTagRef)current.Tag;
                            if (tagRefInList.id == tagRef.tagId)
                            {
                                current.IsSelected = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (ListBoxItem current in lstGalleryTagListInclude.Items)
                        {
                            TagListTagRef tagRefInList = (TagListTagRef)current.Tag;
                            if (tagRefInList.id == tagRef.tagId)
                            {
                                current.IsSelected = true;
                                break;
                            }
                        }
                    }
                }

                currentGallery = gallery;

                return true;
            }
            return false;
        }

        private void GalleryRefreshTagsList()
        {

            lstGalleryTagListExclude.Items.Clear();
            lstGalleryTagListInclude.Items.Clear();
            //Load existing tags into the tag
            foreach (TagListTagRef tagRef in state.tagList.TagRef)
            {
                ListBoxItem newItemInclude = new ListBoxItem();
                newItemInclude.Content = tagRef.name;
                newItemInclude.Tag = tagRef;
                newItemInclude.Selected += new RoutedEventHandler(GalleryCheckForIncludeConflict);

                ListBoxItem newItemExclude = new ListBoxItem();
                newItemExclude.Content = tagRef.name;
                newItemExclude.Tag = tagRef;
                newItemExclude.Selected += new RoutedEventHandler(GalleryCheckForExcludeConflict);

                lstGalleryTagListExclude.Items.Add(newItemExclude);
                lstGalleryTagListInclude.Items.Add(newItemInclude);
            }

            if (currentPane == PaneMode.GalleryEdit)
            {
                //TODO Update tag list from XML.
            }
        }

        private void GalleryCheckForIncludeConflict(object sender, RoutedEventArgs e)
        {
            //e.Handled = true;

            ListBoxItem listBoxItem = (ListBoxItem)sender;
            TagListTagRef tagRef = (TagListTagRef)listBoxItem.Tag;

            foreach (ListBoxItem current in lstGalleryTagListExclude.Items)
            {
                TagListTagRef tagRefInList = (TagListTagRef)current.Tag;
                if (tagRefInList.id == tagRef.id)
                {
                    current.IsSelected = false;
                    break;
                }
            }
        }

        private void GalleryCheckForExcludeConflict(object sender, RoutedEventArgs e)
        {
            //e.Handled = true;

            ListBoxItem listBoxItem = (ListBoxItem)sender;
            TagListTagRef tagRef = (TagListTagRef)listBoxItem.Tag;

            foreach (ListBoxItem current in lstGalleryTagListInclude.Items)
            {
                TagListTagRef tagRefInList = (TagListTagRef)current.Tag;
                if (tagRefInList.id == tagRef.id)
                {
                    current.IsSelected = false;
                    break;
                }
            }
        }

        private void GalleryRefreshCategoryList()
        {
            CategoryListCategoryRef baseCategory = state.categoryList.CategoryRef.Single<CategoryListCategoryRef>(r => r.parentId == 0);

            treeGalleryCategoryView.Items.Clear();
            GalleryCategoryAddTreeViewLevel(baseCategory.id, null);

            if (currentPane == PaneMode.GalleryEdit)
            {
                //TODO Update tag list from XML.
            }
        }

        private void GalleryCategoryAddTreeViewLevel(long parentId, TreeViewItem currentHeader)
        {
            foreach (CategoryListCategoryRef current in state.categoryList.CategoryRef.Where(r => r.parentId == parentId))
            {
                TreeViewItem newItem = new TreeViewItem();
                newItem.Header = current.name;
                newItem.ToolTip = current.desc;
                newItem.Tag = current;
                newItem.IsExpanded = true;
                //newItem.Style = (Style)FindResource("styleRadioButton");
                //newItem.Template = (ControlTemplate)FindResource("templateRadioButton");
                //newItem.Selected += new RoutedEventHandler(FetchCategoryImagesFirstAsync);

                if (currentHeader == null)
                {
                    treeGalleryCategoryView.Items.Add(newItem);
                }
                else
                {
                    currentHeader.Items.Add(newItem);
                }

                CategoryAddTreeViewLevel(current.id, newItem);
            }
        }

        private void cmdGalleryView_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO");
        }

        private void cmdGalleryCopyUrl_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO");
        }

        private void cmdGalleryAdd_Click(object sender, RoutedEventArgs e)
        {
            txtGalleryName.Text = "";
            txtGalleryDescription.Text = "";
            txtGalleryPassword.Text = "";
            cmbGalleryAccessType.SelectedIndex = 0;
            cmbGalleryGroupingType.SelectedIndex = 0;

            GalleryRefreshTagsList();
            GalleryRefreshCategoryList();

            RefreshOverallPanesStructure(PaneMode.GalleryAdd);
            RefreshPanesAllControls(PaneMode.GalleryAdd);
        }

        async private void cmdGalleryEdit_Click(object sender, RoutedEventArgs e)
        {
            GalleryRefreshTagsList();
            GalleryRefreshCategoryList();

            if (await PopulateGalleryMetaData())
            {
                RefreshOverallPanesStructure(PaneMode.GalleryEdit);
                RefreshPanesAllControls(PaneMode.GalleryEdit);
            }
        }

        private void txtGalleryName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Filter out non-digits, except spaces input
            foreach (char c in e.Text)
                if (!Char.IsLetterOrDigit(c) && !Char.IsWhiteSpace(c))
                {
                    e.Handled = true;
                    break;
                }
        }

        private void cmbGalleryAccessType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshPanesAllControls(currentPane);
        }

        async private void cmdGallerySave_Click(object sender, RoutedEventArgs e)
        {
            //TODO check for selected categories....
            int tagsCount = lstGalleryTagListInclude.SelectedItems.Count + lstGalleryTagListExclude.SelectedItems.Count;

            if (lstGalleryTagListInclude.SelectedItems.Count == 0)
            {
                DisplayMessage("The gallery does not have any catgories or tags associated with it, so cannot be saved.", MessageSeverity.Info, true);
                return;
            }

            if (cmbGalleryAccessType.SelectedIndex == 1 && txtGalleryPassword.Text.Length == 0)
            {
                DisplayMessage("This gallery has been marked as password protected, but the password does not meet the minumimum criteria of being 8 charactors long.", MessageSeverity.Info, true);
                return;
            }

            if (txtGalleryName.Text.Length == 0)
            {
                DisplayMessage("You must select a name for your Gallery to continue.", MessageSeverity.Info, true);
                return;
            }

            if (currentPane == PaneMode.GalleryAdd)
            {
                currentGallery = new Gallery();
            }

            string oldGalleryName = currentGallery.Name;
            currentGallery.Name = txtGalleryName.Text;
            currentGallery.Desc = txtGalleryDescription.Text;
            currentGallery.Password = txtGalleryPassword.Text;
            currentGallery.AccessType = cmbGalleryAccessType.SelectedIndex;
            currentGallery.GroupingType = cmbGalleryGroupingType.SelectedIndex;

            /* Category add to object ************************************************ */
            //TODO


            /* Tags add to object ************************************************ */
            currentGallery.Tags = new GalleryTagRef[tagsCount];
            int currentItemIndex = 0;
            foreach (ListBoxItem current in lstGalleryTagListInclude.SelectedItems)
            {
                TagListTagRef tagRef = (TagListTagRef)current.Tag;

                GalleryTagRef galleryTag = new GalleryTagRef();
                galleryTag.tagId = tagRef.id;
                galleryTag.tagIdSpecified = true;
                galleryTag.exclude = false;
                galleryTag.excludeSpecified = true;
                currentGallery.Tags[currentItemIndex] = galleryTag;
                currentItemIndex++;
            }

            foreach (ListBoxItem current in lstGalleryTagListExclude.SelectedItems)
            {
                TagListTagRef tagRef = (TagListTagRef)current.Tag;

                GalleryTagRef galleryTag = new GalleryTagRef();
                galleryTag.tagId = tagRef.id;
                galleryTag.tagIdSpecified = true;
                galleryTag.exclude = true;
                galleryTag.excludeSpecified = true;
                currentGallery.Tags[currentItemIndex] = galleryTag;
                currentItemIndex++;
            }

            /* Sorts add to object ************************************************ */
            //TODO

            string response = null;
            if (currentPane == PaneMode.GalleryAdd)
            {
                response = await controller.GalleryCreateAsync(currentGallery);
            }
            else
            {
                response = await controller.GalleryUpdateAsync(currentGallery, oldGalleryName);
            }

            if (response != "OK")
            {
                DisplayMessage(response, MessageSeverity.Error, false);
                return;
            }

            RefreshOverallPanesStructure(PaneMode.GalleryView);
            RefreshPanesAllControls(PaneMode.GalleryView);
            RefreshAndDisplayGalleryList(true);
        }

        private void cmdGalleryCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(PaneMode.GalleryView);
            RefreshPanesAllControls(PaneMode.GalleryView);
        }

        async private void cmdGalleryDelete_Click(object sender, RoutedEventArgs e)
        {
            string response = await controller.GalleryDeleteAsync(currentGallery);
            if (response != "OK")
            {
                DisplayMessage(response, MessageSeverity.Error, false);
                return;
            }

            RefreshOverallPanesStructure(PaneMode.GalleryView);
            RefreshAndDisplayGalleryList(true);
        }

        private void cmdGalleryPreview_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO");
        }

        

        private void cmdGalleryRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAndDisplayGalleryList(true);
        }

        #endregion


    }
}
