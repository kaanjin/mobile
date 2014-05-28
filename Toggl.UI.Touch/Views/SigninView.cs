using Cirrious.MvvmCross.Binding.BindingContext;
using Cirrious.MvvmCross.Touch.Views;
using MonoTouch.UIKit;
using Toggl.Core.ViewModels;
using Toggle.UI.Touch.Converters;

namespace Toggle.UI.Touch.Views
{
    public partial class SigninView : MvxViewController
    {
        public SigninView()
            : base("SigninView", null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.CreateBinding(EmailTextField).To((SigninViewModel vm) => vm.Email).Apply();
            this.CreateBinding(PasswordTextField).To((SigninViewModel vm) => vm.Password).Apply();
            this.CreateBinding(SigninButton).To((SigninViewModel vm) => vm.SigninCommand).Apply();

            this.CreateBinding(SigninButton).To((SigninViewModel vm) => vm.IsSigningin).OneWay().WithConversion(new BoolToEnabledValueConverter(SigninButton), "reverse").Apply();

            View.AddGestureRecognizer(new UITapGestureRecognizer(() => EmailTextField.ResignFirstResponder()));
            View.AddGestureRecognizer(new UITapGestureRecognizer(() => PasswordTextField.ResignFirstResponder()));
        }
    }
}