using Cirrious.MvvmCross.Binding.BindingContext;
using Cirrious.MvvmCross.Touch.Views;
using MonoTouch.UIKit;
using Toggl.Core.ViewModels;

namespace Toggle.UI.Touch.Views
{
    public partial class WelcomeView : MvxViewController
    {
        public WelcomeView () : base ("WelcomeView", null)
        {
        }

        public override void ViewDidLoad ()
        {
            base.ViewDidLoad();

            this.CreateBinding(SigninButton).To((WelcomeViewModel vm) => vm.SigninCommand).Apply();
            this.CreateBinding(SignupButton).To((WelcomeViewModel vm) => vm.SignupCommand).Apply();

            View.AddGestureRecognizer(new UITapGestureRecognizer(() => SigninButton.ResignFirstResponder()));
        }
    }
}

