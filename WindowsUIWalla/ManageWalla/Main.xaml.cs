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

        private PaneMode currentPane;
        private Tag currentTag = null;

        private MainController controller = null;
        public UploadUIState uploadUIState = null;
        public UploadImageFileList uploadFots = null;
        public UploadStatusList uploadStatusList = null;
        public GlobalState state = null;

        #endregion

        #region Init Close Window
        public MainWindow()
        {
            InitializeComponent();

            uploadFots = (UploadImageFileList)FindResource("uploadImagefileListKey");
            uploadUIState = (UploadUIState)FindResource("uploadUIStateKey");
            uploadStatusList = (UploadStatusList)FindResource("uploadStatusListKey");
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Apply busy panes overlay

            //Kick off asyncronous data syncronising.
            //This will update all UI Elements eventually
            controller = new MainController(this);
            controller.RetrieveGeneralUserConfig();

            HideAllContent();

            currentPane = PaneMode.CategoryView;
            this.cmdCategory.IsChecked = true;
        }

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            controller.Dispose();
        }
        #endregion

        #region Controls control logic based on Pane
        private void SetWindowHeights(PaneMode mode)
        {

            switch (mode)
            {
                case PaneMode.CategoryView:
                case PaneMode.CategoryAdd:
                case PaneMode.CategoryEdit:

                    /*
                    if (gridLeft.RowDefinitions[3].ActualHeight > 0)
                    {
                        //gridLeft.RowDefinitions[3].MaxHeight = gridLeft.RowDefinitions[3].ActualHeight;

                        DoubleAnimation anim = new DoubleAnimation();
                        anim.From = gridLeft.RowDefinitions[3].ActualHeight;
                        anim.To = 0;
                        anim.FillBehavior = FillBehavior.Stop;
                        anim.Duration = new Duration(TimeSpan.Parse("0:0:5"));

                        gridLeft.RowDefinitions[3].BeginAnimation(RowDefinition.MaxHeightProperty, anim);
                        //gridLeft.RowDefinitions[3].Height = new GridLength(0);
                    }
                    */

                    /*
                    DoubleAnimation catAnim = new DoubleAnimation();
                    catAnim.From = gridLeft.RowDefinitions[1].ActualHeight;
                    catAnim.To = mainWindow.ActualHeight - 68;
                    catAnim.Duration = new Duration(TimeSpan.Parse("0:0:5"));

                    gridLeft.RowDefinitions[1].BeginAnimation(RowDefinition.MaxHeightProperty, catAnim);
                    */

                    //gridLeft.RowDefinitions[1].ClearValue(RowDefinition.MaxHeightProperty);
                    //gridLeft.RowDefinitions[1].MaxHeight = 10000;
                    //gridLeft.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                    //gridLeft.RowDefinitions[5].Height = new GridLength(0);


                    gridLeft.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[3].Height = new GridLength(0);
                    gridLeft.RowDefinitions[5].Height = new GridLength(0);

                    break;
                case PaneMode.TagView:
                case PaneMode.TagAdd:
                case PaneMode.TagEdit:

                    /*
                    if (gridLeft.RowDefinitions[1].ActualHeight > 0)
                    {
                        //gridLeft.RowDefinitions[3].MaxHeight = gridLeft.RowDefinitions[3].ActualHeight;

                        DoubleAnimation anim = new DoubleAnimation();
                        anim.From = gridLeft.RowDefinitions[1].ActualHeight;
                        anim.To = 0;
                        anim.Duration = new Duration(TimeSpan.Parse("0:0:5"));
                        anim.FillBehavior = FillBehavior.Stop;
                        gridLeft.RowDefinitions[1].BeginAnimation(RowDefinition.MaxHeightProperty, anim);
                        //gridLeft.RowDefinitions[3].Height = new GridLength(0);
                    }
                    */

                    //gridLeft.RowDefinitions[3].ClearValue(RowDefinition.MaxHeightProperty);
                    //gridLeft.RowDefinitions[3].MaxHeight = 10000;
                    //gridLeft.RowDefinitions[3].Height = new GridLength(1, GridUnitType.Star);
                    /*
                    DoubleAnimation tagAnim = new DoubleAnimation();
                    tagAnim.From = gridLeft.RowDefinitions[3].ActualHeight;
                    tagAnim.To = mainWindow.ActualHeight - 68;
                    tagAnim.Duration = new Duration(TimeSpan.Parse("0:0:5"));

                    gridLeft.RowDefinitions[3].BeginAnimation(RowDefinition.MaxHeightProperty, tagAnim);
                    */

                        
                    //gridLeft.RowDefinitions[3].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[1].Height = new GridLength(0);
                    gridLeft.RowDefinitions[3].Height = new GridLength(1, GridUnitType.Star);
                    gridLeft.RowDefinitions[5].Height = new GridLength(0);

                    break;
                case PaneMode.Settings:
                    gridLeft.RowDefinitions[1].Height = new GridLength(0);
                    gridLeft.RowDefinitions[3].Height = new GridLength(0);
                    gridLeft.RowDefinitions[5].Height = new GridLength(1,GridUnitType.Star);
                    break;
                case PaneMode.ViewView:
                case PaneMode.ViewEdit:
                case PaneMode.ViewAdd:
                    gridLeft.RowDefinitions[1].Height = new GridLength(2, GridUnitType.Star);
                    gridLeft.RowDefinitions[3].Height = new GridLength(2, GridUnitType.Star);
                    gridLeft.RowDefinitions[5].Height = new GridLength(0);
                    break;
                case PaneMode.ImageViewFull:
                    break;
                case PaneMode.Upload:
                    gridLeft.RowDefinitions[1].Height = new GridLength(2, GridUnitType.Star);
                    gridLeft.RowDefinitions[3].Height = new GridLength(2, GridUnitType.Star);
                    gridLeft.RowDefinitions[5].Height = new GridLength(0);
                    break;
            }
        }

        private void SetPanePositions(PaneMode mode)
        {
            switch (mode)
            {
                case PaneMode.CategoryView:
                    cmdTag.IsChecked = false;
                    cmdView.IsChecked = false;
                    cmdSettings.IsChecked = false;
                    cmdUpload.IsChecked = false;

                    HideAllContent();

                    gridCategory.RowDefinitions[1].MaxHeight = 34;
                    gridCategory.RowDefinitions[2].MaxHeight = 0;
                    gridCategory.RowDefinitions[3].MaxHeight = 0;
                    gridCategory.RowDefinitions[4].MaxHeight = 0;

                    //treeCategoryView.IsEnabled = true;
                    gridCategory.Visibility = Visibility.Visible;



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
                case PaneMode.TagView:
                    cmdCategory.IsChecked = false;
                    cmdView.IsChecked = false;
                    cmdSettings.IsChecked = false;
                    cmdUpload.IsChecked = false;

                    HideAllContent();

                    gridTag.RowDefinitions[1].MaxHeight = 34;
                    gridTag.RowDefinitions[2].MaxHeight = 0;
                    gridTag.RowDefinitions[3].MaxHeight = 0;
                    gridTag.RowDefinitions[4].MaxHeight = 0;

                    gridTag.Visibility = Visibility.Visible;

                    cmdAssociateTag.Visibility = Visibility.Collapsed;
                    cmdAddTag.Visibility = Visibility.Visible;
                    cmdEditTag.Visibility = Visibility.Visible;

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
                case PaneMode.ViewView:
                    cmdCategory.IsChecked = false;
                    cmdTag.IsChecked = false;
                    cmdSettings.IsChecked = false;
                    cmdUpload.IsChecked = false;

                    HideAllContent();
                    stackView.Visibility = Visibility.Visible;
                    //stackTag.Visibility = Visibility.Visible;
                    //stackCategory.Visibility = Visibility.Visible;

                    gridView.Visibility = System.Windows.Visibility.Visible;
                    gridViewAddEdit.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case PaneMode.ViewEdit:


                    gridView.Visibility = System.Windows.Visibility.Collapsed;
                    gridViewAddEdit.Visibility = System.Windows.Visibility.Visible;

                    cmdAddEditViewDelete.Visibility = System.Windows.Visibility.Visible;
                    cmdAddEditViewSave.Content = "Save Update";

                    break;

                case PaneMode.ViewAdd:

                    gridView.Visibility = System.Windows.Visibility.Collapsed;
                    gridViewAddEdit.Visibility = System.Windows.Visibility.Visible;

                    cmdAddEditViewDelete.Visibility = System.Windows.Visibility.Collapsed;
                    cmdAddEditViewSave.Content = "Save New View";

                    break;

                case PaneMode.Upload:

                    cmdCategory.IsChecked = false;
                    cmdTag.IsChecked = false;
                    cmdSettings.IsChecked = false;
                    cmdView.IsChecked = false;

                    HideAllContent();

                    //Sort out Tag view options
                    cmdAssociateTag.Visibility = Visibility.Visible;
                    cmdAddTag.Visibility = Visibility.Collapsed;
                    cmdEditTag.Visibility = Visibility.Collapsed;
                    //gridTagAddEdit.Visibility = Visibility.Collapsed;
                    
                    //gridCategory.Visibility = Visibility.Visible;
                    gridTag.Visibility = Visibility.Visible;

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
                    
                    stackUpload.Visibility = Visibility.Visible;

                    break;
                case PaneMode.ImageViewFull:

                    break;
                case PaneMode.Settings:
                    cmdCategory.IsChecked = false;
                    cmdTag.IsChecked = false;
                    cmdUpload.IsChecked = false;
                    cmdView.IsChecked = false;

                    HideAllContent();
                    gridSettings.Visibility = Visibility.Visible;

                    break;
            }
            SetWindowHeights(mode);
            currentPane = mode;
        }

        private void HideAllContent()
        {
            gridCategory.Visibility = Visibility.Collapsed;
            //stackCategory.Visibility = Visibility.Collapsed;
            gridTag.Visibility = Visibility.Collapsed;
            gridSettings.Visibility = Visibility.Collapsed;
            stackView.Visibility = Visibility.Collapsed;
            stackUpload.Visibility = Visibility.Collapsed;
        }
        #endregion

        #region View UI Control
        private void cmdEditView_Click(object sender, RoutedEventArgs e)
        {
            SetPanePositions(PaneMode.ViewEdit);
        }

        private void cmdAddNewView_Click(object sender, RoutedEventArgs e)
        {
            SetPanePositions(PaneMode.ViewAdd);
        }

        private void cmdAddEditViewCancel_Click(object sender, RoutedEventArgs e)
        {
            SetPanePositions(PaneMode.ViewView);
        }
        #endregion

        #region Category UI Control
        private void cmdAddCategory_Click(object sender, RoutedEventArgs e)
        {
            SetPanePositions(PaneMode.CategoryAdd);
        }

        private void cmdAddEditCategoryCancel_Click(object sender, RoutedEventArgs e)
        {
            SetPanePositions(PaneMode.CategoryView);
        }

        private void cmdEditCategory_Click(object sender, RoutedEventArgs e)
        {
            SetPanePositions(PaneMode.CategoryEdit);
        }

        public void RefreshCategoryTreeView()
        {

        }
        #endregion

        #region Tag UI Control
        async private void RefreshTagsList()
        {
            wrapMyTags.Children.Clear();
            TagList tagList = null;
            try
            {
                tagList = await controller.GetTagsAvailable();
            }
            catch (Exception ex)
            {
                if (tagList == null)
                {
                    MessageBox.Show("There was an error retrieving the tags from the server.  Message: " + ex.Message);
                    return;
                }
                else
                {
                    MessageBox.Show("Displaying previous tag list.  An error was encountered, message: " + ex.Message);
                }
            }

            foreach (TagListTagRef tag in tagList.TagRef)
            {
                RadioButton newRadioButton = new RadioButton();

                newRadioButton.Content = tag.name + " (" + tag.count + ")";
                newRadioButton.Style = (Style)FindResource("styleRadioButton");
                newRadioButton.Template = (ControlTemplate)FindResource("templateRadioButton");
                newRadioButton.GroupName = "GroupTag";
                newRadioButton.Tag = tag;
                wrapMyTags.Children.Add(newRadioButton);
            }
        }

        private void PopulateTagData()
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

            SetPanePositions(PaneMode.TagView);
            RefreshTagsList();
        }

        private void cmdAddTag_Click(object sender, RoutedEventArgs e)
        {
            txtTagAddEditName.Text = "";
            txtTagAddEditDescription.Text = "";
            SetPanePositions(PaneMode.TagAdd);
        }

        private void cmdEditTag_Click(object sender, RoutedEventArgs e)
        {
            PopulateTagData();
            SetPanePositions(PaneMode.TagEdit);
        }

        private void cmdAddEditTagCancel_Click(object sender, RoutedEventArgs e)
        {
            SetPanePositions(PaneMode.TagView);
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

            SetPanePositions(PaneMode.TagView);
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
                SetPanePositions(PaneMode.Upload);
            }
        }

        private void cmdUploadClear_Click(object sender, RoutedEventArgs e)
        {
            uploadFots.Clear();
            ResetUploadState(true);
            SetPanePositions(PaneMode.Upload);
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
            }
            else
            {
                chkUploadCameraAll.IsChecked = false;
                //Plus all the others.
            }
            //TODO = get list of all controls with a name starts chkUpload% and finishes %All
            //Loop through and set checked to false


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
                SetPanePositions(PaneMode.Upload);
            }
        }

        async private void cmdUploadResetMeta_Click(object sender, RoutedEventArgs e)
        {
            ResetUploadState(false);
            await controller.ResetMeFotsMeta(uploadFots);
            SetPanePositions(PaneMode.Upload);
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
            SetPanePositions(currentPane);
        }

        private void chkUploadToNewCategory_Unchecked(object sender, RoutedEventArgs e)
        {
            SetPanePositions(currentPane);
        }

        /*
        private void CheckForUploadComplete(object sender, TextCompositionEventArgs e)
        {
            if (meFots.Count == 0)
            {
                //Upload Complete.
                SetPanePositions(PaneMode.Upload);
            }
        }
        */

        async private void cmdUploadAll_Click(object sender, RoutedEventArgs e)
        {
            uploadUIState.Uploading = true;
            SetPanePositions(PaneMode.Upload);

            string response = await controller.DoUploadAsync(uploadFots, uploadUIState);
            if (response != null)
            {
                MessageBox.Show("The upload encountered an error.  Message: " + response);
            }

            //TODO - Remove Images which were successfully uploaded.
            uploadUIState.Uploading = false;
            //lstUploadImageFileList.Items.Clear();
            ResetUploadState(true);

            /*
            if (lstUploadImageFileList.Items.Count == 0)
            {
                
            }
            else
            {
                
            }
            */

            SetPanePositions(PaneMode.Upload);
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
            SetPanePositions(PaneMode.CategoryView);
        }

        private void cmdUpload_Checked(object sender, RoutedEventArgs e)
        {
            SetPanePositions(PaneMode.Upload);
        }

        private void cmdView_Checked(object sender, RoutedEventArgs e)
        {
            SetPanePositions(PaneMode.ViewView);
        }

        private void cmdTag_Checked(object sender, RoutedEventArgs e)
        {
            SetPanePositions(PaneMode.TagView);
        }

        private void cmdSettings_Checked(object sender, RoutedEventArgs e)
        {
            SetPanePositions(PaneMode.Settings);
        }
        #endregion

        private void lblUploadProposedImageCount_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var simon = this.lstUploadImageFileList;
        }

        async private void cmdUploadStatusRefresh_Click(object sender, RoutedEventArgs e)
        {
            uploadStatusList = await controller.GetUploadStatusList();

            //ImageUploadRef
            lstUploadStatusList.GetBindingExpression(ListBox.ItemsSourceProperty).UpdateTarget();
            lstUploadStatusItem.GetBindingExpression(ListBoxItem.ContentProperty).UpdateTarget();

            /*
            lstUploadStatusList.DisplayMemberPath = "name";
            Binding binding = new Binding("ImageUploadRef");
            binding.Mode = BindingMode.OneWay;
            binding.Source = uploadStatusList;

            BindingOperations.SetBinding(lstUploadStatusList, ListBox.ItemsSourceProperty, binding);
            */




            //lstUploadStatusList.GetBindingExpression(ListBoxItem.ContentProperty).UpdateTarget();

            //
            //lstUploadStatusList.GetBindingExpression(ListBox.DisplayMemberPathProperty).UpdateTarget();

            //BindingOperations.GetBindingExpressionBase(lstUploadStatusList, ListBox.ItemsSourceProperty).UpdateTarget();

            
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


    }
}
