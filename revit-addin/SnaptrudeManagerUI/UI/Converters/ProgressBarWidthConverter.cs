using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace SnaptrudeManagerUI.Converters
{
    public class ProgressBarWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 4 && values.All(v => v is double))
            {
                double value = (double)values[0];
                double minimum = (double)values[1];
                double maximum = (double)values[2];
                double actualWidth = (double)values[3];

                if (maximum <= minimum)
                    return 0.0;

                return (value - minimum) / (maximum - minimum) * actualWidth;
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
