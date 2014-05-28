using Cirrious.CrossCore.Converters;
using MonoTouch.UIKit;

namespace Toggle.UI.Touch.Converters
{
    public class BoolToEnabledValueConverter : MvxValueConverter<bool>
    {
        private readonly UIControl _control;

        public BoolToEnabledValueConverter()
        {
            
        }

        public BoolToEnabledValueConverter(UIControl control)
        {
            _control = control;
        }

        protected override object Convert(bool value, System.Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return _control.Enabled = "reverse" == parameter as string ? !value : value;
        }
    }
}