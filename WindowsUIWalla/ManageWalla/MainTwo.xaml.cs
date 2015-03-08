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
using System.Windows.Threading;
using log4net;
using log4net.Config;
using System.Configuration;
using System.IO;
using System.Collections;
using System.Threading;
using System.Windows.Media.Animation; 
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Controls.Primitives;

namespace ManageWalla
{
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

        public enum LoadState
        {
            NotLoaded = 0,
            Requested = 1,
            Loaded = 2,
            Error = 3
        }

        //private bool galleryOptionsLoaded = false;
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
        //public GalleryCategoryModel galleryCategoriesList = null;
        public GalleryPresentationList galleryPresentationList = null;
        public GalleryStyleList galleryStyleList = null;
        public GallerySectionList gallerySectionList = null;
        public GlobalState state = null;
        public List<ThumbCache> thumbCacheList = null;
        public List<MainCopyCache> mainCopyCacheList = null;

        public System.Timers.Timer timer = null;

        private bool cacheFilesSetup = false;
        public ImageList currentImageList = null;
        private bool tagListUploadRefreshing = false;
        public CancellationTokenSource cancelTokenSource = null;
        public CancellationTokenSource cancelUploadTokenSource = null;
        public CancellationTokenSource cancelUploadListTokenSource = null;
        private static readonly ILog logger = LogManager.GetLogger(typeof(MainTwo));
        private double previousImageSize = 0.0;
        //private bool tweakMainImageSize = true;
        //private bool tweakUploadImageSize = true;
        private bool startingApplication = true;
        private DateTime lastMarginTweakTime = DateTime.Now;
        private bool isExpanded = false;
        private MessageType currentDialogType = MessageType.None;
        private bool gallerySectionNeedRefresh = true;

        #endregion

        #region Window Initialisation.
        public MainTwo()
        {
            InitializeComponent();

            uploadFots = (UploadImageFileList)FindResource("uploadImagefileListKey");
            uploadUIState = (UploadUIState)FindResource("uploadUIStateKey");
            uploadImageStateList = (UploadImageStateList)FindResource("uploadImageStateListKey");
            imageMainViewerList = (ImageMainViewerList)FindResource("imageMainViewerListKey");
            //galleryCategoriesList = (GalleryCategoryModel)FindResource("galleryCategoryModelKey");
            galleryPresentationList = (GalleryPresentationList)FindResource("galleryPresentationListKey");
            galleryStyleList = (GalleryStyleList)FindResource("galleryStyleListKey");

            gallerySectionList = (GallerySectionList)FindResource("gallerySectionListKey");
        }

        private void mainTwo_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            controller.Dispose();
        }

        async private void mainTwo_Loaded(object sender, RoutedEventArgs e)
        {
            DateTime startTime = DateTime.Now;
            controller = new MainController(this);
            try
            {
                paneBusy.Opacity = 1.0;
                paneBusy.Visibility = Visibility.Visible;
                currentDialogType = MessageType.Other;
                ShowMessage(MessageType.Busy, "Loading fotowalla");

                controller.SetupServerHelper();

                //Initialise UI for logon.
                previousPane = PaneMode.GalleryView;
                RefreshOverallPanesStructure(PaneMode.Account);
                RefreshPanesAllControls(PaneMode.Account);

                string profileName = Properties.Settings.Default.LastUser;
                if (await ApplicationInit(profileName))
                {
                    AccountRefreshFromState();
                    await Login(state.account.ProfileName, "", state.account.Password);
                }

                if (state != null && (state.connectionState == GlobalState.ConnectionState.OfflineMode || state.connectionState == GlobalState.ConnectionState.LoggedOn))
                {
                    if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                    {
                        await GalleryPopulateOptions();

                        timer = new System.Timers.Timer();
                        timer.Elapsed += timer_Elapsed;
                        timer.Interval = 10000.0;
                        timer.Start();
                    }

                    RefreshOverallPanesStructure(PaneMode.GalleryView);
                    RefreshPanesAllControls(PaneMode.GalleryView);
                    radGallery.IsChecked = true;
                }

                ConcludeBusyProcess();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                this.Hide();
                MessageBoxResult result = MessageBox.Show("There was a problem starting fotowalla and will now close.  " + ex.Message, "Problem loading the app.",MessageBoxButton.OK);
                if (result == MessageBoxResult.OK)
                    Application.Current.Shutdown();
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.mainTwo_Loaded()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                Dispatcher.Invoke(new Action(() => { UploadTimerDispatcherAsync(); }));

                //Dispatcher.Invoke(new Action(() => { RefreshUploadStatusStateDispatcherAsync(false, true); }));
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.timer_Elapsed()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task<bool> ApplicationInit(string profileName)
        {
            if (!await controller.CheckOnline())
            {
                //Offline !
                if (profileName.Length > 0 && controller.CacheFilesPresent(profileName))
                {
                    string message = "No internet connection could be established.  Would you like to work with just fotowalla data saved on this machine?";
                    if (MessageBox.Show(message, "fotowalla", MessageBoxButton.YesNo) == MessageBoxResult.No)
                    {
                        throw new Exception("No internet connection and no local data found.");
                    }
                    else
                    {
                        UseCreateLocalCacheFiles(profileName);
                        state.connectionState = GlobalState.ConnectionState.OfflineMode;
                        this.Title = "fotowalla - offline mode";
                        cmdUseOffline.Content = "Online mode";

                        return false;
                    }
                }
                else
                {
                    throw new Exception("No internet connection and no local data found.");
                }
            }

            if (!await controller.VerifyAppAndPlatform(true))
            {
                throw new Exception("The application/platform failed validation with the server.  Please check www.fotowalla.com/support for the latest versions supported.");
            }

            //if (!await controller.SetPlatform())
            //{
            ///   throw new Exception("The platform is not supported by fotowalla.  Please check the web site for our latest application details.");
            //}

            if (profileName.Length > 0 && controller.CacheFilesPresent(profileName))
            {
                UseCreateLocalCacheFiles(profileName);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void UseCreateLocalCacheFiles(string profileName)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                uploadImageStateList.Clear();
                controller.SetUpCacheFiles(profileName, uploadImageStateList, galleryPresentationList, galleryStyleList);
                state = controller.GetState();
                thumbCacheList = controller.GetThumbCacheList();
                mainCopyCacheList = controller.GetMainCopyCacheList();
                

                cacheFilesSetup = true;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.UseCreateLocalCacheFiles()", (int)duration.TotalMilliseconds, ""); }
            }
        }
        #endregion

        #region Controls display methods
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

                if (cancelUploadListTokenSource != null)
                    cancelUploadListTokenSource.Cancel();
            }

            gridAlertDialog.Visibility = Visibility.Collapsed;
            paneBusy.Visibility = Visibility.Collapsed;
            currentDialogType = MessageType.None;
        }

        private void UpdateDialogsAndShow(MessageType messageType, string message)
        {
            DateTime startTime = DateTime.Now;
            try
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
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.UpdateDialogsAndShow()", (int)duration.TotalMilliseconds, message); }
            }
        }

        private void cmdAlertDialogResponse_Click(object sender, RoutedEventArgs e)
        {
            UserConcludeProcess();
        }

        private void RefreshOverallPanesStructure(PaneMode mode)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                //Ensure panes are all correctly setup each time a refresh is called.
                //gridLeft.ColumnDefinitions[0].Width = new GridLength(0); //Sidebar
                //gridLeft.ColumnDefinitions[1].Width = new GridLength(300); //Main control
                gridLeft.ColumnDefinitions[2].Width = new GridLength(1, GridUnitType.Star); //Image display grid
                gridLeft.ColumnDefinitions[3].Width = new GridLength(0);
                //gridRight.RowDefinitions[0].Height = new GridLength(60); //Working Pane
                gridRight.ColumnDefinitions[1].Width = new GridLength(0);
                grdImageView.Visibility = Visibility.Collapsed;

                if (isExpanded || mode == PaneMode.ImageEdit || mode == PaneMode.ImageView)
                {
                    gridLeft.ColumnDefinitions[0].Width = new GridLength(40);
                    gridLeft.ColumnDefinitions[1].Width = new GridLength(0);
                    gridRight.RowDefinitions[0].Height = new GridLength(0);

                    if (mode == PaneMode.ImageEdit || mode == PaneMode.ImageView)
                    {
                        cmdShowMenuLayout.Visibility = Visibility.Collapsed;
                        cmdBackFromMainImageLayout.Visibility = Visibility.Visible;

                        lblImageViewNameVert.Visibility = Visibility.Visible;
                        lblImageListNameVert.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        cmdShowMenuLayout.Visibility = Visibility.Visible;
                        cmdBackFromMainImageLayout.Visibility = Visibility.Collapsed;

                        lblImageViewNameVert.Visibility = Visibility.Collapsed;
                        lblImageListNameVert.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    gridLeft.ColumnDefinitions[0].Width = new GridLength(0);
                    gridLeft.ColumnDefinitions[1].Width = new GridLength(300);
                    gridRight.RowDefinitions[0].Height = new GridLength(60);
                }

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

                        gridRight.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);

                        gridGallerySelection.Visibility = Visibility.Collapsed;
                        tabGalleryPreview.Visibility = Visibility.Collapsed;
                        tabGalleryConfiguration.Visibility = Visibility.Collapsed;
                        panGridRightHeader.Visibility = Visibility.Visible;

                        break;
                    case PaneMode.GalleryEdit:
                    case PaneMode.GalleryAdd:
                        gridRight.RowDefinitions[0].Height = new GridLength(1, GridUnitType.Star);
                        gridRight.RowDefinitions[1].Height = new GridLength(0);

                        lstImageMainViewerList.Visibility = Visibility.Collapsed;

                        gridGallerySelection.Visibility = Visibility.Visible;
                        tabGalleryConfiguration.Visibility = Visibility.Visible;
                        tabGalleryPreview.Visibility = Visibility.Visible;
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
                        if (state != null && (state.connectionState == GlobalState.ConnectionState.LoggedOn || state.connectionState == GlobalState.ConnectionState.OfflineMode))
                        {
                            gridAccount.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                            gridAccount.RowDefinitions[2].Height = new GridLength(0);
                        }
                        else
                        {
                            gridAccount.RowDefinitions[1].Height = new GridLength(0);
                            gridAccount.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);
                        }
                        break;
                    case PaneMode.ImageView:
                    case PaneMode.ImageEdit:
                        //Switch to full width mode.
                        //gridLeft.ColumnDefinitions[0].Width = new GridLength(40);
                        //gridLeft.ColumnDefinitions[1].Width = new GridLength(0);
                        //gridRight.RowDefinitions[0].Height = new GridLength(0); //Hide header
                        lstImageMainViewerList.Visibility = Visibility.Hidden;
                        grdImageView.Visibility = Visibility.Visible;
                        break;
                }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.RefreshOverallPanesStructure()", (int)duration.TotalMilliseconds, mode.ToString()); }
            }
        }

        private void RefreshPanesAllControls(PaneMode mode)
        {
            DateTime startTime = DateTime.Now;
            try
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
                        radUpload.IsEnabled = true;

                        if (state != null && state.connectionState == GlobalState.ConnectionState.OfflineMode)
                        {
                            cmdCategoryAdd.IsEnabled = false;
                            cmdCategoryEdit.IsEnabled = false;
                        }
                        else
                        {
                            cmdCategoryAdd.IsEnabled = true;
                            cmdCategoryEdit.IsEnabled = true;
                        }

                        PaneEnablePageControls(true);

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

                        PaneEnablePageControls(false);
                        break;
                    #endregion

                    #region Tag
                    case PaneMode.TagView:
                        gridTag.RowDefinitions[1].Height = new GridLength(48.0);
                        gridTag.RowDefinitions[2].MaxHeight = 0;
                        gridTag.RowDefinitions[3].MaxHeight = 0;
                        gridTag.RowDefinitions[4].Height = new GridLength(0.0);

                        cmdTagAdd.Visibility = Visibility.Visible;
                        cmdTagEdit.Visibility = Visibility.Visible;
                        wrapMyTags.IsEnabled = true;
                        wrapSystemTags.IsEnabled = true;

                        radCategory.IsEnabled = true;
                        radGallery.IsEnabled = true;
                        radUpload.IsEnabled = true;

                        if (state != null && state.connectionState == GlobalState.ConnectionState.OfflineMode)
                        {
                            cmdTagAdd.IsEnabled = false;
                            cmdTagEdit.IsEnabled = false;
                        }
                        else
                        {
                            cmdTagAdd.IsEnabled = true;
                            cmdTagEdit.IsEnabled = true;
                        }

                        PaneEnablePageControls(true);
                        break;
                    case PaneMode.TagAdd:
                    case PaneMode.TagEdit:
                        gridTag.RowDefinitions[1].Height = new GridLength(0.0);
                        gridTag.RowDefinitions[2].MaxHeight = 30;
                        gridTag.RowDefinitions[3].MaxHeight = 80;
                        gridTag.RowDefinitions[4].Height = new GridLength(48.0);
                        wrapMyTags.IsEnabled = false;
                        wrapSystemTags.IsEnabled = false;

                        radCategory.IsEnabled = false;
                        radGallery.IsEnabled = false;
                        radUpload.IsEnabled = false;

                        PaneEnablePageControls(false);

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

                        cmdGalleryPreview.IsChecked = false;
                        radCategory.IsEnabled = true;
                        radTag.IsEnabled = true;
                        radUpload.IsEnabled = true;
                        wrapMyGalleries.IsEnabled = true;

                        if (state != null && state.connectionState == GlobalState.ConnectionState.OfflineMode)
                        {
                            cmdGalleryAdd.IsEnabled = false;
                            cmdGalleryEdit.IsEnabled = false;
                            cmdGalleryCopyUrl.IsEnabled = false;
                            cmdGalleryView.IsEnabled = false;
                        }
                        else
                        {
                            cmdGalleryAdd.IsEnabled = true;
                            cmdGalleryEdit.IsEnabled = true;
                            cmdGalleryCopyUrl.IsEnabled = true;
                            cmdGalleryView.IsEnabled = true;
                        }

                        PaneEnablePageControls(true);
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
                        PaneEnablePageControls(false);
                        wrapMyGalleries.IsEnabled = false;

                        if (GalleryGetSelectionType() == 0)
                        {
                            //Categories ONLY
                            gridGallerySelection.ColumnDefinitions[0].Width = new GridLength(2, GridUnitType.Star);
                            gridGallerySelection.ColumnDefinitions[1].Width = new GridLength(30);
                            gridGallerySelection.ColumnDefinitions[2].Width = new GridLength(0);
                            gridGallerySelection.ColumnDefinitions[3].Width = new GridLength(0);
                            gridGallerySelection.ColumnDefinitions[4].Width = new GridLength(2, GridUnitType.Star);

                            itemGalleryGroupCategory.IsEnabled = true;
                            itemGalleryGroupTag.IsEnabled = false;

                        }
                        else if (GalleryGetSelectionType() == 1)
                        {
                            //Tags ONLY
                            gridGallerySelection.ColumnDefinitions[0].Width = new GridLength(0);
                            gridGallerySelection.ColumnDefinitions[1].Width = new GridLength(0);
                            gridGallerySelection.ColumnDefinitions[2].Width = new GridLength(2, GridUnitType.Star);
                            gridGallerySelection.ColumnDefinitions[3].Width = new GridLength(30);
                            gridGallerySelection.ColumnDefinitions[4].Width = new GridLength(2, GridUnitType.Star);
                            itemGalleryGroupCategory.IsEnabled = false;
                            itemGalleryGroupTag.IsEnabled = true;
                        }

                        GalleryPresentationItem presentation = (GalleryPresentationItem)lstGalleryPresentationList.SelectedItem;
                        if (presentation.MaxSections == 0)
                        {
                            GallerySetGroupingType(0);
                            lstGalleryGroupOptions.IsEnabled = false;
                        }
                        else
                        {
                            lstGalleryGroupOptions.IsEnabled = true;
                        }

                        if (GalleryGetGroupingType() == 0)
                        {
                            //No grouping, so hide the grouping options.
                            chkGalleryShowGroupingDesc.IsEnabled = false;
                            datGallerySections.IsEnabled = false;
                            cmdGallerySectionReset.IsEnabled = false;
                        }
                        else
                        {
                            //Show grouping options.
                            chkGalleryShowGroupingDesc.IsEnabled = true;
                            datGallerySections.IsEnabled = true;
                            cmdGallerySectionReset.IsEnabled = true;
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
                            cmdUploadResumePauseClear.Visibility = Visibility.Visible;
                            cmdUploadResumePauseClear.IsEnabled = true;
                            cmdUploadChangeCategory.Visibility = Visibility.Collapsed;
                            if (uploadUIState.Mode != UploadUIState.UploadMode.Auto)
                            {
                                //cmdUploadChangeCategory.IsEnabled = true;
                                //grdUploadSettings.RowDefinitions[9].Height = new GridLength(60);
                                grdUploadSettings.RowDefinitions[5].Height = new GridLength(40);

                                lblUploadType.Content = "Uploading images...";
                                cmdUploadResumePauseClear.Content = "Pause Upload";
                            }
                            else
                            {
                                grdUploadSettings.RowDefinitions[2].Height = new GridLength(30.0);
                                grdUploadSettings.RowDefinitions[3].Height = new GridLength(30.0);
                                grdUploadSettings.RowDefinitions[4].Height = new GridLength(30.0);

                                lblUploadType.Content = "Uploading images (auto)...";
                                cmdUploadResumePauseClear.Content = "Pause Upload";
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
                            cmdUploadResumePauseClear.Visibility = Visibility.Collapsed;
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
                            cmdUploadResumePauseClear.Content = "Resume";
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
                            cmdUploadResumePauseClear.Content = "Clear";
                            cmdUploadResumePauseClear.IsEnabled = true;
                            cmdUploadResumePauseClear.Visibility = Visibility.Visible;
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
                        if (state != null && state.connectionState == GlobalState.ConnectionState.LoggedOn)
                        {
                            lblAccountPaneTitle.Content = "Account " + state.account.ProfileName;
                            cmdUserAppEdit.Visibility = Visibility.Visible;
                            cmdUserAppSave.Visibility = Visibility.Collapsed;
                            cmdUserAppCancel.Visibility = Visibility.Collapsed;
                            
                            cmdAccountRefresh.IsEnabled = true;
                            cmdAccountClose.Visibility = Visibility.Visible;
                            cmdAccountClose.IsEnabled = true;
                            tabDownloadList.IsEnabled = true;
                            tabUploadStatusList.IsEnabled = true;

                            cmdUseOffline.IsEnabled = true;
                            cmdAccountLogout.IsEnabled = true;
                            cmdUpdateProfileLoggedOn.IsEnabled = true;

                            chkAccountAutoUpload.IsEnabled = false;
                            sldAccountImageCopySize.IsEnabled = false;
                            cmdAccountChangeAutoUploadFolder.IsEnabled = false;
                            cmdAccountChangeImageCopyFolder.IsEnabled = false;
                        }
                        else if (state != null && state.connectionState == GlobalState.ConnectionState.OfflineMode)
                        {
                            lblAccountPaneTitle.Content = "Account " + state.account.ProfileName;
                            cmdUserAppEdit.Visibility = Visibility.Collapsed;
                            cmdUserAppSave.Visibility = Visibility.Collapsed;
                            cmdUserAppCancel.Visibility = Visibility.Collapsed;

                            cmdAccountRefresh.IsEnabled = false;
                            cmdAccountClose.Visibility = Visibility.Visible;
                            cmdAccountClose.IsEnabled = true;

                            cmdUploadRefresh.IsEnabled = false;

                            cmdUseOffline.IsEnabled = true;
                            cmdAccountLogout.IsEnabled = false;
                            cmdUpdateProfileLoggedOn.IsEnabled = false;

                            chkAccountAutoUpload.IsEnabled = false;
                            sldAccountImageCopySize.IsEnabled = false;
                            cmdAccountChangeAutoUploadFolder.IsEnabled = false;
                            cmdAccountChangeImageCopyFolder.IsEnabled = false;
                        }
                        else
                        {
                            lblAccountPaneTitle.Content = "";
                            cmdAccountClose.Visibility = Visibility.Collapsed;
                        }

                        break;
                    case PaneMode.AccountEdit:
                        //tabAccount.IsEnabled = false;
                        //cmdAccountClose.IsEnabled = false;
                        cmdUserAppEdit.Visibility = Visibility.Collapsed;
                        cmdUserAppCancel.Visibility = Visibility.Visible;
                        cmdUserAppSave.Visibility = Visibility.Visible;
                        //cmdAccountLogin.Visibility = Visibility.Collapsed;

                        cmdAccountRefresh.IsEnabled = false;
                        cmdAccountClose.IsEnabled = false;
                        tabDownloadList.IsEnabled = false;
                        tabUploadStatusList.IsEnabled = false;
                        cmdUseOffline.IsEnabled = false;
                        cmdUpdateProfileLoggedOn.IsEnabled = false;


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
                        //lblImageViewNameVert.Visibility = Visibility.Visible;
                        //lblImageListNameVert.Visibility = Visibility.Collapsed;
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
                            cmdImageViewEdit.IsEnabled = true;
                            cmdImageViewCancel.IsEnabled = true;
                            cmdImageViewDetailToggle.IsEnabled = false;
                            cmdImageViewNext.IsEnabled = false;
                            cmdImageViewPrevious.IsEnabled = false;
                            lstImageViewTagList.IsEnabled = true;

                            cmdBackFromMainImageLayout.IsEnabled = false;
                        }
                        else
                        {
                            txtImageViewName.IsEnabled = false;
                            txtImageViewDescription.IsEnabled = false;
                            datImageViewDate.IsEnabled = false;
                            cmdImageViewEdit.Content = "Edit";
                            cmdImageViewEdit.IsEnabled = true;
                            cmdImageViewCancel.IsEnabled = false;
                            cmdImageViewDetailToggle.IsEnabled = true;
                            cmdImageViewNext.IsEnabled = true;
                            cmdImageViewPrevious.IsEnabled = true;
                            lstImageViewTagList.IsEnabled = false;

                            cmdBackFromMainImageLayout.IsEnabled = true;

                            //PaneEnablePageControls(true);
                        }


                        if (state != null && state.connectionState == GlobalState.ConnectionState.OfflineMode)
                            cmdImageViewEdit.IsEnabled = false;

                        PaneEnablePageControls(false);

                        break;
                    #endregion
                }

                currentPane = mode;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.RefreshPanesAllControls()", (int)duration.TotalMilliseconds, mode.ToString()); }
            }
        }

        private void PaneEnablePageControls(bool enable)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                cmdShowMenuLayout.IsEnabled = enable;

                cmdShowExpandedLayout.IsEnabled = enable;
                sldImageSize.IsEnabled = enable;
                cmdShowActionsMenu.IsEnabled = enable;
                cmbGallerySection.IsEnabled = enable;
                cmdMultiSelectionMode.IsEnabled = enable;

                Button cmdGalleryRefresh = (Button)radGallery.Template.FindName("cmdGalleryRefresh", radGallery);
                if (cmdGalleryRefresh != null) { cmdGalleryRefresh.IsEnabled = enable; }

                Button cmdCategoryRefresh = (Button)radCategory.Template.FindName("cmdCategoryRefresh", radCategory);
                if (cmdGalleryRefresh != null) { cmdCategoryRefresh.IsEnabled = enable; }

                Button cmdTagRefresh = (Button)radTag.Template.FindName("cmdTagRefresh", radTag);
                if (cmdGalleryRefresh != null) { cmdTagRefresh.IsEnabled = enable; }


                if (lstImageMainViewerList != null)
                    lstImageMainViewerList.IsEnabled = enable; 

                if (enable)
                {
                    ImagesSetNavigationButtons();
                }
                else
                {
                    cmdImageNavigationLast.IsEnabled = enable;
                    cmdImageNavigationNext.IsEnabled = enable;
                    cmdImageNavigationPrevious.IsEnabled = enable;
                    cmdImageNavigationBegin.IsEnabled = enable;

                    cmdImageNavigationLastVert.IsEnabled = enable;
                    cmdImageNavigationNextVert.IsEnabled = enable;
                    cmdImageNavigationPreviousVert.IsEnabled = enable;
                    cmdImageNavigationBeginVert.IsEnabled = enable;
                }

                if (state != null && state.connectionState == GlobalState.ConnectionState.OfflineMode)
                {
                    cmdMultiSelectionMode.Visibility = Visibility.Collapsed;
                    cmdShowActionsMenu.Visibility = Visibility.Collapsed;
                    cmdReturnToAccount.Visibility = Visibility.Visible;
                    radUpload.IsEnabled = false;

                    if (cmdGalleryRefresh != null) { cmdGalleryRefresh.IsEnabled = false; }
                    if (cmdGalleryRefresh != null) { cmdCategoryRefresh.IsEnabled = false; }
                    if (cmdGalleryRefresh != null) { cmdTagRefresh.IsEnabled = false; }

                    if (cmdImageViewDetailToggle != null) { cmdImageViewDetailToggle.Visibility = Visibility.Collapsed; }
                }
                else
                {
                    cmdMultiSelectionMode.Visibility = Visibility.Visible;
                    cmdShowActionsMenu.Visibility = Visibility.Visible;
                    cmdReturnToAccount.Visibility = Visibility.Collapsed;

                    if (cmdImageViewDetailToggle != null) { cmdImageViewDetailToggle.Visibility = Visibility.Visible; }
                }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.PaneEnablePageControls()", (int)duration.TotalMilliseconds, ""); }
            }
        }
        #endregion

        #region Pane \ Menu Event Handlers
        async private void radCategory_Checked(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(PaneMode.CategoryView);
            RefreshPanesAllControls(PaneMode.CategoryView);
            try
            {
                await RefreshAndDisplayCategoryList(false);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem retrieving the category list.  Error: " + ex.Message);
            }
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

            try
            {
                await RefreshAndDisplayGalleryList(false);

                if (startingApplication)
                {
                    await RefreshAndDisplayTagList(false);
                    await RefreshAndDisplayCategoryList(false);

                    foreach (RadioButton button in wrapMyGalleries.Children.OfType<RadioButton>())
                    {
                        GalleryListGalleryRef galleryRef = (GalleryListGalleryRef)button.Tag;
                        if (galleryRef.id == state.userApp.GalleryId)
                        {
                            button.IsChecked = true;
                        }
                        continue;
                    }

                    startingApplication = false;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem retrieving the lists.  Error: " + ex.Message);
            }
        }

        async private void radTag_Checked(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(PaneMode.TagView);
            RefreshPanesAllControls(PaneMode.TagView);
            try
            {
                await RefreshAndDisplayTagList(false);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem retrieving the tag list.  Error: " + ex.Message);
            }
        }

        async private void cmdShowExpandedLayout_Click(object sender, RoutedEventArgs e)
        {
            isExpanded = true;
            RefreshOverallPanesStructure(currentPane);
            RefreshPanesAllControls(currentPane);

            DateTime eventTime = DateTime.Now;
            await WaitAsynchronouslyAsync();
            TweakImageMarginSize(eventTime, currentPane);
        }

        async private void cmdShowMenuLayout_Click(object sender, RoutedEventArgs e)
        {
            isExpanded = false;

            RefreshOverallPanesStructure(currentPane);

            DateTime eventTime = DateTime.Now;
            await WaitAsynchronouslyAsync();
            TweakImageMarginSize(eventTime, currentPane);
        }
        #endregion

        #region Image \ Navigation methods
        async private void FetchGalleryImagesSectionChangeAsync(long sectionId)
        {
            DateTime startTime = DateTime.Now;

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
                    if (logger.IsDebugEnabled) { logger.Debug("FetchTagImagesFirstAsync has been cancelled by a subsequent request. "); }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MessageType.Error, "Unexpected error loading images: " + ex.Message);
                }
                finally
                {
                    TimeSpan duration = DateTime.Now - startTime;
                    if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.FetchGalleryImagesSectionChangeAsync()", (int)duration.TotalMilliseconds, ""); }
                }
            }
        }

        async private Task FetchMoreImagesAsync(FetchDirection direction)
        {
            DateTime startTime = DateTime.Now;
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
                    if ((currentImageList.imageCursor + state.userApp.FetchSize) <= currentImageList.totalImageCount)
                        cursor = currentImageList.imageCursor + state.userApp.FetchSize;
                    break;
                case FetchDirection.Previous:
                    cursor = Math.Max(currentImageList.imageCursor - state.userApp.FetchSize, 0);
                    break;
                case FetchDirection.Last:
                    cursor = Math.Abs(currentImageList.totalImageCount / state.userApp.FetchSize) * state.userApp.FetchSize;
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
                if (logger.IsDebugEnabled) { logger.Debug("FetchMoreImagesAsync has been cancelled by a subsequent request. "); }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Unexpected error loading images: " + ex.Message);
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.FetchMoreImagesAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        public async Task WaitAsynchronouslyAsync()
        {
            await Task.Delay(750);
        }

        private void TweakUploadImageListMargin()
        {
            double paneWidth = lstUploadImageFileList.ActualWidth - System.Windows.SystemParameters.VerticalScrollBarWidth - 10.0;
            double imageWidth = 91.0;
            double imageWidthWithMargin = imageWidth + 6.0;

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

            /*
            double total = (imageWidthCount * (newMargin * 2)) + (imageWidthCount * imageWidth);
            Console.Out.WriteLine("Upload");
            Console.Out.WriteLine("New total:" + total.ToString());
            Console.Out.WriteLine("Image Size:" + imageWidth.ToString());
            Console.Out.WriteLine("Margin Changed:" + newMargin.ToString());
            Console.Out.WriteLine("Altered width:" + paneWidth.ToString());
            Console.Out.WriteLine("Actual width:" + lstUploadImageFileList.ActualWidth.ToString());
            */
        }

        private void TweakMainImageListMargin()
        {
            

            bool isDetail = (bool)cmdShowInlineImageDetail.IsChecked;

            double paneWidth = lstImageMainViewerList.ActualWidth - System.Windows.SystemParameters.VerticalScrollBarWidth - 10.0; // -22.0;
            double imageWidth = sldImageSize.Value + 1;
            double imageWidthWithMargin = imageWidth + 6.0;

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

            if (imageMainViewerList.Count <= imageWidthCount)
                newThickness = new Thickness(2);

            newStyle.Setters.Add(new Setter(MarginProperty, newThickness));
            lstImageMainViewerList.ItemContainerStyle = newStyle;

            lastMarginTweakTime = DateTime.Now;

            //Console.Out.WriteLine(sldImageSize.Value.ToString());

            //double total = (imageWidthCount * (newMargin * 2)) + (imageWidthCount * imageWidth);
            /*
            Console.Out.WriteLine("Main");
            Console.Out.WriteLine("New total:" + total.ToString());
            Console.Out.WriteLine("Image Size:" + imageWidth.ToString());
            Console.Out.WriteLine("Margin Changed:" + newMargin.ToString());
            Console.Out.WriteLine("Altered width:" + paneWidth.ToString());
            
            Console.Out.WriteLine("Actual width:" + lstImageMainViewerList.ActualWidth.ToString());
            Console.Out.WriteLine("New total:" + total.ToString());
            Console.Out.WriteLine("Count:" + imageWidthCount.ToString());
            */
        }

        private void TweakImageMarginSize(DateTime eventTime, PaneMode mode)
        {
            if (eventTime < lastMarginTweakTime)
            {
                //Console.Out.WriteLine("TweakImageMarginSize not run cause of later update.");
                return;
            }

            if (mode == PaneMode.Upload)
            {
                if (lstUploadImageFileList == null) { return; }
                TweakUploadImageListMargin();
            }
            else if (mode == PaneMode.CategoryView || mode == PaneMode.CategoryAdd || mode == PaneMode.CategoryEdit || mode == PaneMode.GalleryView || mode == PaneMode.TagView || mode == PaneMode.TagAdd || mode == PaneMode.TagEdit)
            {
                if (lstImageMainViewerList == null) { return; }
                TweakMainImageListMargin();
            }
        }

        void ItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {

            if (lstImageMainViewerList.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {

                lstImageMainViewerList.ScrollIntoView(lstImageMainViewerList.Items[0]);
                lstImageMainViewerList.ItemContainerGenerator.StatusChanged -= ItemContainerGenerator_StatusChanged;

            }

        }

        async private Task ImageListUpdateControls(CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            try
            {



                imageMainViewerList.Clear();

                //tweakMainImageSize = false;

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


                    if (lstImageMainViewerList.Items.Count > 0)
                    {
                        lstImageMainViewerList.ItemContainerGenerator.StatusChanged += ItemContainerGenerator_StatusChanged;
                        //lstImageMainViewerList.UpdateLayout();
                        //lstImageMainViewerList.ScrollIntoView(lstImageMainViewerList.Items[0]);
                    }
                }

                lblImageListName.Content = bannerName;
                lblImageListNameVert.Text = bannerName;

                ImagesSetNavigationButtons();

                TweakImageMarginSize(DateTime.Now, currentPane);

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
                            tasks[i] = imageMainViewerList[cursor + i].LoadThumb(cancelToken, thumbCacheList, state.userApp.ThumbCacheSizeMB, state.connectionState);
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
                if (logger.IsDebugEnabled) { logger.Debug("ImageListUpdateControls has been cancelled."); }
                throw (cancelEx);
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.ImageListUpdateControls()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        private void ImagesSetNavigationButtons()
        {
            if (currentImageList == null)
                return;

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

                if ((currentImageList.sectionId > 0 && currentImageList.sectionImageCount > (currentImageList.imageCursor + state.userApp.FetchSize))
                    || (currentImageList.sectionId < 1 && currentImageList.totalImageCount > (currentImageList.imageCursor + state.userApp.FetchSize)))
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
        }

        //TODO add functionality + server side.
        private string GetSearchQueryString()
        {
            return null;
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
        
        //Note - Added awaits to next, previous
        async private void ImageViewUpdateNextPrevious()
        {
            DateTime startTime = DateTime.Now;

            try
            {
                GeneralImage current = (GeneralImage)lstImageMainViewerList.Items.CurrentItem;
                if (current == null)
                    return;

                cancelTokenSource = new CancellationTokenSource();
                await current.LoadMainCopyImage(cancelTokenSource.Token, mainCopyCacheList, state.userApp.MainCopyFolder, state.userApp.MainCopyCacheSizeMB, state.connectionState);
                await current.LoadMeta(false, cancelTokenSource.Token, state.connectionState);
                ImageViewTagsUpdateFromMeta();

                if (lstImageMainViewerList.SelectedIndex == 0)
                {
                    cmdImageViewPrevious.IsEnabled = false;
                }
                else
                {
                    GeneralImage previous = (GeneralImage)lstImageMainViewerList.Items[lstImageMainViewerList.SelectedIndex - 1];
                    await previous.LoadMainCopyImage(cancelTokenSource.Token, mainCopyCacheList, state.userApp.MainCopyFolder, state.userApp.MainCopyCacheSizeMB, state.connectionState);
                    await previous.LoadMeta(false, cancelTokenSource.Token, state.connectionState);

                    cmdImageViewPrevious.IsEnabled = true;
                }

                if (lstImageMainViewerList.SelectedIndex == lstImageMainViewerList.Items.Count - 1)
                {
                    cmdImageViewNext.IsEnabled = false;
                }
                else
                {
                    GeneralImage next = (GeneralImage)lstImageMainViewerList.Items[lstImageMainViewerList.SelectedIndex + 1];
                    await next.LoadMainCopyImage(cancelTokenSource.Token, mainCopyCacheList, state.userApp.MainCopyFolder, state.userApp.MainCopyCacheSizeMB, state.connectionState);
                    await next.LoadMeta(false, cancelTokenSource.Token, state.connectionState);
                    cmdImageViewNext.IsEnabled = true;
                }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.ImageViewUpdateNextPrevious()", (int)duration.TotalMilliseconds, ""); }
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
                    newImageMetaTagRef.op = "C";
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
                        newImageMetaTagRef.op = "C";
                        imageMetaTagRefTemp.Add(newImageMetaTagRef);
                    }
                }


                foreach (ImageMetaTagRef tagRef in current.Meta.Tags)
                {
                    TagListTagRef stateTag = state.tagList.TagRef.First<TagListTagRef>(r => r.id == tagRef.id);
                    if (!stateTag.systemOwned)
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
            }
            if (imageMetaTagRefTemp != null)
                current.Meta.Tags = imageMetaTagRefTemp.ToArray<ImageMetaTagRef>();
        }
        #endregion

        #region Image \ navigation event handlers
        async private void FetchCategoryImagesFirstAsync(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (currentPane == PaneMode.CategoryAdd || currentPane == PaneMode.CategoryEdit)
                return;

            DateTime startTime = DateTime.Now;
            try
            {
                cmbGallerySectionVert.Visibility = Visibility.Collapsed;
                cmbGallerySection.Visibility = Visibility.Collapsed;

                RadioButton checkedTagButton = (RadioButton)wrapMyTags.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
                if (checkedTagButton != null)
                    checkedTagButton.IsChecked = false;

                RadioButton checkedGalleryButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
                if (checkedGalleryButton != null)
                    checkedGalleryButton.IsChecked = false;

                //e.Handled = true;

                TreeViewItem selectedTreeViewItem = (TreeViewItem)sender;

                if (selectedTreeViewItem != null)
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
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("FetchCategoryImagesFirstAsync has been cancelled by a subsequent request."); }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Unexpected error loading images: " + ex.Message);
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.FetchCategoryImagesFirstAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private void FetchTagImagesFirstAsync(object sender, RoutedEventArgs e)
        {
            DateTime startTime = DateTime.Now;
            try
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
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("FetchTagImagesFirstAsync has been cancelled by a subsequent request. "); }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Unexpected error loading images: " + ex.Message);
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.FetchTagImagesFirstAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private void FetchGalleryImagesFirstAsync(object sender, RoutedEventArgs e)
        {
            DateTime startTime = DateTime.Now;
            try
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
                    if (!GalleryPopulateSectionDropdown(galleryListRefTemp))
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
                }
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("FetchGalleryImagesFirstAsync has been cancelled by a subsequent request. "); }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Unexpected error loading images: " + ex.Message);
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.FetchGalleryImagesFirstAsync()", (int)duration.TotalMilliseconds, ""); }
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
                if (MessageBox.Show("Do you want to delete the " + count.ToString() + " selected images permanently from fotowalla ?", "fotowalla", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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
                        if (logger.IsDebugEnabled) { logger.Debug("DeleteImages_Click has been cancelled"); }
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
            lstImageMainViewerList.SelectedItem = null;
        }

        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (lstImageMainViewerList.SelectionMode == SelectionMode.Single && lstImageMainViewerList.SelectedItems.Count > 0)
                {
                    previousPane = currentPane;
                    cmdImageViewDetailToggle.IsChecked = false;

                    RefreshPanesAllControls(PaneMode.ImageView);
                    RefreshOverallPanesStructure(currentPane);

                    ImageViewUpdateNextPrevious();

                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected error: " + ex.Message);
            }
        }
        
        //async private void RevertFromImageView_MouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    try
        //    {
        //        RefreshPanesAllControls(previousPane);
        //        RefreshOverallPanesStructure(currentPane);

        //        DateTime eventTime = DateTime.Now;
        //        await WaitAsynchronouslyAsync();
        //        TweakImageMarginSize(eventTime, currentPane);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error(ex);
        //        ShowMessage(MainTwo.MessageType.Error, "There was an unexpected error: " + ex.Message);
        //    }
        //}

        private void ImageViewPrevious_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected error: " + ex.Message);
            }
        }

        private void ImageViewNext_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected error: " + ex.Message);
            }
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
                    if (logger.IsDebugEnabled) { logger.Debug("ImageViewEditSave_Click has been cancelled."); }
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
            try
            {
                RefreshPanesAllControls(PaneMode.ImageView);
                GeneralImage current = (GeneralImage)lstImageMainViewerList.Items.CurrentItem;
                if (current != null)
                {
                    current.LoadMeta(true, cancelTokenSource.Token, state.connectionState);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected error: " + ex.Message);
            }
        }

        async private void ImageView_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected error: " + ex.Message);
            }
        }

        private void ImageViewDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshPanesAllControls(currentPane);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected error: " + ex.Message);
            }
        }
        #endregion

        #region Category Methods
        async public Task RefreshAndDisplayCategoryList(bool forceRefresh)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                bool isBusy = bool.Parse(radCategory.Tag.ToString());
                if (isBusy) { return; }

                bool redrawList = false;

                //Catch first time loads, user intiated refresh and when user was offline and is now online.  But only if logged on.
                if (state.connectionState != GlobalState.ConnectionState.NoAccount &&
                    (state.categoryLoadState == GlobalState.DataLoadState.No || forceRefresh || state.categoryLoadState == GlobalState.DataLoadState.LocalCache))
                {
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
                if (logger.IsDebugEnabled) { logger.Debug("RefreshAndDisplayCategoryList has been cancelled."); }
            }
            catch (Exception ex)
            {
                state.categoryList = null;
                state.categoryLoadState = GlobalState.DataLoadState.Unavailable;
                throw ex;
            }
            finally
            {
                radCategory.Tag = false;
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.RefreshAndDisplayCategoryList()", (int)duration.TotalMilliseconds, ""); }
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
                  CategorySelect(categoryId, null, treeCategoryView);
            }
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
            if (currentHeader == null)
            {
                foreach (TreeViewItem item in treeViewToUpdate.Items)
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
            else
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
        }

        async private Task CategoryPopulateMetaData(CategoryListCategoryRef current)
        {
            DateTime startTime = DateTime.Now; 
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
                if (logger.IsDebugEnabled) { logger.Debug("CategoryPopulateMetaData has been cancelled"); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.CategoryPopulateMetaData()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task CategorySave()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                ShowMessage(MessageType.Busy, "Updating server with category info");

                currentCategory.Name = txtCategoryName.Text;
                currentCategory.Desc = txtCategoryDescription.Text;

                if (currentPane == PaneMode.CategoryAdd)
                {
                    await controller.CategoryCreateAsync(currentCategory, cancelTokenSource.Token);
                }
                else
                {
                    await controller.CategoryUpdateAsync(currentCategory, cancelTokenSource.Token);
                }

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("CategoryPopulateMetaData has been cancelled"); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.CategorySave()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task CategoryDelete()
        {
            DateTime startTime = DateTime.Now;
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
                if (logger.IsDebugEnabled) { logger.Debug("cmdCategoryDelete_Click has been cancelled"); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.CategoryDelete()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        private String GetCategoryName(long categoryId)
        {
            if (state.categoryList == null)
                return "";

            CategoryListCategoryRef parentCategory = state.categoryList.CategoryRef.FirstOrDefault<CategoryListCategoryRef>(r => r.id == categoryId);
            if (parentCategory != null)
                return parentCategory.name;

            return "";
        }

        async private Task MoveImagesToCategory(long categoryId)
        {
            DateTime startTime = DateTime.Now;
            ImageIdList moveList = new ImageIdList();
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

                if (lstImageMainViewerList.SelectedItems != null)
                    lstImageMainViewerList.SelectedItems.Clear();

                ConcludeBusyProcess();
                await RefreshAndDisplayCategoryList(true);

                string message = moveList.ImageRef.Length.ToString() + " image(s) were successfully moved to the category.";
                ShowMessage(MessageType.Info, message);
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("MoveImagesToCategory has been cancelled"); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.MoveImagesToCategory()", (int)duration.TotalMilliseconds, ""); }
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
        async private void cmdCategoryRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RefreshAndDisplayCategoryList(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem loading the category list.  Error: " + ex.Message);
            }
        }

        private void cmdCategoryCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.CategoryView);
        }

        private void cmdCategoryAdd_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem preparing to add a new category.  Error: " + ex.Message);
            }
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

                try
                {
                    await CategoryPopulateMetaData(currentSelectedCategory);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MainTwo.MessageType.Error, "There was a problem retrieving the category.  Error: " + ex.Message);
                }
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
                await CategorySave();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem updating category data on the server.  Error: " + ex.Message);
            }

            try
            {
                RefreshPanesAllControls(PaneMode.CategoryView);
                await RefreshAndDisplayCategoryList(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem refreshing the category view.  Error: " + ex.Message);
            }
        }

        async private void cmdCategoryDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await CategoryDelete();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem deleting the category.  Error: " + ex.Message);
            }

            try
            {
                RefreshPanesAllControls(PaneMode.CategoryView);
                await RefreshAndDisplayCategoryList(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem refreshing the category view.  Error: " + ex.Message);
            }
        }

        private void menuCategoryMoveImage_Click(object sender, RoutedEventArgs e)
        {
            int count = lstImageMainViewerList.SelectedItems.Count;
            if (count == 0)
            {
                ShowMessage(MessageType.Warning, "You must select at least one image to continue.");
                return;
            }

            try
            {
                cmdCategorySelect.Content = "Add Images";
                UpdateDialogsAndShow(MessageType.Other, "");
                CategorySelectRefreshCategoryList();
                gridCategorySelectDialog.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected problem.  Error: " + ex.Message);
            }
        }

        async private void cmdCategorySelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TreeViewItem item = (TreeViewItem)treeCategorySelect.SelectedItem;
                if (item != null)
                {
                    CategoryListCategoryRef currentSelectedCategory = (CategoryListCategoryRef)item.Tag;

                    gridCategorySelectDialog.Visibility = Visibility.Collapsed;
                    if (currentPane == PaneMode.CategoryAdd || currentPane == PaneMode.CategoryEdit)
                    {
                        gridCategorySelectDialog.Visibility = Visibility.Collapsed;
                        currentCategory.parentId = currentSelectedCategory.id;
                        lblCategoryParentName.Content = GetCategoryName(currentSelectedCategory.id);
                        paneBusy.Visibility = Visibility.Collapsed;
                    }
                    else if (currentPane == PaneMode.Upload)
                    {
                        uploadUIState.RootCategoryId = currentSelectedCategory.id;
                        uploadUIState.RootCategoryName = GetCategoryName(currentSelectedCategory.id);
                        if (uploadUIState.UploadToNewCategory && uploadUIState.CategoryName.Length == 0)
                            uploadUIState.UploadToNewCategory = false;

                        paneBusy.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        if (currentSelectedCategory.id == state.userApp.UserDefaultCategoryId)
                        {
                            ShowMessage(MessageType.Warning, "You cannot move images to the root category.");
                            gridCategorySelectDialog.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            //currentSelectedCategory = (CategoryListCategoryRef)item.Tag;
                            await MoveImagesToCategory(currentSelectedCategory.id);
                        }
                    }
                }
                else
                {
                    ShowMessage(MessageType.Warning, "You must select a category to continue.");
                    return;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected problem.  Error: " + ex.Message);
            }
        }

        private void cmdCategorySelectCancel_Click(object sender, RoutedEventArgs e)
        {
            gridCategorySelectDialog.Visibility = Visibility.Collapsed;
            paneBusy.Visibility = Visibility.Collapsed;
        }

        private void cmdCategoryMoveParent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateDialogsAndShow(MessageType.Other, "");
                CategorySelectRefreshCategoryList();

                cmdCategorySelect.Content = "Select";
                gridCategorySelectDialog.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected problem.  Error: " + ex.Message);
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
            DateTime startTime = DateTime.Now; 
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
                if (logger.IsDebugEnabled) { logger.Debug("RefreshAndDisplayTagList has been cancelled."); }
            }
            catch (Exception ex)
            {
                state.tagList = null;
                state.tagLoadState = GlobalState.DataLoadState.Unavailable;
                throw ex;
            }
            finally
            {
                radTag.Tag = false;
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.RefreshAndDisplayTagList()", (int)duration.TotalMilliseconds, ""); }
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
                    if (tag.name.Length>3)
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

        async private Task PopulateTagMetaData(TagListTagRef tagListTagRef)
        {
            DateTime startTime = DateTime.Now;
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
                if (logger.IsDebugEnabled) { logger.Debug("PopulateTagMetaData has been cancelled"); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.PopulateTagMetaData()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task AddRemoveImagesFromTag(bool add, string[] tagName)
        {
            DateTime startTime = DateTime.Now;

            ImageIdList moveList = new ImageIdList();
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

                ConcludeBusyProcess();

                string message = moveList.ImageRef.Length.ToString() + " images were removed from the tag: " + tagName[0];
                if (add)
                    message = moveList.ImageRef.Length.ToString() + " images were added to the tags selected.";

                ShowMessage(MessageType.Info, message);
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("AddRemoveImagesFromTag has been cancelled"); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.XXXXXX()", (int)duration.TotalMilliseconds, ""); }
            }
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

        async private Task TagDelete()
        {
            DateTime startTime = DateTime.Now;
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
                if (logger.IsDebugEnabled) { logger.Debug("TagDelete has been cancelled"); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.TagDelete()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task TagSave()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                ShowMessage(MessageType.Busy, "Saving Tag");

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

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("TagSave has been cancelled"); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.TagSave()", (int)duration.TotalMilliseconds, ""); }
            }
        }
        #endregion

        #region Tag Event Handlers
        async private void cmdTagRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RefreshAndDisplayTagList(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem loading the tag list.  Error: " + ex.Message);
            }

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
                await TagDelete();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem deleting the tag.  Error: " + ex.Message);
            }

            try
            {
                RefreshPanesAllControls(PaneMode.TagView);
                await RefreshAndDisplayTagList(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem refreshing the tag view.  Error: " + ex.Message);
            }
        }

        private void cmdTagAdd_Click(object sender, RoutedEventArgs e)
        {
            txtTagName.Text = "";
            txtTagDescription.Text = "";
            RefreshPanesAllControls(PaneMode.TagAdd);
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
                await TagSave();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem updating tag data on the server.  Error: " + ex.Message);
            }

            try
            {
                RefreshPanesAllControls(PaneMode.TagView);
                await RefreshAndDisplayTagList(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem refreshing the tags view.  Error: " + ex.Message);
            }
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

            try
            {
                TagListTagRef tagListTagRef = (TagListTagRef)checkedButton.Tag;
                await PopulateTagMetaData(tagListTagRef);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem retrieving tag data from the server.  Error: " + ex.Message);
            }
        }

        private void cmdTagCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshPanesAllControls(PaneMode.TagView);
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
                        if (MessageBox.Show("Do you want to remove the " + count.ToString() + " selected images from the tag: " + tagListTagRefTemp.name + "?", "fotowalla", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                        {
                            try
                            {
                                await AddRemoveImagesFromTag(false, new string[1] { tagListTagRefTemp.name });
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex);
                                ShowMessage(MainTwo.MessageType.Error, "Images could not be removed from the Tag, there was an error on the server: " + ex.Message);
                            }

                            try
                            {
                                await RefreshAndDisplayTagList(true);
                            }
                            catch (Exception ex)
                            {
                                logger.Error(ex);
                                ShowMessage(MainTwo.MessageType.Error, "There was a problem refreshing the tags view.  Error: " + ex.Message);
                            }
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
            int tagCount = lstTagAddImagesInclude.SelectedItems.Count;
            if (tagCount > 0)
            {
                gridTagSelectDialog.Visibility = Visibility.Collapsed;

                string[] tagName = new string[tagCount];
                int i = 0;
                foreach (ListBoxItem current in lstTagAddImagesInclude.SelectedItems)
                {
                    TagListTagRef tagRef = (TagListTagRef)current.Tag;
                    tagName[i] = tagRef.name;
                    i++;
                }

                try
                {
                    await AddRemoveImagesFromTag(true, tagName);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MainTwo.MessageType.Error, "Images could not be added to the Tag, there was an error on the server: " + ex.Message);
                }

                if (lstImageMainViewerList.SelectedItems != null)
                    lstImageMainViewerList.SelectedItems.Clear();

                try
                {
                    await RefreshAndDisplayTagList(true);
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MainTwo.MessageType.Error, "There was a problem refreshing the tags view.  Error: " + ex.Message);
                }
            }
            else
            {
                ShowMessage(MessageType.Warning, "You must select at least one tag.");
                return;
            }
        }

        private void cmdTagAddImagesCancel_Click(object sender, RoutedEventArgs e)
        {
            gridTagSelectDialog.Visibility = Visibility.Collapsed;
            paneBusy.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Upload Methods
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

        async private Task ResetAllMetaUpdates()
        {
            uploadUIState.MetaUdfChar1 = null;
            uploadUIState.MetaUdfChar2 = null;
            uploadUIState.MetaUdfChar3 = null;
            uploadUIState.MetaUdfText1 = null;
            uploadUIState.MetaUdfNum1 = 0;
            uploadUIState.MetaUdfNum2 = 0;
            uploadUIState.MetaUdfNum3 = 0;
            uploadUIState.MetaUdfDate1 = DateTime.Now;
            uploadUIState.MetaUdfDate2 = DateTime.Now;
            uploadUIState.MetaUdfDate3 = DateTime.Now;

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

            for (int i = 0; i < uploadFots.Count; i++)
            {
                await uploadFots[i].ResetMeta();
            }
        }

        private void ResetUploadState()
        {
            uploadUIState.GotSubFolders = false;
            uploadUIState.CategoryName = "";
            uploadUIState.CategoryDesc = "";
            uploadUIState.MapToSubFolders = false;
            uploadUIState.UploadToNewCategory = true;
            uploadUIState.Mode = UploadUIState.UploadMode.None;
            uploadUIState.RootCategoryId = state.userApp.UserDefaultCategoryId;
            uploadUIState.RootCategoryName = GetCategoryName(state.userApp.UserDefaultCategoryId);
            uploadUIState.RootFolder = "";
            uploadUIState.AutoUploadCategoryName = GetCategoryName(state.userApp.UserAppCategoryId);
            uploadUIState.AutoUploadFolder = state.userApp.AutoUploadFolder;
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

        async private void UploadTimerDispatcherAsync()
        {
            try
            {
                if (currentPane != PaneMode.Upload && currentPane != PaneMode.AccountEdit && uploadUIState.Mode == UploadUIState.UploadMode.None && state.userApp.AutoUpload)
                    await DoAutoUploadAsync(false);

                bool force = false;

                if (currentPane == PaneMode.Account && tabAccount.SelectedIndex == 1)
                    force = true;

                await RefreshUploadStatusStateAsync(force, true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "There was an unexpected error whilst checking upload status.  Error: " + ex.Message);
            }
        }

        async private Task DoAutoUploadAsync(bool resume)
        {
            DateTime startTime = DateTime.Now;
            int uploadCount = 0;

            if (!resume)
            {
                //UploadImageStateApplyServerState();

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

                uploadCount = uploadFots.Count;

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
                if (logger.IsDebugEnabled) { logger.Debug("DoAutoUploadAsync has been cancelled."); }
                ResetUploadState();
            }
            catch (Exception ex)
            {
                uploadFots.Clear();
                ResetUploadState();
            }
            finally
            {
                uploadUIState.Uploading = false;
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.DoAutoUploadAsync()", (int)duration.TotalMilliseconds, uploadCount.ToString()); }

            }
        }

        async private Task<List<string>> DoUploadAsync()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                if (cancelUploadTokenSource != null)
                    cancelUploadTokenSource.Cancel();

                CancellationTokenSource newCancelUploadTokenSource = new CancellationTokenSource();
                cancelUploadTokenSource = newCancelUploadTokenSource;

                List<string> responses = await controller.UploadManualAsync(uploadFots, uploadUIState, cancelUploadTokenSource.Token);

                if (newCancelUploadTokenSource == cancelUploadTokenSource)
                    cancelUploadTokenSource = null;

                return responses;
            }
            catch (OperationCanceledException cancelEx)
            {
                if (logger.IsDebugEnabled) { logger.Debug("DoUploadAsync has been cancelled."); }
                throw cancelEx;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.DoUploadAsync()", (int)duration.TotalMilliseconds, ""); }

            }
        }

        /*
        async private void RefreshUploadStatusStateDispatcherAsync(bool force, bool silent)
        {
            try
            {
                await RefreshUploadStatusStateAsync(true, true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem refreshing the account information.  Error: " + ex.Message);
            }
        }
        */

        async private Task RefreshUploadStatusStateAsync(bool force, bool silent)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                bool isBusy = bool.Parse(cmdUploadRefresh.Tag.ToString());
                if (isBusy) { return; }

                if (!force && cancelUploadTokenSource != null)
                    return;

                //Get Ids for any images not completed, but which have been sent.
                long[] orderIds = CacheHelper.GetUploadImageListQueryIds(uploadImageStateList);

                if (!force && orderIds.Length == 0)
                {
                    CacheHelper.DeleteUploadedFiles(uploadImageStateList, uploadUIState.AutoUploadFolder, state.userApp.MachineName, logger);
                    return;
                }

                if (!silent)
                    ShowMessage(MessageType.Busy, "Refreshing upload history list");

                cmdUploadRefresh.Tag = true;

                CancellationTokenSource newCancelUploadListTokenSource = new CancellationTokenSource();
                cancelUploadListTokenSource = newCancelUploadListTokenSource;

                await controller.RefreshUploadStatusListAsync(orderIds, cancelUploadListTokenSource.Token, uploadImageStateList);

                CacheHelper.DeleteUploadedFiles(uploadImageStateList, uploadUIState.AutoUploadFolder, state.userApp.MachineName, logger);

                if (newCancelUploadListTokenSource == cancelUploadListTokenSource)
                    cancelUploadListTokenSource = null;

                switch (state.uploadStatusListState)
                {
                    case GlobalState.DataLoadState.Loaded:
                    case GlobalState.DataLoadState.LocalCache:
                        RefreshUploadStatusListBinding();
                        panUploadStatusListUnavailable.Visibility = System.Windows.Visibility.Collapsed;
                        break;
                    case GlobalState.DataLoadState.Unavailable:
                        panUploadStatusListUnavailable.Visibility = System.Windows.Visibility.Visible;
                        break;
                }

                if (!silent)
                    ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("RefreshUploadStatusStateAsync has been cancelled."); }
            }
            finally
            {
                cmdUploadRefresh.Tag = false;
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.RefreshUploadStatusStateAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        private void UploadImageStateApplyServerState()
        {
            //TODO
            //Get current server updates to apply to the local upload list history.
        }
        #endregion

        #region Upload Event Handlers

        async private void cmdUploadImportFolder_Click(object sender, RoutedEventArgs e)
        {
            var folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = folderDialog.ShowDialog();
            if (folderDialog.SelectedPath.Length > 0)
            {
                DateTime startTime = DateTime.Now;

                if (cancelUploadTokenSource != null)
                    cancelUploadTokenSource.Cancel();

                CancellationTokenSource newCancelUploadTokenSource = new CancellationTokenSource();
                cancelUploadTokenSource = newCancelUploadTokenSource;

                uploadUIState.MapToSubFolders = false;
                DirectoryInfo folder = new DirectoryInfo(folderDialog.SelectedPath);

                try
                {
                    ShowMessage(MessageType.Busy, "Files being analysed for upload");

                    TweakImageMarginSize(DateTime.Now, currentPane);

                    List<string> responses = null;
                    if (folder.GetDirectories().Length > 0)
                    {
                        uploadUIState.GotSubFolders = true;
                        uploadUIState.RootFolder = folderDialog.SelectedPath;
                        if (MessageBox.Show("Do you want to add images from the sub folders too ?", "fotowalla", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
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
                    if (logger.IsDebugEnabled) { logger.Debug("cmdUploadImportFolder_Click has been cancelled."); }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MessageType.Error, "There was an unexpected error whilst preparing files for uploading.  Error: " + ex.Message);
                }
                finally
                {
                    TimeSpan duration = DateTime.Now - startTime;
                    if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.cmdUploadImportFolder_Click()", (int)duration.TotalMilliseconds, folder.Name); }
                }

                if (lstUploadImageFileList.Items.Count > 0) //&& lstUploadImageFileList.SelectedItems.Count == 0
                {
                    uploadUIState.Mode = UploadUIState.UploadMode.Folder;
                    RefreshPanesAllControls(PaneMode.Upload);
                    lstUploadImageFileList.SelectedIndex = 0;
                }
                RefreshOverallPanesStructure(currentPane);

                DateTime eventTime = DateTime.Now;
                await WaitAsynchronouslyAsync();
                TweakImageMarginSize(eventTime, currentPane); 
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
                DateTime startTime = DateTime.Now;

                if (cancelUploadTokenSource != null)
                    cancelUploadTokenSource.Cancel();

                CancellationTokenSource newCancelUploadTokenSource = new CancellationTokenSource();
                cancelUploadTokenSource = newCancelUploadTokenSource;

                try
                {
                    ShowMessage(MessageType.Busy, "Files being analysed for upload");
                    TweakImageMarginSize(DateTime.Now, currentPane);

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
                    if (logger.IsDebugEnabled) { logger.Debug("cmdUploadImportFiles_Click has been cancelled."); }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    ShowMessage(MessageType.Error, "There was an unexpected error whilst preparing files for uploading.  Error: " + ex.Message);
                }
                finally
                {
                    TimeSpan duration = DateTime.Now - startTime;
                    if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.cmdUploadImportFiles_Click()", (int)duration.TotalMilliseconds, ""); }
                }
            }

            if (lstUploadImageFileList.Items.Count > 0) //&& lstUploadImageFileList.SelectedItems.Count == 0
            {
                uploadUIState.GotSubFolders = false;
                uploadUIState.Mode = UploadUIState.UploadMode.Images;
                RefreshPanesAllControls(PaneMode.Upload);
                lstUploadImageFileList.SelectedIndex = 0;
            }
            RefreshOverallPanesStructure(currentPane);

            DateTime eventTime = DateTime.Now;
            await WaitAsynchronouslyAsync();
            TweakImageMarginSize(eventTime, currentPane);  
        }

        async private void cmdUploadResumePauseClear_Click(object sender, RoutedEventArgs e)
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
                    try
                    {
                        await DoAutoUploadAsync(true);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        ShowMessage(MessageType.Error, "There was an unexpected error whilst preparing files for uploading.  Error: " + ex.Message);
                    }
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
            try
            {
                await ResetAllMetaUpdates();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem resetting the uploads.  Error: " + ex.Message);
            }

            RefreshPanesAllControls(PaneMode.Upload);
        }

        async private void cmdUploadAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
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

                if (!uploadUIState.UploadToNewCategory && (uploadUIState.RootCategoryId == state.userApp.UserDefaultCategoryId && uploadUIState.Mode == UploadUIState.UploadMode.Images))
                {
                    ShowMessage(MessageType.Warning, "You cannot upload images directly to the root category, please create a new category.");
                    return;
                }

                uploadUIState.Uploading = true;
                RefreshOverallPanesStructure(PaneMode.Upload);
                RefreshPanesAllControls(PaneMode.Upload);

                TweakImageMarginSize(DateTime.Now, currentPane);

                List<string> responses = await DoUploadAsync();

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

                ResetUploadState();
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("cmdUploadAll_Click has been cancelled."); }
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

        private void chkUploadOverrideDateAll_Checked(object sender, RoutedEventArgs e)
        {
            if (uploadUIState.MetaTakenDateSetAll)
            {
                UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
                uploadUIState.MetaTakenDate = current.Meta.TakenDate;

                BindingOperations.ClearBinding(datUploadOverrideDate, DatePicker.SelectedDateProperty);
                Binding binding = new Binding("MetaTakenDate");
                binding.Mode = BindingMode.TwoWay;
                binding.Source = uploadUIState;
                BindingOperations.SetBinding(datUploadOverrideDate, DatePicker.SelectedDateProperty, binding);

                grdUploadImageDetails.RowDefinitions[6].Height = new GridLength(0);
                lblUploadOverrideDateAll.Content = "Override date (All fotos)";
            }
            else
            {
                BindingOperations.ClearBinding(datUploadOverrideDate, DatePicker.SelectedDateProperty);
                Binding binding = new Binding("/Meta.TakenDate");
                binding.Mode = BindingMode.TwoWay;
                BindingOperations.SetBinding(datUploadOverrideDate, DatePicker.SelectedDateProperty, binding);

                grdUploadImageDetails.RowDefinitions[6].Height = new GridLength(30);
                lblUploadOverrideDateAll.Content = "Apply to all";
            }
        }

        private void chkUploadOverrideDate_Checked(object sender, RoutedEventArgs e)
        {
            grdUploadImageDetails.RowDefinitions[7].Height = new GridLength(30);
            datUploadOverrideDate.IsEnabled = true;
        }

        private void chkUploadOverrideDate_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!uploadUIState.MetaTakenDateSetAll)
            {
                grdUploadImageDetails.RowDefinitions[7].Height = new GridLength(0);
                datUploadOverrideDate.IsEnabled = false;
            }
        }

        public void UploadImageStateInProgress_Filter(object sender, FilterEventArgs e)
        {
            UploadImageState item = e.Item as UploadImageState;
            if (item.status != UploadImage.ImageStatus.Complete && item.status != UploadImage.ImageStatus.Inactive)
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
            if (item.status == UploadImage.ImageStatus.Complete)
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

        private void cmdUploadRemoveImage_Click(object sender, RoutedEventArgs e)
        {
            UploadImage uploadImage = (UploadImage)lstUploadImageFileList.SelectedItem;
            if (uploadImage != null)
            {
                uploadFots.Remove(uploadImage);
            }
        }
        #endregion

        #region Upload Binding Remapping
        private void chkUploadTagsAll_Checked(object sender, RoutedEventArgs e)
        {
            UpdateUploadTagCollection();
            UploadTagListReload();
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

        #region Account Methods
        public void RefreshUploadStatusListBinding()
        {
            Dispatcher.Invoke(RefreshUploadStatusListBindingDispatcher);
        }

        private void RefreshUploadStatusListBindingDispatcher()
        {
            CollectionViewSource pendingFilter = (CollectionViewSource)FindResource("uploadImageStateListInProgressKey");
            pendingFilter.View.Refresh();

            CollectionViewSource completedFilter = (CollectionViewSource)FindResource("uploadImageStateListCompleteKey");
            completedFilter.View.Refresh();
        }

        private void AccountRefreshFromState()
        {
            if (state!= null && state.account != null)
            {
                lblAccountType.Content = state.account.AccountTypeName;
                lblAccountOpen.Content = state.account.OpenDate.ToShortDateString();

                //TODO get storage details.
                //lblAccountStorageLimitGB.Content = state.account + " GB";
                //lblAccountCurrentUtil.Content = state.account.StorageGBCurrent + " GB - " + state.account.TotalImages.ToString() + " Images";
                //lblAccountEmail.Content = state.account.Email;
                lblAccountProfileName.Content = state.account.ProfileName;
            }

            if (state != null && state.userApp != null)
            {
                chkAccountAutoUpload.IsChecked = state.userApp.AutoUpload;
                lblAccountAutoUploadFolder.Content = state.userApp.AutoUploadFolder;
                lblAccountAutoUploadFolderAbbrev.Content = StringTrim(state.userApp.AutoUploadFolder, 80);

                lblAccountImageCopyFolder.Content = state.userApp.MainCopyFolder;
                lblAccountImageCopyFolderAbbrev.Content = StringTrim(state.userApp.MainCopyFolder, 80);

                WallaByteToMBConverter converter = new WallaByteToMBConverter();
                lblAccountImageCopyStatus.Content = converter.Convert(mainCopyCacheList.Sum<MainCopyCache>(r => r.imageSize), null, null, null)
                    + " - " + mainCopyCacheList.Count().ToString() + " Images";

                sldAccountImageCopySize.Value = state.userApp.MainCopyCacheSizeMB;
            }

            if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                this.Title = "fotowalla - connected";
            else
                this.Title = "fotowalla - offline";
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
            DateTime startTime = DateTime.Now;
            try
            {
                ShowMessage(MessageType.Busy, "Application settings being saved");

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                int mainCopySize = state.userApp.MainCopyCacheSizeMB;

                UserApp changedUserApp = new UserApp();
                changedUserApp.AppId = state.userApp.AppId;
                changedUserApp.id = state.userApp.id;
                changedUserApp.MachineName = state.userApp.MachineName;
                changedUserApp.version = state.userApp.version;
                changedUserApp.FetchSize = state.userApp.FetchSize;
                changedUserApp.GalleryId = state.userApp.GalleryId;
                changedUserApp.UserAppCategoryId = state.userApp.UserAppCategoryId;
                changedUserApp.UserDefaultCategoryId = state.userApp.UserDefaultCategoryId;
                changedUserApp.ThumbCacheSizeMB = state.userApp.ThumbCacheSizeMB;

                changedUserApp.AutoUpload = (bool)chkAccountAutoUpload.IsChecked;
                changedUserApp.AutoUploadFolder = (string)lblAccountAutoUploadFolder.Content;

                changedUserApp.MainCopyFolder = (string)lblAccountImageCopyFolder.Content;
                changedUserApp.MainCopyCacheSizeMB = Convert.ToInt32(sldAccountImageCopySize.Value);

                await controller.UserAppUpdateAsync(changedUserApp, cancelTokenSource.Token);

                if (changedUserApp.MainCopyCacheSizeMB < mainCopySize)
                    CacheHelper.ReduceMainCopyCacheSize(mainCopyCacheList, changedUserApp.MainCopyFolder, changedUserApp.MainCopyCacheSizeMB);

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                await AccountStatusRefresh(null);
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("AccountSave has been cancelled."); }
            }
            finally
            {
                AccountRefreshFromState();
                ResetUploadState();
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.UserAppSave()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task Login(string profileName, string email, string password)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                ShowMessage(MessageType.Busy, "Logging onto FotoWalla");

                string logonResponse = await controller.Logon(profileName, email, password);

                if (logonResponse == "OK")
                {
                    if (profileName != Properties.Settings.Default.LastUser)
                    {
                        Properties.Settings.Default.LastUser = profileName;
                        Properties.Settings.Default.Save();
                    }

                    if (!cacheFilesSetup)
                        UseCreateLocalCacheFiles(profileName);

                    state.connectionState = GlobalState.ConnectionState.LoggedOn;

                    await AccountStatusRefresh(password);

                    ShowMessage(MessageType.Info, "Account: " + state.account.ProfileName + " has been connected with FotoWalla");
                }
                else
                {
                    if (cacheFilesSetup)
                        state.connectionState = GlobalState.ConnectionState.FailedLogin;

                    ShowMessage(MessageType.Warning, "The logon for: " + profileName + email + ", failed with the message: " + logonResponse);
                }
                ConcludeBusyProcess();
            }
            finally
            {
                AccountRefreshFromState();
                ResetUploadState();
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.Login()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task AccountStatusRefresh(string password)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                ShowMessage(MessageType.Busy, "Refreshing Account information");
                cmdAccountRefresh.Tag = true;

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                if (password == null)
                    password = state.account.Password;

                await controller.AccountDetailsGet(cancelTokenSource.Token);
                state.account.Password = password;

                if (!await controller.VerifyAppAndPlatform(false))
                {
                    throw new Exception("The application/platform failed validation with the server.  Please check www.fotowalla.com/support for the latest versions supported.");
                }

                await controller.SetUserApp(cancelTokenSource.Token);

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("AccountStatusRefresh has been cancelled."); }
            }
            finally
            {
                cmdAccountRefresh.Tag = false;
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.AccountStatusRefresh()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task Logout()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                ShowMessage(MessageType.Busy, "Logging out of FotoWalla");

                await controller.Logout();

                Properties.Settings.Default.LastUser = "";
                Properties.Settings.Default.Save();

                timer.Stop();
                radGallery.IsChecked = false;
                state = null;
                thumbCacheList = null;
                mainCopyCacheList = null;
                imageMainViewerList.Clear();
                //uploadImageStateList = null;
                //currentImageList = null;
                startingApplication = true;
                cacheFilesSetup = false;

                ShowMessage(MessageType.Info, "Account has been logged out.");

                ConcludeBusyProcess();
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.Logout()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        #endregion

        #region Account Event Handlers

        private void cmdAccountClose_Click(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(previousPane);
            RefreshPanesAllControls(previousPane);
        }
        
        private void cmdUserAppCancel_Click(object sender, RoutedEventArgs e)
        {
            AccountRefreshFromState();
            RefreshPanesAllControls(PaneMode.Account);
        }

        async private void cmdUserAppSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await UserAppSave();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem saving the settings.  Error: " + ex.Message);
            }

            RefreshPanesAllControls(PaneMode.Account);
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
                string email = txtAccountEmail.Text;
                string password = txtAccountPassword.Password;

                if (profileName.Length < 1 && email.Length < 1)
                {
                    ShowMessage(MessageType.Warning, "You must enter your profile name or email to continue");
                    return;
                }

                if (password.Length < 1)
                {
                    ShowMessage(MessageType.Warning, "You must enter a password to continue");
                    return;
                }

                await Login(profileName, email, password);
                if (state.connectionState == GlobalState.ConnectionState.LoggedOn)
                {


                    await GalleryPopulateOptions();

                    timer = new System.Timers.Timer();
                    timer.Elapsed += timer_Elapsed;
                    timer.Interval = 10000.0;
                    timer.Start();

                    RefreshOverallPanesStructure(PaneMode.GalleryView);
                    RefreshPanesAllControls(PaneMode.GalleryView);
                    //cmdAccountClose.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    radGallery.IsChecked = true;
                }

                txtAccountProfileName.Text = "";
                txtAccountEmail.Text = "";
                txtAccountPassword.Password = "";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "The logon process failed with an unexpected problem: " + ex.Message);
            }
        }

        async private void cmdAccountLogout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                timer.Stop();
                await Logout();

                cacheFilesSetup = false;
                this.Title = "fotowalla";

                Properties.Settings.Default.LastUser = "";
                Properties.Settings.Default.Save();


                RefreshOverallPanesStructure(PaneMode.Account);
                RefreshPanesAllControls(PaneMode.Account);

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "The logout process failed with an unexpected problem: " + ex.Message);
            }
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

        async private void cmdAccountRefresh_Click(object sender, RoutedEventArgs e)
        {
            DateTime startTime = DateTime.Now;

            try
            {
                await AccountStatusRefresh(null);
                AccountRefreshFromState();
                ResetUploadState();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem refreshing the account information.  Error: " + ex.Message);
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.cmdAccountRefresh_Click()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private void cmdUploadRefresh_Click(object sender, RoutedEventArgs e)
        {
            DateTime startTime = DateTime.Now;

            try
            {
                await RefreshUploadStatusStateAsync(true, true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem refreshing the upload information.  Error: " + ex.Message);
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.cmdUploadRefresh_Click()", (int)duration.TotalMilliseconds, ""); }
            }
        }
        #endregion

        #region Gallery Methods
        async private Task GalleryInit()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                ShowMessage(MessageType.Busy, "Setting up gallery details");

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;



                gallerySectionNeedRefresh = true;
                tabGallery.SelectedIndex = 0;

                txtGalleryName.Text = "";
                txtGalleryDescription.Text = "";
                txtGalleryPassword.Text = "";
                cmbGalleryAccessType.SelectedIndex = 0;

                GallerySetGroupingType(0);
                GallerySetSelectionType(0);
                GallerySetPresentationType(1);
                GallerySetStyleType(1);

                chkGalleryShowName.IsChecked = true;
                chkGalleryShowDesc.IsChecked = true;
                chkGalleryShowImageName.IsChecked = false;
                chkGalleryShowImageDesc.IsChecked = false;
                chkGalleryShowImageMeta.IsChecked = false;
                chkGalleryShowGroupingDesc.IsChecked = false;

                gallerySectionList.Clear();

                GalleryRefreshTagsListFromState();
                GalleryRefreshCategoryList();

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("InitNewGallery has been cancelled."); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.GalleryInit()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task GalleryPopulateOptions()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                await controller.GalleryOptionsRefreshAsync(galleryPresentationList, galleryStyleList, cancelTokenSource.Token);
                lstGalleryPresentationList.Items.Refresh();
                lstGallerySelectionOptions.Items.Refresh();

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("Loading allery options has been cancelled"); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.GalleryPopulateOptions()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task GallerySave()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                ShowMessage(MessageType.Busy, "Saving gallery details");

                if (gallerySectionNeedRefresh)
                    await GalleryReloadSection(false);

                Gallery gallery = GalleryCreateFromGUI(false);

                if (!GalleryValidate(gallery, false))
                    return;

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                if (currentPane == PaneMode.GalleryAdd)
                {
                    await controller.GalleryCreateAsync(gallery, cancelTokenSource.Token);
                }
                else
                {
                    await controller.GalleryUpdateAsync(gallery, currentGallery.Name, cancelTokenSource.Token);
                }

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("GallerySave has been cancelled."); }
            }
            finally
            {
                ConcludeBusyProcess();
	            TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.GallerySave()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task GalleryDelete()
        {
            DateTime startTime = DateTime.Now;
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
                if (logger.IsDebugEnabled) { logger.Debug("GalleryDelete has been cancelled."); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.GalleryDelete()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task RefreshAndDisplayGalleryList(bool forceRefresh)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                bool isBusy = bool.Parse(radGallery.Tag.ToString());
                if (isBusy) { return; }

                bool redrawList = false;

                //Catch first time loads, user intiated refresh and when user was offline and is now online.  But only if logged on.
                if (state.connectionState != GlobalState.ConnectionState.NoAccount &&
                    (state.galleryLoadState == GlobalState.DataLoadState.No || forceRefresh || state.galleryLoadState == GlobalState.DataLoadState.LocalCache))
                {
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
                if (logger.IsDebugEnabled) { logger.Debug("RefreshAndDisplayGalleryList has been cancelled."); }
            }
            catch (Exception ex)
            {
                state.galleryList = null;
                state.galleryLoadState = GlobalState.DataLoadState.Unavailable;
                throw ex;
            }
            finally
            {
                radGallery.Tag = false;
	            TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.RefreshAndDisplayGalleryList()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task GalleryPopulateMetaData(GalleryListGalleryRef galleryListGalleryRef)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                ShowMessage(MessageType.Busy, "Loading gallery details");

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                //if (!galleryOptionsLoaded)
                //{
                //    await controller.GalleryOptionsRefreshAsync(galleryPresentationList, galleryStyleList, cancelTokenSource.Token);
                //    galleryOptionsLoaded = true;
                //}

                GalleryRefreshTagsListFromState();
                GalleryRefreshCategoryList();

                Gallery gallery = await controller.GalleryGetMetaAsync(galleryListGalleryRef, cancelTokenSource.Token);

                gallerySectionNeedRefresh = true;
                tabGallery.SelectedIndex = 0;
                txtGalleryName.Text = gallery.Name;
                txtGalleryDescription.Text = gallery.Desc;
                txtGalleryPassword.Text = gallery.Password;
                cmbGalleryAccessType.SelectedIndex = gallery.AccessType;

                GallerySetGroupingType(gallery.GroupingType);
                GallerySetSelectionType(gallery.SelectionType);
                GallerySetPresentationType(gallery.PresentationId);
                GallerySetStyleType(gallery.StyleId);

                chkGalleryShowName.IsChecked = gallery.ShowGalleryName;
                chkGalleryShowDesc.IsChecked = gallery.ShowGalleryDesc;
                chkGalleryShowImageName.IsChecked = gallery.ShowImageName;
                chkGalleryShowImageDesc.IsChecked = gallery.ShowImageDesc;
                chkGalleryShowImageMeta.IsChecked = gallery.ShowImageMeta;
                chkGalleryShowGroupingDesc.IsChecked = gallery.ShowGroupingDesc;

                gallerySectionList.Clear();
                foreach (GallerySectionRef current in gallery.Sections)
                {
                    GallerySectionItem newSection = new GallerySectionItem();
                    newSection.sectionId = current.id;
                    newSection.name = current.name;
                    newSection.nameOveride = current.name;
                    newSection.desc = current.desc;
                    newSection.descOveride = current.desc;
                    newSection.sequence = 0;

                    gallerySectionList.Add(newSection);
                }

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

                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("PopulateGalleryMetaData has been cancelled."); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.GalleryPopulateMetaData()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task GalleryReloadSection(bool isReset)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                if (!gallerySectionNeedRefresh && !isReset)
                    return;

                ShowMessage(MessageType.Busy, "Gallery sections being refreshed");

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                await controller.GalleryGetSectionListAndMerge(GalleryCreateFromGUI(false), gallerySectionList, isReset, cancelTokenSource.Token);
                gallerySectionNeedRefresh = false;

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                datGallerySections.Items.Refresh();

                ConcludeBusyProcess();
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("GalleryReloadSection has been cancelled."); }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.GalleryReloadSection()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task<string> GallerySetupPreview()
        {
            DateTime startTime = DateTime.Now;
            try
            {
                ShowMessage(MessageType.Busy, "Creating preview");

                if (cancelTokenSource != null)
                    cancelTokenSource.Cancel();

                CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                cancelTokenSource = newCancelTokenSource;

                Gallery gallery = GalleryCreateFromGUI(true);

                if (!GalleryValidate(gallery, true))
                    return "";

                string galleryPreviewKey = await controller.GalleryCreatePreviewAsync(gallery, cancelTokenSource.Token);

                if (newCancelTokenSource == cancelTokenSource)
                    cancelTokenSource = null;

                ConcludeBusyProcess();

                return controller.GetGalleryPreviewUrl(galleryPreviewKey);
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("GallerySetupPreview has been cancelled."); }
                return "";
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.GallerySetupPreview()", (int)duration.TotalMilliseconds, ""); }
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

        private bool GalleryPopulateSectionDropdown(GalleryListGalleryRef galleryListRefTemp)
        {
            if (galleryListRefTemp.SectionRef != null && galleryListRefTemp.SectionRef.Length > 1)
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
                TreeViewItem newItem = GalleryGetTreeView(current.id, current.name, current.desc);
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

        private TreeViewItem GalleryGetTreeView(long categoryId, string name, string desc)
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
            newCmb.Width = 40.0;

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

            //stackEntryNone.Children.Add(imgEntryNone);
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

            //stackInclude.Children.Add(imgEntryInclude);
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

            //stackEntryAll.Children.Add(imgEntryAll);
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
                cmbCurrent.SelectedIndex = 1;
                currentItem.Foreground = (Brush)FindResource("FontBorderBrushSelected");
            }
            else
            {
                currentItem.IsEnabled = true;


                if (cmbCurrent.SelectedIndex == 0)
                {
                    currentItem.Foreground = (Brush)FindResource("FontBorderBrush");
                }
                else
                {
                    currentItem.Foreground = (Brush)FindResource("FontBorderBrushSelected");
                    if (cmbCurrent.SelectedIndex == 2)
                        parentRecursive = true;
                }
            }

            foreach (TreeViewItem child in currentItem.Items)
                GalleryCategoryRecursiveRelatedUpdates(child, parentRecursive);
        }

        private Gallery GalleryCreateFromGUI(bool preview)
        {
            Gallery gallery = new Gallery();
            gallery.Name = txtGalleryName.Text;
            gallery.Desc = txtGalleryDescription.Text;
            gallery.Password = txtGalleryPassword.Text;
            gallery.AccessType = cmbGalleryAccessType.SelectedIndex;

            gallery.GroupingType = GalleryGetGroupingType();
            gallery.SelectionType = GalleryGetSelectionType();
            gallery.PresentationId = GalleryGetPresentationType();
            gallery.StyleId = GalleryGetStyleType();

            if (gallery.GroupingType > 0)
            {
                if (preview)
                {
                    int sectionCount = gallerySectionList.Count<GallerySectionItem>();
                    if (sectionCount > 0)
                    {
                        int i = 0;
                        gallery.Sections = new GallerySectionRef[sectionCount];
                        foreach (GallerySectionItem item in gallerySectionList.OrderBy(r => r.sequence))
                        {
                            GallerySectionRef newSection = new GallerySectionRef();
                            newSection.id = i;
                            newSection.idSpecified = true;
                            newSection.name = (item.nameOveride != item.name) ? item.nameOveride : item.name;
                            newSection.desc = (item.descOveride != item.desc) ? item.descOveride : item.desc;
                            if (item.sequence > 0)
                            {
                                newSection.sequence = item.sequence;
                                newSection.sequenceSpecified = true;
                            }

                            gallery.Sections[i] = newSection;
                            i++;
                        }
                    }
                }
                else
                {
                    int sectionCount = gallerySectionList.Count<GallerySectionItem>(r => (r.sequence > 0 || r.name != r.nameOveride || r.desc != r.descOveride));
                    if (sectionCount > 0)
                    {
                        int i = 0;
                        gallery.Sections = new GallerySectionRef[sectionCount];
                        foreach (GallerySectionItem item in gallerySectionList.Where<GallerySectionItem>(r => (r.sequence > 0 || r.name != r.nameOveride || r.desc != r.descOveride)))
                        {
                            GallerySectionRef newSection = new GallerySectionRef();
                            newSection.id = item.sectionId;
                            newSection.idSpecified = true;
                            newSection.name = (item.nameOveride != item.name) ? item.nameOveride : "";
                            newSection.desc = (item.descOveride != item.desc) ? item.descOveride : "";
                            if (item.sequence > 0)
                            {
                                newSection.sequence = item.sequence;
                                newSection.sequenceSpecified = true;
                            }

                            gallery.Sections[i] = newSection;
                            i++;
                        }
                    }
                }
            }

            gallery.ShowGalleryName = (bool)chkGalleryShowName.IsChecked;
            gallery.ShowGalleryDesc = (bool)chkGalleryShowDesc.IsChecked;
            gallery.ShowImageName = (bool)chkGalleryShowImageName.IsChecked;
            gallery.ShowImageDesc = (bool)chkGalleryShowImageDesc.IsChecked;
            gallery.ShowImageMeta = (bool)chkGalleryShowImageMeta.IsChecked;
            gallery.ShowGroupingDesc = (bool)chkGalleryShowGroupingDesc.IsChecked;

            /* Category add to object ************************************************ */
            if (gallery.SelectionType != 1)
                gallery.Categories = GalleryCategoryGetUpdateList();

            /* Tags add to object ************************************************ */
            gallery.Tags = GalleryTagsUpdateList(gallery.SelectionType);

            if (currentPane == PaneMode.GalleryEdit)
            {
                gallery.id = currentGallery.id;
                gallery.idSpecified = true;
                gallery.version = currentGallery.version;
                gallery.versionSpecified = true;
            }

            return gallery;
        }

        private bool GalleryValidate(Gallery gallery, bool isPreview)
        {
            if (gallery.SelectionType == 0 && (gallery.Categories == null || gallery.Categories.Length == 0))
            {
                ShowMessage(MessageType.Warning, "The gallery is invalid.  Its is setup for Category selection, but does not have any catgories associated with it.");
                return false;
            }

            if (gallery.SelectionType == 1 && (gallery.Tags == null || gallery.Tags.Count(r => r.exclude == false) == 0))
            {
                ShowMessage(MessageType.Warning, "The gallery is invalid.  Its is setup for Tag selection, but does not have any tags associated with it.");
                return false;
            }
            
            if ((gallery.Categories == null || gallery.Categories.Length == 0) && (gallery.Tags == null || gallery.Tags.Count(r => r.exclude == false) == 0))
            {
                ShowMessage(MessageType.Warning, "The gallery is invalid.  The gallery does not have any Catgories or Tags associated with it, so cannot be saved.");
                return false;
            }

            if (!isPreview)
            {
                if (gallery.AccessType == 1 && gallery.Password.Length == 0)
                {
                    ShowMessage(MessageType.Warning, "This gallery has been marked as password protected, but the password does not meet the minumimum criteria of being 8 charactors long.");
                    return false;
                }

                if (gallery.Name.Length == 0)
                {
                    ShowMessage(MessageType.Warning, "You must select a name for your Gallery to continue.");
                    return false;
                }
            }
            return true;
        }

        private int GalleryGetGroupingType()
        {

            ListBoxItem current = (ListBoxItem)lstGalleryGroupOptions.SelectedItem;
            if (current == null)
            {
                throw new Exception("Could not retrieve the grouping type.");
            }
            return int.Parse(current.Tag.ToString());
        }

        private void GallerySetGroupingType(int value)
        {
            foreach (ListBoxItem current in lstGalleryGroupOptions.Items.OfType<ListBoxItem>())
            {
                if (value == int.Parse(current.Tag.ToString()))
                    current.IsSelected = true;
            }
        }

        private int GalleryGetSelectionType()
        {
            ListBoxItem current = (ListBoxItem)lstGallerySelectionOptions.SelectedItem;
            if (current == null)
            {
                throw new Exception("Could not retrieve the selection type.");
            }
            return int.Parse(current.Tag.ToString());
        }

        private void GallerySetSelectionType(int value)
        {
            foreach (ListBoxItem current in lstGallerySelectionOptions.Items.OfType<ListBoxItem>())
            {
                if (value == int.Parse(current.Tag.ToString()))
                    current.IsSelected = true;
            }
        }

        private int GalleryGetPresentationType()
        {
            GalleryPresentationItem current = (GalleryPresentationItem)lstGalleryPresentationList.SelectedItem;
            if (current == null)
            {
                throw new Exception("Could not retrieve the presentation type.");
            }
            return current.PresentationId;
        }

        private void GallerySetPresentationType(int value)
        {
            
            foreach (GalleryPresentationItem current in lstGalleryPresentationList.Items.OfType<GalleryPresentationItem>())
            {
                if (value == current.PresentationId)
                {
                    lstGalleryPresentationList.SelectedItem = current;
                }
            }
        }

        private int GalleryGetStyleType()
        {
            GalleryStyleItem current = (GalleryStyleItem)lstGalleryStylesList.SelectedItem;
            if (current == null)
            {
                throw new Exception("Could not retrieve the style type.");
            }
            return current.StyleId;
        }

        private void GallerySetStyleType(int value)
        {
            foreach (GalleryStyleItem current in lstGalleryStylesList.Items.OfType<GalleryStyleItem>())
            {
                if (value == current.StyleId)
                {
                    lstGalleryStylesList.SelectedItem = current;
                }
            }
        }
        #endregion

        #region Gallery Event Handlers
        async private void cmdGalleryView_Click(object sender, RoutedEventArgs e)
        {
            DateTime startTime = DateTime.Now;
            string url = "";
            GalleryListGalleryRef galleryListGalleryRef;
            RadioButton checkedButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();
            if (checkedButton != null)
            {
                galleryListGalleryRef = (GalleryListGalleryRef)checkedButton.Tag;
            }
            else
            {
                ShowMessage(MessageType.Info, "No Gallery selected to view");
                return;
            }




            try
            {
                ShowMessage(MessageType.Busy, "Loading gallery");

                if (galleryListGalleryRef.urlComplex.Length > 0)
                {
                    url = controller.GetGalleryUrl(galleryListGalleryRef.name, galleryListGalleryRef.urlComplex);
                }
                else
                {
                    if (cancelTokenSource != null)
                        cancelTokenSource.Cancel();

                    CancellationTokenSource newCancelTokenSource = new CancellationTokenSource();
                    cancelTokenSource = newCancelTokenSource;

                    url = await controller.GetGalleryLogonUrlAsync(galleryListGalleryRef.name, cancelTokenSource.Token);

                    if (newCancelTokenSource == cancelTokenSource)
                        cancelTokenSource = null;
                }
            }
            catch (OperationCanceledException)
            {
                if (logger.IsDebugEnabled) { logger.Debug("cmdGalleryView_Click has been cancelled."); }
            }
            finally
            {
                ConcludeBusyProcess();
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.cmdGalleryView_Click()", (int)duration.TotalMilliseconds, ""); }
            }

            try
            {
                if (url.Length > 0)
                {
                    System.Diagnostics.Process.Start(url);
                    ShowMessage(MessageType.Info, "Browser will load web site");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Gallery could not be loaded, unexpected error: " + ex.Message);
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

        async private void cmdGalleryAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await GalleryInit();
                RefreshOverallPanesStructure(PaneMode.GalleryAdd);
                RefreshPanesAllControls(PaneMode.GalleryAdd);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem setting up the gallery.  Error: " + ex.Message);
            }
        }

        async private void cmdGallerySave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await GallerySave();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Gallery could not be saved, there was an error on the server:" + ex.Message);
            }

            try
            {
                RefreshOverallPanesStructure(PaneMode.GalleryView);
                RefreshPanesAllControls(PaneMode.GalleryView);
                await RefreshAndDisplayGalleryList(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Gallery list could not be loaded, there was an error: " + ex.Message);
            }
        }

        async private void cmdGalleryDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await GalleryDelete();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Gallery could not be deleted, there was an error on the server:" + ex.Message);
            }

            try
            {
                RefreshOverallPanesStructure(PaneMode.GalleryView);
                RefreshPanesAllControls(PaneMode.GalleryView);
                await RefreshAndDisplayGalleryList(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Gallery list could not be loaded, there was an error: " + ex.Message);
            }
        }

        private void cmdGalleryCancel_Click(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(PaneMode.GalleryView);
            RefreshPanesAllControls(PaneMode.GalleryView);
        }

        async private void cmdGalleryRefresh_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RefreshAndDisplayGalleryList(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem loading the gallery list.  Error: " + ex.Message);
            }
        }

        async private void cmdGalleryEdit_Click(object sender, RoutedEventArgs e)
        {
            RadioButton checkedButton = (RadioButton)wrapMyGalleries.Children.OfType<RadioButton>().Where(r => r.IsChecked == true).FirstOrDefault();

            if (checkedButton != null)
            {
                GalleryListGalleryRef galleryListGalleryRef = (GalleryListGalleryRef)checkedButton.Tag;
                try
                {
                    await GalleryPopulateMetaData(galleryListGalleryRef);
                    RefreshOverallPanesStructure(PaneMode.GalleryEdit);
                    RefreshPanesAllControls(PaneMode.GalleryEdit);
                }
                catch (Exception ex)
                {
                    RefreshOverallPanesStructure(PaneMode.GalleryView);
                    RefreshPanesAllControls(PaneMode.GalleryView);
                    logger.Error(ex);
                    ShowMessage(MainTwo.MessageType.Error, "There was a problem loading the gallery.  Error: " + ex.Message);
                }
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

        private void GalleryCategory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (TreeViewItem child in treeGalleryCategoryView.Items)
                GalleryCategoryRecursiveRelatedUpdates(child, false);

            gallerySectionNeedRefresh = true;
        }

        private void lstGallerySelectionOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gallerySectionNeedRefresh = true;

            if (lstGalleryGroupOptions == null || lstGallerySelectionOptions == null)
                return;

            //Category.
            if (GalleryGetSelectionType() == 0 && GalleryGetGroupingType() == 2)
                GallerySetGroupingType(0);

            //Tag
            if (GalleryGetSelectionType() == 1 && GalleryGetGroupingType() == 1)
                GallerySetGroupingType(0);

            RefreshPanesAllControls(currentPane);
        }

        private void lstGalleryPresentationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshPanesAllControls(currentPane);
        }

        private void lstGalleryStylesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshPanesAllControls(currentPane);
        }

        private void cmdGalleryPreview_Checked(object sender, RoutedEventArgs e)
        {
            RefreshOverallPanesStructure(currentPane);
        }

        async private void cmdGalleryPreviewView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (gallerySectionNeedRefresh)
                    await GalleryReloadSection(false);

                string url = await GallerySetupPreview();
                if (url.Length > 0)
                {
                    System.Diagnostics.Process.Start(url);
                    ShowMessage(MessageType.Info, "Browser will load web site");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem generating the gallery link.  Error: " + ex.Message);
            }
        }

        async private void tabGallery_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (tabGallery.SelectedIndex == 2)
                    await GalleryReloadSection(false);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem resetting the gallery sections.  Error: " + ex.Message);
            }
            
        }

        private void GalleryTags_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            gallerySectionNeedRefresh = true;
        }

        private void lstGalleryGroupOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshPanesAllControls(currentPane);
        }

        async private void cmdGallerySectionReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await GalleryReloadSection(true);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was a problem resetting the gallery sections.  Error: " + ex.Message);
            }
        }
        #endregion

        private void menuSettings_Click(object sender, RoutedEventArgs e)
        {
            previousPane = currentPane;
            RefreshOverallPanesStructure(PaneMode.Account);
            RefreshPanesAllControls(PaneMode.Account);
        }

        async private void cmdUseOffline_Click(object sender, RoutedEventArgs e)
        {




            DateTime startTime = DateTime.Now;
            try
            {
                if (state.connectionState == GlobalState.ConnectionState.OfflineMode)
                {
                    controller.LogoutFromOffline();

                    cacheFilesSetup = false;
                    this.Title = "fotowalla";
                    cmdUseOffline.Content = "Offline mode";
                }
                else
                {
                    string profileName = state.account.ProfileName;
                    ShowMessage(MessageType.Busy, "Logging out of FotoWalla");
                    await controller.Logout();
                    ShowMessage(MessageType.Info, "Account has been logged out.");
                    ConcludeBusyProcess();

                    UseCreateLocalCacheFiles(profileName);

                    state.connectionState = GlobalState.ConnectionState.OfflineMode;
                    this.Title = "fotowalla - offline mode";
                    cmdUseOffline.Content = "Online mode";
                }
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.cmdUseOffline_Click()", (int)duration.TotalMilliseconds, ""); }
            }

            RefreshOverallPanesStructure(PaneMode.Account);
            RefreshPanesAllControls(PaneMode.Account);
        }

        private void cmdForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = controller.AccountForgotPasswordUrl();
                System.Diagnostics.Process.Start(url);
                ShowMessage(MessageType.Info, "Browser will load web site");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Web site could not be launched, unexpected error: " + ex.Message);
            }
        }

        private void cmdSignupNewUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string url = controller.AccountNewUserUrl();
                System.Diagnostics.Process.Start(url);
                ShowMessage(MessageType.Info, "Browser will load web site");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MessageType.Error, "Web site could not be launched, unexpected error: " + ex.Message);
            }
        }

        async private void cmdBackFromMainImageLayout_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshPanesAllControls(previousPane);
                RefreshOverallPanesStructure(currentPane);

                DateTime eventTime = DateTime.Now;
                await WaitAsynchronouslyAsync();
                TweakImageMarginSize(eventTime, currentPane);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected error: " + ex.Message);
            }
        }

        async private void ImageMain_Error_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                try
                {
                    GeneralImage current = (GeneralImage)lstImageMainViewerList.Items.CurrentItem;
                    if (current == null)
                        return;

                    cancelTokenSource = new CancellationTokenSource();
                    await current.LoadMainCopyImage(cancelTokenSource.Token, mainCopyCacheList, state.userApp.MainCopyFolder, state.userApp.MainCopyCacheSizeMB, state.connectionState);
                    await current.LoadMeta(false, cancelTokenSource.Token, state.connectionState);
                    ImageViewTagsUpdateFromMeta();
                }
                finally
                {
                    TimeSpan duration = DateTime.Now - startTime;
                    if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.imageThumbError_MouseUp()", (int)duration.TotalMilliseconds, ""); }
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected error: " + ex.Message);
            }
        }

        async private void ImageThumb_Error_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DateTime startTime = DateTime.Now;
                try
                {
                    var buttonClicked = sender as Button;
                    DependencyObject dependencyItem = buttonClicked;
                    while (dependencyItem is ListBoxItem == false)
                    {
                        dependencyItem = VisualTreeHelper.GetParent(dependencyItem);
                    }
                    var clickedListBoxItem = (ListBoxItem)dependencyItem;

                    GeneralImage current = (GeneralImage)clickedListBoxItem.DataContext;
                    //Button button = (Button)sender;
                    //DependencyObject parentObj = LogicalTreeHelper.GetParent(button);
                    //ListBoxItem current = parentObj as ListBoxItem;


                    //GeneralImage current = (GeneralImage)lstImageMainViewerList.Items.CurrentItem;
                    if (current == null)
                        return;

                    cancelTokenSource = new CancellationTokenSource();

                    await current.LoadThumb(cancelTokenSource.Token, thumbCacheList, state.userApp.ThumbCacheSizeMB, state.connectionState);
                }
                finally
                {
                    TimeSpan duration = DateTime.Now - startTime;
                    if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "MainTwo.imageThumbError_MouseUp()", (int)duration.TotalMilliseconds, ""); }
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                ShowMessage(MainTwo.MessageType.Error, "There was an unexpected error: " + ex.Message);
            }
        }
        




    }
}