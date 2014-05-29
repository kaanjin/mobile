using System;
using Cirrious.CrossCore.Converters;

namespace Toggle.UI.Touch.Converters
{
    public class ActionValueConverter : MvxValueConverter
    {
        private readonly Action _action;

        public ActionValueConverter(Action action)
        {
            _action = action;
        }

        public override object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            _action();
            return null;
        }
    }
}