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




        private MainController controller = null;


        public MainWindow()
        {
            InitializeComponent();
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
            controller = new MainController();
            controller.RetrieveGeneralUserConfig();

            HideAllContent();
            HideBusyPanes();

            this.cmdCategory.IsChecked = true;
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
                    stackCategory.Visibility = Visibility.Visible;

                    break;
                case PaneMode.CategoryEdit:

                    break;
                case PaneMode.TagView:
                    cmdCategory.IsChecked = false;
                    cmdView.IsChecked = false;
                    cmdSettings.IsChecked = false;
                    cmdUpload.IsChecked = false;

                    HideAllContent();
                    stackTag.Visibility = Visibility.Visible;

                    break;
                case PaneMode.TagEdit:

                    break;
                case PaneMode.ViewView:
                    cmdCategory.IsChecked = false;
                    cmdTag.IsChecked = false;
                    cmdSettings.IsChecked = false;
                    cmdUpload.IsChecked = false;

                    HideAllContent();
                    stackView.Visibility = Visibility.Visible;
                
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

        }

        private void HideAllContent()
        {
            stackCategory.Visibility = Visibility.Collapsed;
            stackTag.Visibility = Visibility.Collapsed;
            stackSettings.Visibility = Visibility.Collapsed;
            stackView.Visibility = Visibility.Collapsed;
            stackUpload.Visibility = Visibility.Collapsed;
        }

        private void ShowBusyPanes()
        {
            rectangleCategoryBusy.Visibility = Visibility.Visible;
            rectangleTagBusy.Visibility = Visibility.Visible;
            rectangleSettingsBusy.Visibility = Visibility.Visible;
            rectangleViewBusy.Visibility = Visibility.Visible;
            rectangleUploadBusy.Visibility = Visibility.Visible;
        }

        private void HideBusyPanes()
        {
            rectangleCategoryBusy.Visibility = Visibility.Collapsed;
            rectangleTagBusy.Visibility = Visibility.Collapsed;
            rectangleSettingsBusy.Visibility = Visibility.Collapsed;
            rectangleViewBusy.Visibility = Visibility.Collapsed;
            rectangleUploadBusy.Visibility = Visibility.Collapsed;
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


    }
}
