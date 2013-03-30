using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;
using System.Globalization;

namespace ManageWalla
{
    public class HeightCalculator : IValueConverter
    {


        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double initialWindowHeight = (double)value;

            switch ((string)parameter)
            {
                case "imagePane":

                    initialWindowHeight = (double)100;

                    break;
                case "viewPane":

                    initialWindowHeight = (double)300;

                    break;
                case "uploadPane":

                    initialWindowHeight = (double)100;

                    break;
            }

            
            return initialWindowHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;

        }
    }
}
