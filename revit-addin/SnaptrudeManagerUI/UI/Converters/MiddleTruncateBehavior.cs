using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace SnaptrudeManagerUI.UI.Converters
{
    public class MiddleTruncateBehavior : DependencyObject
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached("Text", typeof(string), typeof(MiddleTruncateBehavior), new PropertyMetadata(null, OnTextChanged));

        public static string GetText(DependencyObject obj)
        {
            return (string)obj.GetValue(TextProperty);
        }

        public static void SetText(DependencyObject obj, string value)
        {
            obj.SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TextBlock textBlock = d as TextBlock;
            if (textBlock != null)
            {
                AdjustText(textBlock);
                textBlock.Loaded += (s, args) => AdjustText(textBlock);
                textBlock.SizeChanged += (s, args) => AdjustText(textBlock);
            }
        }

        private static void AdjustText(TextBlock textBlock)
        {
            string text = GetText(textBlock);
            if (string.IsNullOrEmpty(text))
                return;

            double maxWidth = textBlock.ActualWidth;
            if (maxWidth == 0) return;
            Typeface typeface = new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch);
            FormattedText formattedText = new FormattedText(text, System.Globalization.CultureInfo.CurrentCulture, textBlock.FlowDirection, typeface, textBlock.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);

            if (formattedText.Width <= maxWidth)
            {
                textBlock.Text = text;
                return;
            }

            int maxLength = text.Length;
            while (maxLength > 0)
            {
                int charsToShow = maxLength - 3 - 4; // 3 dots
                int frontChars = charsToShow / 2;
                int backChars = charsToShow - frontChars + 4;
                string truncatedText = $"{text.Substring(0, frontChars)} ... {text.Substring(text.Length - backChars)}";

                FormattedText truncatedFormattedText = new FormattedText(truncatedText, System.Globalization.CultureInfo.CurrentCulture, textBlock.FlowDirection, typeface, textBlock.FontSize, Brushes.Black, VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);

                if (truncatedFormattedText.Width <= maxWidth)
                {
                    textBlock.Text = truncatedText;
                    break;
                }

                maxLength--;
            }
        }
    }
}
