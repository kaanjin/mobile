using System.Windows.Input;
using Cirrious.MvvmCross.ViewModels;
using Toggl.Core.Services;

namespace Toggl.Core.ViewModels
{
    public class SigninViewModel : MvxViewModel
    {
        private readonly ITogglRestService _togglRestService;

        public SigninViewModel(ITogglRestService togglRestService)
        {
            _togglRestService = togglRestService;
        }

        private string _email;

        public string Email
        {
            get { return _email; }
            set
            {
                _email = value;
                RaisePropertyChanged(() => Email);
            }
        }

        private string _password;

        public string Password
        {
            get { return _password; }
            set
            {
                _password = value;
                RaisePropertyChanged(() => Password);
            }
        }

        private bool _isSigningin;

        public bool IsSigningin
        {
            get { return _isSigningin; } 
            set
            {
                _isSigningin = value; RaisePropertyChanged(() => IsSigningin);
            }
        }

        public ICommand SigninCommand
        {
            get
            {
                return new MvxCommand(() =>
                {
                    IsSigningin = true;
                    var waiter = _togglRestService.Signin(Email, Password).GetAwaiter();
                    waiter.OnCompleted(() =>
                    {
                        IsSigningin = false;

                        if (waiter.GetResult())
                            ShowViewModel<LogViewModel>();
                    });
                });
            }
        }
    }
}