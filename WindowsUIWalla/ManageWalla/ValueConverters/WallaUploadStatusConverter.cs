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
    public class WallaUploadStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            /*	private enum ImageStatus {None 0, File received 1, Awaiting processing 2, Being processed 3, Complete 4, Inactive 5}	*/
            switch (value.ToString())
            {
                case "None":
                    return "Not processed";
                case "FileReceived":
                    return "File received";
                case "AwaitingProcessed":
                    return "Awaiting processing";
                case "BeingProcessed":
                    return "Being processed";
                case "Complete":
                    return "Complete";
                case "Inactive":
                    return "Inactive";
            }
            return "";
        }

        //Is this correct ?
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;

        }
    }
}