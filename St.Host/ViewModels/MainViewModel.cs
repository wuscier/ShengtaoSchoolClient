using Serilog;
using St.Common;
using St.Common.Contract;

namespace St.Host.ViewModels
{
    public class MainViewModel : ISignOutHandler
    {
        private readonly IVisualizeShell _visualizeShellService;

        public MainViewModel()
        {
            _visualizeShellService = DependencyResolver.Current.GetService<IVisualizeShell>();
        }

        public void SignOut()
        {
            Log.Logger.Debug("【log out, for same account logged in】");
            _visualizeShellService.Logout();
        }
    }
}