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
    public class WallaCacheSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double current = (double)value;
            if (current == 0.0)
            {
                return "Off";
            }
            else if (current == 500.0)
            {
                return "Unlimited";
            }
            else
            {
                return Math.Floor(current).ToString() + "MB";
            }
        }

        //Is this correct ?
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;

        }
    }
}