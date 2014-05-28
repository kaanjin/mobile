using MonoTouch.UIKit;

namespace Toggle.UI.Touch
{
    public class LinkerPleaseInclude
    {
        public void IncludeTextSet()
        {
            var u = new UITextField();
            u.Text = u.Text + "h";

            var l = new UILabel();
            l.Text = l.Text + "h";
        }
    }
}