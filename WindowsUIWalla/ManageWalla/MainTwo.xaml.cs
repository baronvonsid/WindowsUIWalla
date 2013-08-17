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
using log4net;
using log4net.Config;
using System.Configuration;

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
            ViewView = 6,
            ViewEdit = 7,
            ViewAdd = 8,
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
        private MainController controller = null;
        public UploadUIState uploadUIState = null;
        public UploadImageFileList uploadFots = null;
        public UploadStatusListBind uploadStatusListBind = null;
        public ImageMainViewerList imageMainViewerList = null;
        public GlobalState state = null;
        public TagImageList currentTagImageList = null;
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
        private void cmdCategory_Checked(object sender, RoutedEventArgs e)
        {
            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Visible;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdViewRefresh.Visibility = System.Windows.Visibility.Hidden;

            RefreshOverallPanesStructure(PaneMode.CategoryView);
            RefreshPanesAllControls(PaneMode.CategoryView);
        }

        private void cmdUpload_Checked(object sender, RoutedEventArgs e)
        {
            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdViewRefresh.Visibility = System.Windows.Visibility.Hidden;

            RefreshOverallPanesStructure(PaneMode.Upload);
            RefreshPanesAllControls(PaneMode.Upload);
        }

        private void cmdView_Checked(object sender, RoutedEventArgs e)
        {
            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdViewRefresh.Visibility = System.Windows.Visibility.Visible;

            RefreshOverallPanesStructure(PaneMode.ViewView);
            RefreshPanesAllControls(PaneMode.TagView);
        }

        private void cmdTag_Checked(object sender, RoutedEventArgs e)
        {
            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Visible;
            cmdViewRefresh.Visibility = System.Windows.Visibility.Hidden;

            RefreshOverallPanesStructure(PaneMode.TagView);
            RefreshPanesAllControls(PaneMode.TagView);
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
                    panViewUnavailable.Visibility = Visibility.Collapsed;
                    panUploadUnavailable.Visibility = Visibility.Collapsed;

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
                    panViewUnavailable.Visibility = Visibility.Collapsed;
                    panUploadUnavailable.Visibility = Visibility.Collapsed;

                    gridLeft.RowDefinitions[2].Height = new GridLength(0);
                    gridLeft.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[6].Height = new GridLength(0);
                    gridLeft.RowDefinitions[8].Height = new GridLength(0);
                    break;
                case PaneMode.ViewView:
                case PaneMode.ViewEdit:
                case PaneMode.ViewAdd:
                    panViewUnavailable.Visibility = Visibility.Visible;
                    panCategoryUnavailable.Visibility = Visibility.Collapsed;
                    panTagUnavailable.Visibility = Visibility.Collapsed;
                    panUploadUnavailable.Visibility = Visibility.Collapsed;

                    gridLeft.RowDefinitions[2].Height = new GridLength(0);
                    gridLeft.RowDefinitions[4].Height = new GridLength(0);
                    gridLeft.RowDefinitions[6].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[8].Height = new GridLength(0);
                    break;
                case PaneMode.Upload:
                    panUploadUnavailable.Visibility = Visibility.Visible;
                    panCategoryUnavailable.Visibility = Visibility.Collapsed;
                    panTagUnavailable.Visibility = Visibility.Collapsed;
                    panViewUnavailable.Visibility = Visibility.Collapsed;

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
            /*
            
            switch (currentPane)
            {
                //These actions indicate a change has occured which requires a window re-jig.
                case PaneMode.CategoryView:
                case PaneMode.TagView:
                case PaneMode.ViewView:
                case PaneMode.Upload:
                case PaneMode.Account:
                    //RefreshPanesLoadingState(mode, null, false);
                    RefreshOverallPanesStructure();
                    break;
            }
            */
            

            switch (mode)
            {

                #region Category
                /*    
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
                 */
                #endregion

                #region Tag
                case PaneMode.TagView:
                    gridTag.RowDefinitions[1].MaxHeight = 34;
                    gridTag.RowDefinitions[2].MaxHeight = 0;
                    gridTag.RowDefinitions[3].MaxHeight = 0;
                    gridTag.RowDefinitions[4].MaxHeight = 0;

                    RefreshAndDisplayTagList(false);

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

                    break;
                case PaneMode.ViewAdd:

                    break;
                #endregion

                #region Upload
                case PaneMode.Upload:
                    /*
                    if (uploadUIState.Uploading == true)
                    {
                        lstUploadImageFileList.IsEnabled = false;
                        cmdUploadImportFiles.Visibility = Visibility.Hidden;
                        cmdUploadImportFolder.Visibility = Visibility.Hidden;
                        cmdUploadClear.IsEnabled = true;
                        cmdUploadClear.Content = "Cancel Uploads";
                        break;
                    }

                    //Initialise upload controls, no state to consider.
                    if (uploadUIState.Mode == UploadUIState.UploadMode.None)
                    {
                        //Tag Setup
                        gridTag.RowDefinitions[1].MaxHeight = 34;
                        gridTag.RowDefinitions[2].MaxHeight = 0;
                        gridTag.RowDefinitions[3].MaxHeight = 0;
                        gridTag.RowDefinitions[4].MaxHeight = 0;
                        cmdAssociateTag.Visibility = Visibility.Visible;
                        cmdAddTag.Visibility = Visibility.Collapsed;
                        cmdEditTag.Visibility = Visibility.Collapsed;
                        wrapMyTags.IsEnabled = false;
                        cmdAssociateTag.IsEnabled = false;
                        RefreshAndDisplayTagList(false);

                        //Upload controls setup
                        lstUploadImageFileList.IsEnabled = false;
                        cmdUploadImportFolder.Visibility = Visibility.Visible;
                        cmdUploadImportFiles.Visibility = Visibility.Visible;
                        cmdUploadClear.Visibility = Visibility.Collapsed;
                        grdUploadSettings.RowDefinitions[2].MaxHeight = 0;
                        grdUploadSettings.RowDefinitions[3].MaxHeight = 0;
                        grdUploadSettings.RowDefinitions[4].MaxHeight = 0;
                        grdUploadSettings.RowDefinitions[5].MaxHeight = 0;
                        cmdUploadAll.IsEnabled = false;
                        tabUploadImageDetails.IsEnabled = false;
                    }
                    else
                    {
                        //Upload has been initialised, set controls to reflect upload options.
                        lstUploadImageFileList.IsEnabled = true;
                        tabUploadImageDetails.IsEnabled = true;
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
                        cmdUploadClear.Content = "Clear";
                        cmdUploadClear.IsEnabled = true;
                        cmdUploadClear.IsEnabled = true;
                        cmdUploadClear.Visibility = Visibility.Visible;

                        //Enable Tags
                        wrapMyTags.IsEnabled = true;
                        cmdAssociateTag.IsEnabled = true;


                        if (uploadUIState.Mode == UploadUIState.UploadMode.Images)
                        {
                            grdUploadImageDetails.RowDefinitions[0].MaxHeight = 0; //Sub category marker
                            grdUploadSettings.RowDefinitions[2].MaxHeight = 0; //Map to sub folders
                            chkUploadMapToSubFolders.IsEnabled = false;

                            cmdUploadImportFolder.Visibility = Visibility.Hidden;
                        }
                        else if (uploadUIState.Mode == UploadUIState.UploadMode.Folder)
                        {

                            if (uploadUIState.GotSubFolders)
                            {
                                grdUploadImageDetails.RowDefinitions[0].MaxHeight = 25; //Sub category marker
                                grdUploadSettings.RowDefinitions[2].MaxHeight = 25; //Maintain sub folders.
                                chkUploadMapToSubFolders.IsEnabled = true;
                            }
                            else
                            {
                                grdUploadImageDetails.RowDefinitions[0].MaxHeight = 0; //Sub category marker
                                grdUploadSettings.RowDefinitions[2].MaxHeight = 0; //Map to sub folders
                                chkUploadMapToSubFolders.IsEnabled = false;
                            }
                            cmdUploadImportFiles.Visibility = Visibility.Hidden;
                        }
                    }
                    */
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

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void cmdAddTag_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmdEditTag_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmdAssociateTag_Click(object sender, RoutedEventArgs e)
        {

        }

        private void txtTagAddEditName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void cmdAddEditTagSave_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmdAddEditTagCancel_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmdAddEditTagDelete_Click(object sender, RoutedEventArgs e)
        {

        }

        private void cmdTagRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAndDisplayTagList(true);
        }




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

                string response = await controller.RefreshTagsListAsync();
                if (response != "OK")
                {
                    DisplayMessage(response, MessageSeverity.Error);
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
            RadioButton checkedButton = (RadioButton)sender;
            //RadioButton checkedButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();

            /* Refresh tag image state */
            if (checkedButton != null)
            {
                TagListTagRef tagListTagRefTemp = (TagListTagRef)checkedButton.Tag;
                currentTagImageList = await controller.GetTagImagesAsync(tagListTagRefTemp.id, tagListTagRefTemp.name, 0, GetTagSearchQueryString());

                /* Populate tag image list from state */
                TagImageListUpdateControls();
            }


        }

        async private Task FetchMoreTagImagesAsync(FetchDirection direction)
        {
            if (currentTagImageList == null)
                return;

            /* Update current tag image list */
            int cursor = 0;
            switch (direction)
            {
                case FetchDirection.Begin:
                    cursor = 0;
                    break;
                case FetchDirection.Next:
                    if ((currentTagImageList.imageCursor + state.imageFetchSize) <= currentTagImageList.totalImageCount)
                        cursor = currentTagImageList.imageCursor + state.imageFetchSize;
                    break;
                case FetchDirection.Previous:
                    cursor = Math.Max(currentTagImageList.imageCursor - state.imageFetchSize, 0);
                    break;
                case FetchDirection.Last:
                    cursor = Math.Abs(currentTagImageList.totalImageCount / state.imageFetchSize) * state.imageFetchSize;
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

            if (currentTagImageList.imageCursor == 0)
            {
                cmdImageNavigationBegin.IsEnabled = false;
                cmdImageNavigationPrevious.IsEnabled = false;
            }
            else
            {
                cmdImageNavigationBegin.IsEnabled = true;
                cmdImageNavigationPrevious.IsEnabled = true;
            }

            if ((currentTagImageList.imageCursor + state.imageFetchSize) > currentTagImageList.totalImageCount)
            {
                cmdImageNavigationLast.IsEnabled = false;
                cmdImageNavigationNext.IsEnabled = false;
            }
            else
            {
                cmdImageNavigationLast.IsEnabled = true;
                cmdImageNavigationNext.IsEnabled = true;
            }
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
                    DisplayMessage("Account: " + state.userName + " has been connected with FotoWalla", MessageSeverity.Info);
                    cmdCategory.RaiseEvent(new RoutedEventArgs(CheckBox.CheckedEvent));
                    break;
                case GlobalState.ConnectionState.Offline:
                    DisplayMessage("No internet connection could be established with FotoWalla", MessageSeverity.Warning);
                    cmdCategory.RaiseEvent(new RoutedEventArgs(CheckBox.CheckedEvent));
                    break;
                case GlobalState.ConnectionState.NoAccount:
                    DisplayMessage("There is no account settings saved for this user, you must associate an account", MessageSeverity.Info);

                    cmdAccount.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                    break;
                case GlobalState.ConnectionState.FailedLogin:
                    DisplayMessage("The logon for account: " + state.userName + ", failed with the message: " + response, MessageSeverity.Warning);
                    cmdAccount.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    break;
            }

            DisplayConnectionStatus();
        }

        public void DisplayMessage(string message, MessageSeverity severity)
        {
            //TODO sort out severity.
            MessageBox.Show(message);
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

        async private void cmdImageNavigationLast_Click(object sender, RoutedEventArgs e)
        {
            await FetchMoreTagImagesAsync(FetchDirection.Last);
        }

        async private void cmdImageNavigationNext_Click(object sender, RoutedEventArgs e)
        {
            await FetchMoreTagImagesAsync(FetchDirection.Next);
        }

        async private void cmdImageNavigationPrevious_Click(object sender, RoutedEventArgs e)
        {
            await FetchMoreTagImagesAsync(FetchDirection.Previous);
        }

        async private void cmdImageNavigationBegin_Click(object sender, RoutedEventArgs e)
        {
            await FetchMoreTagImagesAsync(FetchDirection.Begin);
        }


    }
}
