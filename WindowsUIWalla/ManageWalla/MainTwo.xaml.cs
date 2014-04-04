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
using System.IO;
using System.Collections;
using System.Threading;
using System.Windows.Media.Animation; 
using System.Runtime.Serialization.Formatters.Binary;

namespace ManageWalla
{
    /// <summary>
    /// Interaction logic for MainTwo.xaml
    /// </summary>
    public partial class MainTwo : Window
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
            GalleryView = 6,
            GalleryEdit = 7,
            GalleryAdd = 8,
            Upload = 9,
            Account = 10,
            AccountEdit = 11,
            ImageView = 12,
            ImageEdit = 13
        }

        private enum FetchDirection
        {
            Begin = 0,
            Last = 1,
            Next = 2,
            Previous = 3
        }

        public enum MessageType
        {
            None = 0,
            Busy = 1,
            Info = 2,
            Warning = 3,
            Error = 4,
            Other = 5
        }

        private PaneMode currentPane;
        private PaneMode previousPane;
        private Tag currentTag = null;
        private Gallery currentGallery = null;
        private Category currentCategory = null;
        private MainController controller = null;
        public UploadUIState uploadUIState = null;
        public UploadImageFileList uploadFots = null;
        public UploadImageStateList uploadImageStateList = null;
        public ImageMainViewerList imageMainViewerList = null;
        public GalleryCategoryModel galleryCategoriesList = null;
        public GlobalState state = null;
        public List<ThumbCache> thumbCacheList = null;
        public List<MainCopyCache> mainCopyCacheList = null;
         
        public ImageList currentImageList = null;
        private bool tagListUploadRefreshing = false;
        public CancellationTokenSource cancelTokenSource = null;
        public CancellationTokenSource cancelUploadTokenSource = null;
        private static readonly ILog logger = LogManager.GetLogger(typeof(MainTwo));
        private double previousImageSize = 0.0;
        private bool tweakMainImageSize = true;
        private bool tweakUploadImageSize = true;
        private bool startingApplication = true;
        private DateTime lastMarginTweakTime = DateTime.Now;

        private MessageType currentDialogType = MessageType.None;
        #endregion

        #region Window Initialise and control.
        public MainTwo()
        {
            InitializeComponent();

            uploadFots = (UploadImageFileList)FindResource("uploadImagefileListKey");
            uploadUIState = (UploadUIState)FindResource("uploadUIStateKey");
            uploadImageStateList = (UploadImageStateList)FindResource("uploadImageStateListKey");
            imageMainViewerList = (ImageMainViewerList)FindResource("imageMainViewerListKey");
            galleryCategoriesList = (GalleryCategoryModel)FindResource("galleryCategoryModelKey");
        }

        private void mainTwo_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            controller.Dispose();
        }

        async private void mainTwo_Loaded(object sender, RoutedEventArgs e)
        {
            controller = new MainController(this);

            try
            {
                paneBusy.Opacity = 1.0;
                paneBusy.Visibility = Visibility.Visible;
                currentDialogType = MessageType.Other;
                ShowMessage(MessageType.Busy, "Loading fotowalla");

                controller.InitApplication();

                /*
                if (!await controller.CheckOnline())
                {

                    ShowMessage(MessageType.Warning, "No internet connection could be established.  Working with local data only");
                    state.connectionState = GlobalState.ConnectionState.Offline;
                }

                if (!await controller.VerifyApp())
                {
                    throw new Exception("The application failed validation with the server.  Please check for the latest update.");
                }
                 */

                string profileName = Properties.Settings.Default.LastUser;
                if (profileName.Length > 0)
                {
                    await Initialise(profileName, "", false);
                }
                else
                {
                    state.connectionState = GlobalState.ConnectionState.NoAccount;
                }

                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {
                    radGallery.IsChecked = true;
                    ResetUploadState();
                }
                else
                {
                    currentPane = PaneMode.GalleryView;
                    cmdAccount.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                }

                ConcludeBusyProcess();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "There was an unexpected error starting the application and must now close.  Error was: " + ex.Message);
                //Application.Current.Shutdown();
            }
        }

        async private Task Initialise(string profileName, string password, bool onAccountForm)
        {
            controller.SetUpCacheFiles(profileName, uploadImageStateList);
            state = controller.GetState();
            thumbCacheList = controller.GetThumbCacheList();
            mainCopyCacheList = controller.GetMainCopyCacheList();

            if (!await controller.CheckOnline())
            {
                if (state.account.ProfileName.Length > 0)
                {
                    ShowMessage(MessageType.Warning, "No internet connection could be established.  Working with local data only");
                }
                else
                {
                    state.connectionState = GlobalState.ConnectionState.NoAccount;
                }
                return;
            }

            if (!await controller.VerifyApp())
            {
                throw new Exception("The application failed validation with the server.  Please check for the latest app version.");
            }

            if (!onAccountForm)
            {
                password = state.account.Password;
            }

            await Login(profileName, password);
        }

        public void UserConcludeProcess()
        {
            Dispatcher.Invoke(UserConcludeProcessApply);
        }

        public void ConcludeBusyProcess()
        {
            Dispatcher.Invoke(ConcludeBusyProcessApply);
        }

        public void ShowMessage(MessageType messageType, string message)
        {

            Dispatcher.Invoke(new Action(() => { UpdateDialogsAndShow(messageType, message); }));


            //cmdAlertDialogResponse.Click += cmdDialogResponse_Click;
            /*
            if (messageType == MessageType.Info)
            {
                
            }
            else
            {
                Dispatcher.Invoke(new Action(() => { UpdateAlertDialog(messageType, message); }));
            }
            Dispatcher.Invoke(new Action(() => { ShowBusyApply(messageType); }));
             */
        }

        private void ConcludeBusyProcessApply()
        {
            if (currentDialogType == MessageType.Busy)
            {
                gridAlertDialog.Visibility = Visibility.Collapsed;
                paneBusy.Visibility = Visibility.Collapsed;
                currentDialogType = MessageType.None;
            }
        }

        private void UserConcludeProcessApply()
        {
            if (currentDialogType == MessageType.Busy)
            {
                //If a valid cancellation is live, then cancel it.
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();
            }

            gridAlertDialog.Visibility = Visibility.Collapsed;
            paneBusy.Visibility = Visibility.Collapsed;
            currentDialogType = MessageType.None;
        }

        private void UpdateDialogsAndShow(MessageType messageType, string message)
        {
            switch (messageType)
            {
                case MessageType.Info:
                    if (currentDialogType == MessageType.Busy)
                    {
                        ConcludeBusyProcessApply();
                    }

                    if (currentDialogType != MessageType.None) { return; }

                    lblInfoDialogMessage.Text = message;

                    gridInfoAlert.BeginAnimation(Grid.OpacityProperty, null);
                    gridInfoAlert.BeginAnimation(Grid.VisibilityProperty, null);
                    gridInfoAlert.Opacity = 0.0;
                    gridInfoAlert.Visibility = Visibility.Collapsed;

                    DoubleAnimationUsingKeyFrames opacityFrameAnimInfo = new DoubleAnimationUsingKeyFrames();
                    opacityFrameAnimInfo.FillBehavior = FillBehavior.HoldEnd;
                    opacityFrameAnimInfo.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(2.0)));
                    opacityFrameAnimInfo.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(6.0)));
                    opacityFrameAnimInfo.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromSeconds(7.0)));
                    gridInfoAlert.BeginAnimation(Border.OpacityProperty, opacityFrameAnimInfo);

                    ObjectAnimationUsingKeyFrames visibilityAnimInfo = new ObjectAnimationUsingKeyFrames();
                    visibilityAnimInfo.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Visible, TimeSpan.FromSeconds(0.1)));
                    visibilityAnimInfo.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Collapsed, TimeSpan.FromSeconds(7.0)));
                    gridInfoAlert.BeginAnimation(Grid.VisibilityProperty, visibilityAnimInfo);

                    currentDialogType = MessageType.None;
                    break;
                case MessageType.Busy:
                    if (currentDialogType == MessageType.Busy) { return; }

                    if (currentDialogType != MessageType.Other)
                    {
                        //Special clause, if moving from dialog box to busy.
                        paneBusy.BeginAnimation(Border.OpacityProperty, null);
                        paneBusy.Opacity = 0.0;
                        paneBusy.Visibility = Visibility.Visible;

                        DoubleAnimationUsingKeyFrames opacityBusyFrameAnim = new DoubleAnimationUsingKeyFrames();
                        opacityBusyFrameAnim.FillBehavior = FillBehavior.HoldEnd;
                        opacityBusyFrameAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromSeconds(2.0)));
                        opacityBusyFrameAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.5, TimeSpan.FromSeconds(4.0)));
                        paneBusy.BeginAnimation(Border.OpacityProperty, opacityBusyFrameAnim);
                    }

                    currentDialogType = messageType;
                    lblAlertDialogMessage.Text = message;
                    lblAlertDialogHeader.Text = "Processing request...";
                    cmdAlertDialogResponse.Content = "Cancel";
                    imgAlert.Visibility = Visibility.Collapsed;
                    imgBusy.Visibility = Visibility.Visible;
                    gridAlertDialog.BeginAnimation(Grid.OpacityProperty, null);
                    gridAlertDialog.Opacity = 0.0;
                    gridAlertDialog.Visibility = Visibility.Visible;

                    DoubleAnimationUsingKeyFrames opacityBusyActionsAnim = new DoubleAnimationUsingKeyFrames();
                    opacityBusyActionsAnim.FillBehavior = FillBehavior.HoldEnd;
                    opacityBusyActionsAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.0, TimeSpan.FromSeconds(2.0)));
                    opacityBusyActionsAnim.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(2.5)));
                    gridAlertDialog.BeginAnimation(Grid.OpacityProperty, opacityBusyActionsAnim);

                    break;
                case MessageType.Warning:
                case MessageType.Error:
                    if (currentDialogType == MessageType.Warning || currentDialogType == MessageType.Error) { return; }

                    currentDialogType = messageType;
                    paneBusy.BeginAnimation(Border.OpacityProperty, null);
                    gridAlertDialog.BeginAnimation(Grid.OpacityProperty, null);

                    lblAlertDialogMessage.Text = message;
                    cmdAlertDialogResponse.Content = "OK";

                    imgAlert.Visibility = Visibility.Visible;
                    imgBusy.Visibility = Visibility.Collapsed;
                    if (messageType == MessageType.Warning)
                    {
                        imgAlert.Source = (ImageSource)FindResource("warningImageSrc");
                        lblAlertDialogHeader.Text = "Warning";
                    }
                    else
                    {
                        imgAlert.Source = (ImageSource)FindResource("errorImageSrc");
                        lblAlertDialogHeader.Text = "Error";
                    }

                    if (currentDialogType == MessageType.Busy)
                    {
                        //Existing working dialog, switch to Error\Warning without animations.
                        gridAlertDialog.Opacity = 1.0;
                        gridAlertDialog.Visibility = Visibility.Visible;
                        paneBusy.Opacity = 0.5;
                        paneBusy.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        paneBusy.Opacity = 0.0;
                        paneBusy.Visibility = Visibility.Visible;
                        gridAlertDialog.Opacity = 0.0;
                        gridAlertDialog.Visibility = Visibility.Visible;

                        DoubleAnimationUsingKeyFrames opacityFrameAnim = new DoubleAnimationUsingKeyFrames();
                        opacityFrameAnim.FillBehavior = FillBehavior.HoldEnd;
                        opacityFrameAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.5, TimeSpan.FromSeconds(0.5)));
                        paneBusy.BeginAnimation(Border.OpacityProperty, opacityFrameAnim);

                        DoubleAnimationUsingKeyFrames opacityActionsAnim = new DoubleAnimationUsingKeyFrames();
                        opacityActionsAnim.FillBehavior = FillBehavior.HoldEnd;
                        opacityActionsAnim.KeyFrames.Add(new LinearDoubleKeyFrame(1.0, TimeSpan.FromSeconds(0.5)));
                        gridAlertDialog.BeginAnimation(Grid.OpacityProperty, opacityActionsAnim);
                    }
                    break;
                case MessageType.Other:
                    currentDialogType = MessageType.Other;

                    paneBusy.BeginAnimation(Border.OpacityProperty, null);
                    paneBusy.Opacity = 0.0;
                    paneBusy.Visibility = Visibility.Visible;

                    DoubleAnimationUsingKeyFrames opacityPaneFrameAnim = new DoubleAnimationUsingKeyFrames();
                    opacityPaneFrameAnim.FillBehavior = FillBehavior.HoldEnd;
                    opacityPaneFrameAnim.KeyFrames.Add(new LinearDoubleKeyFrame(0.5, TimeSpan.FromSeconds(2.0)));
                    paneBusy.BeginAnimation(Border.OpacityProperty, opacityPaneFrameAnim);
                    break;
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

        private void cmdAlertDialogResponse_Click(object sender, RoutedEventArgs e)
        {
            UserConcludeProcess();
        }

        private void RefreshOverallPanesStructure(PaneMode mode)
        {
            //Ensure panes are all correctly setup each time a refresh is called.
            gridLeft.ColumnDefinitions[0].Width = new GridLength(0); //Sidebar
            gridLeft.ColumnDefinitions[1].Width = new GridLength(300); //Main control
            gridLeft.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star); //Image display grid
            gridLeft.ColumnDefinitions[3].Width = new GridLength(0);
            gridRight.RowDefinitions[0].Height = new GridLength(60); //Working Pane
            gridRight.ColumnDefinitions[1].Width = new GridLength(0);
            grdImageView.Visibility = Visibility.Collapsed;

            switch (mode)
            {
                case PaneMode.GalleryView:
                    panGalleryUnavailable.Visibility = Visibility.Visible;
                    panCategoryUnavailable.Visibility = Visibility.Collapsed;
                    panTagUnavailable.Visibility = Visibility.Collapsed;
                    lstImageMainViewerList.Visibility = Visibility.Visible;
                    lstUploadImageFileList.Visibility = Visibility.Collapsed;
                    panUpload.Visibility = System.Windows.Visibility.Collapsed;

                    gridLeft.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[4].Height = new GridLength(0);
                    gridLeft.RowDefinitions[6].Height = new GridLength(0);
                    gridLeft.RowDefinitions[8].Height = new GridLength(0);

                    //gridRight.RowDefinitions[0].Height = new GridLength(40);
                    gridRight.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);

                    gridGallerySelection.Visibility = Visibility.Collapsed;
                    tabGalleryConfiguration.Visibility = Visibility.Collapsed;
                    panGridRightHeader.Visibility = Visibility.Visible;
                    break;
                case PaneMode.GalleryEdit:
                case PaneMode.GalleryAdd:
                    gridRight.RowDefinitions[0].Height = new GridLength(2, GridUnitType.Star);
                    gridRight.RowDefinitions[1].Height = new GridLength(2, GridUnitType.Star);

                    lstImageMainViewerList.Visibility = Visibility.Collapsed;

                    gridGallerySelection.Visibility = Visibility.Visible;
                    tabGalleryConfiguration.Visibility = Visibility.Visible;
                    panGridRightHeader.Visibility = Visibility.Collapsed;
                    break;
                case PaneMode.CategoryView:
                case PaneMode.CategoryAdd:
                case PaneMode.CategoryEdit:
                    panCategoryUnavailable.Visibility = Visibility.Visible;
                    panTagUnavailable.Visibility = Visibility.Collapsed;
                    panGalleryUnavailable.Visibility = Visibility.Collapsed;
                    lstImageMainViewerList.Visibility = Visibility.Visible;
                    lstUploadImageFileList.Visibility = Visibility.Collapsed;
                    panUpload.Visibility = System.Windows.Visibility.Collapsed;

                    panGridRightHeader.Visibility = Visibility.Visible;
                    gridGallerySelection.Visibility = Visibility.Collapsed;
                    tabGalleryConfiguration.Visibility = Visibility.Collapsed;

                    gridLeft.RowDefinitions[2].Height = new GridLength(0);
                    gridLeft.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);
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
                    panGridRightHeader.Visibility = Visibility.Visible;

                    gridLeft.RowDefinitions[2].Height = new GridLength(0);
                    gridLeft.RowDefinitions[4].Height = new GridLength(0);
                    gridLeft.RowDefinitions[6].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[8].Height = new GridLength(0);
                    break;
                case PaneMode.Upload:
                    panCategoryUnavailable.Visibility = Visibility.Collapsed;
                    panTagUnavailable.Visibility = Visibility.Collapsed;
                    panGalleryUnavailable.Visibility = Visibility.Collapsed;
                    lstImageMainViewerList.Visibility = Visibility.Collapsed;
                    lstUploadImageFileList.Visibility = Visibility.Visible;
                    panUpload.Visibility = System.Windows.Visibility.Visible;

                    gridRight.RowDefinitions[0].Height = new GridLength(0); //Working Pane

                    if (uploadFots.Count > 0 && uploadUIState.Mode != UploadUIState.UploadMode.Auto && uploadUIState.Uploading == false)
                        gridRight.ColumnDefinitions[1].Width = new GridLength(255);
                    
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
                case PaneMode.ImageView:
                case PaneMode.ImageEdit:
                    //Switch to full width mode.
                    gridLeft.ColumnDefinitions[0].Width = new GridLength(40);
                    gridLeft.ColumnDefinitions[1].Width = new GridLength(0);
                    gridRight.RowDefinitions[0].Height = new GridLength(0); //Hide header
                    lstImageMainViewerList.Visibility = Visibility.Hidden;
                    grdImageView.Visibility = Visibility.Visible;
                    break;
            }
            TweakImageMarginSize(DateTime.Now, mode);
        }

        private void RefreshPanesAllControls(PaneMode mode)
        {
            switch (mode)
            {
                #region Category
                case PaneMode.CategoryView:
                    gridCategory.RowDefinitions[1].Height = new GridLength(48.0);
                    gridCategory.RowDefinitions[2].MaxHeight = 0;
                    gridCategory.RowDefinitions[3].MaxHeight = 0;
                    gridCategory.RowDefinitions[4].MaxHeight = 0;
                    gridCategory.RowDefinitions[5].Height = new GridLength(0.0);

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
                    gridCategory.RowDefinitions[1].Height = new GridLength(0.0);
                    gridCategory.RowDefinitions[2].MaxHeight = 30;
                    gridCategory.RowDefinitions[3].MaxHeight = 80;
                    gridCategory.RowDefinitions[4].MaxHeight = 30;
                    gridCategory.RowDefinitions[5].Height = new GridLength(48.0);

                    cmdCategoryCancel.Visibility = Visibility.Visible;
                    treeCategoryView.IsEnabled = false;

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
                    gridTag.RowDefinitions[2].Height = new GridLength(48.0);
                    gridTag.RowDefinitions[3].MaxHeight = 0;
                    gridTag.RowDefinitions[4].MaxHeight = 0;
                    gridTag.RowDefinitions[5].Height = new GridLength(0.0);

                    cmdTagAdd.Visibility = Visibility.Visible;
                    cmdTagEdit.Visibility = Visibility.Visible;
                    wrapMyTags.IsEnabled = true;
                    wrapSystemTags.IsEnabled = true;

                    radCategory.IsEnabled = true;
                    radGallery.IsEnabled = true;
                    radUpload.IsEnabled = true;
                    cmdAccount.IsEnabled = true;

                    break;
                case PaneMode.TagAdd:
                case PaneMode.TagEdit:
                    gridTag.RowDefinitions[2].Height = new GridLength(0.0);
                    gridTag.RowDefinitions[3].MaxHeight = 30;
                    gridTag.RowDefinitions[4].MaxHeight = 80;
                    gridTag.RowDefinitions[5].Height = new GridLength(48.0);
                    wrapMyTags.IsEnabled = false;
                    wrapSystemTags.IsEnabled = false;

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
                <!-- wrap galleries --> *
                <!-- add edit buttons --> 34
                <!-- Name --> 25
                <!-- Desc --> 81
                <!-- Access Type --> 25
                <!-- Password --> 25
                <!-- Save buttons --> 34
                    */
                    gridGallery.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);  //Working Pane
                    gridGallery.RowDefinitions[1].Height = new GridLength(48.0);
                    gridGallery.RowDefinitions[2].Height = new GridLength(0.0);
                    gridGallery.RowDefinitions[3].MaxHeight = 0;
                    gridGallery.RowDefinitions[4].MaxHeight = 0;
                    gridGallery.RowDefinitions[5].MaxHeight = 0;
                    gridGallery.RowDefinitions[6].Height = new GridLength(0);

                    radCategory.IsEnabled = true;
                    radTag.IsEnabled = true;
                    radUpload.IsEnabled = true;
                    cmdAccount.IsEnabled = true;
                    wrapMyGalleries.IsEnabled = true;

                    break;
                case PaneMode.GalleryEdit:
                case PaneMode.GalleryAdd:
                    gridGallery.RowDefinitions[0].Height = new GridLength(0);
                    gridGallery.RowDefinitions[1].Height = new GridLength(0);
                    gridGallery.RowDefinitions[2].Height = new GridLength(30.0);
                    gridGallery.RowDefinitions[3].MaxHeight = 80;
                    gridGallery.RowDefinitions[4].MaxHeight = 50;
                    gridGallery.RowDefinitions[5].MaxHeight = 30;
                    gridGallery.RowDefinitions[6].Height = new GridLength(1, GridUnitType.Star);

                    radCategory.IsEnabled = false;
                    radTag.IsEnabled = false;
                    radUpload.IsEnabled = false;
                    cmdAccount.IsEnabled = false;

                    wrapMyGalleries.IsEnabled = false;

                    if (cmbGallerySelectionType.SelectedIndex == 0)
                    {
                        //categories ONLY
                        lblGallerySelectionAndOr.Content = "";

                        gridGallerySelection.ColumnDefinitions[0].Width = new GridLength(2, GridUnitType.Star);
                        gridGallerySelection.ColumnDefinitions[1].Width = new GridLength(0);
                        gridGallerySelection.ColumnDefinitions[2].Width = new GridLength(0);
                        gridGallerySelection.ColumnDefinitions[3].Width = new GridLength(40);
                        gridGallerySelection.ColumnDefinitions[4].Width = new GridLength(2, GridUnitType.Star);

                    }
                    else if (cmbGallerySelectionType.SelectedIndex == 1)
                    {
                        //Tags only
                        lblGallerySelectionAndOr.Content = "";

                        gridGallerySelection.ColumnDefinitions[0].Width = new GridLength(0);
                        gridGallerySelection.ColumnDefinitions[1].Width = new GridLength(0);
                        gridGallerySelection.ColumnDefinitions[2].Width = new GridLength(2, GridUnitType.Star);
                        gridGallerySelection.ColumnDefinitions[3].Width = new GridLength(40);
                        gridGallerySelection.ColumnDefinitions[4].Width = new GridLength(2, GridUnitType.Star);

                    }
                    else
                    {
                        //Categories AND Tags only
                        if (cmbGallerySelectionType.SelectedIndex == 2)
                        {
                            lblGallerySelectionAndOr.Content = "AND";
                        }
                        else
                        {
                            //Categories OR Tags only
                            lblGallerySelectionAndOr.Content = "OR";
                        }

                        gridGallerySelection.ColumnDefinitions[0].Width = new GridLength(3, GridUnitType.Star);
                        gridGallerySelection.ColumnDefinitions[1].Width = new GridLength(40);
                        gridGallerySelection.ColumnDefinitions[2].Width = new GridLength(3, GridUnitType.Star);
                        gridGallerySelection.ColumnDefinitions[3].Width = new GridLength(40);
                        gridGallerySelection.ColumnDefinitions[4].Width = new GridLength(3, GridUnitType.Star);
                    }

                    if (cmbGalleryPresentationType.SelectedIndex == 0)
                    {
                        cmbGalleryGroupingType.IsEnabled = true;
                    }
                    else
                    {
                        cmbGalleryGroupingType.SelectedIndex = 0;
                        cmbGalleryGroupingType.IsEnabled = false;
                    }

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
                    /*
                    <RowDefinition Height="30" /> <!-- Upload Type -->
                    <RowDefinition Height="5" />
                    <RowDefinition Height="30" /> <!-- Auto folder details -->
                    <RowDefinition Height="30" />
                    <RowDefinition Height="30" /> <!-- Auto category details -->
                    <RowDefinition Height="30" />

                    <RowDefinition Height="40" /> <!-- 5 Root category -->
                    <RowDefinition Height="30" /> <!-- New Category -->
                    <RowDefinition Height="30" /> <!-- New category Name -->
                    <RowDefinition Height="80" /> <!-- New category Desc -->
                    <RowDefinition Height="60" /> <!-- Map sub Folders -->

                    <RowDefinition Height="*" />
                    <RowDefinition Height="50" />  <!-- Buttons -->
                    */

                    cmbGallerySectionVert.Visibility = Visibility.Collapsed;
                    cmbGallerySection.Visibility = Visibility.Collapsed;

                    if (uploadUIState.Uploading == true)
                    {
                        grdUploadSettings.RowDefinitions[2].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[3].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[4].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[5].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[6].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[7].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[8].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[9].Height = new GridLength(0);

                        lstUploadImageFileList.IsEnabled = false;
                        cmdUploadImportFiles.Visibility = Visibility.Collapsed;
                        cmdUploadImportFolder.Visibility = Visibility.Collapsed;
                        cmdUploadClear.Visibility = Visibility.Visible;
                        cmdUploadClear.IsEnabled = true;
                        cmdUploadChangeCategory.Visibility = Visibility.Collapsed;
                        if (uploadUIState.Mode != UploadUIState.UploadMode.Auto)
                        {
                            //cmdUploadChangeCategory.IsEnabled = true;
                            //grdUploadSettings.RowDefinitions[9].Height = new GridLength(60);
                            grdUploadSettings.RowDefinitions[5].Height = new GridLength(40);

                            lblUploadType.Content = "Uploading images...";
                            cmdUploadClear.Content = "Pause Upload";
                        }
                        else
                        {
                            grdUploadSettings.RowDefinitions[2].Height = new GridLength(30.0);
                            grdUploadSettings.RowDefinitions[3].Height = new GridLength(30.0);
                            grdUploadSettings.RowDefinitions[4].Height = new GridLength(30.0);

                            lblUploadType.Content = "Uploading images (auto)...";
                            cmdUploadClear.Content = "Pause Upload";
                            cmdUploadTurnAutoOff.Visibility = Visibility.Collapsed;
                        }
                        break;
                    }



                    //Initialise upload controls, no state to consider.
                    if (uploadUIState.Mode == UploadUIState.UploadMode.None)
                    {
                        lblUploadType.Content = "Choose images or folders to upload";
                        grdUploadSettings.RowDefinitions[2].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[3].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[4].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[5].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[6].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[7].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[8].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[9].Height = new GridLength(0);

                        //Upload controls setup
                        lstUploadImageFileList.IsEnabled = false;
                        cmdUploadImportFolder.Visibility = Visibility.Visible;
                        cmdUploadImportFiles.Visibility = Visibility.Visible;
                        cmdUploadClear.Visibility = Visibility.Collapsed;
                        cmdUploadTurnAutoOff.Visibility = Visibility.Collapsed;
                        cmdUploadAll.Visibility = Visibility.Visible;
                        cmdUploadAll.IsEnabled = false;
                        panUpload.IsEnabled = false;
                    }
                    else if (uploadUIState.Mode == UploadUIState.UploadMode.Auto)
                    {
                        lblUploadType.Content = "Auto upload - paused";
                        grdUploadSettings.RowDefinitions[2].Height = new GridLength(30.0);
                        grdUploadSettings.RowDefinitions[3].Height = new GridLength(30.0);
                        grdUploadSettings.RowDefinitions[4].Height = new GridLength(30.0);
                        grdUploadSettings.RowDefinitions[5].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[6].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[7].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[8].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[9].Height = new GridLength(0);

                        cmdUploadAll.Visibility = Visibility.Collapsed;
                        cmdUploadClear.Content = "Resume";
                        cmdUploadTurnAutoOff.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        grdUploadSettings.RowDefinitions[2].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[3].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[4].Height = new GridLength(0);
                        grdUploadSettings.RowDefinitions[5].Height = new GridLength(40);
                        if (uploadUIState.UploadToNewCategory)
                        {
                            grdUploadSettings.RowDefinitions[7].Height = new GridLength(30.0); //Category Name
                            grdUploadSettings.RowDefinitions[8].Height = new GridLength(80.0); //Category Description
                        }
                        else
                        {
                            grdUploadSettings.RowDefinitions[7].Height = new GridLength(0);
                            grdUploadSettings.RowDefinitions[8].Height = new GridLength(0);
                        }
                        grdUploadSettings.RowDefinitions[6].Height = new GridLength(30);
                        grdUploadSettings.RowDefinitions[9].Height = new GridLength(30);

                        //Upload has been initialised, set controls to reflect upload options.
                        lstUploadImageFileList.IsEnabled = true;
                        panUpload.IsEnabled = true;

                        cmdUploadAll.Visibility = Visibility.Visible;
                        cmdUploadAll.IsEnabled = true;
                        cmdUploadClear.Content = "Clear";
                        cmdUploadClear.IsEnabled = true;
                        cmdUploadClear.Visibility = Visibility.Visible;
                        cmdUploadTurnAutoOff.Visibility = Visibility.Collapsed;
                        //cmdUploadChangeCategory.IsEnabled = true;
                        cmdUploadChangeCategory.Visibility = Visibility.Visible;
                        //Enable Tags
                        //wrapMyTags.IsEnabled = true;
                        //cmdAssociateTag.IsEnabled = true;

                        if (uploadUIState.Mode == UploadUIState.UploadMode.Images)
                        {
                            lblUploadType.Content = "Upload - Files";
                            grdUploadSettings.RowDefinitions[9].Height = new GridLength(0); //Map to sub folders
                            chkUploadMapToSubFolders.IsChecked = false;

                            cmdUploadImportFolder.Visibility = Visibility.Hidden;
                        }
                        else if (uploadUIState.Mode == UploadUIState.UploadMode.Folder)
                        {
                            lblUploadType.Content = "Upload - Folders";
                            if (uploadUIState.GotSubFolders)
                            {
                                grdUploadSettings.RowDefinitions[9].Height = new GridLength(60);
                            }
                            else
                            {
                                grdUploadSettings.RowDefinitions[9].Height = new GridLength(0); //Map to sub folders
                                chkUploadMapToSubFolders.IsChecked = false;
                            }
                            cmdUploadImportFiles.Visibility = Visibility.Hidden;
                        }
                    }

                    break;
                #endregion

                #region Account
                case PaneMode.Account:
                    if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                    {
                        //tabAccount.IsEnabled = true;
                        //cmdAccountClose.IsEnabled = true;
                        cmdUserAppEdit.Visibility = Visibility.Visible;
                        cmdUserAppSave.Visibility = Visibility.Collapsed;
                        cmdUserAppCancel.Visibility = Visibility.Collapsed;
                        cmdAccountLogin.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        //tabAccount.IsEnabled = false;
                        //cmdAccountClose.IsEnabled = false;
                        cmdUserAppEdit.Visibility = Visibility.Collapsed;
                        cmdUserAppCancel.Visibility = Visibility.Collapsed;
                        cmdUserAppSave.Visibility = Visibility.Collapsed;
                        cmdAccountLogin.Visibility = Visibility.Visible;
                    }

                    chkAccountAutoUpload.IsEnabled = false;
                    sldAccountImageCopySize.IsEnabled = false;
                    cmdAccountChangeAutoUploadFolder.IsEnabled = false;
                    cmdAccountChangeImageCopyFolder.IsEnabled = false;

                    break;
                case PaneMode.AccountEdit:
                    //tabAccount.IsEnabled = false;
                    //cmdAccountClose.IsEnabled = false;
                    cmdUserAppEdit.Visibility = Visibility.Collapsed;
                    cmdUserAppCancel.Visibility = Visibility.Visible;
                    cmdUserAppSave.Visibility = Visibility.Visible;
                    cmdAccountLogin.Visibility = Visibility.Collapsed;

                    chkAccountAutoUpload.IsEnabled = true;
                    sldAccountImageCopySize.IsEnabled = true;
                    cmdAccountChangeAutoUploadFolder.IsEnabled = true;
                    cmdAccountChangeImageCopyFolder.IsEnabled = true;
                    break;
                #endregion

                #region Image
                case PaneMode.ImageView:
                case PaneMode.ImageEdit:
                    panNavigationVert.Visibility = Visibility.Hidden;
                    lblImageViewNameVert.Visibility = Visibility.Visible;
                    lblImageListNameVert.Visibility = Visibility.Collapsed;
                    cmbGallerySectionVert.Visibility = Visibility.Collapsed;

                    if (cmdImageViewDetailToggle.IsChecked == true)
                    {
                        grdImageView.ColumnDefinitions[1].Width = new GridLength(240);
                    }
                    else
                    {
                        grdImageView.ColumnDefinitions[1].Width = new GridLength(0);
                    }

                    if (mode == PaneMode.ImageEdit)
                    {
                        txtImageViewName.IsEnabled = true;
                        txtImageViewDescription.IsEnabled = true;
                        datImageViewDate.IsEnabled = true;
                        cmdImageViewEdit.Content = "Save";
                        cmdImageViewCancel.IsEnabled = true;
                        cmdImageViewDetailToggle.IsEnabled = false;
                        cmdImageViewNext.IsEnabled = false;
                        cmdImageViewPrevious.IsEnabled = false;
                        lstImageViewTagList.IsEnabled = true;
                    }
                    else
                    {
                        txtImageViewName.IsEnabled = false;
                        txtImageViewDescription.IsEnabled = false;
                        datImageViewDate.IsEnabled = false;
                        cmdImageViewEdit.Content = "Edit";
                        cmdImageViewCancel.IsEnabled = false;
                        cmdImageViewDetailToggle.IsEnabled = true;
                        cmdImageViewNext.IsEnabled = true;
                        cmdImageViewPrevious.IsEnabled = true;
                        lstImageViewTagList.IsEnabled = false;
                    }

                    break;
                #endregion
            }

            currentPane = mode;
           // TweakImageMarginSize(DateTime.Now, currentPane);
        }
        #endregion

        #region Pane Control Event Handelers
        async private void radCategory_Checked(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(PaneMode.CategoryView);
            RefreshPanesAllControls(PaneMode.CategoryView);
            await RefreshAndDisplayCategoryList(false);
        }

        private void radUpload_Checked(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(PaneMode.Upload);
            RefreshPanesAllControls(PaneMode.Upload);
        }

        async private void radGallery_Checked(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(PaneMode.GalleryView);
            RefreshPanesAllControls(PaneMode.GalleryView);
            await RefreshAndDisplayGalleryList(false);


            if (startingApplication)
            {
                await RefreshAndDisplayTagList(false);
                await RefreshAndDisplayCategoryList(false);

                foreach (RadioButton button in wrapMyGalleries.Children.OfType<RadioButton>())
                {
                    GalleryListGalleryRef galleryRef = (GalleryListGalleryRef)button.Tag;
                    long tempGalleryId = 400001;
                    if (galleryRef.id == tempGalleryId)
                    {
                        button.IsChecked = true;
                    }
                    continue;
                }

                startingApplication = false;
            }
        }

        async private void radTag_Checked(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(PaneMode.TagView);
            RefreshPanesAllControls(PaneMode.TagView);
            await RefreshAndDisplayTagList(false);
        }

        private void cmdAccount_Click(object sender, RoutedEventArgs e)
        {
            previousPane = currentPane;
            RefreshOverallPanesStructure(PaneMode.Account);
            RefreshPanesAllControls(PaneMode.Account);
        }

        private void cmdAccountClose_Click(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(previousPane);
            RefreshPanesAllControls(previousPane);
        }

        private void cmdContract_Click(object sender, RoutedEventArgs e)
        {
            panNavigationVert.Visibility = Visibility.Visible;
            lblImageViewNameVert.Visibility = Visibility.Collapsed;
            lblImageListNameVert.Visibility = Visibility.Visible;
            //cmbGallerySectionVert.Visibility = Visibility.Visible;

            gridLeft.ColumnDefinitions[0].Width = new GridLength(40);
            gridLeft.ColumnDefinitions[1].Width = new GridLength(0);

            gridRight.RowDefinitions[0].Height = new GridLength(0);
        }

        private void cmdExpand_Click(object sender, RoutedEventArgs e)
        {
            if (currentPane == PaneMode.ImageEdit || currentPane == PaneMode.ImageView)
            {
                RefreshPanesAllControls(previousPane);
                RefreshOverallPanesStructure(currentPane);
            }
            else
            {
                gridLeft.ColumnDefinitions[0].Width = new GridLength(0);
                gridLeft.ColumnDefinitions[1].Width = new GridLength(300);
                gridRight.RowDefinitions[0].Height = new GridLength(60);
            }
        }

        async private void ImageView_Click(object sender, RoutedEventArgs e)
        {
            //GeneralImage current = this.lstImageMainViewerList.Items.CurrentItem as GeneralImage;

            //Deselect everything from this view.
            while (lstImageMainViewerList.SelectedItems.Count > 0)
            {
                ListBoxItem listBoxItem = this.lstImageMainViewerList.ItemContainerGenerator.ContainerFromItem(lstImageMainViewerList.SelectedItems[0]) as ListBoxItem;
                listBoxItem.IsSelected = false;
            }

            var buttonClicked = sender as Button;
            DependencyObject dependencyItem = buttonClicked;
            while (dependencyItem is ListBoxItem == false)
            {
                dependencyItem = VisualTreeHelper.GetParent(dependencyItem);
            }
            var clickedListBoxItem = (ListBoxItem)dependencyItem;
            clickedListBoxItem.IsSelected = true;

            previousPane = currentPane;
            cmdImageViewDetailToggle.IsChecked = false;

            RefreshPanesAllControls(PaneMode.ImageView);
            RefreshOverallPanesStructure(currentPane);

            ImageViewUpdateNextPrevious();
        }

        private void ImageViewDetails_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(currentPane);
            //if (currentPane == PaneMode.ImageView)
                //ImageViewTagsUpdateFromMeta();
        }

        async private void ImageViewUpdateNextPrevious()
        {
            GeneralImage current = (GeneralImage)lstImageMainViewerList.Items.CurrentItem;
            cancelTokenSource = new CancellationTokenSource();
            await current.LoadMainCopyImage(cancelTokenSource.Token, mainCopyCacheList, state.mainCopyFolder);
            await current.LoadMeta(false, cancelTokenSource.Token);
            ImageViewTagsUpdateFromMeta();

            if (lstImageMainViewerList.SelectedIndex == 0)
            {
                cmdImageViewPrevious.IsEnabled = false;
            }
            else
            {
                GeneralImage previous = (GeneralImage)lstImageMainViewerList.Items[lstImageMainViewerList.SelectedIndex - 1];
                previous.LoadMainCopyImage(cancelTokenSource.Token, mainCopyCacheList, state.mainCopyFolder);
                previous.LoadMeta(false, cancelTokenSource.Token);

                cmdImageViewPrevious.IsEnabled = true;
            }

            if (lstImageMainViewerList.SelectedIndex == lstImageMainViewerList.Items.Count -1)
            {
                cmdImageViewNext.IsEnabled = false;
            }
            else
            {
                GeneralImage next = (GeneralImage)lstImageMainViewerList.Items[lstImageMainViewerList.SelectedIndex + 1];
                next.LoadMainCopyImage(cancelTokenSource.Token, mainCopyCacheList, state.mainCopyFolder);
                next.LoadMeta(false, cancelTokenSource.Token);
                cmdImageViewNext.IsEnabled = true;
            }
        }

        private void ImageViewPrevious_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = lstImageMainViewerList.SelectedIndex - 1;

            if (newIndex >= 0)
            {
                ListBoxItem oldListBoxItem = this.lstImageMainViewerList.ItemContainerGenerator.ContainerFromItem(lstImageMainViewerList.SelectedItems[0]) as ListBoxItem;
                oldListBoxItem.IsSelected = false;

                ListBoxItem newListBoxItem = this.lstImageMainViewerList.ItemContainerGenerator.ContainerFromItem(lstImageMainViewerList.Items[newIndex]) as ListBoxItem;
                newListBoxItem.IsSelected = true;

                ImageViewUpdateNextPrevious();
            }
        }

        private void ImageViewNext_Click(object sender, RoutedEventArgs e)
        {
            int newIndex = lstImageMainViewerList.SelectedIndex + 1;

            if (lstImageMainViewerList.Items.Count > newIndex)
            {
                ListBoxItem oldListBoxItem = this.lstImageMainViewerList.ItemContainerGenerator.ContainerFromItem(lstImageMainViewerList.SelectedItems[0]) as ListBoxItem;
                oldListBoxItem.IsSelected = false;

                ListBoxItem newListBoxItem = this.lstImageMainViewerList.ItemContainerGenerator.ContainerFromItem(lstImageMainViewerList.Items[newIndex]) as ListBoxItem;
                newListBoxItem.IsSelected = true;

                ImageViewUpdateNextPrevious();
            }
        }

        private void ImageViewUpdateMetaTags(GeneralImage current)
        {
            List<ImageMetaTagRef> imageMetaTagRefTemp = new List<ImageMetaTagRef>();

            if (current.Meta.Tags == null)
            {
                foreach (ListBoxItem tagItem in lstImageViewTagList.SelectedItems)
                {
                    TagListTagRef currentTagRef = (TagListTagRef)tagItem.Tag;
                    ImageMetaTagRef newImageMetaTagRef = new ImageMetaTagRef();
                    newImageMetaTagRef.id = currentTagRef.id;
                    newImageMetaTagRef.op = "I";
                    imageMetaTagRefTemp.Add(newImageMetaTagRef);
                }
            }
            else
            {
                foreach (ListBoxItem tagItem in lstImageViewTagList.SelectedItems)
                {
                    TagListTagRef currentTagRef = (TagListTagRef)tagItem.Tag;

                    if (!current.Meta.Tags.Any<ImageMetaTagRef>(r => r.id == currentTagRef.id))
                    {
                        ImageMetaTagRef newImageMetaTagRef = new ImageMetaTagRef();
                        newImageMetaTagRef.id = currentTagRef.id;
                        newImageMetaTagRef.op = "I";
                        imageMetaTagRefTemp.Add(newImageMetaTagRef);
                    }
                }


                foreach (ImageMetaTagRef tagRef in current.Meta.Tags)
                {
                    bool found = false;
                    foreach (ListBoxItem tagItem in lstImageViewTagList.SelectedItems)
                    {
                        TagListTagRef currentTagRef = (TagListTagRef)tagItem.Tag;
                        if (currentTagRef.id == tagRef.id)
                            found = true;
                    }

                    if (!found)
                    {
                        ImageMetaTagRef newImageMetaTagRef = new ImageMetaTagRef();
                        newImageMetaTagRef.id = tagRef.id;
                        newImageMetaTagRef.op = "D";
                        imageMetaTagRefTemp.Add(newImageMetaTagRef);
                    }
                }
            }
            if (imageMetaTagRefTemp != null)
                current.Meta.Tags = imageMetaTagRefTemp.ToArray<ImageMetaTagRef>();


        }

        async private void ImageViewEditSave_Click(object sender, RoutedEventArgs e)
        {
            GeneralImage current = (GeneralImage)lstImageMainViewerList.Items.CurrentItem;
            if (currentPane == PaneMode.ImageEdit)
            {
                if (txtImageViewName.Text.Length < 1)
                {
                    ShowMessage(MessageType.Warning, "You must enter a name to continue saving");
                    return;
                }

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                try
                {
                    ShowMessage(MessageType.Busy, "Saving image data updates");

                    ImageViewUpdateMetaTags(current);
                    await current.SaveMeta(cancelTokenSource.Token);

                    if (newCancelTokenSource == cancelTokenSource)
                        cancelTokenSource = null;
                    
                    ImageViewTagsUpdateFromMeta();

                    ConcludeBusyProcess();
                }
                catch (OperationCanceledException)
                {
                    logger.Debug("cmdGallerySave_Click has been cancelled.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MessageType.Error, "Image data could not be saved, there was an error:" + ex.Message);
                }
                RefreshPanesAllControls(PaneMode.ImageView);
            }
            else
            {
                if (current.Meta == null)
                {
                    ShowMessage(MessageType.Info, "The image meta data has not been retrieved from the server, please wait...");
                }
                else
                {
                    //currentPane = PaneMode.ImageEdit;
                    RefreshPanesAllControls(PaneMode.ImageEdit);
                }
            }
        }

        private void ImageViewEditCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.ImageView);
            GeneralImage current = (GeneralImage)lstImageMainViewerList.Items.CurrentItem;
            current.LoadMeta(true, cancelTokenSource.Token);
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            Image meImage = sender as Image;
            if (meImage != null && e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(meImage, "dunno", DragDropEffects.Copy);
            }
        }

        private void cmdShowActionsMenu_Click(object sender, RoutedEventArgs e)
        {
            //cmdShowActionsMenu.ContextMenu.IsOpen = (bool)cmdShowActionsMenu.IsChecked;
        }
        #endregion

        #region Image and menu handlers
        async private void FetchCategoryImagesFirstAsync(object sender, RoutedEventArgs e)
        {
            if (currentPane == PaneMode.CategoryAdd || currentPane == PaneMode.CategoryEdit)
                return;

            cmbGallerySectionVert.Visibility = Visibility.Collapsed;
            cmbGallerySection.Visibility = Visibility.Collapsed;

            RadioButton checkedTagButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedTagButton != null)
                checkedTagButton.IsChecked = false;

            RadioButton checkedGalleryButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedGalleryButton != null)
                checkedGalleryButton.IsChecked = false;

            e.Handled = true;

            TreeViewItem selectedTreeViewItem = (TreeViewItem)sender;

            if (selectedTreeViewItem != null)
            {
                try
                {
                    if (cancelTokenSource != null)
                        cancelTokenSource.Cancel();

                    CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                    cancelTokenSource = newCancelTokenSource;

                    CategoryListCategoryRef categoryRef = (CategoryListCategoryRef)selectedTreeViewItem.Tag;
                    currentImageList = await controller.CategoryGetImagesAsync(categoryRef.id, 0, GetSearchQueryString(), cancelTokenSource.Token);
                    if (currentImageList != null)
                        await ImageListUpdateControls(cancelTokenSource.Token);

                    if (newCancelTokenSource == cancelTokenSource)
                        cancelTokenSource = null;
                }
                catch (OperationCanceledException)
                {
                    logger.Debug("FetchCategoryImagesFirstAsync has been cancelled by a subsequent request.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MessageType.Error, "Unexpected error loading images: " + ex.Message);
                }
            }
        }

        async private void FetchTagImagesFirstAsync(object sender, RoutedEventArgs e)
        {
            cmbGallerySectionVert.Visibility = Visibility.Collapsed;
            cmbGallerySection.Visibility = Visibility.Collapsed;

            TreeViewItem selectedTreeViewItem = (TreeViewItem)treeCategoryView.SelectedItem;
            if (selectedTreeViewItem != null)
                selectedTreeViewItem.IsSelected = false;

            RadioButton checkedGalleryButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedGalleryButton != null)
                checkedGalleryButton.IsChecked = false;

            RadioButton checkedButton = (RadioButton)sender;
            //RadioButton checkedButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();

            /* Refresh tag image state */
            if (checkedButton != null)
            {
                try
                {
                    if (cancelTokenSource != null)
                        cancelTokenSource.Cancel();

                    CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                    cancelTokenSource = newCancelTokenSource;

                    TagListTagRef tagListTagRefTemp = (TagListTagRef)checkedButton.Tag;
                    currentImageList = await controller.TagGetImagesAsync(tagListTagRefTemp.id, tagListTagRefTemp.name, 0, GetSearchQueryString(), cancelTokenSource.Token);
                    if (currentImageList != null)
                        await ImageListUpdateControls(cancelTokenSource.Token);

                    if (newCancelTokenSource == cancelTokenSource)
                        cancelTokenSource = null;
                }
                catch (OperationCanceledException)
                {
                    logger.Debug("FetchTagImagesFirstAsync has been cancelled by a subsequent request. ");
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MessageType.Error, "Unexpected error loading images: " + ex.Message);
                }
            }
        }

        async private void FetchGalleryImagesFirstAsync(object sender, RoutedEventArgs e)
        {
            //Uncheck any other selected imagelists.
            TreeViewItem selectedTreeViewItem = (TreeViewItem)treeCategoryView.SelectedItem;
            if (selectedTreeViewItem != null)
                selectedTreeViewItem.IsSelected = false;

            RadioButton checkedTagButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedTagButton != null)
                checkedTagButton.IsChecked = false;

            RadioButton checkedButton = (RadioButton)sender;

            /* Refresh tag image state */
            if (checkedButton != null)
            {
                GalleryListGalleryRef galleryListRefTemp = (GalleryListGalleryRef)checkedButton.Tag;
                if (!GalleryPopulateSectionList(galleryListRefTemp))
                {
                    try
                    {
                        if (cancelTokenSource != null)
                            cancelTokenSource.Cancel();

                        CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                        cancelTokenSource = newCancelTokenSource;

                        currentImageList = await controller.GalleryGetImagesAsync(galleryListRefTemp.id, galleryListRefTemp.name, 0, -1, GetSearchQueryString(), cancelTokenSource.Token);
                        if (currentImageList != null)
                        {
                            newCancelTokenSource = new CancellationTokenSource();
                            cancelTokenSource = newCancelTokenSource;

                            await ImageListUpdateControls(cancelTokenSource.Token);
                        }

                        if (newCancelTokenSource == cancelTokenSource)
                            cancelTokenSource = null;
                    }
                    catch (OperationCanceledException)
                    {
                        logger.Debug("FetchGalleryImagesFirstAsync has been cancelled by a subsequent request. ");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        ShowMessage(MessageType.Error, "Unexpected error loading images: " + ex.Message);
                    }
                }
            }
        }

        async private void FetchGalleryImagesSectionChangeAsync(long sectionId)
        {
            RadioButton checkedGalleryButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedGalleryButton != null)
            {
                try
                {
                    if (cancelTokenSource != null)
                        cancelTokenSource.Cancel();

                    CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                    cancelTokenSource = newCancelTokenSource;

                    GalleryListGalleryRef galleryListRefTemp = (GalleryListGalleryRef)checkedGalleryButton.Tag;
                    currentImageList = await controller.GalleryGetImagesAsync(galleryListRefTemp.id, galleryListRefTemp.name, 0, sectionId, GetSearchQueryString(), cancelTokenSource.Token);
                    if (currentImageList != null)
                        await ImageListUpdateControls(cancelTokenSource.Token);

                    if (newCancelTokenSource == cancelTokenSource)
                        cancelTokenSource = null;
                }
                catch (OperationCanceledException)
                {
                    logger.Debug("FetchTagImagesFirstAsync has been cancelled by a subsequent request. ");
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MessageType.Error, "Unexpected error loading images: " + ex.Message);
                }
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

            try
            {
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                switch (currentPane)
                {
                    case PaneMode.CategoryView:
                    case PaneMode.CategoryEdit:
                    case PaneMode.CategoryAdd:
                        currentImageList = await controller.CategoryGetImagesAsync(currentImageList.id, cursor, GetSearchQueryString(), cancelTokenSource.Token);
                        break;
                    case PaneMode.TagAdd:
                    case PaneMode.TagEdit:
                    case PaneMode.TagView:
                        currentImageList = await controller.TagGetImagesAsync(currentImageList.id, currentImageList.Name, cursor, GetSearchQueryString(), cancelTokenSource.Token);
                        break;
                    case PaneMode.GalleryView:

                        long sectionId = -1;
                        ComboBoxItem cmbItemSection = (ComboBoxItem)cmbGallerySection.SelectedItem;
                        if (cmbItemSection != null && currentImageList.sectionId > 0)
                        {
                            GalleryListGalleryRefSectionRef section = (GalleryListGalleryRefSectionRef)cmbItemSection.Tag;
                            sectionId = section.id;
                        }
                        currentImageList = await controller.GalleryGetImagesAsync(currentImageList.id, currentImageList.Name, cursor, sectionId, GetSearchQueryString(), cancelTokenSource.Token);
                        break;
                }
                
                if (currentImageList != null)
                    await ImageListUpdateControls(cancelTokenSource.Token);

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;
            }
            catch (OperationCanceledException)
            {
                logger.Debug("FetchMoreImagesAsync has been cancelled by a subsequent request. ");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Unexpected error loading images: " + ex.Message);
            }
        }

        async private void sldImageSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            DateTime eventTime = DateTime.Now;
            await WaitAsynchronouslyAsync();
            TweakImageMarginSize(eventTime, currentPane);
        }

        async private void mainTwo_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DateTime eventTime = DateTime.Now;
            await WaitAsynchronouslyAsync();
            TweakImageMarginSize(eventTime, currentPane);
        }

        private void cmdShowInlineImageDetail_CheckedUnChecked(object sender, RoutedEventArgs e)
        {
            if (cmdShowInlineImageDetail.IsChecked)
            {
                previousImageSize = sldImageSize.Value;
                sldImageSize.Visibility = Visibility.Hidden;
            }
            else
            {
                sldImageSize.Visibility = Visibility.Visible;
                sldImageSize.Value = previousImageSize;
            }
            TweakImageMarginSize(DateTime.Now, currentPane);
        }

        public async Task WaitAsynchronouslyAsync()
        {
            await Task.Delay(500);
        }

        private bool IsScrollBarVisible()
        {
            double imageSize = sldImageSize.Value + 4.0;
            double areaForImages = (lstImageMainViewerList.Items.Count + 5) * (imageSize * imageSize);

            double areaOfCanvas = gridRight.ColumnDefinitions[0].ActualWidth * gridRight.RowDefinitions[1].ActualHeight;

            if (areaForImages > areaOfCanvas)
            {
                return true;
            }
            else
            {
                return false;
            }

            ScrollViewer sv = FindVisualChild<ScrollViewer>(lstImageMainViewerList);
            Visibility scrollbarVisibility = sv.ComputedVerticalScrollBarVisibility;
            if (scrollbarVisibility == Visibility.Visible)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            // Search immediate children first (breadth-first)
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);

                if (child != null && child is childItem)
                    return (childItem)child;

                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);

                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void TweakUploadImageListMargin()
        {
            /* Upload image list margin */
            double paneWidth = lstUploadImageFileList.ActualWidth - 22.0;
            double imageWidth = 130.0;
            double imageWidthWithMargin = imageWidth + 4.0;

            double imageWidthCount = Math.Floor(paneWidth / (imageWidthWithMargin));
            double remainder = paneWidth - (imageWidthCount * (imageWidth));
            double newMargin = Math.Floor((remainder / imageWidthCount) / 2.0);

            Style newStyle = new Style();
            newStyle.BasedOn = (Style)lstUploadImageFileList.ItemContainerStyle;
            newStyle.TargetType = typeof(ListBoxItem);
            Thickness newThickness = new Thickness(newMargin);
            newStyle.Setters.Add(new Setter(MarginProperty, newThickness));
            lstUploadImageFileList.ItemContainerStyle = newStyle;

            lastMarginTweakTime = DateTime.Now;

            double total = (imageWidthCount * (newMargin * 2)) + (imageWidthCount * imageWidth);
            Console.Out.WriteLine("Upload");
            Console.Out.WriteLine("New total:" + total.ToString());
            Console.Out.WriteLine("Image Size:" + imageWidth.ToString());
            Console.Out.WriteLine("Margin Changed:" + newMargin.ToString());
            Console.Out.WriteLine("Altered width:" + paneWidth.ToString());
            Console.Out.WriteLine("Actual width:" + lstUploadImageFileList.ActualWidth.ToString());
        }

        private void TweakMainImageListMargin()
        {
            bool isDetail = (bool)cmdShowInlineImageDetail.IsChecked;
            double paneWidth = lstImageMainViewerList.ActualWidth - 22.0;
            double imageWidth = sldImageSize.Value + 1;
            double imageWidthWithMargin = imageWidth + 4.0;

            if (isDetail)
            {
                imageWidthWithMargin = imageWidthWithMargin + 140.0;
                imageWidth = imageWidth + 140.0;
            }

            double imageWidthCount = Math.Floor(paneWidth / (imageWidthWithMargin));
            double remainder = paneWidth - (imageWidthCount * (imageWidth));
            double newMargin = Math.Floor((remainder / imageWidthCount) / 2.0);

            Style newStyle = new Style();
            newStyle.BasedOn = (Style)lstImageMainViewerList.ItemContainerStyle;
            newStyle.TargetType = typeof(ListBoxItem);
            Thickness newThickness = new Thickness(newMargin);

            if (isDetail)
                newThickness = new Thickness(newMargin, 2, newMargin, 2);

            newStyle.Setters.Add(new Setter(MarginProperty, newThickness));
            lstImageMainViewerList.ItemContainerStyle = newStyle;

            lastMarginTweakTime = DateTime.Now;

            double total = (imageWidthCount * (newMargin * 2)) + (imageWidthCount * imageWidth);
            Console.Out.WriteLine("Main");
            Console.Out.WriteLine("New total:" + total.ToString());
            Console.Out.WriteLine("Image Size:" + imageWidth.ToString());
            Console.Out.WriteLine("Margin Changed:" + newMargin.ToString());
            Console.Out.WriteLine("Altered width:" + paneWidth.ToString());
            Console.Out.WriteLine("Actual width:" + lstImageMainViewerList.ActualWidth.ToString());
        }

        private void TweakImageMarginSize(DateTime eventTime, PaneMode mode)
        {
            if (eventTime < lastMarginTweakTime)
            {
                Console.Out.WriteLine("TweakImageMarginSize not run cause of later update.");
                return;
            }

            if (mode == PaneMode.Upload)
            {
                if (lstUploadImageFileList == null) { return; }
                TweakUploadImageListMargin();
            }
            else
            {
                if (lstImageMainViewerList == null) { return; }
                TweakMainImageListMargin();
            }
        }

        /*  Could of worked better
        private void TweakImageSize(DateTime eventTime)
        {
            if (lstImageMainViewerList.Items.Count < 1)
                return;

            if (eventTime < lastMarginTweakTime)
            {
                Console.Out.WriteLine("TweakImageSize not run cause of later update.");
                return;
            }

            //Work out optimum image size.
            double imageCountAlongWidth;
            double remainder;
            double newImageWidth = 0.0;
            double currentPaneWidth = gridRight.ColumnDefinitions[0].ActualWidth;

            bool isDetail = (bool)cmdShowInlineImageDetail.IsChecked;
            if (isDetail)
            {
                newImageWidth = 140.0;
            }
            else
            {
                //if (IsScrollBarVisible())
                //    currentPaneWidth = currentPaneWidth - 23.0;

                double imageWidth = sldImageSize.Value;
                double imageWidthWithMargin = imageWidth + 4.0;

                imageCountAlongWidth = Math.Round(currentPaneWidth / imageWidthWithMargin,0.0);
                remainder = (currentPaneWidth / imageWidthWithMargin) - Math.Floor(currentPaneWidth / imageWidthWithMargin);
                if (remainder < 0.25 || remainder > 0.75)
                {
                    //Same number of images, just a little larger or a little smaller.
                    newImageWidth = (currentPaneWidth / imageCountAlongWidth) - 4.0;
                }
                else if (remainder >= 0.25 && remainder < 0.50)
                {
                    //One more image please.
                    newImageWidth = (currentPaneWidth / (imageCountAlongWidth + 1)) - 4.0;
                }
                else if (remainder >= 0.50 && remainder <= 0.75)
                {
                    //One less image please.
                    newImageWidth = (currentPaneWidth / (imageCountAlongWidth - 1)) - 4.0;
                }
            }

            if (newImageWidth > 0.0 && newImageWidth != sldImageSize.Value)
            {
                sldImageSize.Value = Math.Max(Math.Min(Math.Floor(newImageWidth), 300.0), 75.0);
                Console.Out.WriteLine("Image Changed:" + sldImageSize.Value.ToString());
            }
            else
            {
                TweakImageMarginSize();
                Console.Out.WriteLine("Image Not Changed:" + sldImageSize.Value.ToString());
            }
            Console.Out.WriteLine("Pane width:" + currentPaneWidth.ToString());
            
            lastMarginTweakTime = DateTime.Now;
        }
 */
        async private Task ImageListUpdateControls(CancellationToken cancelToken)
        {
            try
            {
                imageMainViewerList.Clear();

                if (tweakMainImageSize)
                {
                    TweakImageMarginSize(DateTime.Now, currentPane);
                    tweakMainImageSize = false;
                }

                if (currentImageList == null)
                    return;



                string bannerName = "";
                menuTagRemoveFrom.Header = "Remove images from tag";
                menuTagRemoveFrom.IsEnabled = false;
                menuCategoryMoveImage.IsEnabled = false;

                if (currentPane == PaneMode.CategoryView)
                {
                    bannerName = "Category: " + currentImageList.Name;
                    menuCategoryMoveImage.IsEnabled = true;
                }
                else if (currentPane == PaneMode.TagView)
                {
                    bannerName = "Tag: " + currentImageList.Name;
                    menuTagRemoveFrom.Header = "Remove images from tag: " + currentImageList.Name;
                    menuTagRemoveFrom.IsEnabled = true;
                }
                else if (currentPane == PaneMode.GalleryView)
                {
                    bannerName = "Gallery: " + currentImageList.Name;
                }

                if (currentImageList.Images == null)
                {
                    bannerName = bannerName + " (no images)";
                }
                else
                {
                    foreach (ImageListImageRef imageRef in currentImageList.Images)
                    {
                        GeneralImage newImage = new GeneralImage(controller.GetServerHelper());
                        newImage.imageId = imageRef.id;
                        newImage.name = imageRef.name;
                        newImage.description = imageRef.desc;
                        newImage.shotSummary = imageRef.shotSummary;
                        newImage.fileSummary = imageRef.fileSummary;
                        newImage.uploadDate = imageRef.uploadDate;
                        newImage.metaVersion = imageRef.metaVersion;
                        newImage.categoryId = imageRef.categoryId;

                        imageMainViewerList.Add(newImage);
                    }
                }

                lblImageListName.Content = bannerName;
                lblImageListNameVert.Text = bannerName;

                if ((currentImageList.sectionId > 0 && currentImageList.sectionImageCount > currentImageList.imageCount)
                    || (currentImageList.sectionId < 1 && currentImageList.totalImageCount > currentImageList.imageCount))
                {
                    cmdImageNavigationBegin.Visibility = Visibility.Visible;
                    cmdImageNavigationPrevious.Visibility = Visibility.Visible;
                    cmdImageNavigationLast.Visibility = Visibility.Visible;
                    cmdImageNavigationNext.Visibility = Visibility.Visible;

                    cmdImageNavigationBeginVert.Visibility = Visibility.Visible;
                    cmdImageNavigationPreviousVert.Visibility = Visibility.Visible;
                    cmdImageNavigationLastVert.Visibility = Visibility.Visible;
                    cmdImageNavigationNextVert.Visibility = Visibility.Visible;

                    lineActionsMenu.Visibility = Visibility.Visible;

                    if (currentImageList.imageCursor == 0)
                    {
                        cmdImageNavigationBegin.IsEnabled = false;
                        cmdImageNavigationPrevious.IsEnabled = false;
                        cmdImageNavigationBeginVert.IsEnabled = false;
                        cmdImageNavigationPreviousVert.IsEnabled = false;
                    }
                    else
                    {
                        cmdImageNavigationBegin.IsEnabled = true;
                        cmdImageNavigationPrevious.IsEnabled = true;
                        cmdImageNavigationBeginVert.IsEnabled = true;
                        cmdImageNavigationPreviousVert.IsEnabled = true;
                    }

                    if ((currentImageList.sectionId > 0 && currentImageList.sectionImageCount > (currentImageList.imageCursor + state.imageFetchSize))
                        || (currentImageList.sectionId < 1 && currentImageList.totalImageCount > (currentImageList.imageCursor + state.imageFetchSize)))
                    {
                        cmdImageNavigationLast.IsEnabled = true;
                        cmdImageNavigationNext.IsEnabled = true;
                        cmdImageNavigationLastVert.IsEnabled = true;
                        cmdImageNavigationNextVert.IsEnabled = true;
                    }
                    else
                    {
                        cmdImageNavigationLast.IsEnabled = false;
                        cmdImageNavigationNext.IsEnabled = false;
                        cmdImageNavigationLastVert.IsEnabled = false;
                        cmdImageNavigationNextVert.IsEnabled = false;
                    }
                }
                else
                {
                    cmdImageNavigationBegin.Visibility = Visibility.Hidden;
                    cmdImageNavigationPrevious.Visibility = Visibility.Hidden;
                    cmdImageNavigationLast.Visibility = Visibility.Hidden;
                    cmdImageNavigationNext.Visibility = Visibility.Hidden;

                    cmdImageNavigationBeginVert.Visibility = Visibility.Hidden;
                    cmdImageNavigationPreviousVert.Visibility = Visibility.Hidden;
                    cmdImageNavigationLastVert.Visibility = Visibility.Hidden;
                    cmdImageNavigationNextVert.Visibility = Visibility.Hidden;

                    lineActionsMenu.Visibility = Visibility.Hidden;
                }

                if (currentImageList.Images == null)
                    return;

                int cursor = 0;
                bool moreToLoad = true;
                while (moreToLoad)
                {
                    if (cancelToken != null)
                        cancelToken.ThrowIfCancellationRequested();

                    Task[] tasks = new Task[10];

                    for (int i = 0; i < 10; i++)
                    {
                        if (cursor + i < imageMainViewerList.Count)
                            tasks[i] = imageMainViewerList[cursor + i].LoadThumb(cancelToken, thumbCacheList);
                    }

                    for (int i = 0; i < 10; i++)
                    {
                        if (tasks[i] != null)
                            await tasks[i];
                    }

                    /*
                    for (int i = 0; i < 10; i++)
                    {
                        if (cursor + i < imageMainViewerList.Count)
                        {
                            //await imageMainViewerList[cursor + i].LoadMainImage(cancelToken);
                            await imageMainViewerList[cursor + i].LoadMeta(false, cancelToken);
                            //await imageMainViewerList[cursor + i].SaveMeta(cancelToken);
                        }
                    }
                    */

                    cursor = cursor + 10;
                    if (cursor >= imageMainViewerList.Count)
                        moreToLoad = false;
                }

            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("ImageListUpdateControls has been cancelled.");
                throw (cancelEx);
            }
        }

        //TODO add functionality + server side.
        private string GetSearchQueryString()
        {
            return null;
        }

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

        async private void DeleteImages_Click(object sender, RoutedEventArgs e)
        {
            int count = lstImageMainViewerList.SelectedItems.Count;
            if (count > 0)
            {
                if (MessageBox.Show("Do you want to delete the " + count.ToString() + " selected images permanently from fotowalla ?", "ManageWalla", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    ShowMessage(MessageType.Busy, "Deleting Images");

                    try
                    {
                        ImageList deleteList = new ImageList();
                        deleteList.Images = new ImageListImageRef[lstImageMainViewerList.SelectedItems.Count];

                        List<GeneralImage> toRemoveList = new List<GeneralImage>();

                        int i = 0;
                        foreach (GeneralImage image in lstImageMainViewerList.SelectedItems)
                        {
                            deleteList.Images[i] = new ImageListImageRef();
                            deleteList.Images[i].id = image.imageId;
                            deleteList.Images[i].idSpecified = true;
                            i++;

                            toRemoveList.Add(image);
                        }

                        if (cancelTokenSource != null)
                            cancelTokenSource.Cancel();

                        CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                        cancelTokenSource = newCancelTokenSource;

                        await controller.DeleteImagesAsync(deleteList, cancelTokenSource.Token);

                        foreach (GeneralImage current in toRemoveList)
                        {
                            imageMainViewerList.Remove(current);
                        }

                        ConcludeBusyProcess();

                        string message = count.ToString() + " images were successfully deleted";
                        ShowMessage(MessageType.Info, message);

                        if (newCancelTokenSource == cancelTokenSource)
                            cancelTokenSource = null;
                    }
                    catch (OperationCanceledException)
                    {
                        logger.Debug("DeleteImages_Click has been cancelled");
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        ShowMessage(MainTwo.MessageType.Error, "There was a problem deleting the images on the server.  Error: " + ex.Message);
                    }

                }
            }
            else
            {
                ShowMessage(MessageType.Warning, "You must select at least one image to perform a deletion");
            }
        }

        private void cmdMultiSelectionMode_Checked(object sender, RoutedEventArgs e)
        {
            while (lstImageMainViewerList.SelectedItems.Count > 0)
            {
                ListBoxItem listBoxItem = this.lstImageMainViewerList.ItemContainerGenerator.ContainerFromItem(lstImageMainViewerList.SelectedItems[0]) as ListBoxItem;
                listBoxItem.IsSelected = false;
            }

            lstImageMainViewerList.SelectionMode = SelectionMode.Multiple;
        }

        private void cmdMultiSelectionMode_Unchecked(object sender, RoutedEventArgs e)
        {
            lstImageMainViewerList.SelectionMode = SelectionMode.Single;
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (lstImageMainViewerList.SelectionMode == SelectionMode.Single)
            {
                previousPane = currentPane;
                cmdImageViewDetailToggle.IsChecked = false;

                RefreshPanesAllControls(PaneMode.ImageView);
                RefreshOverallPanesStructure(currentPane);

                ImageViewUpdateNextPrevious();
            }
        }

        #endregion

        #region Category Methods
        async public Task RefreshAndDisplayCategoryList(bool forceRefresh)
        {
            try
            {
                bool isBusy = bool.Parse(radCategory.Tag.ToString());
                if (isBusy) { return; }

                bool redrawList = false;

                //Catch first time loads, user intiated refresh and when user was offline and is now online.  But only if logged on.
                if (state.connectionState != GlobalState.ConnectionState.NoAccount &&
                    (state.categoryLoadState == GlobalState.DataLoadState.No || forceRefresh || state.categoryLoadState == GlobalState.DataLoadState.LocalCache))
                {
                    //panCategoryUnavailable.Visibility = System.Windows.Visibility.Visible;
                    //gridCategory.Visibility = Visibility.Collapsed;

                    if (cancelTokenSource != null)
                        cancelTokenSource.Cancel();

                    CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                    cancelTokenSource = newCancelTokenSource;

                    radCategory.Tag = true;

                    await controller.CategoryRefreshListAsync(cancelTokenSource.Token);
                    redrawList = true;

                    if (newCancelTokenSource == cancelTokenSource)
                        cancelTokenSource = null;
                }

                switch (state.categoryLoadState)
                {
                    case GlobalState.DataLoadState.Loaded:
                    case GlobalState.DataLoadState.LocalCache:

                        if (redrawList) { CategoryListReloadFromState(); }
                        panCategoryUnavailable.Visibility = System.Windows.Visibility.Collapsed;
                        gridCategory.Visibility = Visibility.Visible;
                        break;
                    case GlobalState.DataLoadState.Unavailable:
                        panCategoryUnavailable.Visibility = System.Windows.Visibility.Visible;
                        gridCategory.Visibility = Visibility.Collapsed;
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                logger.Debug("RefreshAndDisplayCategoryList has been cancelled.");
            }
            catch (Exception ex)
            {
                radCategory.Tag = false;
                state.categoryList = null;
                state.categoryLoadState = GlobalState.DataLoadState.Unavailable;
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Category list could not be loaded, there was an error: " + ex.Message);
            }
            finally
            {
                radCategory.Tag = false;
            }
        }

        public void CategoryListReloadFromState()
        {
            long categoryId = 0;
            //Keep a reference to the currently selected category item list.
            TreeViewItem item = (TreeViewItem)treeCategoryView.SelectedItem;
            if (item != null)
            {
                CategoryListCategoryRef category = (CategoryListCategoryRef)item.Tag;
                categoryId = category.id;
            }
            else
            {
                categoryId = 0;
            }

            CategoryListCategoryRef baseCategory = state.categoryList.CategoryRef.Single<CategoryListCategoryRef>(r => r.parentId == 0);

            treeCategoryView.Items.Clear();
            CategoryAddTreeViewLevel(baseCategory.id, null);

            if (categoryId > 0)
            {
                CategorySelect(categoryId, (TreeViewItem)treeCategoryView.Items[0], treeCategoryView);
            }

            //TreeViewItem baseItem = (TreeViewItem)treeCategoryView.Items[0];
            //CategoryListCategoryRef baseCategoryObj = (CategoryListCategoryRef)baseItem.Tag;
            //if (baseCategoryObj.id == categoryId || categoryId == 0)
            //{
            //    baseItem.IsSelected = true;
            //    treeCategoryView.Items.MoveCurrentTo(baseItem);

            //}
            //else
            //{
            //    CategorySelect(categoryId, (TreeViewItem)treeCategoryView.Items[0], treeCategoryView);
            //}

            //UploadRefreshCategoryList();
        }

        private void CategoryAddTreeViewLevel(long parentId, TreeViewItem currentHeader)
        {
            foreach (CategoryListCategoryRef current in state.categoryList.CategoryRef.Where(r => r.parentId == parentId))
            {
                TreeViewItem newItem = new TreeViewItem();
                int totalCount = current.count;
                CategoryGetImageCountRecursive(current.id, ref totalCount);
                newItem.Header = current.name;

                StringBuilder builder = new StringBuilder();
                if (current.desc != null && current.desc.Length > 0) { builder.Append(current.desc + "\r\n"); }
                builder.Append("Photos - " + current.count.ToString() + "\r\n");
                builder.Append("Photos (inc. sub category) - " + totalCount.ToString());
                newItem.ToolTip = builder.ToString();

                newItem.Padding = new Thickness(2.0);
                newItem.Tag = current;
                newItem.IsExpanded = true;
                newItem.AllowDrop = true;
                //newItem.Drop += new DragEventHandler(CategoryDroppedImages);
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

        async private Task CategoryPopulateMetaData(CategoryListCategoryRef current)
        {
            try
            {
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                ShowMessage(MessageType.Busy, "Retrieving Category");

                Category category = await controller.CategoryGetMetaAsync(current, cancelTokenSource.Token);
                txtCategoryName.Text = category.Name;
                txtCategoryDescription.Text = category.Desc;
                lblCategoryParentName.Content = GetCategoryName(current.parentId);

                currentCategory = category;

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                RefreshPanesAllControls(PaneMode.CategoryEdit);
                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                logger.Debug("CategoryPopulateMetaData has been cancelled");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem retrieving the category.  Error: " + ex.Message);
            }
        }

        private String GetCategoryName(long categoryId)
        {
            CategoryListCategoryRef parentCategory = state.categoryList.CategoryRef.Single<CategoryListCategoryRef>(r => r.id == categoryId);
            return parentCategory.name;
        }

        async private Task MoveImagesToCategory(long categoryId)
        {
            ImageMoveList moveList = new ImageMoveList();
            moveList.ImageRef = new long[lstImageMainViewerList.SelectedItems.Count];

            int i = 0;
            foreach (GeneralImage image in lstImageMainViewerList.SelectedItems)
            {
                moveList.ImageRef[i] = image.imageId;
                if (image.categoryId == categoryId)
                {
                    ShowMessage(MessageType.Warning, "The update cannot be done, you have selected images which are already in the category.");
                    return;
                }
                i++;
            }

            try
            {
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                ShowMessage(MessageType.Busy, "Updating images in category");
                await controller.CategoryMoveImagesAsync(categoryId, moveList, cancelTokenSource.Token);

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                ConcludeBusyProcess();
                await RefreshAndDisplayCategoryList(true);

                string message = moveList.ImageRef.Length.ToString() + " image(s) were successfully moved to the category.";
                ShowMessage(MessageType.Info, message);
            }
            catch (OperationCanceledException)
            {
                logger.Debug("MoveImagesToCategory has been cancelled");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "Images could not be moved to the category, there was an error on the server: " + ex.Message);
            }
        }

        private void CategorySelectRefreshCategoryList()
        {
            treeCategorySelect.Items.Clear();

            CategoryListCategoryRef baseCategory = state.categoryList.CategoryRef.Single<CategoryListCategoryRef>(r => r.parentId == 0);
            CategorySelectAddToTreeView(baseCategory.id, null);
        }

        private void CategorySelectAddToTreeView(long parentId, TreeViewItem currentHeader)
        {
            foreach (CategoryListCategoryRef current in state.categoryList.CategoryRef.Where(r => r.parentId == parentId))
            {
                if (current.SystemOwned == false || (current.SystemOwned == true && current.id == state.userApp.UserDefaultCategoryId))
                {
                    TreeViewItem newItem = new TreeViewItem();
                    newItem.Header = current.name;
                    StringBuilder builder = new StringBuilder();
                    if (current.desc != null && current.desc.Length > 0) { builder.Append(current.desc + "\r\n"); }
                    builder.Append("Photos - " + current.count.ToString());
                    newItem.ToolTip = builder.ToString();
                    newItem.Tag = current;
                    newItem.IsExpanded = true;
                    newItem.Padding = new Thickness(2.0);

                    if (currentHeader == null)
                    {
                        treeCategorySelect.Items.Add(newItem);
                    }
                    else
                    {
                        currentHeader.Items.Add(newItem);
                    }

                    CategorySelectAddToTreeView(current.id, newItem);
                }
            }
        }
        #endregion

        #region Category Event Handlers
        private void cmdCategoryRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAndDisplayCategoryList(true);
        }

        private void cmdCategoryCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.CategoryView);
        }

        private void cmdCategoryAdd_Click(object sender, RoutedEventArgs e)
        {
            currentCategory = new Category();
            TreeViewItem item = (TreeViewItem)treeCategoryView.SelectedItem;
            if (item != null)
            {
                CategoryListCategoryRef currentSelectedCategory = (CategoryListCategoryRef)item.Tag;

                if (currentSelectedCategory.SystemOwned == true && currentSelectedCategory.id != state.userApp.UserDefaultCategoryId)
                {
                    ShowMessage(MessageType.Warning, "You cannot add new categories here, these are reserved for uploads.  Please select a user category");
                    return;
                }
                
                currentCategory.parentId = currentSelectedCategory.id;
            }
            else
            {
                CategorySelect(state.userApp.UserDefaultCategoryId, (TreeViewItem)treeCategoryView.Items[0], treeCategoryView);
                currentCategory.parentId = state.userApp.UserDefaultCategoryId;
            }

            lblCategoryParentName.Content = GetCategoryName(currentCategory.parentId);
            txtCategoryName.Text = "";
            txtCategoryDescription.Text = "";
            RefreshPanesAllControls(PaneMode.CategoryAdd);
        }

        async private void cmdCategoryEdit_Click(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)treeCategoryView.SelectedItem;
            if (item != null)
            {
                CategoryListCategoryRef currentSelectedCategory = (CategoryListCategoryRef)item.Tag;

                if (currentSelectedCategory.SystemOwned == true)
                {
                    ShowMessage(MessageType.Warning, "You cannot edit this category, it is reserved for system usage");
                    return;
                }
                await CategoryPopulateMetaData(currentSelectedCategory);
            }
            else
            {
                ShowMessage(MessageType.Warning, "You must select a category to continue.");
            }
        }

        async private void cmdCategorySave_Click(object sender, RoutedEventArgs e)
        {
            if (txtCategoryName.Text.Length < 1)
            {
                ShowMessage(MessageType.Warning, "You must enter a category name to continue.");
                return;
            }

            try
            {

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                ShowMessage(MessageType.Busy, "Updating server with category info");

                if (currentPane == PaneMode.CategoryAdd)
                {
                    currentCategory.Name = txtCategoryName.Text;
                    currentCategory.Desc = txtCategoryDescription.Text;

                    await controller.CategoryCreateAsync(currentCategory, cancelTokenSource.Token);
                }
                else
                {
                    currentCategory.Name = txtCategoryName.Text;
                    currentCategory.Desc = txtCategoryDescription.Text;

                    await controller.CategoryUpdateAsync(currentCategory, cancelTokenSource.Token);
                }

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                logger.Debug("cmdCategorySave_Click has been cancelled");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem updating category data on the server.  Error: " + ex.Message);
            }
            finally
            {
                RefreshPanesAllControls(PaneMode.CategoryView);
                RefreshAndDisplayCategoryList(true);
            }
        }

        async private void cmdCategoryDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                ShowMessage(MessageType.Busy, "Deleting category");

                await controller.CategoryDeleteAsync(currentCategory, cancelTokenSource.Token);

                imageMainViewerList.Clear();
                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                logger.Debug("cmdCategoryDelete_Click has been cancelled");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem deleting the category.  Error: " + ex.Message);
            }
            finally
            {
                RefreshPanesAllControls(PaneMode.CategoryView);
                RefreshAndDisplayCategoryList(true);
            }
        }

        /*
        async private void CategoryDroppedImages(object sender, DragEventArgs e)
        {
            e.Handled = true;

            TreeViewItem meTreeviewNode = sender as TreeViewItem;
            CategoryListCategoryRef meToCategory = (CategoryListCategoryRef)meTreeviewNode.Tag;

            int count = lstImageMainViewerList.SelectedItems.Count;
            if (count > 0)
            {
                if (MessageBox.Show("Do you want to move the " + count.ToString() + " selected images to the category: " + meToCategory.name + "?", "ManageWalla", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    await MoveImagesToCategory(meToCategory.id);
                }
            }
        }*/

        #endregion

        #region Tag Methods
        async private Task RefreshAndDisplayTagList(bool forceRefresh)
        {
            try
            {
                bool isBusy = bool.Parse(radTag.Tag.ToString());
                if (isBusy) { return; }

                bool redrawList = false;
                //Catch first time loads, user intiated refresh and when user was offline and is now online.  But only if logged on.
                if (state.connectionState != GlobalState.ConnectionState.NoAccount &&
                    (state.tagLoadState == GlobalState.DataLoadState.No || forceRefresh || state.tagLoadState == GlobalState.DataLoadState.LocalCache))
                {
                    if (cancelTokenSource != null)
                        cancelTokenSource.Cancel();

                    CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                    cancelTokenSource = newCancelTokenSource;

                    radTag.Tag = true;

                    await controller.TagRefreshListAsync(cancelTokenSource.Token);

                    redrawList = true;

                    if (newCancelTokenSource == cancelTokenSource)
                        cancelTokenSource = null;
                }

                switch (state.tagLoadState)
                {
                    case GlobalState.DataLoadState.Loaded:
                    case GlobalState.DataLoadState.LocalCache:

                        if (redrawList) { TagListReloadFromState(); }
                        panTagUnavailable.Visibility = System.Windows.Visibility.Collapsed;
                        gridTag.Visibility = Visibility.Visible;
                        break;
                    case GlobalState.DataLoadState.Unavailable:
                        panTagUnavailable.Visibility = System.Windows.Visibility.Visible;
                        gridTag.Visibility = Visibility.Collapsed;
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                logger.Debug("RefreshAndDisplayTagList has been cancelled.");
            }
            catch (Exception ex)
            {
                radTag.Tag = false;
                state.tagList = null;
                state.tagLoadState = GlobalState.DataLoadState.Unavailable;
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Tag list could not be loaded, there was an error: " + ex.Message);
            }
            finally
            {
                radTag.Tag = false;
            }
        }

        public void TagListReloadFromState()
        {
            long tagId = 0;
            RadioButton checkedButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedButton == null)
            {
                checkedButton = (RadioButton)wrapSystemTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            }

            if (checkedButton != null)
            {
                TagListTagRef current = (TagListTagRef)checkedButton.Tag;
                tagId = current.id;
            }

            wrapMyTags.Children.Clear();
            wrapSystemTags.Children.Clear();

            foreach (TagListTagRef tag in state.tagList.TagRef)
            {
                RadioButton newRadioButton = new RadioButton();

                newRadioButton.Content = tag.name;
                newRadioButton.Style = (Style)FindResource("styleRadioButton");
                newRadioButton.Template = (ControlTemplate)FindResource("templateRadioButton");
                newRadioButton.GroupName = "GroupTag";
                newRadioButton.Tag = tag;
                newRadioButton.AllowDrop = false;
                newRadioButton.Checked += new RoutedEventHandler(FetchTagImagesFirstAsync);

                if (tag.systemOwned == true)
                {
                    wrapSystemTags.Children.Add(newRadioButton);
                }
                else
                {
                    wrapMyTags.Children.Add(newRadioButton);
                }
            }

            //Re-check the selected checkbox, else check the first
            RadioButton recheckButton = null;

            if (tagId != 0)
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

                foreach (RadioButton currentButton in wrapSystemTags.Children.OfType<RadioButton>())
                {
                    TagListTagRef current = (TagListTagRef)currentButton.Tag;
                    if (current.id == tagId)
                    {
                        recheckButton = currentButton;
                        break;
                    }
                }
                if (recheckButton != null)
                    recheckButton.IsChecked = true;
            }

            UploadRefreshTagsList();
            ImageViewTagsRefreshList();
        }

        private void TagAddImagesRefreshTagsList()
        {
            lstTagAddImagesInclude.Items.Clear();

            foreach (TagListTagRef tagRef in state.tagList.TagRef.Where<TagListTagRef>(r => r.systemOwned == false))
            {
                if ((currentImageList.type == "Tag" && currentImageList.id != tagRef.id) || currentImageList.type != "Tag")
                {
                    ListBoxItem newItem = new ListBoxItem();
                    newItem.Content = tagRef.name;
                    newItem.Tag = tagRef;
                    lstTagAddImagesInclude.Items.Add(newItem);
                }
            }
        }

        async private Task PopulateTagMetaData(TagListTagRef tagListTagRef)
        {
            try
            {
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                ShowMessage(MessageType.Busy, "Retrieving Tag");

                Tag tag = await controller.TagGetMetaAsync(tagListTagRef, cancelTokenSource.Token);

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                txtTagName.Text = tag.Name;
                txtTagDescription.Text = tag.Desc;
                currentTag = tag;

                RefreshPanesAllControls(PaneMode.TagEdit);
                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                logger.Debug("PopulateTagMetaData has been cancelled");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem retrieving the tag meta data.  Error: " + ex.Message);
            }
        }

        async private Task AddRemoveImagesFromTag(bool add, string[] tagName)
        {
            ImageMoveList moveList = new ImageMoveList();
            moveList.ImageRef = new long[lstImageMainViewerList.SelectedItems.Count];
            
            int i = 0;
            foreach (GeneralImage image in lstImageMainViewerList.SelectedItems)
            {
                moveList.ImageRef[i] = image.imageId;
                i++;
            }

            try
            {
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                ShowMessage(MessageType.Busy, "Updating images in tag");
                for (int ii = 0; ii < tagName.Length; ii++)
                {
                    await controller.TagAddRemoveImagesAsync(tagName[ii], moveList, add, cancelTokenSource.Token);
                }
                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                await RefreshAndDisplayTagList(true);
                ConcludeBusyProcess();

                string message = moveList.ImageRef.Length.ToString() + " images were removed from the tag: " + tagName[0];
                if (add)
                    message = moveList.ImageRef.Length.ToString() + " images were added to the tags selected.";

                ShowMessage(MessageType.Info, message);
            }
            catch (OperationCanceledException)
            {
                logger.Debug("AddRemoveImagesFromTag has been cancelled");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "Images could not be add\removed from the Tag, there was an error on the server: " + ex.Message);
            }
        }

        private void ImageViewTagsRefreshList()
        {
            lstImageViewTagList.Items.Clear();
            foreach (TagListTagRef tagRef in state.tagList.TagRef.Where<TagListTagRef>(r => r.systemOwned == false))
            {
                ListBoxItem newItem = new ListBoxItem();
                newItem.Content = tagRef.name;
                newItem.Tag = tagRef;
                lstImageViewTagList.Items.Add(newItem);
            }
        }
        #endregion

        #region Tag Event Handlers
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
            try
            {
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                ShowMessage(MessageType.Busy, "Deleting tag");

                await controller.TagDeleteAsync(currentTag, cancelTokenSource.Token);

                imageMainViewerList.Clear();
                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                logger.Debug("cmdTagDelete_Click has been cancelled");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem deleting the tag.  Error: " + ex.Message);
            }
            finally
            {
                RefreshPanesAllControls(PaneMode.TagView);
                RefreshAndDisplayTagList(true);
            }
        }

        private void cmdTagAdd_Click(object sender, RoutedEventArgs e)
        {
            txtTagName.Text = "";
            txtTagDescription.Text = "";
            RefreshPanesAllControls(PaneMode.TagAdd);
        }

        async private void cmdTagEdit_Click(object sender, RoutedEventArgs e)
        {
            RadioButton checkedButton = (RadioButton)wrapSystemTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedButton != null)
            {
                ShowMessage(MessageType.Warning, "You can't edit a system Tag, only user created tags can be changed.");
                return;
            }

            checkedButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedButton == null)
            {
                ShowMessage(MessageType.Warning, "You must select a tag to continue.");
                return;
            }

            TagListTagRef tagListTagRef = (TagListTagRef)checkedButton.Tag;

            await PopulateTagMetaData(tagListTagRef);
        }

        private void cmdTagCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.TagView);
        }

        async private void cmdTagSave_Click(object sender, RoutedEventArgs e)
        {
            if (txtTagName.Text.Length < 1)
            {
                ShowMessage(MessageType.Warning, "You must enter a tag name to continue.");
                return;
            }

            //Check tag name is unique
            foreach (TagListTagRef tagRef in state.tagList.TagRef)
            {
                if (txtTagName.Text == tagRef.name)
                {
                    if (currentPane == PaneMode.TagAdd || currentTag.id != tagRef.id)
                    {
                        ShowMessage(MessageType.Warning, "Tag name must be unique.  " + tagRef.name + " has already been used.");
                        return;
                    }
                }
            }

            try
            {

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                ShowMessage(MessageType.Busy, "Saving Tag data");

                if (currentPane == PaneMode.TagAdd)
                {
                    Tag tag = new Tag();
                    tag.Name = txtTagName.Text;
                    tag.Desc = txtTagDescription.Text;

                    await controller.TagCreateAsync(tag, cancelTokenSource.Token);
                }
                else
                {
                    string oldTagName = currentTag.Name;
                    currentTag.Name = txtTagName.Text;
                    currentTag.Desc = txtTagDescription.Text;

                    await controller.TagUpdateAsync(currentTag, oldTagName, cancelTokenSource.Token);
                }

                ConcludeBusyProcess();

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;
            }
            catch (OperationCanceledException)
            {
                logger.Debug("cmdTagSave_Click has been cancelled");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem updating tag data on the server.  Error: " + ex.Message);
            }
            finally
            {
                RefreshPanesAllControls(PaneMode.TagView);
                RefreshAndDisplayTagList(true);
            }

        }

        async private void TagRemoveImages_Click(object sender, RoutedEventArgs e)
        {
            if (currentPane == PaneMode.TagView)
            {
                RadioButton checkedButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
                if (checkedButton != null)
                {
                    TagListTagRef tagListTagRefTemp = (TagListTagRef)checkedButton.Tag;
                    int count = lstImageMainViewerList.SelectedItems.Count;
                    if (count > 0)
                    {
                        if (MessageBox.Show("Do you want to remove the " + count.ToString() + " selected images from the tag: " + tagListTagRefTemp.name + "?", "ManageWalla", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            await AddRemoveImagesFromTag(false, new string[1] { tagListTagRefTemp.name });
                        }
                    }
                    else
                    {
                        ShowMessage(MessageType.Warning, "You must select at least one image to remove it from the tag: " + tagListTagRefTemp.name);
                    }
                }
                else
                {
                    ShowMessage(MessageType.Warning, "You can only remove images from a tag when you have selected a tag.");
                }
            }
            else
            {
                ShowMessage(MessageType.Warning, "You can only remove images from a tag when you have selected a tag.");
            }
        }

        #endregion

        #region Upload Method and Event Handlers
        
        private void UploadRefreshTagsList()
        {
            lstUploadTagList.Items.Clear();
            foreach (TagListTagRef tagRef in state.tagList.TagRef.Where<TagListTagRef>(r => r.systemOwned == false))
            {
                ListBoxItem newItem = new ListBoxItem();
                newItem.Content = tagRef.name;
                newItem.Tag = tagRef;
                lstUploadTagList.Items.Add(newItem);
            }
        }

        private void ResetAllMetaUpdates()
        {
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
            for (int i = 0; i < uploadFots.Count; i++)
            {
                uploadFots[i].ResetMeta();
            }
        }

        private void ResetUploadState()
        {
            uploadUIState.GotSubFolders = false;
            uploadUIState.CategoryName = "";
            uploadUIState.CategoryDesc = "";
            uploadUIState.MapToSubFolders = false;
            uploadUIState.UploadToNewCategory = false;
            uploadUIState.Mode = UploadUIState.UploadMode.None;
            uploadUIState.RootCategoryId = state.userApp.UserDefaultCategoryId;
            uploadUIState.RootCategoryName = GetCategoryName(state.userApp.UserDefaultCategoryId);
            uploadUIState.RootFolder = "";
            uploadUIState.AutoUploadCategoryName = GetCategoryName(state.userApp.UserAppCategoryId);
            uploadUIState.AutoUploadFolder = @"C:\temp\AutoUpload";
            uploadUIState.AutoCategoryId = state.userApp.UserAppCategoryId;
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

        async private void cmdUploadImportFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
            if (folderDialog.SelectedPath.Length > 0)
            {
                if (tweakUploadImageSize)
                {
                    TweakImageMarginSize(DateTime.Now, currentPane);
                    tweakUploadImageSize = false;
                }

                if (cancelUploadTokenSource != null)
                    cancelUploadTokenSource.Cancel();

                CancellationTokenSource newCancelUploadTokenSource = new CancellationTokenSource();
                cancelUploadTokenSource = newCancelUploadTokenSource;

                uploadUIState.MapToSubFolders = false;
                DirectoryInfo folder = new DirectoryInfo(folderDialog.SelectedPath);

                try
                {
                    ShowMessage(MessageType.Busy, "Files being analysed for upload");

                    List<string> responses = null;
                    if (folder.GetDirectories().Length > 0)
                    {
                        uploadUIState.GotSubFolders = true;
                        uploadUIState.RootFolder = folderDialog.SelectedPath;
                        if (MessageBox.Show("Do you want to add images from the sub folders too ?", "ManageWalla", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            responses = await controller.LoadImagesFromFolder(folder, true, uploadFots, cancelUploadTokenSource.Token);
                            uploadUIState.MapToSubFolders = true;
                        }
                        else
                        {
                            responses = await controller.LoadImagesFromFolder(folder, false, uploadFots, cancelUploadTokenSource.Token);
                        }
                    }
                    else
                    {
                        uploadUIState.RootFolder = "";
                        responses = await controller.LoadImagesFromFolder(folder, false, uploadFots, cancelUploadTokenSource.Token);
                    }

                    if (newCancelUploadTokenSource == cancelUploadTokenSource)
                        cancelUploadTokenSource = null;

                    if (responses.Count == 0)
                    {
                        ConcludeBusyProcess();
                    }
                    else
                    {
                        StringBuilder messageBuilder = new StringBuilder();
                        messageBuilder.AppendLine("Some files could not be prepared for upload:");
                        foreach (string response in responses)
                        {
                            messageBuilder.AppendLine(response);
                        }
                        ShowMessage(MessageType.Error, messageBuilder.ToString());
                    }
                    
                }
                catch (OperationCanceledException)
                {
                    logger.Debug("cmdUploadImportFolder_Click has been cancelled.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MessageType.Error, "There was an unexpected error whilst preparing files for uploading.  Error: " + ex.Message);
                }

                if (lstUploadImageFileList.Items.Count > 0) //&& lstUploadImageFileList.SelectedItems.Count == 0
                {
                    TweakImageMarginSize(DateTime.Now, currentPane);  
                    uploadUIState.Mode = UploadUIState.UploadMode.Folder;
                    RefreshPanesAllControls(PaneMode.Upload);
                    lstUploadImageFileList.SelectedIndex = 0;
                }
                RefreshOverallPanesStructure(currentPane);
            }
        }

        async private void cmdUploadImportFiles_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog();

            openDialog.Filter = @"Image Files (*.JPG;*.JPEG;*.TIF;*.TIFF;*.PNG;*.GIF;*.PSD;*.CR2;*.ARW;*.NEF;*.BMP)|*.JPG;*.JPEG;*.TIF;*.TIFF;*.PNG;*.GIF;*.PSD;*.CR2;*.ARW;*.NEF;*.BMP";

            openDialog.Multiselect = true;
            System.Windows.Forms.DialogResult result = openDialog.ShowDialog();
            if (openDialog.FileNames.Length > 0)
            {
                if (tweakUploadImageSize)
                {
                    TweakImageMarginSize(DateTime.Now, currentPane);
                    tweakUploadImageSize = false;
                }

                if (cancelUploadTokenSource != null)
                    cancelUploadTokenSource.Cancel();

                CancellationTokenSource newCancelUploadTokenSource = new CancellationTokenSource();
                cancelUploadTokenSource = newCancelUploadTokenSource;

                try
                {
                    ShowMessage(MessageType.Busy, "Files being analysed for upload");

                    List<string> responses = null;
                    responses = await controller.LoadImagesFromArray(openDialog.FileNames, uploadFots, cancelUploadTokenSource.Token);

                    if (newCancelUploadTokenSource == cancelUploadTokenSource)
                        cancelUploadTokenSource = null;

                    if (responses.Count == 0)
                    {
                        ConcludeBusyProcess();
                    }
                    else
                    {
                        StringBuilder messageBuilder = new StringBuilder();
                        messageBuilder.AppendLine("Some files could not be prepared for upload:");
                        foreach (string response in responses)
                        {
                            messageBuilder.AppendLine(response);
                        }
                        ShowMessage(MessageType.Error, messageBuilder.ToString());
                    }
                }
                catch (OperationCanceledException)
                {
                    logger.Debug("cmdUploadImportFiles_Click has been cancelled.");
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MessageType.Error, "There was an unexpected error whilst preparing files for uploading.  Error: " + ex.Message);
                }
            }

            if (lstUploadImageFileList.Items.Count > 0) //&& lstUploadImageFileList.SelectedItems.Count == 0
            {
                TweakImageMarginSize(DateTime.Now, currentPane);  
                uploadUIState.GotSubFolders = false;
                uploadUIState.Mode = UploadUIState.UploadMode.Images;
                RefreshPanesAllControls(PaneMode.Upload);
                lstUploadImageFileList.SelectedIndex = 0;
            }
            RefreshOverallPanesStructure(currentPane);
        }

        async private void cmdUploadClear_Click(object sender, RoutedEventArgs e)
        {
            if (uploadUIState.Uploading)
            {
                uploadUIState.Uploading = false;
                cancelUploadTokenSource.Cancel();
                ShowMessage(MessageType.Info, "Uploading has been cancelled.");
            }
            else
            {
                if (uploadUIState.Mode == UploadUIState.UploadMode.Auto)
                {
                    //await Task.Run(new Action(() => {  }));
                    DoAutoUploadAsync(true);
                }
                else
                {
                    uploadFots.Clear();
                    ResetUploadState();
                }
            }
            RefreshPanesAllControls(PaneMode.Upload);
            RefreshOverallPanesStructure(currentPane);
        }

        async private void cmdUploadResetMeta_Click(object sender, RoutedEventArgs e)
        {
            ResetAllMetaUpdates();
            await controller.ResetMeFotsMeta(uploadFots);
            RefreshPanesAllControls(PaneMode.Upload);
        }

        private void lstUploadImageFileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UploadTagListReload();
        }

        private void lstUploadTagList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
               UpdateUploadTagCollection();
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
            try
            {

                //TreeViewItem item = (TreeViewItem)treeUploadCategoryView.SelectedItem;
                if (uploadUIState.RootCategoryId < 0)
                {
                    ShowMessage(MessageType.Warning, "You must select a Category for your uploaded images to be stored in.");
                    return;
                }

                if (uploadUIState.UploadToNewCategory && uploadUIState.CategoryName.Length < 1)
                {
                    ShowMessage(MessageType.Warning, "You have selected to add a new category, you must enter a name to continue.");
                    return;
                }

                if (!uploadUIState.UploadToNewCategory && uploadUIState.RootCategoryId == state.userApp.UserDefaultCategoryId)
                {
                    ShowMessage(MessageType.Warning, "You cannot upload images directly to the root category, please create a new category.");
                    return;
                }

                //CategoryListCategoryRef category = (CategoryListCategoryRef)item.Tag;
                //long categoryId = category.id;

                uploadUIState.Uploading = true;
                RefreshOverallPanesStructure(PaneMode.Upload);
                RefreshPanesAllControls(PaneMode.Upload);

                if (cancelUploadTokenSource != null)
                    cancelUploadTokenSource.Cancel();

                CancellationTokenSource newCancelUploadTokenSource = new CancellationTokenSource();
                cancelUploadTokenSource = newCancelUploadTokenSource;

                List<string> responses = await controller.UploadManualAsync(uploadFots, uploadUIState, cancelUploadTokenSource.Token);

                if (newCancelUploadTokenSource == cancelUploadTokenSource)
                    cancelUploadTokenSource = null;

                if (responses.Count > 0)
                {
                    StringBuilder messageBuilder = new StringBuilder();
                    messageBuilder.AppendLine("Some images encountered errors being uploaded.  Check the Upload Status page for more details.");
                    foreach (string response in responses)
                    {
                        messageBuilder.AppendLine(response);
                    }
                    ShowMessage(MessageType.Error, messageBuilder.ToString());
                }

                if (newCancelUploadTokenSource == cancelUploadTokenSource)
                    cancelUploadTokenSource = null;

                ResetUploadState();
            }
            catch (OperationCanceledException)
            {
                logger.Debug("cmdUploadAll_Click has been cancelled.");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "During the upload there was an unexpected error: " + ex.Message + "  Please check the upload status window for details.");
            }
            finally
            {
                uploadUIState.Uploading = false;
                RefreshPanesAllControls(PaneMode.Upload);
            }
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

        #region Account Methods and Handlers
        private void RefreshUploadStatusFromStateList()
        {
            /* Clear list and add local image load errors */
            //uploadStatusListBind.Clear();

            datUploadsInProgress.GetBindingExpression(DataGrid.ItemsSourceProperty).UpdateTarget();
            return;

            //foreach (UploadImage currentUploadImage in uploadFots.Where(r => r.State == UploadImage.UploadState.ClientError))
            foreach (UploadImage currentUploadImage in uploadFots)
            {
                UploadStatusListImageUploadRef newImageRef = new UploadStatusListImageUploadRef();
                newImageRef.imageStatus = -1;
                newImageRef.name = currentUploadImage.Meta.Name;
                newImageRef.lastUpdated = DateTime.Now;
                //newImageRef.errorMessage = currentUploadImage.;

                //uploadStatusListBind.Add(newImageRef);
            }

            /* Load in existing upload entries */
            if (state.uploadStatusList != null)
            {
                foreach (UploadStatusListImageUploadRef currentImageUploadRef in state.uploadStatusList.ImageUploadRef)
                {
                    //uploadStatusListBind.Add(currentImageUploadRef);
                }
            }

            /* Refresh message and icon */
            

            //TODO update message and icon
        }

        async private Task RefreshUploadStatusStateAsync(bool forceUpdate)
        {
            try
            {
                if (state.connectionState != GlobalState.ConnectionState.NoAccount &&
                    (state.uploadStatusListState == GlobalState.DataLoadState.No || forceUpdate || state.tagLoadState == GlobalState.DataLoadState.LocalCache))
                {
                    ShowMessage(MessageType.Busy, "Refreshing upload history list");

                    if (cancelTokenSource != null)
                        cancelTokenSource.Cancel();

                    CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                    cancelTokenSource = newCancelTokenSource;

                    await controller.RefreshUploadStatusListAsync(cancelTokenSource.Token);

                    if (newCancelTokenSource == cancelTokenSource)
                        cancelTokenSource = null;
                }

                switch (state.uploadStatusListState)
                {
                    case GlobalState.DataLoadState.Loaded:
                    case GlobalState.DataLoadState.LocalCache:
                        RefreshUploadStatusFromStateList();
                        panUploadStatusListUnavailable.Visibility = System.Windows.Visibility.Collapsed;
                        //datUploadStatusList.Visibility = Visibility.Visible;
                        break;
                    case GlobalState.DataLoadState.Unavailable:
                        panUploadStatusListUnavailable.Visibility = System.Windows.Visibility.Visible;
                        //datUploadStatusList.Visibility = Visibility.Collapsed;
                        break;
                }

                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                logger.Debug("RefreshUploadStatusStateAsync has been cancelled.");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Retrieving the upload history has failed with an unexpected problem: " + ex.Message);
            }
        }

        async private void tabAccount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabAccount.SelectedIndex == 1)
            {
                await RefreshUploadStatusStateAsync(false);
            }
        }

        async private void cmdUploadStatusRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshUploadStatusStateAsync(true);
        }

        private void cmdUserAppCancel_Click(object sender, RoutedEventArgs e)
        {
            AccountRefreshFromState();
            RefreshPanesAllControls(PaneMode.Account);
        }

        async private void cmdUserAppSave_Click(object sender, RoutedEventArgs e)
        {
            await UserAppSave();
            //AccountRefreshFromState();
            //RefreshPanesAllControls(PaneMode.Account);
        }

        private void cmdUserAppEdit_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.AccountEdit);
        }

        async private void cmdAccountLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string profileName = txtAccountProfileName.Text;
                string password = txtAccountPassword.Text;

                if (profileName.Length < 1 || password.Length < 1)
                {
                    ShowMessage(MessageType.Warning, "You must enter your profile name and password to continue");
                    return;
                }

                await Initialise(profileName, password, true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "The logon process failed with an unexpected problem: " + ex.Message);
                RefreshPanesAllControls(PaneMode.Account);
            }
        }

        private void AccountRefreshFromState()
        {
            lblAccountType.Content = state.account.AccountTypeName;
            lblAccountOpen.Content = state.account.OpenDate.ToShortDateString();
            lblAccountStorageLimitGB.Content = state.account.StorageGBLimit + "GB";
            lblAccountCurrentUtil.Content = state.account.StorageGBCurrent + "GB " + state.account.TotalImages.ToString() + " Images";

            chkAccountAutoUpload.IsChecked = state.userApp.AutoUpload;
            lblAccountAutoUploadFolder.Content = state.userApp.AutoUploadFolder;
            lblAccountAutoUploadFolderAbbrev.Content = StringTrim(state.userApp.AutoUploadFolder,80);

            lblAccountImageCopyFolder.Content = state.userApp.MainCopyFolder;
            lblAccountImageCopyFolderAbbrev.Content = StringTrim(state.userApp.MainCopyFolder,80);

            lblAccountImageCopyStatus.Content = " TODO ";
            sldAccountImageCopySize.Value = state.userApp.MainCopyCacheSizeMB;

            lblAccountEmail.Content = state.account.Email;
            txtAccountPassword.Text = state.account.Password;
            txtAccountProfileName.Text = state.account.ProfileName;
        }

        private string StringTrim(string input, int length)
        {
            string abbrev = input;
            if (abbrev.Length > length-3)
            {
                abbrev = "..." + input.Substring(input.Length - length - 3);
            }
            return abbrev; 
        }

        async private Task UserAppSave()
        {
            try
            {
                ShowMessage(MessageType.Busy, "Application settings being saved");

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                UserApp changedUserApp = new UserApp();
                changedUserApp.AppId = state.userApp.AppId;
                changedUserApp.id = state.userApp.id;
                changedUserApp.MachineName = state.userApp.MachineName;
                changedUserApp.version = state.userApp.version;

                changedUserApp.AutoUpload = (bool)chkAccountAutoUpload.IsChecked;
                changedUserApp.AutoUploadFolder = (string)lblAccountAutoUploadFolder.Content;

                changedUserApp.MainCopyFolder = (string)lblAccountImageCopyFolder.Content;
                changedUserApp.MainCopyCacheSizeMB = Convert.ToInt32(sldAccountImageCopySize.Value);

                await controller.UserAppUpdateAsync(changedUserApp, cancelTokenSource.Token);
                await controller.SetUserApp(cancelTokenSource.Token);

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;
            }
            catch (OperationCanceledException)
            {
                logger.Debug("AccountSave has been cancelled.");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "The account save process failed with an unexpected problem: " + ex.Message);
            }
            finally
            {
                AccountRefreshFromState();
                RefreshPanesAllControls(PaneMode.Account);
            }
        }

        async private Task Login(string profileName, string password)
        {
            try
            {
                ShowMessage(MessageType.Busy, "Logging onto FotoWalla");

                //string profileName = (onAccountForm) ? txtAccountProfileName.Text : state.account.ProfileName;
                //string password = (onAccountForm) ? txtAccountPassword.Text : state.account.Password;
                string logonResponse = await controller.Logon(profileName, password);

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                switch (state.connectionState)
                {
                    case GlobalState.ConnectionState.LoggedOn:
                        await controller.AccountDetailsGet(cancelTokenSource.Token);
                        state.account.Password = password;
                        await controller.SetPlatform();
                        await controller.SetUserApp(cancelTokenSource.Token);
                        AccountRefreshFromState();
                        ShowMessage(MessageType.Info, "Account: " + state.account.ProfileName + " has been connected with FotoWalla");
                        break;
                    //case GlobalState.ConnectionState.Offline:
                    //    ShowMessage(MessageType.Info, "No internet connection could be established with FotoWalla, working in Offline mode");
                    //    break;
                    case GlobalState.ConnectionState.FailedLogin:
                        ShowMessage(MessageType.Info, "The logon for: " + profileName + ", failed with the message: " + logonResponse);
                        //cmdAccount.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        break;
                }

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;
            }
            catch (OperationCanceledException)
            {
                logger.Debug("Login has been cancelled.");
                RefreshPanesAllControls(PaneMode.Account);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "The logon process failed with an unexpected problem: " + ex.Message);
                RefreshPanesAllControls(PaneMode.Account);
            }
            finally
            {
                //Double check working message is closed.
                DisplayConnectionStatus();
                ConcludeBusyProcess();
            }
        }
        #endregion

        #region Gallery Methods
        async private Task RefreshAndDisplayGalleryList(bool forceRefresh)
        {
            try
            {

                bool isBusy = bool.Parse(radGallery.Tag.ToString());
                if (isBusy) { return; }

                bool redrawList = false;

                //Catch first time loads, user intiated refresh and when user was offline and is now online.  But only if logged on.
                if (state.connectionState != GlobalState.ConnectionState.NoAccount &&
                    (state.galleryLoadState == GlobalState.DataLoadState.No || forceRefresh || state.galleryLoadState == GlobalState.DataLoadState.LocalCache))
                {
                    //gridGallery.Visibility = Visibility.Collapsed;

                    if (cancelTokenSource != null)
                        cancelTokenSource.Cancel();

                    CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                    cancelTokenSource = newCancelTokenSource;

                    radGallery.Tag = true;

                    await controller.GalleryRefreshListAsync(cancelTokenSource.Token);
                    redrawList = true;

                    if (newCancelTokenSource == cancelTokenSource)
                        cancelTokenSource = null;
                }

                switch (state.galleryLoadState)
                {
                    case GlobalState.DataLoadState.Loaded:
                    case GlobalState.DataLoadState.LocalCache:
                        if (redrawList) { GalleryListReloadFromState(); }
                        panGalleryUnavailable.Visibility = System.Windows.Visibility.Collapsed;
                        gridGallery.Visibility = Visibility.Visible;
                        break;
                    case GlobalState.DataLoadState.Unavailable:
                        panGalleryUnavailable.Visibility = System.Windows.Visibility.Visible;
                        gridGallery.Visibility = Visibility.Collapsed;
                        break;
                }
            }
            catch (OperationCanceledException)
            {
                logger.Debug("RefreshAndDisplayGalleryList has been cancelled.");
            }
            catch (Exception ex)
            {
                radGallery.Tag = false;
                state.galleryList = null;
                state.galleryLoadState = GlobalState.DataLoadState.Unavailable;
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Gallery list could not be loaded, there was an error: " + ex.Message);
            }
            finally
            {
                radGallery.Tag = false;
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
            if (state.galleryList.GalleryRef == null)
                return;

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

            RadioButton checkedGalleryButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedButton != null)
            {
                checkedButton.IsChecked = false;
            }

            //Re-check the selected checkbox, else check the first
            RadioButton recheckButton = null;
            if (galleryId != 0)
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

                if (recheckButton != null)
                    recheckButton.IsChecked = true;
            }
        }

        private bool GalleryPopulateSectionList(GalleryListGalleryRef galleryListRefTemp)
        {
            if (galleryListRefTemp.SectionRef != null && galleryListRefTemp.SectionRef.Length > 0)
            {
                cmbGallerySectionVert.Items.Clear();
                cmbGallerySection.Items.Clear();

                foreach (GalleryListGalleryRefSectionRef section in galleryListRefTemp.SectionRef)
                {
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = section.name;
                    item.Tag = section;
                    cmbGallerySection.Items.Add(item);

                    ComboBoxItem itemVert = new ComboBoxItem();
                    itemVert.Content = section.name;
                    itemVert.Tag = section;
                    cmbGallerySectionVert.Items.Add(itemVert);
                }

                cmbGallerySectionVert.SelectedIndex = 0;
                
                cmbGallerySection.Visibility = Visibility.Visible;
                cmbGallerySectionVert.Visibility = Visibility.Visible;
                return true;
            }
            else
            {
                cmbGallerySection.Visibility = Visibility.Collapsed;
                cmbGallerySectionVert.Visibility = Visibility.Collapsed;
                return false;
            }
        }

        async private Task PopulateGalleryMetaData(GalleryListGalleryRef galleryListGalleryRef)
        {
            try
            {
                ShowMessage(MessageType.Busy, "Loading gallery details");

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                GalleryRefreshTagsListFromState();
                GalleryRefreshCategoryList();

                Gallery gallery = await controller.GalleryGetMetaAsync(galleryListGalleryRef, cancelTokenSource.Token);

                txtGalleryName.Text = gallery.Name;
                txtGalleryDescription.Text = gallery.Desc;
                txtGalleryPassword.Text = gallery.Password;
                cmbGalleryAccessType.SelectedIndex = gallery.AccessType;
                cmbGalleryGroupingType.SelectedIndex = gallery.GroupingType;

                cmbGallerySelectionType.SelectedIndex = gallery.SelectionType;
                cmbGalleryPresentationType.SelectedIndex = gallery.PresentationId;
                cmbGalleryStyleType.SelectedIndex = gallery.StyleId;

                chkGalleryShowName.IsChecked = gallery.ShowGalleryName;
                chkGalleryShowDesc.IsChecked = gallery.ShowGalleryDesc;
                chkGalleryShowImageName.IsChecked = gallery.ShowImageName;
                chkGalleryShowImageDesc.IsChecked = gallery.ShowImageDesc;
                chkGalleryShowImageMeta.IsChecked = gallery.ShowImageMeta;

                //TODO select radio buttons for tags.
                foreach (GalleryTagRef tagRef in gallery.Tags)
                {
                    if (tagRef.exclude)
                    {
                        foreach (ListBoxItem current in lstGalleryMyTagListExclude.Items)
                        {
                            TagListTagRef tagRefInList = (TagListTagRef)current.Tag;
                            if (tagRefInList.id == tagRef.tagId)
                            {
                                current.IsSelected = true;
                                break;
                            }
                        }

                        foreach (ListBoxItem current in lstGallerySystemTagListExclude.Items)
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
                        foreach (ListBoxItem current in lstGalleryMyTagListInclude.Items)
                        {
                            TagListTagRef tagRefInList = (TagListTagRef)current.Tag;
                            if (tagRefInList.id == tagRef.tagId)
                            {
                                current.IsSelected = true;
                                break;
                            }
                        }

                        foreach (ListBoxItem current in lstGallerySystemTagListInclude.Items)
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

                GalleryCategoryApplyGallerySettings(gallery.Categories);

                foreach (TreeViewItem child in treeGalleryCategoryView.Items)
                    GalleryCategoryRecursiveRelatedUpdates(child, false);

                currentGallery = gallery;

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                RefreshOverallPanesStructure(PaneMode.GalleryEdit);
                RefreshPanesAllControls(PaneMode.GalleryEdit);
            }
            catch (OperationCanceledException)
            {
                logger.Debug("PopulateGalleryMetaData has been cancelled.");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem retrieving the gallery data.  Error: " + ex.Message);
            }
            finally
            {
                ConcludeBusyProcess();
            }
        }

        private void GalleryRefreshTagsListFromState()
        {
            lstGalleryMyTagListExclude.Items.Clear();
            lstGallerySystemTagListExclude.Items.Clear();
            lstGalleryMyTagListInclude.Items.Clear();
            lstGallerySystemTagListInclude.Items.Clear();

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

                if (tagRef.systemOwned == false)
                {
                    lstGalleryMyTagListExclude.Items.Add(newItemExclude);
                    lstGalleryMyTagListInclude.Items.Add(newItemInclude);
                }
                else
                {
                    lstGallerySystemTagListExclude.Items.Add(newItemExclude);
                    lstGallerySystemTagListInclude.Items.Add(newItemInclude);
                }
            }
        }

        private void GalleryCheckForIncludeConflict(object sender, RoutedEventArgs e)
        {
            ListBoxItem listBoxItem = (ListBoxItem)sender;
            TagListTagRef tagRef = (TagListTagRef)listBoxItem.Tag;

            foreach (ListBoxItem current in lstGalleryMyTagListExclude.Items)
            {
                TagListTagRef tagRefInList = (TagListTagRef)current.Tag;
                if (tagRefInList.id == tagRef.id)
                {
                    current.IsSelected = false;
                    break;
                }
            }

            foreach (ListBoxItem current in lstGallerySystemTagListExclude.Items)
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
            ListBoxItem listBoxItem = (ListBoxItem)sender;
            TagListTagRef tagRef = (TagListTagRef)listBoxItem.Tag;

            foreach (ListBoxItem current in lstGalleryMyTagListInclude.Items)
            {
                TagListTagRef tagRefInList = (TagListTagRef)current.Tag;
                if (tagRefInList.id == tagRef.id)
                {
                    current.IsSelected = false;
                    break;
                }
            }

            foreach (ListBoxItem current in lstGallerySystemTagListInclude.Items)
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
            if (baseCategory == null) { return; }

            treeGalleryCategoryView.Items.Clear();
            GalleryCategoryAddTreeViewLevel(baseCategory.id, null);

            if (currentPane == PaneMode.GalleryEdit)
            {
                GalleryCategoryApplyGallerySettings(currentGallery.Categories);
            }

            foreach (TreeViewItem child in treeGalleryCategoryView.Items)
                GalleryCategoryRecursiveRelatedUpdates(child, false);
        }

        private void GalleryCategoryAddTreeViewLevel(long parentId, TreeViewItem currentHeader)
        {
            foreach (CategoryListCategoryRef current in state.categoryList.CategoryRef.Where(r => r.parentId == parentId))
            {
                TreeViewItem newItem = GetTreeView(current.id, current.name, current.desc);
                newItem.Tag = current.id;

                if (currentHeader == null)
                {
                    treeGalleryCategoryView.Items.Add(newItem);
                }
                else
                {
                    currentHeader.Items.Add(newItem);
                }

                GalleryCategoryAddTreeViewLevel(current.id, newItem);
            }
        }

        private TreeViewItem GetTreeView(long categoryId, string name, string desc)
        {
            /*
                <StackPanel Orientation="Horizontal">
                    <ComboBox SelectionChanged="GalleryCategory_SelectionChanged">
                        <ComboBoxItem> - </ComboBoxItem>
                        <ComboBoxItem> Y </ComboBoxItem>
                        <ComboBoxItem>ALL</ComboBoxItem>
                    </ComboBox>
                    <TextBlock Text="" ToolTip=""/>
                </StackPanel>
             */

            TreeViewItem item = new TreeViewItem();
            item.IsExpanded = true;
            item.IsEnabled = true;
            item.Style = (Style)FindResource("styleTreeViewItemWithCombo");

            // create stack panel
            StackPanel stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            ComboBox newCmb = new ComboBox();
            newCmb.Name = "cmbGalleryCategoryItem" + categoryId.ToString();
            newCmb.Style = (Style)FindResource("styleComboBoxInTreeView");
            newCmb.SelectedIndex = 0;
            newCmb.Width = 70.0;

            try { this.UnregisterName(newCmb.Name); }
            catch { }

            this.RegisterName(newCmb.Name, newCmb);

            ComboBoxItem entryNone = new ComboBoxItem();
            StackPanel stackEntryNone = new StackPanel();
            stackEntryNone.Orientation = Orientation.Horizontal;
            stackEntryNone.VerticalAlignment = VerticalAlignment.Center;

            Image imgEntryNone = new Image();
            imgEntryNone.Source = (ImageSource)FindResource("menuItemImageSrc");
            imgEntryNone.Height = 20.0;
            imgEntryNone.Width = 20.0;

            TextBlock txtEntryNone = new TextBlock();
            txtEntryNone.Text = "N";
            txtEntryNone.Margin = new Thickness(4.0);

            stackEntryNone.Children.Add(imgEntryNone);
            stackEntryNone.Children.Add(txtEntryNone);
            entryNone.Content = stackEntryNone;


            ComboBoxItem entryInclude = new ComboBoxItem();
            StackPanel stackInclude = new StackPanel();
            stackInclude.Orientation = Orientation.Horizontal;
            stackInclude.VerticalAlignment = VerticalAlignment.Center;

            Image imgEntryInclude = new Image();
            imgEntryInclude.Source = (ImageSource)FindResource("menuItemImageSrc");
            imgEntryInclude.Height = 20.0;
            imgEntryInclude.Width = 20.0;

            TextBlock txtEntryInclude = new TextBlock();
            txtEntryInclude.Text = "Y";
            txtEntryInclude.Margin = new Thickness(4.0);

            stackInclude.Children.Add(imgEntryInclude);
            stackInclude.Children.Add(txtEntryInclude);
            entryInclude.Content = stackInclude;


            ComboBoxItem entryAll = new ComboBoxItem();
            StackPanel stackEntryAll = new StackPanel();
            stackEntryAll.Orientation = Orientation.Horizontal;
            stackEntryAll.VerticalAlignment = VerticalAlignment.Center;

            Image imgEntryAll = new Image();
            imgEntryAll.Source = (ImageSource)FindResource("menuItemImageSrc");
            imgEntryAll.Height = 20.0;
            imgEntryAll.Width = 20.0;

            TextBlock txtEntryAll = new TextBlock();
            txtEntryAll.Text = "All";
            txtEntryAll.Margin = new Thickness(4.0);

            stackEntryAll.Children.Add(imgEntryAll);
            stackEntryAll.Children.Add(txtEntryAll);
            entryAll.Content = stackEntryAll;


            newCmb.Items.Add(entryNone);
            newCmb.Items.Add(entryInclude);
            newCmb.Items.Add(entryAll);

            newCmb.SelectionChanged += new SelectionChangedEventHandler(GalleryCategory_SelectionChanged);
            

            TextBlock newTextBlock = new TextBlock();
            newTextBlock.Text = name;
            newTextBlock.ToolTip = desc;
            newTextBlock.VerticalAlignment = VerticalAlignment.Center;

            // Add into stack
            stack.Children.Add(newCmb);
            stack.Children.Add(newTextBlock);

            // assign stack to header
            item.Header = stack;
            return item;
        }

        private void GalleryCategoryApplyGallerySettings(GalleryCategoryRef[] galleryCategories)
        {
            foreach (GalleryCategoryRef current in galleryCategories)
            {
                //Find category object related.
                ComboBox cmbCurrent = (ComboBox)treeGalleryCategoryView.FindName("cmbGalleryCategoryItem" + current.categoryId.ToString());
                if (cmbCurrent != null)
                {
                    if (current.recursive)
                    {
                        cmbCurrent.SelectedIndex = 2; //ALL
                    }
                    else
                    {
                        cmbCurrent.SelectedIndex = 1; //Y
                    }
                }
            }

            foreach (TreeViewItem child in treeGalleryCategoryView.Items)
                GalleryCategoryRecursiveRelatedUpdates(child, false);
        }

        private GalleryCategoryRef[] GalleryCategoryGetUpdateList()
        {
            ArrayList newUpdateList = new ArrayList();

            foreach (TreeViewItem child in treeGalleryCategoryView.Items)
                GalleryCategoryRecursiveGetUpdateList(child, newUpdateList);

            if (newUpdateList.Count == 0)
                return null;

            GalleryCategoryRef[] newCategoryRefs = new GalleryCategoryRef[newUpdateList.Count];
            for (int i = 0; i < newUpdateList.Count; i++)
            {
                newCategoryRefs[i] = (GalleryCategoryRef)newUpdateList[i];
            }

            return newCategoryRefs;
        }

        private void GalleryCategoryRecursiveGetUpdateList(TreeViewItem currentItem, ArrayList newUpdateList)
        {
            long currentId = (long)currentItem.Tag;
            ComboBox cmbCurrent = (ComboBox)treeGalleryCategoryView.FindName("cmbGalleryCategoryItem" + currentId.ToString());

            if (cmbCurrent.SelectedIndex == 2)
            {
                GalleryCategoryRef newRef = new GalleryCategoryRef();
                newRef.categoryId = currentId;
                newRef.categoryIdSpecified = true;
                newRef.recursive = true;
                newRef.recursiveSpecified = true;

                newUpdateList.Add(newRef);
                return;
            }
            else if (cmbCurrent.SelectedIndex == 1)
            {
                GalleryCategoryRef newRef = new GalleryCategoryRef();
                newRef.categoryId = currentId;
                newRef.categoryIdSpecified = true;
                newRef.recursive = false;
                newRef.recursiveSpecified = true;
                newUpdateList.Add(newRef);
            }

            foreach (TreeViewItem child in currentItem.Items)
                GalleryCategoryRecursiveGetUpdateList(child, newUpdateList);
        }

        private GalleryTagRef[] GalleryTagsUpdateList(int selectionType)
        {
            GalleryTagRef[] newTagUpdates = null;
            int currentItemIndex = 0;
            int excludeCount = lstGalleryMyTagListExclude.SelectedItems.Count + lstGallerySystemTagListExclude.SelectedItems.Count;
            int includeCount = lstGalleryMyTagListInclude.SelectedItems.Count + lstGallerySystemTagListInclude.SelectedItems.Count;
            
            if (selectionType == 0)
            {
                //Ignore tags to include.
                newTagUpdates = new GalleryTagRef[excludeCount];
            }
            else
            {
                newTagUpdates = new GalleryTagRef[excludeCount + includeCount];
            }

            if (selectionType != 0)
            {
                foreach (ListBoxItem current in lstGalleryMyTagListInclude.SelectedItems)
                {
                    TagListTagRef tagRef = (TagListTagRef)current.Tag;

                    GalleryTagRef galleryTag = new GalleryTagRef();
                    galleryTag.tagId = tagRef.id;
                    galleryTag.tagIdSpecified = true;
                    galleryTag.exclude = false;
                    galleryTag.excludeSpecified = true;
                    newTagUpdates[currentItemIndex] = galleryTag;
                    currentItemIndex++;
                }

                foreach (ListBoxItem current in lstGallerySystemTagListInclude.SelectedItems)
                {
                    TagListTagRef tagRef = (TagListTagRef)current.Tag;

                    GalleryTagRef galleryTag = new GalleryTagRef();
                    galleryTag.tagId = tagRef.id;
                    galleryTag.tagIdSpecified = true;
                    galleryTag.exclude = false;
                    galleryTag.excludeSpecified = true;
                    newTagUpdates[currentItemIndex] = galleryTag;
                    currentItemIndex++;
                }
            }

            foreach (ListBoxItem current in lstGalleryMyTagListExclude.SelectedItems)
            {
                TagListTagRef tagRef = (TagListTagRef)current.Tag;

                GalleryTagRef galleryTag = new GalleryTagRef();
                galleryTag.tagId = tagRef.id;
                galleryTag.tagIdSpecified = true;
                galleryTag.exclude = true;
                galleryTag.excludeSpecified = true;
                newTagUpdates[currentItemIndex] = galleryTag;
                currentItemIndex++;
            }

            foreach (ListBoxItem current in lstGallerySystemTagListExclude.SelectedItems)
            {
                TagListTagRef tagRef = (TagListTagRef)current.Tag;

                GalleryTagRef galleryTag = new GalleryTagRef();
                galleryTag.tagId = tagRef.id;
                galleryTag.tagIdSpecified = true;
                galleryTag.exclude = true;
                galleryTag.excludeSpecified = true;
                newTagUpdates[currentItemIndex] = galleryTag;
                currentItemIndex++;
            }
            return newTagUpdates;
        }

        private void GalleryCategoryRecursiveRelatedUpdates(TreeViewItem currentItem, bool parentRecursive)
        {
            long currentId = (long)currentItem.Tag;
            ComboBox cmbCurrent = (ComboBox)treeGalleryCategoryView.FindName("cmbGalleryCategoryItem" + currentId.ToString());

            if (parentRecursive)
            {
                currentItem.IsEnabled = false;
                cmbCurrent.SelectedIndex = 0;
            }
            else
            {
                currentItem.IsEnabled = true;
                if (cmbCurrent.SelectedIndex == 2)
                    parentRecursive = true;
            }

            foreach (TreeViewItem child in currentItem.Items)
                GalleryCategoryRecursiveRelatedUpdates(child, parentRecursive);
        }
        #endregion

        #region Gallery Event Handlers
        private void cmdGalleryView_Click(object sender, RoutedEventArgs e)
        {
            RadioButton checkedButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();

            if (checkedButton != null)
            {
                GalleryListGalleryRef galleryListGalleryRef = (GalleryListGalleryRef)checkedButton.Tag;
                string url = controller.GetGalleryUrl(galleryListGalleryRef.name, galleryListGalleryRef.urlComplex);
                System.Diagnostics.Process.Start(url);
                ShowMessage(MessageType.Info, "Browser will load web site");
            }
            else
            {
                ShowMessage(MessageType.Info, "No Gallery selected to view");
            }
        }

        private void cmdGalleryCopyUrl_Click(object sender, RoutedEventArgs e)
        {
            RadioButton checkedButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedButton != null)
            {
                GalleryListGalleryRef galleryListGalleryRef = (GalleryListGalleryRef)checkedButton.Tag;
                string url = controller.GetGalleryUrl(galleryListGalleryRef.name, galleryListGalleryRef.urlComplex);
                System.Windows.Clipboard.SetText(url);
                ShowMessage(MessageType.Info, "Web site URL copied to the clipboard");
            }
            else
            {
                ShowMessage(MessageType.Info, "No Gallery selected, for copying URL");
            }
            
        }

        private void cmdGalleryAdd_Click(object sender, RoutedEventArgs e)
        {
            txtGalleryName.Text = "";
            txtGalleryDescription.Text = "";
            txtGalleryPassword.Text = "";
            cmbGalleryAccessType.SelectedIndex = 0;
            cmbGalleryGroupingType.SelectedIndex = 0;
            cmbGalleryStyleType.SelectedIndex = 0;
            cmbGalleryPresentationType.SelectedIndex = 0;
            cmbGallerySelectionType.SelectedIndex = 0;
            chkGalleryShowName.IsChecked = false;
            chkGalleryShowDesc.IsChecked = false;
            chkGalleryShowImageName.IsChecked = false;
            chkGalleryShowImageDesc.IsChecked = false;
            chkGalleryShowImageMeta.IsChecked = false;

            GalleryRefreshTagsListFromState();
            GalleryRefreshCategoryList();

            RefreshOverallPanesStructure(PaneMode.GalleryAdd);
            RefreshPanesAllControls(PaneMode.GalleryAdd);
        }

        async private void cmdGalleryEdit_Click(object sender, RoutedEventArgs e)
        {
            RadioButton checkedButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();

            if (checkedButton != null)
            {
                GalleryListGalleryRef galleryListGalleryRef = (GalleryListGalleryRef)checkedButton.Tag;
                await PopulateGalleryMetaData(galleryListGalleryRef);
            }
            else
            {
                ShowMessage(MessageType.Warning, "You must select a Gallery to continue");
            }
        }

        private void txtCategoryGalleryName_PreviewTextInput(object sender, TextCompositionEventArgs e)
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
            if (currentPane == PaneMode.GalleryAdd || currentPane == PaneMode.GalleryEdit)
                RefreshPanesAllControls(currentPane);
        }

        async private void cmdGallerySave_Click(object sender, RoutedEventArgs e)
        {
            GalleryCategoryRef[] galleryCategories = null;
            int excludeCount = lstGalleryMyTagListExclude.SelectedItems.Count + lstGallerySystemTagListExclude.SelectedItems.Count;
            int includeCount = lstGalleryMyTagListInclude.SelectedItems.Count + lstGallerySystemTagListInclude.SelectedItems.Count;
            

            if (cmbGallerySelectionType.SelectedIndex != 1)
                galleryCategories = GalleryCategoryGetUpdateList();

            if (cmbGallerySelectionType.SelectedIndex == 0 && (galleryCategories == null || galleryCategories.Length == 0))
            {
                ShowMessage(MessageType.Warning, "The gallery does not have any catgories associated with it, so cannot be saved.");
                return;
            }
            else if (cmbGallerySelectionType.SelectedIndex == 1 && includeCount == 0)
            {
                ShowMessage(MessageType.Warning, "The gallery does not have any tags associated with it, so cannot be saved.");
                return;
            }
            else if (includeCount == 0 && galleryCategories.Length == 0)
            {
                ShowMessage(MessageType.Warning, "The gallery does not have any catgories or tags associated with it, so cannot be saved.");
                return;
            }

            if (cmbGalleryAccessType.SelectedIndex == 1 && txtGalleryPassword.Text.Length == 0)
            {
                ShowMessage(MessageType.Warning, "This gallery has been marked as password protected, but the password does not meet the minumimum criteria of being 8 charactors long.");
                return;
            }

            if (txtGalleryName.Text.Length == 0)
            {
                ShowMessage(MessageType.Warning, "You must select a name for your Gallery to continue.");
                return;
            }

            ShowMessage(MessageType.Busy, "Saving Gallery data");

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
            currentGallery.SelectionType = cmbGallerySelectionType.SelectedIndex;
            currentGallery.PresentationId = cmbGalleryPresentationType.SelectedIndex;
            currentGallery.StyleId = cmbGalleryStyleType.SelectedIndex;

            currentGallery.ShowGalleryName = (bool)chkGalleryShowName.IsChecked;
            currentGallery.ShowGalleryDesc = (bool)chkGalleryShowDesc.IsChecked;
            currentGallery.ShowImageName = (bool)chkGalleryShowImageName.IsChecked;
            currentGallery.ShowImageDesc = (bool)chkGalleryShowImageDesc.IsChecked;
            currentGallery.ShowImageMeta = (bool)chkGalleryShowImageMeta.IsChecked;

            /* Category add to object ************************************************ */
            if (currentGallery.SelectionType != 1)
                currentGallery.Categories = galleryCategories;

            /* Tags add to object ************************************************ */
            currentGallery.Tags = GalleryTagsUpdateList(currentGallery.SelectionType);

            /* Sorts add to object ************************************************ */
            //TODO


            try
            {
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;
                
                if (currentPane == PaneMode.GalleryAdd)
                {
                    await controller.GalleryCreateAsync(currentGallery, cancelTokenSource.Token);
                }
                else
                {
                    await controller.GalleryUpdateAsync(currentGallery, oldGalleryName, cancelTokenSource.Token);
                }

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                logger.Debug("cmdGallerySave_Click has been cancelled.");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Gallery could not be saved, there was an error on the server:" + ex.Message);
            }
            finally
            {
                RefreshOverallPanesStructure(PaneMode.GalleryView);
                RefreshPanesAllControls(PaneMode.GalleryView);
                RefreshAndDisplayGalleryList(true);
            }
        }

        private void cmdGalleryCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(PaneMode.GalleryView);
            RefreshPanesAllControls(PaneMode.GalleryView);
        }

        async private void cmdGalleryDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowMessage(MessageType.Busy, "Deleting Gallery");

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                await controller.GalleryDeleteAsync(currentGallery, cancelTokenSource.Token);

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                logger.Debug("cmdGalleryDelete_Click has been cancelled.");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Gallery could not be deleted, there was an error on the server:" + ex.Message);
            }
            finally
            {
                RefreshOverallPanesStructure(PaneMode.GalleryView);
                RefreshPanesAllControls(PaneMode.GalleryView);
                RefreshAndDisplayGalleryList(true);
            }
        }

        private void cmdGalleryRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshAndDisplayGalleryList(true);
        }

        private void cmbGallerySelectionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshPanesAllControls(currentPane);
        }

        private void cmbGallerySection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox sections = (ComboBox)sender;
            ComboBoxItem cmbItemSection = (ComboBoxItem)sections.SelectedItem;
            if (cmbItemSection != null)
            {
                GalleryListGalleryRefSectionRef section = (GalleryListGalleryRefSectionRef)cmbItemSection.Tag;
                FetchGalleryImagesSectionChangeAsync(section.id);
            }
        }

        private void cmbGallerySectionVert_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbGallerySection != null)
                cmbGallerySection.SelectedIndex = cmbGallerySectionVert.SelectedIndex;
        }

        private void cmbGalleryPresentationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshPanesAllControls(currentPane);
        }

        private void GalleryCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (TreeViewItem child in treeGalleryCategoryView.Items)
                GalleryCategoryRecursiveRelatedUpdates(child, false);
        }
        #endregion

        private void cmdUploadRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            UploadImage uploadImage = (UploadImage)lstUploadImageFileList.SelectedItem;
            if (uploadImage != null)
            {
                uploadFots.Remove(uploadImage);
            }
        }

        private void menuTagAddTo_Click(object sender, RoutedEventArgs e)
        {
            int count = lstImageMainViewerList.SelectedItems.Count;
            if (count == 0)
            {
                ShowMessage(MessageType.Warning, "You must select at least one image to continue.");
                return;
            }

            UpdateDialogsAndShow(MessageType.Other, "");

            TagAddImagesRefreshTagsList();
            gridTagSelectDialog.Visibility = Visibility.Visible;
        }

        async private void cmdTagAddImagesTo_Click(object sender, RoutedEventArgs e)
        {
            int count = lstTagAddImagesInclude.SelectedItems.Count;
            if (count == 0)
            {
                ShowMessage(MessageType.Warning, "You must select at least one tag.");
                return;
            }

            gridTagSelectDialog.Visibility = Visibility.Collapsed;

            string[] tagName = new string[count];
            int i = 0;
            foreach (ListBoxItem current in lstTagAddImagesInclude.SelectedItems)
            {
                TagListTagRef tagRef = (TagListTagRef)current.Tag;
                tagName[i] = tagRef.name;
                i++;
            }

            await AddRemoveImagesFromTag(true, tagName);
            //UpdateDialogsAndShow(MessageType.Other, "");
        }

        private void cmdTagAddImagesCancel_Click(object sender, RoutedEventArgs e)
        {
            gridTagSelectDialog.Visibility = Visibility.Collapsed;
            paneBusy.Visibility = Visibility.Collapsed;
        }



        private void menuCategoryMoveImage_Click(object sender, RoutedEventArgs e)
        {
            int count = lstImageMainViewerList.SelectedItems.Count;
            if (count == 0)
            {
                ShowMessage(MessageType.Warning, "You must select at least one image to continue.");
                return;
            }

            cmdCategorySelect.Content = "Add Images";
            UpdateDialogsAndShow(MessageType.Other, "");
            CategorySelectRefreshCategoryList();
            gridCategorySelectDialog.Visibility = Visibility.Visible;
        }

        async private void cmdCategorySelect_Click(object sender, RoutedEventArgs e)
        {
            //CategoryListCategoryRef currentSelectedCategory = null;
            TreeViewItem item = (TreeViewItem)treeCategorySelect.SelectedItem;
            if (item != null)
            {
                CategoryListCategoryRef currentSelectedCategory = (CategoryListCategoryRef)item.Tag;

                gridCategorySelectDialog.Visibility = Visibility.Collapsed;
                if (currentPane == PaneMode.CategoryAdd || currentPane == PaneMode.CategoryEdit)
                {
                    currentCategory.parentId = currentSelectedCategory.id;
                    lblCategoryParentName.Content = GetCategoryName(currentSelectedCategory.id);
                    paneBusy.Visibility = Visibility.Collapsed;
                }
                else if (currentPane == PaneMode.Upload)
                {
                    uploadUIState.RootCategoryId = currentSelectedCategory.id;
                    uploadUIState.RootCategoryName = GetCategoryName(currentSelectedCategory.id);
                    paneBusy.Visibility = Visibility.Collapsed;
                }
                else
                {
                    currentSelectedCategory = (CategoryListCategoryRef)item.Tag;
                    await MoveImagesToCategory(currentSelectedCategory.id);
                }
            }
            else
            {
                ShowMessage(MessageType.Warning, "You must select a category to continue.");
                return;
            }
        }

        private void cmdCategorySelectCancel_Click(object sender, RoutedEventArgs e)
        {
            gridCategorySelectDialog.Visibility = Visibility.Collapsed;
            paneBusy.Visibility = Visibility.Collapsed;
        }

        private void cmdCategoryMoveParent_Click(object sender, RoutedEventArgs e)
        {
            UpdateDialogsAndShow(MessageType.Other, "");
            CategorySelectRefreshCategoryList();

            cmdCategorySelect.Content = "Select";
            gridCategorySelectDialog.Visibility = Visibility.Visible;
        }



        private void ImageViewTagsUpdateFromMeta()
        {

            GeneralImage current = (GeneralImage)lstImageMainViewerList.SelectedItem;
            if (current == null)
                return;

            //Deselect each tag.
            foreach (ListBoxItem tagItem in lstImageViewTagList.Items)
            {
                tagItem.IsSelected = false;
            }

            if (current.Meta != null && current.Meta.Tags != null)
            {
                foreach (ImageMetaTagRef tagRef in current.Meta.Tags)
                {
                    foreach (ListBoxItem tagItem in lstImageViewTagList.Items)
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

        private void RevertFromImageView_MouseUp(object sender, MouseButtonEventArgs e)
        {
            RefreshPanesAllControls(previousPane);
            RefreshOverallPanesStructure(currentPane);
            TweakImageMarginSize(DateTime.Now, currentPane);   
        }



        public void UploadImageStateInProgress_Filter(object sender, FilterEventArgs e)
        {
            UploadImageState item = e.Item as UploadImageState;
            if (item.uploadState != UploadImage.UploadState.Complete && item.uploadState != UploadImage.UploadState.Inactive)
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }

        public void UploadImageStateComplete_Filter(object sender, FilterEventArgs e)
        {
            UploadImageState item = e.Item as UploadImageState;
            if (item.uploadState == UploadImage.UploadState.Complete)
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }

        private void cmdUploadChangeCategory_Click(object sender, RoutedEventArgs e)
        {
            UpdateDialogsAndShow(MessageType.Other, "");
            CategorySelectRefreshCategoryList();

            cmdCategorySelect.Content = "Select";
            gridCategorySelectDialog.Visibility = Visibility.Visible;
        }

        private void cmdUploadTurnAutoOff_Click(object sender, RoutedEventArgs e)
        {
            //TODO set account level flag and update on server
            uploadFots.Clear();
            ResetUploadState();
        }

        private void UploadImageStateApplyServerState()
        {

        }

        async private void DoAutoUploadAsync(bool resume)
        {
            if (!resume)
            {
                UploadImageStateApplyServerState();

                //TODO check account level flag, if false return

                if (currentPane == PaneMode.Upload &&
                    uploadUIState.Mode == UploadUIState.UploadMode.Auto &&
                    uploadUIState.Uploading == true)
                    return;

                if (uploadFots.Count > 0)
                    return;
            }

            if (cancelUploadTokenSource != null)
                cancelUploadTokenSource.Cancel();

            CancellationTokenSource newCancelUploadTokenSource = new CancellationTokenSource();
            cancelUploadTokenSource = newCancelUploadTokenSource;

            DirectoryInfo folder = new DirectoryInfo(uploadUIState.AutoUploadFolder);

            try
            {
                uploadUIState.Uploading = true;

                if (!resume)
                {

                    uploadUIState.Mode = UploadUIState.UploadMode.Auto;

                    List<string> responses = await controller.CheckImagesForAutoUploadAsync(folder, uploadFots, cancelUploadTokenSource.Token);

                    if (responses.Count > 1)
                    {
                        StringBuilder messageBuilder = new StringBuilder();
                        messageBuilder.AppendLine("Some files could not be prepared for upload:");
                        foreach (string response in responses)
                        {
                            messageBuilder.AppendLine(response);
                        }
                        ShowMessage(MessageType.Error, messageBuilder.ToString());
                    }
                }

                int uploadCount = uploadFots.Count;

                if (uploadCount > 0)
                {
                    ShowMessage(MessageType.Info, uploadCount.ToString() + " images were found and are being uploaded automatically");
                    await controller.UploadAutoAsync(uploadFots, uploadUIState, cancelUploadTokenSource.Token);
                    ShowMessage(MessageType.Info, uploadCount.ToString() + " images were uploaded successfully");
                }

                if (newCancelUploadTokenSource == cancelUploadTokenSource)
                    cancelUploadTokenSource = null;

                ResetUploadState();
            }
            catch (OperationCanceledException)
            {
                //uploadFots.Clear();
                logger.Debug("DoAutoUploadAsync has been cancelled.");
            }
            catch (Exception ex)
            {
                uploadFots.Clear();
                logger.Error(ex);
                ShowMessage(MessageType.Error, "There was an unexpected error whilst preparing files for uploading.  Error: " + ex.Message);
                //uploadUIState.Mode = UploadUIState.UploadMode.None;
                ResetUploadState();
            }
            finally
            {
                uploadUIState.Uploading = false;
                RefreshPanesAllControls(currentPane);
                RefreshOverallPanesStructure(currentPane);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            DoAutoUploadAsync(false);
        }

        async private void lstImageMainViewerList_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            //DateTime eventTime = DateTime.Now;
            //await WaitAsynchronouslyAsync();
            //TweakImageMarginSize(eventTime, currentPane);
        }

        private void cmdAccountChangeImageCopyFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
            if (folderDialog.SelectedPath.Length > 0)
            {
                lblAccountImageCopyFolder.Content = folderDialog.SelectedPath;
                lblAccountImageCopyFolderAbbrev.Content = StringTrim(folderDialog.SelectedPath, 80);
            }
        }

        private void cmdAccountChangeAutoUploadFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
            if (folderDialog.SelectedPath.Length > 0)
            {
                chkAccountAutoUpload.IsChecked = true;
                lblAccountAutoUploadFolder.Content = folderDialog.SelectedPath;
                lblAccountAutoUploadFolderAbbrev.Content = StringTrim(folderDialog.SelectedPath, 80);
            }
        }


    }
}