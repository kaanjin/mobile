using System;
using MonoTouch.UIKit;

namespace Toggl.Ross.ViewControllers
{
    /// <summary>
    /// Base navigation controller reimplements the UINavigationController for better NavigationBar customizability
    /// (height and transition animations).
    /// </summary>
    public class StackController : UIViewController
    {
        public StackController ()
        {
        }

        public StackController (IntPtr handle) : base (handle)
        {
        }

        public void SetNavigationBarHidden (bool hidden, bool animated)
        {
            throw new NotImplementedException ();
        }

        public void SetViewControllers (UIViewController[] controllers, bool animated)
        {
            throw new NotImplementedException ();
        }

        public UIViewController[] ViewControllers {
            get { throw new NotImplementedException (); }
            set { SetViewControllers (value, false); }
        }

        public void PushViewController (UIViewController controller, bool animated)
        {
            throw new NotImplementedException ();
        }

        public void PopViewController (bool animated)
        {
            throw new NotImplementedException ();
        }

        public void PopToViewController (UIViewController controller, bool animated)
        {
            throw new NotImplementedException ();
        }

        public void PopToRootViewController (bool animated)
        {
            throw new NotImplementedException ();
        }

        public UIView NavigationBar {
            get { throw new NotImplementedException (); }
        }
    }
}
