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


namespace ManageWalla
{
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class MainWindow : Window
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
            ImageViewFull = 10,
            Settings = 11
        }

        private PaneMode currentPane;
        private Tag currentTag = null;

        private MainController controller = null;
        public UploadUIState uploadUIState = null;
        public UploadImageFileList meFots = null;

        //private UploadImageFileList meFots = new UploadImageFileList();

        public MainWindow()
        {
            InitializeComponent();

            meFots = (UploadImageFileList)FindResource("uploadImagefileListKey");
            uploadUIState = (UploadUIState)FindResource("uploadUIStateKey");
        }

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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Apply busy panes overlay


            //Kick off asyncronous data syncronising.
            //This will update all UI Elements eventually
            controller = new MainController(this);
            controller.RetrieveGeneralUserConfig();

            HideAllContent();

            
            //uploadUIState = new UploadUIState();

            //UploadImageFileList meFots = new UploadImageFileList();

            

            currentPane = PaneMode.CategoryView;
            this.cmdCategory.IsChecked = true;
        }

        public void RefreshCategoryTreeView()
        {

        }

        private void SetWindowHeights(PaneMode mode)
        {
            const double headingHeight = 46.0;
            double windowAdjustHeight = mainWindow.Height - 36.0;

            switch (mode)
            {
                case PaneMode.CategoryView:
                case PaneMode.CategoryAdd:
                case PaneMode.CategoryEdit:
                    wrapImages.Height = windowAdjustHeight - (headingHeight * 2.0);
                    stackCategory.Height = windowAdjustHeight - (headingHeight * 3.0);

                    break;
                case PaneMode.TagView:
                case PaneMode.TagAdd:
                case PaneMode.TagEdit:
                    wrapImages.Height = windowAdjustHeight - (headingHeight * 2.0);
                    stackTag.Height = windowAdjustHeight - (headingHeight * 3.0);
                    break;
                case PaneMode.ViewView:
                case PaneMode.ViewEdit:
                case PaneMode.ViewAdd:
                    wrapImages.Height = ((windowAdjustHeight - (headingHeight * 3.0)) / 2.0) + headingHeight;
                    stackView.Height = ((windowAdjustHeight - (headingHeight * 3.0)) / 2.0);
                    stackTag.Height = ((windowAdjustHeight - (headingHeight * 3.0)) / 2.0);
                    stackCategory.Height = ((windowAdjustHeight - (headingHeight * 3.0)) / 2.0);

                    break;
                case PaneMode.ImageViewFull:
                    wrapImages.Height = windowAdjustHeight;
                    break;
                case PaneMode.Settings:
                    stackSettings.Height = windowAdjustHeight - (headingHeight * 3.0);

                    break;

                case PaneMode.Upload:
                    stackUpload.Height = windowAdjustHeight - (headingHeight * 2.0);
                    stackTag.Height = ((windowAdjustHeight - (headingHeight * 3.0)) / 2.0);
                    stackCategory.Height = ((windowAdjustHeight - (headingHeight * 3.0)) / 2.0);
                    wrapImages.Height = 0.0;
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

                    //treeCategoryView.IsEnabled = true;
                    gridCatgeoryAddEdit.Visibility = Visibility.Collapsed;
                    stackCategory.Visibility = Visibility.Visible;

                    break;
                case PaneMode.CategoryAdd:
                    //treeCategoryView.IsEnabled = false;

                    gridCatgeoryAddEdit.Visibility = Visibility.Visible;
                    cmdAddEditCategoryMove.Visibility = Visibility.Collapsed;
                    cmdAddEditCategorySave.Content = "Save New";
                    cmdAddEditCategoryDelete.Visibility = Visibility.Collapsed;

                    break;
                case PaneMode.CategoryEdit:
                    //treeCategoryView.IsEnabled = false;

                    gridCatgeoryAddEdit.Visibility = Visibility.Visible;
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

                    gridTagView.IsEnabled = true;
                    gridTagAddEdit.Visibility = Visibility.Collapsed;
                    stackTag.Visibility = Visibility.Visible;

                    cmdAssociateTag.Visibility = Visibility.Collapsed;
                    cmdAddTag.Visibility = Visibility.Visible;
                    cmdEditEdit.Visibility = Visibility.Visible;

                    break;
                case PaneMode.TagAdd:

                    this.cmdAddEditTagSave.Content = "Save New";
                    gridTagAddEdit.Visibility = Visibility.Visible;
                    gridTagView.IsEnabled = false;

                    break;
                case PaneMode.TagEdit:
                    this.cmdAddEditTagSave.Content = "Save Edit";
                    gridTagAddEdit.Visibility = Visibility.Visible;
                    gridTagView.IsEnabled = false;

                    break;
                case PaneMode.ViewView:
                    cmdCategory.IsChecked = false;
                    cmdTag.IsChecked = false;
                    cmdSettings.IsChecked = false;
                    cmdUpload.IsChecked = false;

                    HideAllContent();
                    stackView.Visibility = Visibility.Visible;
                    stackTag.Visibility = Visibility.Visible;
                    stackCategory.Visibility = Visibility.Visible;

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

                    cmdAssociateTag.Visibility = Visibility.Visible;
                    cmdAddTag.Visibility = Visibility.Collapsed;
                    cmdEditEdit.Visibility = Visibility.Collapsed;


                    if (uploadUIState.Mode == UploadUIState.UploadMode.Images || uploadUIState.Mode == UploadUIState.UploadMode.Folder)
                    {
                        //Common
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

                        if (uploadUIState.Mode == UploadUIState.UploadMode.Images)
                        {
                            grdUploadImageDetails.RowDefinitions[0].MaxHeight = 0; //Sub category marker
                            grdUploadSettings.RowDefinitions[2].MaxHeight = 0; //Map to sub folders
                            cmdUploadImportFolder.Visibility = Visibility.Hidden;
                        }
                        else if (uploadUIState.Mode == UploadUIState.UploadMode.Folder)
                        {
                            if (uploadUIState.MapToSubFolders)
                            {
                                grdUploadImageDetails.RowDefinitions[0].MaxHeight = 25; //Sub category marker
                                grdUploadSettings.RowDefinitions[2].MaxHeight = 25; //Maintain sub folders.
                            }
                            else
                            {
                                grdUploadImageDetails.RowDefinitions[0].MaxHeight = 0; //Sub category marker
                                grdUploadSettings.RowDefinitions[2].MaxHeight = 0; //Map to sub folders
                            }
                            cmdUploadImportFiles.Visibility = Visibility.Hidden;
                        }

                    }
                    else
                    {



                        //Check if there are outstanding upload items then just allow progress or cancel.
                        if (lstUploadImageFileList.Items.Count > 0)
                        {
                            cmdUploadImportFolder.IsEnabled = false;
                            cmdUploadImportFiles.IsEnabled = false;
                            cmdUploadResetMeta.IsEnabled = false;
                            cmdUploadClear.IsEnabled = true;
                            cmdUploadClear.Content = "Cancel Uploads";
                        }
                        else
                        {
                            //New Upload
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
                    stackSettings.Visibility = Visibility.Visible;

                    break;
            }
            SetWindowHeights(mode);
            currentPane = mode;
        }

        private void HideAllContent()
        {
            stackCategory.Visibility = Visibility.Collapsed;
            stackTag.Visibility = Visibility.Collapsed;
            stackSettings.Visibility = Visibility.Collapsed;
            stackView.Visibility = Visibility.Collapsed;
            stackUpload.Visibility = Visibility.Collapsed;
        }

        /*
        public Image GetImageControl(string filePath)
        {
            BitmapImage myBitmapImage = new BitmapImage();
            myBitmapImage.BeginInit();
            myBitmapImage.DecodePixelWidth = 100;
            myBitmapImage.UriSource = new Uri(filePath);
            myBitmapImage.EndInit();
            myBitmapImage.Freeze();

            Image myImage = new Image();
            myImage.Source = myBitmapImage;
            myImage.Style = (Style)FindResource("styleImageThumb");
            return myImage;
        }
        */





        private void RefreshTagsList()
        {
            wrapMyTags.Children.Clear();

            TagList tagList = controller.GetTagsAvailable();
            if (tagList != null)
            {
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
            else
            {
                MessageBox.Show("There was an error trying to retreive the tags from the server.");
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

        private void CheckForUploadComplete(object sender, TextCompositionEventArgs e)
        {
            if (meFots.Count == 0)
            {
                //Upload Complete.
                SetPanePositions(PaneMode.Upload);
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

        private void mainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            controller.Dispose();
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

        private void cmdUploadImportFolder_Click(object sender, RoutedEventArgs e)
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
                        controller.LoadImagesFromFolder(folder, true, meFots);
                        uploadUIState.MapToSubFolders = true;
                    }
                    else
                    {
                        controller.LoadImagesFromFolder(folder, false, meFots);
                    }
                }
                else
                {
                    controller.LoadImagesFromFolder(folder, false, meFots);
                }

                uploadUIState.Mode = UploadUIState.UploadMode.Folder;
                SetPanePositions(PaneMode.Upload);
            }
        }

        private void cmdUploadClear_Click(object sender, RoutedEventArgs e)
        {
            meFots.Clear();
            ResetUploadState(true);
            SetPanePositions(PaneMode.Upload);
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

            //TODO = get list of all controls with a name starts chkUpload% and finishes %All
            //Loop through and set checked to false

            chkUploadCameraAll.IsChecked = false;
            //Plus all the others.
        }

        private void cmdUploadImportFiles_Click(object sender, RoutedEventArgs e)
        {
            var openDialog = new System.Windows.Forms.OpenFileDialog();
            openDialog.DefaultExt = @"*.JPG;*.BMP";
            openDialog.Multiselect = true;
            System.Windows.Forms.DialogResult result = openDialog.ShowDialog();
            if (openDialog.FileNames.Length > 0)
            {
                controller.LoadImagesFromArray(openDialog.FileNames, meFots);
                uploadUIState.GotSubFolders = false;
                uploadUIState.Mode = UploadUIState.UploadMode.Images;
                SetPanePositions(PaneMode.Upload);
            }
        }

        private void cmdUploadMulti_Click(object sender, RoutedEventArgs e)
        {
            //TODO Begin async, upload process.

            SetPanePositions(PaneMode.Upload);
        }

        private void cmdUploadResetMeta_Click(object sender, RoutedEventArgs e)
        {
            ResetUploadState(false);
            controller.ResetMeFotsMeta(meFots);
            SetPanePositions(PaneMode.Upload);
        }

        private void chkUploadToNewCategory_Checked(object sender, RoutedEventArgs e)
        {
            SetPanePositions(currentPane);
        }

        private void chkUploadToNewCategory_Unchecked(object sender, RoutedEventArgs e)
        {
            SetPanePositions(currentPane);
        }

        private void chkUploadCameraAll_Checked(object sender, RoutedEventArgs e)
        {
            Binding simon = BindingOperations.GetBinding(txtUploadCamera, TextBlock.TextProperty);
            BindingOperations.ClearBinding(txtUploadCamera, TextBlock.TextProperty);
        }

        private void chkUploadCameraAll_Unchecked(object sender, RoutedEventArgs e)
        {
            Binding binding = new Binding("Count");
            //binding.Source = meFots;
            //binding.Path = new PropertyPath("Count");
            binding.Mode = BindingMode.OneWay;

            BindingOperations.SetBinding(txtUploadCamera, TextBlock.TextProperty, binding);
            //txtUploadCamera.SetBinding(TextBlock.TextProperty, binding);
            BindingExpression simon = BindingOperations.GetBindingExpression(txtUploadCamera, TextBlock.TextProperty);
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
                    newTagRef.tagId = tagListTagRefTemp.id;
                    newTagRef.op = "C";
                    //newTagRef.Name = tagListTagRefTemp.name;

                    UploadImage current = (UploadImage)lstUploadImageFileList.SelectedItem;
                    ImageMetaTagRef[] newTagRefArray = new ImageMetaTagRef[current.Meta.TagRef.Length + 1];

                    current.Meta.TagRef.CopyTo(newTagRefArray, 0);
                    newTagRefArray[newTagRefArray.Length - 1] = newTagRef;

                    current.Meta.TagRef = newTagRefArray;
                }
            }
        }




    }
}
