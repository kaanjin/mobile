// WARNING
//
// This file has been generated automatically by MonoDevelop to store outlets and
// actions made in the Xcode designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//

using MonoTouch.Foundation;

namespace Toggle.UI.Touch.Views
{
    [Register("SignupView")]
	partial class SignupView
	{
        [Outlet]
        MonoTouch.UIKit.UITextField EmailTextField { get; set; }

        [Outlet]
        MonoTouch.UIKit.UITextField PasswordTextField { get; set; }

        [Outlet]
        MonoTouch.UIKit.UIButton SignupButton { get; set; }

		void ReleaseDesignerOutlets ()
		{
            if (EmailTextField != null)
            {
                EmailTextField.Dispose();
                EmailTextField = null;
            }

            if (PasswordTextField != null)
            {
                PasswordTextField.Dispose();
                PasswordTextField = null;
            }

            if (SignupButton != null)
            {
                SignupButton.Dispose();
                SignupButton = null;
            }
		}
	}
}
