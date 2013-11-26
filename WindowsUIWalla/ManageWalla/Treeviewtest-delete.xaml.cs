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
    /// Interaction logic for Treeviewtest_delete.xaml
    /// </summary>
    public partial class Treeviewtest_delete : Window
    {
        private void GetSelectionsButton_OnClick(object sender, RoutedEventArgs e)
        {
            
            var selectedMesg = "";
            var selectedItems = multiSelectTreeView.SelectedItems;

            if (selectedItems.Count > 0)
            {
                selectedMesg = selectedItems.Cast<FoodItem>()
                    .Where(modelItem => modelItem != null)
                    .Aggregate(selectedMesg, (current, modelItem) => current + modelItem.Name + Environment.NewLine);
            }
            else
                selectedMesg = "No selected items!";

            MessageBox.Show(selectedMesg, "MultiSelect TreeView Demo", MessageBoxButton.OK);
        }
    }
}
