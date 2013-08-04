using System;
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
using System.IO;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Xml;


namespace ManageWalla
{
    public partial class MainWindow : Window
    {
        #region Variables
        private enum PaneMode
        {
            CategoryView = 0,
            CategoryEdit = 1,
            CategoryAdd = 2,
            TagView = 3,
            TagEdit = 4,
            TagAdd = 5,
            ViewView = 6,
            ViewEdit = 7,
            ViewAdd = 8,
            Upload = 9,
            ImageViewFull = 10,
            Settings = 11
        }

        private enum FetchDirection
        {
            Start = 0,
            End = 1,
            Next = 2,
            Previous = 3
        }

        private PaneMode currentPane;
        private Tag currentTag = null;

        private MainController controller = null;
        public UploadUIState uploadUIState = null;
        public UploadImageFileList uploadFots = null;
        public UploadStatusListBind uploadStatusListBind = null;
        public ImageMainViewerList imageMainViewerList = null;


        public GlobalState state = null;

        public TagImageList currentTagImageList = null;


        #endregion

        #region Init Close Window
        public MainWindow()
        {
            InitializeComponent();

            uploadFots = (UploadImageFileList)FindResource("uploadImagefileListKey");
            uploadUIState = (UploadUIState)FindResource("uploadUIStateKey");
            uploadStatusListBind = (UploadStatusListBind)FindResource("uploadStatusListBindKey");
            imageMainViewerList = (ImageMainViewerList)FindResource("imageMainViewerListKey");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Apply busy panes overlay

            //Kick off asyncronous data syncronising.
            //This will update all UI Elements eventually
            controller = new MainController(this);

            state = controller.GetState();
            currentPane = PaneMode.CategoryView;            
            this.cmdCategory.IsChecked = true;

            //Asyncronously check\update local cache information for Categories\Tags\Views.
            //Task.Run(controller.RetrieveGeneralUserConfigAsync());
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            controller.Dispose();
        }
        #endregion

        #region Controls control logic based on Pane

        //TODO add working icons.
        private void RefreshPanesLoadingState(PaneMode updatedPane, string message, bool updateControlsFromState)
        {
            switch (updatedPane)
            {
                #region Category
                case PaneMode.CategoryView:
                case PaneMode.CategoryAdd:
                case PaneMode.CategoryEdit:
                    switch (currentPane)
                    {
                        //All of these display the category pane.
                        case PaneMode.CategoryView:
                        case PaneMode.CategoryAdd:
                        case PaneMode.CategoryEdit:
                        case PaneMode.ViewView:
                        case PaneMode.ViewEdit:
                        case PaneMode.ViewAdd:
                        case PaneMode.Upload:
                            switch (state.categoryLoadState) 
                            {
                                case GlobalState.DataLoadState.Loaded:
                                    //if (updateControlsFromState) {TagListReloadFromState();}
                                    panCategoryWorking.Visibility = System.Windows.Visibility.Collapsed;
                                    gridCategory.Visibility = Visibility.Visible;
                                    break;
                                case GlobalState.DataLoadState.Pending:
                                    //TODO Just update loading icon
                                    txtCategoryWorkingMessage.Text = "Category information is loading...";
                                    break;
                                case GlobalState.DataLoadState.LocalCache:
                                    //if (updateControlsFromState) {TagListReloadFromState();}
                                    panCategoryWorking.Visibility = System.Windows.Visibility.Collapsed;
                                    gridCategory.Visibility = Visibility.Visible;
                                    break;
                                case GlobalState.DataLoadState.Unavailable:
                                    panCategoryWorking.Visibility = System.Windows.Visibility.Visible;
                                    gridCategory.Visibility = Visibility.Collapsed;
                                    txtCategoryWorkingMessage.Text = "Category information is unavailable.  " + message ?? "";
                                    break;
                                case GlobalState.DataLoadState.No:
                                    //TagListReloadFromState();
                                    break;
                            }
                            break;
                        //Category pane is currently hidden.  So just update icon and text message.
                        default:
                            switch (state.categoryLoadState) 
                            {
                                case GlobalState.DataLoadState.Loaded:
                                    //if (updateControlsFromState) {TagListReloadFromState();}
                                    break;
                                case GlobalState.DataLoadState.Pending:
                                    //TODO Just update loading icon
                                    txtCategoryWorkingMessage.Text = "Category information is loading...";
                                    break;
                                case GlobalState.DataLoadState.LocalCache:
                                    //if (updateControlsFromState) {TagListReloadFromState();}
                                    break;
                                case GlobalState.DataLoadState.Unavailable:
                                    txtCategoryWorkingMessage.Text = "Category information is unavailable.  " + message ?? "";
                                    break;
                            }
                            break;
                    }
                    break;
                #endregion

                #region Tag
                case PaneMode.TagView:
                case PaneMode.TagAdd:
                case PaneMode.TagEdit:
                    switch (currentPane)
                    {
                        //All of these display the tag pane.
                        case PaneMode.TagView:
                        case PaneMode.TagAdd:
                        case PaneMode.TagEdit:
                        case PaneMode.ViewView:
                        case PaneMode.ViewEdit:
                        case PaneMode.ViewAdd:
                        case PaneMode.Upload:
                            switch (state.tagLoadState) 
                            {
                                case GlobalState.DataLoadState.Loaded:
                                    if (updateControlsFromState) { TagListReloadFromState(); }
                                    panTagWorking.Visibility = System.Windows.Visibility.Collapsed;
                                    gridTag.Visibility = Visibility.Visible;
                                    break;
                                case GlobalState.DataLoadState.Pending:
                                    //TODO Just update loading icon
                                    txtTagWorkingMessage.Text = "Tag information is loading...";
                                    break;
                                case GlobalState.DataLoadState.LocalCache:
                                    if (updateControlsFromState) { TagListReloadFromState(); }
                                    panTagWorking.Visibility = System.Windows.Visibility.Collapsed;
                                    gridTag.Visibility = Visibility.Visible;
                                    break;
                                case GlobalState.DataLoadState.Unavailable:
                                    panTagWorking.Visibility = System.Windows.Visibility.Visible;
                                    gridTag.Visibility = Visibility.Collapsed;
                                    txtTagWorkingMessage.Text = "Tag information is unavailable.  " + message ?? "";
                                    break;
                            }
                            break;
                        //Category pane is currently hidden.  So only update icon, message and data.
                        default:
                            switch (state.tagLoadState) 
                            {
                                case GlobalState.DataLoadState.Loaded:
                                    if (updateControlsFromState) { TagListReloadFromState(); }
                                    break;
                                case GlobalState.DataLoadState.Pending:
                                    //TODO Just update loading icon
                                    txtCategoryWorkingMessage.Text = "Tag information is loading...";
                                    break;
                                case GlobalState.DataLoadState.LocalCache:
                                    if (updateControlsFromState) { TagListReloadFromState(); }
                                    break;
                                case GlobalState.DataLoadState.Unavailable:
                                    txtCategoryWorkingMessage.Text = "Tag information is unavailable.  " + message ?? "";
                                    break;
                            }
                            break;
                    }
                    break;
                #endregion

                #region View
                case PaneMode.ViewView:
                case PaneMode.ViewEdit:
                case PaneMode.ViewAdd:

                    switch (currentPane)
                    {
                        //All of these display the view pane.
                        case PaneMode.ViewView:
                        case PaneMode.ViewEdit:
                        case PaneMode.ViewAdd:
                            switch (state.viewLoadState) 
                            {
                                case GlobalState.DataLoadState.Loaded:
                                    //ViewListReloadFromState();
                                    panViewWorking.Visibility = System.Windows.Visibility.Collapsed;
                                    //gridView.Visibility = Visibility.Visible;
                                    break;
                                case GlobalState.DataLoadState.Pending:
                                    //TODO Just update loading icon
                                    txtViewWorkingMessage.Text = "View information is loading...";
                                    break;
                                case GlobalState.DataLoadState.LocalCache:
                                    //ViewListReloadFromState();
                                    panViewWorking.Visibility = System.Windows.Visibility.Collapsed;
                                    //gridView.Visibility = Visibility.Visible;
                                    break;
                                case GlobalState.DataLoadState.Unavailable:
                                    panViewWorking.Visibility = System.Windows.Visibility.Visible;
                                    //gridView.Visibility = Visibility.Collapsed;
                                    txtViewWorkingMessage.Text = "View information is unavailable.  " + message ?? "";
                                    break;
                                case GlobalState.DataLoadState.No:
                                    //RefreshTagsList();
                                    break;
                            }
                            break;
                        //View pane is currently hidden.  So only update icon and message.
                        default:
                            switch (state.viewLoadState) 
                            {
                                case GlobalState.DataLoadState.Loaded:
                                    //TODO
                                    break;
                                case GlobalState.DataLoadState.Pending:
                                    //TODO Just update loading icon
                                    txtViewWorkingMessage.Text = "View information is loading...";
                                    break;
                                case GlobalState.DataLoadState.LocalCache:
                                    //TODO
                                    break;
                                case GlobalState.DataLoadState.Unavailable:
                                    txtViewWorkingMessage.Text = "View information is unavailable.  " + message ?? "";
                                    break;
                            }
                            break;
                    }
                    break;
                #endregion

            }
        }

        private void RefreshOverallPanesStructure()
        {
            switch (currentPane)
            {
                case PaneMode.CategoryView:
                case PaneMode.CategoryAdd:
                case PaneMode.CategoryEdit:
                    gridTag.Visibility = Visibility.Collapsed;
                    gridSettings.Visibility = Visibility.Collapsed;
                    stackView.Visibility = Visibility.Collapsed;
                    stackUpload.Visibility = Visibility.Collapsed;

                    gridLeft.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[3].Height = new GridLength(0);
                    gridLeft.RowDefinitions[5].Height = new GridLength(0);

                    gridRight.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                    gridRight.RowDefinitions[2].Height = new GridLength(0);
                    gridRight.RowDefinitions[4].Height = new GridLength(0);
                    break;
                case PaneMode.TagView:
                case PaneMode.TagAdd:
                case PaneMode.TagEdit:
                    gridCategory.Visibility = Visibility.Collapsed;
                    gridSettings.Visibility = Visibility.Collapsed;
                    stackView.Visibility = Visibility.Collapsed;
                    stackUpload.Visibility = Visibility.Collapsed;

                    gridLeft.RowDefinitions[1].Height = new GridLength(0);
                    gridLeft.RowDefinitions[3].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[5].Height = new GridLength(0);

                    gridRight.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                    gridRight.RowDefinitions[2].Height = new GridLength(0);
                    gridRight.RowDefinitions[4].Height = new GridLength(0);

                    cmdCategory.IsChecked = false;
                    cmdSettings.IsChecked = false;
                    cmdUpload.IsChecked = false;
                    cmdView.IsChecked = false;
                    break;
                case PaneMode.Settings:
                    gridLeft.RowDefinitions[1].Height = new GridLength(0);
                    gridLeft.RowDefinitions[3].Height = new GridLength(0);
                    gridLeft.RowDefinitions[5].Height = new GridLength(1,GridUnitType.Star);

                    gridRight.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                    gridRight.RowDefinitions[2].Height = new GridLength(0);
                    gridRight.RowDefinitions[4].Height = new GridLength(0);

                    cmdCategory.IsChecked = false;
                    cmdTag.IsChecked = false;
                    cmdUpload.IsChecked = false;
                    cmdView.IsChecked = false;
                    break;
                case PaneMode.ViewView:
                case PaneMode.ViewEdit:
                case PaneMode.ViewAdd:
                    gridCategory.Visibility = Visibility.Collapsed;
                    gridSettings.Visibility = Visibility.Collapsed;
                    stackView.Visibility = Visibility.Collapsed;
                    stackUpload.Visibility = Visibility.Collapsed;

                    gridLeft.RowDefinitions[1].Height = new GridLength(2, GridUnitType.Star);
                    gridLeft.RowDefinitions[3].Height = new GridLength(2, GridUnitType.Star);
                    gridLeft.RowDefinitions[5].Height = new GridLength(0);

                    gridRight.RowDefinitions[0].Height = new GridLength(0);
                    gridRight.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
                    gridRight.RowDefinitions[4].Height = new GridLength(0);
                    break;
                case PaneMode.ImageViewFull:
                    break;
                case PaneMode.Upload:

                    gridCategory.Visibility = Visibility.Visible;
                    gridTag.Visibility = Visibility.Visible;
                    stackUpload.Visibility = Visibility.Visible;

                    gridLeft.RowDefinitions[1].Height = new GridLength(2, GridUnitType.Star);
                    gridLeft.RowDefinitions[3].Height = new GridLength(2, GridUnitType.Star);
                    gridLeft.RowDefinitions[5].Height = new GridLength(0);

                    gridRight.RowDefinitions[0].Height = new GridLength(0);
                    gridRight.RowDefinitions[2].Height = new GridLength(0);
                    gridRight.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);
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

                    //treeCategoryView.IsEnabled = true;
                    //gridCatgeoryAddEdit gridCategoryView
                    break;
                case PaneMode.CategoryAdd:
                    //treeCategoryView.IsEnabled = false;

                    gridCategory.RowDefinitions[1].MaxHeight = 0;
                    gridCategory.RowDefinitions[2].MaxHeight = 25;
                    gridCategory.RowDefinitions[3].MaxHeight = 75;
                    gridCategory.RowDefinitions[4].MaxHeight = 34;

                    cmdAddEditCategoryMove.Visibility = Visibility.Collapsed;
                    cmdAddEditCategorySave.Content = "Save New";
                    cmdAddEditCategoryDelete.Visibility = Visibility.Collapsed;

                    break;
                case PaneMode.CategoryEdit:
                    //treeCategoryView.IsEnabled = false;

                    gridCategory.RowDefinitions[1].MaxHeight = 0;
                    gridCategory.RowDefinitions[2].MaxHeight = 25;
                    gridCategory.RowDefinitions[3].MaxHeight = 75;
                    gridCategory.RowDefinitions[4].MaxHeight = 34;
                    cmdAddEditCategoryMove.Visibility = Visibility.Visible;
                    cmdAddEditCategorySave.Content = "Save Edit";
                    cmdAddEditCategoryDelete.Visibility = Visibility.Visible;

                    break;
                #endregion

                #region Tag
                case PaneMode.TagView:
                    gridTag.RowDefinitions[1].MaxHeight = 34;
                    gridTag.RowDefinitions[2].MaxHeight = 0;
                    gridTag.RowDefinitions[3].MaxHeight = 0;
                    gridTag.RowDefinitions[4].MaxHeight = 0;

                    cmdAssociateTag.Visibility = Visibility.Collapsed;
                    cmdAddTag.Visibility = Visibility.Visible;
                    cmdEditTag.Visibility = Visibility.Visible;
                    wrapMyTags.IsEnabled = true;
                    break;
                case PaneMode.TagAdd:

                    this.cmdAddEditTagSave.Content = "Save New";
                    gridTag.RowDefinitions[1].MaxHeight = 0;
                    gridTag.RowDefinitions[2].MaxHeight = 25;
                    gridTag.RowDefinitions[3].MaxHeight = 75;
                    gridTag.RowDefinitions[4].MaxHeight = 34;

                    break;
                case PaneMode.TagEdit:
                    this.cmdAddEditTagSave.Content = "Save Edit";
                    gridTag.RowDefinitions[1].MaxHeight = 0;
                    gridTag.RowDefinitions[2].MaxHeight = 25;
                    gridTag.RowDefinitions[3].MaxHeight = 75;
                    gridTag.RowDefinitions[4].MaxHeight = 34;

                    break;
                #endregion

                #region View
                case PaneMode.ViewView:

                    break;
                case PaneMode.ViewEdit:
                    /*

                    gridView.Visibility = System.Windows.Visibility.Collapsed;
                    gridViewAddEdit.Visibility = System.Windows.Visibility.Visible;

                    cmdAddEditViewDelete.Visibility = System.Windows.Visibility.Visible;
                    cmdAddEditViewSave.Content = "Save Update";
                    */

                    break;

                case PaneMode.ViewAdd:

                    /*
                    gridView.Visibility = System.Windows.Visibility.Collapsed;
                    gridViewAddEdit.Visibility = System.Windows.Visibility.Visible;

                    cmdAddEditViewDelete.Visibility = System.Windows.Visibility.Collapsed;
                    cmdAddEditViewSave.Content = "Save New View";

                     */ 
                    break;
                #endregion

                #region Upload
                case PaneMode.Upload:

                    //Sort out Tag view options
                    gridTag.RowDefinitions[1].MaxHeight = 34;
                    gridTag.RowDefinitions[2].MaxHeight = 0;
                    gridTag.RowDefinitions[3].MaxHeight = 0;
                    gridTag.RowDefinitions[4].MaxHeight = 0;

                    cmdAssociateTag.Visibility = Visibility.Visible;
                    cmdAddTag.Visibility = Visibility.Collapsed;
                    cmdEditTag.Visibility = Visibility.Collapsed;
                    wrapMyTags.IsEnabled = false;
                    cmdAssociateTag.IsEnabled = false;
                    
                    if (!uploadUIState.Uploading && (uploadUIState.Mode == UploadUIState.UploadMode.Images || uploadUIState.Mode == UploadUIState.UploadMode.Folder))
                    {
                        //Common
                        lstUploadImageFileList.IsEnabled = true;
                        tabUploadImageDetails.IsEnabled = true;
                        grdUploadSettings.RowDefinitions[2].MaxHeight = 25; //Maintain sub folders.
                        grdUploadSettings.RowDefinitions[3].MaxHeight = 25; //Upload to new category
                        tabUploadImageDetails.IsEnabled = true;
                        if (uploadUIState.UploadToNewCategory)
                        {
                            grdUploadSettings.RowDefinitions[4].MaxHeight = 25; //Category Name
                            grdUploadSettings.RowDefinitions[5].MaxHeight = 80; //Category Description
                        }
                        else
                        {
                            grdUploadSettings.RowDefinitions[4].MaxHeight = 0;
                            grdUploadSettings.RowDefinitions[5].MaxHeight = 0;
                        }
                        
                        cmdUploadAll.IsEnabled = true;
                        cmdUploadClear.IsEnabled = true;

                        //Enable Tags
                        wrapMyTags.IsEnabled = true;
                        cmdAssociateTag.IsEnabled = true;

                        //Enable Category
                        //TODO

                        if (uploadUIState.Mode == UploadUIState.UploadMode.Images)
                        {
                            grdUploadImageDetails.RowDefinitions[0].MaxHeight = 0; //Sub category marker
                            //grdUploadSettings.RowDefinitions[2].MaxHeight = 0; //Map to sub folders
                            chkUploadMapToSubFolders.IsEnabled = false;

                            cmdUploadImportFolder.Visibility = Visibility.Hidden;
                        }
                        else if (uploadUIState.Mode == UploadUIState.UploadMode.Folder)
                        {
                           
                            if (uploadUIState.GotSubFolders)
                            {
                                grdUploadImageDetails.RowDefinitions[0].MaxHeight = 25; //Sub category marker
                                //grdUploadSettings.RowDefinitions[2].MaxHeight = 25; //Maintain sub folders.
                                chkUploadMapToSubFolders.IsEnabled = true;
                            }
                            else
                            {
                                grdUploadImageDetails.RowDefinitions[0].MaxHeight = 0; //Sub category marker
                                //grdUploadSettings.RowDefinitions[2].MaxHeight = 0; //Map to sub folders
                                chkUploadMapToSubFolders.IsEnabled = false;
                            }
                            cmdUploadImportFiles.Visibility = Visibility.Hidden;
                        }
                    }
                    else
                    {
                        //Check if there are outstanding upload items then just allow progress or cancel.
                        if (uploadUIState.Uploading)
                        {
                            lstUploadImageFileList.IsEnabled = false;
                            cmdUploadImportFiles.Visibility = Visibility.Hidden;
                            cmdUploadImportFolder.Visibility = Visibility.Hidden;
                            cmdUploadClear.IsEnabled = true;
                            cmdUploadClear.Content = "Cancel Uploads";
                        }
                        else
                        {
                            //New Upload
                            lstUploadImageFileList.IsEnabled = false;

                            cmdUploadImportFolder.Visibility = Visibility.Visible;
                            cmdUploadImportFiles.Visibility = Visibility.Visible;
                            cmdUploadClear.Content = "Clear";
                            cmdUploadClear.IsEnabled = false;
                            grdUploadSettings.RowDefinitions[2].MaxHeight = 0;
                            grdUploadSettings.RowDefinitions[3].MaxHeight = 0;
                            grdUploadSettings.RowDefinitions[4].MaxHeight = 0;
                            grdUploadSettings.RowDefinitions[5].MaxHeight = 0;
                        }
                        cmdUploadAll.IsEnabled = false;
                        tabUploadImageDetails.IsEnabled = false;

                        //Disable Tags
                        wrapMyTags.IsEnabled = false;
                        cmdAssociateTag.IsEnabled = false;

                        //Disable Category
                    }
                    break;
                #endregion

                case PaneMode.ImageViewFull:

                    break;
                case PaneMode.Settings:

                    break;
            }

            currentPane = mode;
            switch (currentPane)
            {
                //These actions indicate a change has occured which requires a window re-jig.
                case PaneMode.CategoryView:
                case PaneMode.TagView:
                case PaneMode.ViewView:
                case PaneMode.Upload:
                case PaneMode.ImageViewFull:
                case PaneMode.Settings:
                    RefreshPanesLoadingState(mode, null, false);
                    RefreshOverallPanesStructure();
                    break;
            }
        }
        #endregion

        #region View UI Control
        private void cmdEditView_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.ViewEdit);
        }

        private void cmdAddNewView_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.ViewAdd);
        }

        private void cmdAddEditViewCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.ViewView);
        }
        #endregion

        #region Category UI Control
        private void cmdAddCategory_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.CategoryAdd);
        }

        private void cmdAddEditCategoryCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.CategoryView);
        }

        private void cmdEditCategory_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.CategoryEdit);
        }

        public void RefreshCategoryTreeView()
        {

        }
        #endregion

        #region Tag UI Control
        //Force refresh of the Tags List
        async private void RefreshTagsList()
        {
            state.tagLoadState = GlobalState.DataLoadState.Pending;
            RefreshPanesLoadingState(PaneMode.TagView, null, false);
            string response = await controller.RefreshTagsListAsync();
            RefreshPanesLoadingState(PaneMode.TagView, response, true);
        }

        public void TagListReloadFromState()
        {
            wrapMyTags.Children.Clear();

            foreach (TagListTagRef tag in state.tagList.TagRef)
            {
                RadioButton newRadioButton = new RadioButton();

                newRadioButton.Content = tag.name + " (" + tag.count + ")";
                newRadioButton.Style = (Style)FindResource("styleRadioButton");
                newRadioButton.Template = (ControlTemplate)FindResource("templateRadioButton");
                newRadioButton.GroupName = "GroupTag";
                newRadioButton.Tag = tag;
                newRadioButton.Checked += new RoutedEventHandler(FetchTagImagesFirstAsync);
                wrapMyTags.Children.Add(newRadioButton);
            }
        }

        //TODO - ensure that this is called when a search is applied.
        async private void FetchTagImagesFirstAsync(object sender, RoutedEventArgs e)
        {
            /* Get current tag */
            RadioButton checkedButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            
            /* Refresh tag image state */
            if (checkedButton != null)
            {
                TagListTagRef tagListTagRefTemp = (TagListTagRef)checkedButton.Tag;
                currentTagImageList = await controller.GetTagImagesAsync(tagListTagRefTemp.id, tagListTagRefTemp.name, 0, GetTagSearchQueryString());
            }

            /* Populate tag image list from state */
            TagImageListUpdateControls();
        }

        async private void FetchMoreTagImagesAsync(FetchDirection direction)
        {
            if (currentTagImageList == null)
                return;

            /* Update current tag image list */
            int cursor = 0;
            switch (direction)
            {
                case FetchDirection.Start:
                    cursor = 0;
                    break;
                case FetchDirection.Next:
                    if ((currentTagImageList.imageCursor + state.imageFetchSize) <= currentTagImageList.totalImageCount)
                        cursor = currentTagImageList.imageCursor + state.imageFetchSize;
                    break;
                case FetchDirection.Previous:
                    cursor = Math.Max(cursor - state.imageFetchSize, 0);
                    break;
                case FetchDirection.End:
                    cursor = Math.Abs(currentTagImageList.totalImageCount / state.imageFetchSize);
                    break;
            }

            currentTagImageList = await controller.GetTagImagesAsync(currentTagImageList.id, currentTagImageList.Name, cursor, GetTagSearchQueryString());

            /* Populate tag image list from state */
            TagImageListUpdateControls();
        }

        //TODO add functionality + server side.
        private string GetTagSearchQueryString()
        {
            return null;
        }

        private void TagImageListUpdateControls()
        {
            imageMainViewerList.Clear();

            if (currentTagImageList == null)
                return;

            if (currentTagImageList.Images == null)
                return;

            foreach (TagImageListImageRef imageRef in currentTagImageList.Images)
            {
                GeneralImage newImage = new GeneralImage();
                newImage.imageId = imageRef.id;
                newImage.Name = imageRef.name;
                newImage.Description = imageRef.desc;
                newImage.TakenDate = imageRef.takenDate;
                newImage.UploadDate = imageRef.uploadDate;
                newImage.FilePath = imageRef.localPath;
                newImage.LoadImage();

                imageMainViewerList.Add(newImage);
            }
        }

        private void PopulateTagMetaData()
        {
            RadioButton checkedButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();

            if (checkedButton != null)
            {
                TagListTagRef tagListTagRefTemp = (TagListTagRef)checkedButton.Tag;
                Tag tag = controller.GetTagMeta((TagListTagRef)checkedButton.Tag);
                txtTagAddEditName.Text = tag.Name;
                txtTagAddEditDescription.Text = tag.Desc;
                currentTag = tag;
            }
        }

        private void txtTagAddEditName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Filter out non-digit text input
            foreach (char c in e.Text)
                if (!Char.IsLetterOrDigit(c))
                {
                    e.Handled = true;
                    break;
                }
        }

        private void cmdAddEditTagDelete_Click(object sender, RoutedEventArgs e)
        {
            string response = controller.DeleteTag(currentTag);

            if (response.Length > 0)
            {
                MessageBox.Show(response);
                return;
            }

            RefreshPanesAllControls(PaneMode.TagView);
            RefreshTagsList();
        }

        private void cmdAddTag_Click(object sender, RoutedEventArgs e)
        {
            txtTagAddEditName.Text = "";
            txtTagAddEditDescription.Text = "";
            RefreshPanesAllControls(PaneMode.TagAdd);
        }

        private void cmdEditTag_Click(object sender, RoutedEventArgs e)
        {
            PopulateTagMetaData();
            RefreshPanesAllControls(PaneMode.TagEdit);
        }

        private void cmdAddEditTagCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.TagView);
        }

        private void cmdRefreshTagList_Click(object sender, RoutedEventArgs e)
        {
            RefreshTagsList();
        }

        private void cmdAddEditTagSave_Click(object sender, RoutedEventArgs e)
        {
            string response = null;

            //Check tag name is unique

            if (currentPane == PaneMode.TagAdd)
            {
                Tag tag = new Tag();
                tag.Name = txtTagAddEditName.Text;
                tag.Desc = txtTagAddEditDescription.Text;

                //Add Images selected

                response = controller.SaveNewTag(tag);
            }
            else
            {
                string oldTagName = currentTag.Name;
                currentTag.Name = txtTagAddEditName.Text;
                currentTag.Desc = txtTagAddEditDescription.Text;

                response = controller.UpdateTag(currentTag, oldTagName);
            }


            if (response.Length > 0)
            {
                MessageBox.Show(response);
                return;
            }

            RefreshPanesAllControls(PaneMode.TagView);
            RefreshTagsList();
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
                /*
                uploadUIState.GotSubFolders = false;
                uploadUIState.CategoryName = "";
                uploadUIState.CategoryDesc = "";
                uploadUIState.MapToSubFolders = false;
                uploadUIState.UploadToNewCategory = false;

                uploadUIState.Mode = UploadUIState.UploadMode.None;
                 */
                uploadUIState = new UploadUIState();
                //TODO need to re-initiailse bindings, as controls will not be in sync with object now.

            }
            else
            {
                chkUploadCameraAll.IsChecked = false;
                //Plus all the others.
            }
            //TODO = get list of all controls with a name starts chkUpload% and finishes %All
            //Loop through and set checked to false
        }

        private void chkUploadTagsAll_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaTagRef = current.Meta.Tags;

            BindingOperations.ClearBinding(lstUploadTagList, ListBox.ItemsSourceProperty);
            Binding binding = new Binding("MetaTagRef");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(lstUploadTagList, ListBox.ItemsSourceProperty, binding);
        }

        private void chkUploadTagsAll_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(lstUploadTagList, ListBox.ItemsSourceProperty);
            Binding binding = new Binding("/Meta.Tags");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(lstUploadTagList, ListBox.ItemsSourceProperty, binding);
        }

        async private void cmdUploadImportFiles_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.DefaultExt = @"*.JPG;*.BMP";
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

        async private void cmdUploadResetMeta_Click(object sender, RoutedEventArgs e)
        {
            ResetUploadState(false);
            await controller.ResetMeFotsMeta(uploadFots);
            RefreshPanesAllControls(PaneMode.Upload);
        }

        private void lstUploadImageFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (expUploadAssociateTags.Visibility == Visibility.Visible)
            {
                BindingOperations.GetBindingExpressionBase(lstUploadTagList, ListBox.ItemsSourceProperty).UpdateTarget();
            }

            if (!uploadUIState.MetaTagRefAll)
            {
                UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
                UploadEnableDisableTags(current);
            }
        }

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
            uploadUIState.Uploading = true;
            RefreshPanesAllControls(PaneMode.Upload);

            string response = await controller.DoUploadAsync(uploadFots, uploadUIState);
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

        async private void cmdUploadStatusRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshUploadStatusStateAsync();
        }

        private void RefreshUploadStatusFromStateList(string message)
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

        async private Task RefreshUploadStatusStateAsync()
        {
            state.uploadStatusListState = GlobalState.DataLoadState.Pending;
            string response = await controller.RefreshUploadStatusListAsync();
            RefreshUploadStatusFromStateList(response);
        }

        async private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabUpload.SelectedIndex == 2)
            {
                if (state.uploadStatusListState == GlobalState.DataLoadState.No)
                {
                    state.uploadStatusListState = GlobalState.DataLoadState.LocalCache;

                    RefreshUploadStatusFromStateList(null);

                    await RefreshUploadStatusStateAsync();
                }

            }
        }
        #endregion

        #region Binding re-wire for Upload All Events
        private void chkUploadCameraAll_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaCamera = current.Meta.Camera;

            BindingOperations.ClearBinding(txtUploadCamera, TextBox.TextProperty);
            Binding binding = new Binding("MetaCamera");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(txtUploadCamera, TextBox.TextProperty, binding);
        }

        private void chkUploadCameraAll_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(txtUploadCamera, TextBox.TextProperty);
            Binding binding = new Binding("/Meta.Camera");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(txtUploadCamera, TextBox.TextProperty, binding);
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

        private void chkUploadTakenDateAll_Checked(object sender, RoutedEventArgs e)
        {
            UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
            uploadUIState.MetaTakenDate = current.Meta.TakenDate;

            BindingOperations.ClearBinding(datUploadTakenDate, DatePicker.SelectedDateProperty);
            Binding binding = new Binding("MetaTakenDate");
            binding.Mode = BindingMode.TwoWay;
            binding.Source = uploadUIState;
            BindingOperations.SetBinding(datUploadTakenDate, DatePicker.SelectedDateProperty, binding);
        }

        private void chkUploadTakenDateAll_Unchecked(object sender, RoutedEventArgs e)
        {
            BindingOperations.ClearBinding(datUploadTakenDate, DatePicker.SelectedDateProperty);
            Binding binding = new Binding("/Meta.TakenDate");
            binding.Mode = BindingMode.TwoWay;
            BindingOperations.SetBinding(datUploadTakenDate, DatePicker.SelectedDateProperty, binding);
        }
        #endregion

        #region Pane Control Events
        private void cmdCategory_Checked(object sender, RoutedEventArgs e)
        {
            cmdSettings.IsChecked = false;
            cmdTag.IsChecked = false;
            cmdUpload.IsChecked = false;
            cmdView.IsChecked = false;

            RefreshPanesAllControls(PaneMode.CategoryView);
        }

        private void cmdUpload_Checked(object sender, RoutedEventArgs e)
        {
            cmdSettings.IsChecked = false;
            cmdTag.IsChecked = false;
            cmdCategory.IsChecked = false;
            cmdView.IsChecked = false;


            //One off load from cache if available.
            if (state.tagLoadState == GlobalState.DataLoadState.No)
            {
                if (state.tagList != null)
                {
                    state.tagLoadState = GlobalState.DataLoadState.LocalCache;
                    RefreshPanesLoadingState(PaneMode.TagView, null, true);
                }
                RefreshTagsList();
            }

            RefreshPanesAllControls(PaneMode.Upload);
        }

        private void cmdView_Checked(object sender, RoutedEventArgs e)
        {
            cmdSettings.IsChecked = false;
            cmdTag.IsChecked = false;
            cmdUpload.IsChecked = false;
            cmdCategory.IsChecked = false;

            RefreshPanesAllControls(PaneMode.ViewView);
        }

        private void cmdTag_Checked(object sender, RoutedEventArgs e)
        {
            bool tagLoadFirstTime = false;
            cmdSettings.IsChecked = false;
            cmdCategory.IsChecked = false;
            cmdUpload.IsChecked = false;
            cmdView.IsChecked = false;

            //One off load from cache if available.
            if (state.tagLoadState == GlobalState.DataLoadState.No)
            {
                if (state.tagList != null)
                {
                    state.tagLoadState = GlobalState.DataLoadState.LocalCache;
                    RefreshPanesLoadingState(PaneMode.TagView, null, true);
                }
                tagLoadFirstTime = true;
            }

            RefreshPanesAllControls(PaneMode.TagView);

            if (tagLoadFirstTime)
            {
                RefreshTagsList();
            }
        }

        private void cmdSettings_Checked(object sender, RoutedEventArgs e)
        {
            cmdCategory.IsChecked = false;
            cmdTag.IsChecked = false;
            cmdUpload.IsChecked = false;
            cmdView.IsChecked = false;

            RefreshPanesAllControls(PaneMode.Settings);
        }
        #endregion

        private void cmdRefreshCategoryList_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmdRefreshViewList_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}



#region codegraveyard
        /*
        private void RefreshUploadStatusFromStateListOld(string message)
        {

            XmlDataProvider uploadStatusListXml = (XmlDataProvider)FindResource("uploadStatusListXmlKey");
            XmlDocument uploadstatusXmldoc = new XmlDocument();
            uploadstatusXmldoc.LoadXml(state.uploadStatusListXml);

            XmlDocument newStatusList = new XmlDocument();
            XmlNamespaceManager nsManager = new XmlNamespaceManager(newStatusList.NameTable);
            nsManager.AddNamespace("s", "http://www.example.org/UploadStatusList");

            XmlElement documentRootNode = newStatusList.CreateElement("UploadStatusList", "http://www.example.org/UploadStatusList");
            newStatusList.AppendChild(documentRootNode);

            if (state.uploadStatusListXml != null)
            {
                //foreach (UploadImage currentUploadImage in uploadFots.Where(r => r.State == UploadImage.UploadState.Error))
                foreach (UploadImage currentUploadImage in uploadFots)
                {
                    //<XmlNamespaceMapping Uri="http://www.example.org/UploadStatusList" Prefix="s" />
                    //s:UploadStatusList/s:ImageUploadRef

                    XmlNameTable nameTable = uploadstatusXmldoc.NameTable;
                    XmlNamespaceManager namespaceManager = new XmlNamespaceManager(nameTable);
                    namespaceManager.AddNamespace("s", "http://www.example.org/UploadStatusList");

                    XmlNode rootNode = uploadstatusXmldoc.SelectSingleNode("/s:UploadStatusList", namespaceManager);
                    
                    //Create new entries for local uploads which have failed.
                    //<ImageUploadRef imageId="100310" imageStatus="1" name="051" lastUpdated="2013-07-27T10:45:59.333+01:00" />

                    XmlElement newNode = uploadstatusXmldoc.CreateElement("ImageUploadRef", "http://www.example.org/UploadStatusList");
                    XmlAttribute idAttribute = uploadstatusXmldoc.CreateAttribute("imageId");
                    idAttribute.Value = "-1";
                    newNode.Attributes.Append(idAttribute);

                    XmlAttribute statusAttribute = uploadstatusXmldoc.CreateAttribute("imageStatus");
                    statusAttribute.Value = "-1";
                    newNode.Attributes.Append(statusAttribute);

                    XmlAttribute nameAttribute = uploadstatusXmldoc.CreateAttribute("name");
                    nameAttribute.Value = currentUploadImage.Meta.Name;
                    newNode.Attributes.Append(nameAttribute);

                    XmlAttribute lastUpdatedAttribute = uploadstatusXmldoc.CreateAttribute("lastUpdated");
                    lastUpdatedAttribute.Value = "";
                    newNode.Attributes.Append(lastUpdatedAttribute);

                    XmlAttribute errorMessageAttribute = uploadstatusXmldoc.CreateAttribute("errorMessage");
                    errorMessageAttribute.Value = currentUploadImage.UploadError;
                    newNode.Attributes.Append(errorMessageAttribute);
                    rootNode.AppendChild(newNode);


                }

                uploadStatusListXml.Document = uploadstatusXmldoc;

                //force binding update - TODO get it working.
                datUploadStatusList.GetBindingExpression(DataGrid.ItemsSourceProperty).UpdateTarget();
            }

            //Refresh message + icon.
        }
        */
#endregion