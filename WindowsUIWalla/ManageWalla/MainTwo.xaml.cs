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
    /// Interaction logic for MainTwo.xaml
    /// </summary>
    public partial class MainTwo : Window
    {
        private PaneMode currentPane;

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
            Settings = 11
        }

        public MainTwo()
        {
            InitializeComponent();
        }

        #region Pane Control Events
        private void cmdCategory_Checked(object sender, RoutedEventArgs e)
        {
            cmdTag.IsChecked = false;
            cmdUpload.IsChecked = false;
            cmdView.IsChecked = false;

            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Visible;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdViewRefresh.Visibility = System.Windows.Visibility.Hidden;


            RefreshOverallPanesStructure(PaneMode.CategoryView);
        }

        private void cmdUpload_Checked(object sender, RoutedEventArgs e)
        {
            cmdTag.IsChecked = false;
            cmdCategory.IsChecked = false;
            cmdView.IsChecked = false;

            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdViewRefresh.Visibility = System.Windows.Visibility.Hidden;

            RefreshOverallPanesStructure(PaneMode.Upload);
        }

        private void cmdView_Checked(object sender, RoutedEventArgs e)
        {
            cmdTag.IsChecked = false;
            cmdUpload.IsChecked = false;
            cmdCategory.IsChecked = false;

            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdViewRefresh.Visibility = System.Windows.Visibility.Visible;

            RefreshOverallPanesStructure(PaneMode.ViewView);
        }

        private void cmdTag_Checked(object sender, RoutedEventArgs e)
        {
            cmdCategory.IsChecked = false;
            cmdUpload.IsChecked = false;
            cmdView.IsChecked = false;

            cmdCategoryRefresh.Visibility = System.Windows.Visibility.Hidden;
            cmdTagRefresh.Visibility = System.Windows.Visibility.Visible;
            cmdViewRefresh.Visibility = System.Windows.Visibility.Hidden;

            RefreshOverallPanesStructure(PaneMode.TagView);
        }

        private void cmdSettings_Checked(object sender, RoutedEventArgs e)
        {
            cmdCategory.IsChecked = false;
            cmdTag.IsChecked = false;
            cmdUpload.IsChecked = false;
            cmdView.IsChecked = false;

            RefreshOverallPanesStructure(PaneMode.Settings);
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
            gridLeft.ColumnDefinitions[1].Width = new GridLength(250);
            gridRight.RowDefinitions[0].Height = new GridLength(40);
        }

        private void RefreshOverallPanesStructure(PaneMode mode)
        {
            //Ensure panes are all correctly setup each time a refresh is called.
            gridLeft.ColumnDefinitions[0].Width = new GridLength(0); //Sidebar
            gridLeft.ColumnDefinitions[1].Width = new GridLength(250); //Main control
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
            }
        }
        #endregion

        private void ShowHideFilterSort()
        {
            if (cmdFilter.IsChecked == true)
            {
                gridRight.ColumnDefinitions[1].Width = new GridLength(250);
                panFilter.Visibility = System.Windows.Visibility.Visible;
                panSort.Visibility = System.Windows.Visibility.Collapsed;
                //cmdSort.IsChecked = false;
            }
            else if (cmdSort.IsChecked == true)
            {
                gridRight.ColumnDefinitions[1].Width = new GridLength(250);
                panSort.Visibility = System.Windows.Visibility.Visible;
                panFilter.Visibility = System.Windows.Visibility.Collapsed;
                //cmdFilter.IsChecked = false;
            }
            else
            {
                gridRight.ColumnDefinitions[1].Width = new GridLength(0);
            }
        }


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



    }
}
