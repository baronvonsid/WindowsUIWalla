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
    public class WallaDateConverter1 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null)
            {
                string formatString = parameter.ToString();

                
                string newValue = value.ToString();

                if (!String.IsNullOrEmpty(formatString)  && !String.IsNullOrEmpty(newValue))
                {
                    DateTime tempDate = XmlConvert.ToDateTime(newValue);
                    return String.Format(culture, formatString, tempDate);
                }

            }
            return value.ToString();
        }

        //Is this correct ?
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;

        }

        /*
        //Alternative method
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            TypeConverter typeConverter = TypeDescriptor.GetConverter(targetType);

            if (typeConverter.CanConvertFrom(value.GetType()))
            {
                return typeConverter.ConvertFrom(value);
            }

            return null;
        }
         * */
        
    }
}
