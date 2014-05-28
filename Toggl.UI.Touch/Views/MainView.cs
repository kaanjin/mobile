using Cirrious.MvvmCross.Touch.Views;
using Toggl.Core.ViewModels;

namespace Toggle.UI.Touch.Views
{
    public partial class MainView : MvxViewController
    {
        public MainView() : base("MainView", null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            ((MainViewModel)(ViewModel)).GoNextCommand.Execute(null);
        }
    }
}