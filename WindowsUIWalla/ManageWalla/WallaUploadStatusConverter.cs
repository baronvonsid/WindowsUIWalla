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
            /*	private enum ImageStatus {Pending 0, Queued 1, Processing 2, Complete 3, Error 4, Inactive 5	}	*/
            switch (value.ToString())
            {
                case "-1":
                    return "Error in app";
                case "0":
                    return "Received";
                case "1":
                    return "Queued on Server";
                case "2":
                    return "Processing on Server";
                case "3":
                    return "Complete";
                case "4":
                    return "Error on Server";
                case "5":
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