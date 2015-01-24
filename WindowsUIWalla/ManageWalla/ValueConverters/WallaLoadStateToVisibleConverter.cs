using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Globalization;
using System.ComponentModel;
using System.Xml;

namespace ManageWalla
{
    [ValueConversion(typeof(GeneralImage.LoadState), typeof(Visibility))]
    public class WallaLoadStateToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //elementType = image, loading, unavi
            GeneralImage.LoadState loadState = (GeneralImage.LoadState)value;
            string elementTypeParam = (string)parameter;
            GeneralImage.LoadState elementType = (GeneralImage.LoadState)int.Parse(elementTypeParam);

            //Cheat so notloaded acts the same as requested.
            if (loadState == elementType || (loadState == GeneralImage.LoadState.NotLoaded && elementType == GeneralImage.LoadState.Requested))
            {
                return Visibility.Visible;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;

        }
    }
}