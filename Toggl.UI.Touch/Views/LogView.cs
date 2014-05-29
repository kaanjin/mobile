using System.Drawing;
using Cirrious.MvvmCross.Binding.BindingContext;
using MonoTouch.UIKit;
using Toggl.Core.ViewModels;
using Toggle.UI.Touch.Controls;
using Toggle.UI.Touch.Converters;

namespace Toggle.UI.Touch.Views
{
    public partial class LogView : RevealController
    {
        public LogView() : base()
        {
            _frontViewController = FrontViewController;
            _leftViewController = LeftViewController;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            NavigationController.SetNavigationBarHidden(true, true);           
        }

        UIViewController FrontViewController
        {
            get
            {
                var frontViewController = new UIViewController();
                frontViewController.View.BackgroundColor = UIColor.White;

                var revealButton = new UIButton(new RectangleF(20.0f, 20.0f, 30.0f, 30.0f));
                revealButton.SetTitle("...", UIControlState.Normal);
                revealButton.SetTitleColor(UIColor.Black, UIControlState.Normal);

                var set = this.CreateBindingSet<LogView, LogViewModel>();
                set.Bind(revealButton).To(vm => vm.Toggle);
                set.Bind(revealButton).To(vm => vm.IsMenuShown).WithConversion(new ActionValueConverter(() => AnimateToState(RevealControllerState.ShowsLeftViewController, null)), null);
                set.Apply();

                var frontNavigationController = new UINavigationController(frontViewController);
                frontNavigationController.View.AddSubview(revealButton);
                return frontNavigationController;
            }
        }

        UIViewController LeftViewController
        {
            get
            {
                var leftViewController = new UITableViewController();
                return leftViewController;
            }
        }
    }
}