using System.Windows.Input;
using Cirrious.MvvmCross.ViewModels;

namespace Toggl.Core.ViewModels
{
    public class LogViewModel : MvxViewModel
    {
        public ICommand Toggle
        {
            get { return new MvxCommand(() => IsMenuShown = !IsMenuShown); }
        }

        private bool _isMenuShown;

        public bool IsMenuShown
        {
            get { return _isMenuShown; }
            set { _isMenuShown = value; RaisePropertyChanged(() => IsMenuShown); }
        }
    }
}
