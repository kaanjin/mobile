using Cirrious.MvvmCross.Binding.BindingContext;
using Cirrious.MvvmCross.Touch.Views;
using MonoTouch.UIKit;
using Toggl.Core.ViewModels;
using Toggle.UI.Touch.Converters;

namespace Toggle.UI.Touch.Views
{
    public partial class SignupView : MvxViewController
    {
        public SignupView()
            : base("SignupView", null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.CreateBinding(EmailTextField).To((SignupViewModel vm) => vm.Email).Apply();
            this.CreateBinding(PasswordTextField).To((SignupViewModel vm) => vm.Password).Apply();
            this.CreateBinding(SignupButton).To((SignupViewModel vm) => vm.SignupCommand).Apply();

            this.CreateBinding(SignupButton).To((SignupViewModel vm) => vm.IsSigningup).OneWay().WithConversion(new BoolToEnabledValueConverter(SignupButton), "reverse").Apply();

            View.AddGestureRecognizer(new UITapGestureRecognizer(() => EmailTextField.ResignFirstResponder()));
            View.AddGestureRecognizer(new UITapGestureRecognizer(() => PasswordTextField.ResignFirstResponder()));
        }
    }
}