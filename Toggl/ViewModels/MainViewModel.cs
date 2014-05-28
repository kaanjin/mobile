using System.Reflection;
using System.Windows.Input;
using Cirrious.CrossCore.IoC;
using Cirrious.MvvmCross.ViewModels;
using Toggl.Core.Services;
using System.Linq;

namespace Toggl.Core.ViewModels
{
    public class MainViewModel : MvxViewModel
    {
        private readonly ILocalStorageService _storageService;

        public MainViewModel(ILocalStorageService storageService)
        {
            _storageService = storageService;
        }

        public ICommand GoNextCommand
        {
            get
            {
                var authToken = _storageService.Get<string>("auth_token");

                if (string.IsNullOrEmpty(authToken))
                    return new MvxCommand(() => ShowViewModel<WelcomeViewModel>());
                
                var recentVm = _storageService.Get<string>("recent_vm");

                if (string.IsNullOrEmpty(recentVm))
                    return new MvxCommand(() => ShowViewModel<LogViewModel>());

                var vm = Assembly.GetExecutingAssembly().CreatableTypes().Inherits<MvxViewModel>().EndingWith(recentVm).FirstOrDefault();

                if (vm != null)
                    return new MvxCommand(() => ShowViewModel(vm));

                return new MvxCommand(() => ShowViewModel<LogViewModel>());
            }
        }
    }
}
