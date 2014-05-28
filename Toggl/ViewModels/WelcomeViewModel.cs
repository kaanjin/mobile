using System.Windows.Input;
using Cirrious.MvvmCross.ViewModels;

namespace Toggl.Core.ViewModels
{
    public class WelcomeViewModel : MvxViewModel
    {
        public ICommand SignupCommand
        {
            get
            {
                return new MvxCommand(() => ShowViewModel<SignupViewModel>());
            }
        }

        public ICommand SigninCommand
        {
            get
            {
                return new MvxCommand(() => ShowViewModel<SigninViewModel>());
            }
        }
    }
}