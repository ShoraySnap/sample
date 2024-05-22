using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SnaptrudeManagerUI.Converters
{
    public class WidthToOffsetConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is double buttonWidth && values[1] is double minPopupWidth)
            {
                // If the button is smaller than the popup's minimum width, align to the right
                if (buttonWidth < minPopupWidth)
                {
                    return buttonWidth - minPopupWidth;
                }
                // Otherwise, no offset
                return 0.0;
            }
            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
