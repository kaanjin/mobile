using System.Windows.Input;
using Cirrious.MvvmCross.ViewModels;
using Toggl.Core.Services;

namespace Toggl.Core.ViewModels
{
    public class SignupViewModel : MvxViewModel
    {
        private readonly ITogglRestService _togglRestService;

        public SignupViewModel(ITogglRestService togglRestService)
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

        private bool _isSigningup;

        public bool IsSigningup
        {
            get { return _isSigningup; } 
            set
            {
                _isSigningup = value; RaisePropertyChanged(() => IsSigningup);
            }
        }

        public ICommand SignupCommand
        {
            get
            {
                return new MvxCommand(() =>
                {
                    IsSigningup = true;
                    var waiter = _togglRestService.Signup(Email, Password).GetAwaiter();
                    waiter.OnCompleted(() =>
                    {
                        IsSigningup = false;

                        if (waiter.GetResult())
                            ShowViewModel<LogViewModel>();
                    });
                });
            }
        }
    }
}