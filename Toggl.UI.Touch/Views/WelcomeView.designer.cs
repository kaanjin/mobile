// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//

using MonoTouch.Foundation;

namespace Toggle.UI.Touch.Views
{
    [Register ("WelcomeView")]
    partial class WelcomeView
    {
        [Outlet]
        MonoTouch.UIKit.UIButton SigninButton { get; set; }

        [Outlet]
        MonoTouch.UIKit.UIButton SignupButton { get; set; }

        void ReleaseDesignerOutlets()
        {
            if (SigninButton != null)
            {
                SigninButton.Dispose();
                SigninButton = null;
            }

            if (SignupButton != null)
            {
                SignupButton.Dispose();
                SignupButton = null;
            }
        }
    }
}
