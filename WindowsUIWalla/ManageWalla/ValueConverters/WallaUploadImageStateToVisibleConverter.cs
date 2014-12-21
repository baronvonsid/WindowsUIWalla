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
    [ValueConversion(typeof(UploadImage.ImageViewState), typeof(Visibility))]
    public class WallaUploadImageStateToVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //elementType = image, loading, unavi
            UploadImage.ImageViewState loadState = (UploadImage.ImageViewState)value;
            string elementTypeParam = (string)parameter;
            UploadImage.ImageViewState elementType = (UploadImage.ImageViewState)int.Parse(elementTypeParam);

            //GeneralImage.LoadState elementType = (GeneralImage.LoadState)int.Parse(elementTypeStr);
            if (loadState == elementType)
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