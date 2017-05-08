using System.Threading.Tasks;
using System.Windows;
using Serilog;
using St.Common;
using St.Host.Views;
using St.Meeting;
using St.Common.Contract;
using System;

namespace St.Host
{
    public class VisualizeShellService : IVisualizeShell
    {
        private readonly UserInfo _userInfo;
        private readonly ISdk _sdkService;
        private readonly MainView _shellView;
        private readonly IRtClientService _rtClientService;

        public VisualizeShellService()
        {
            _userInfo = DependencyResolver.Current.GetService<UserInfo>();
            _sdkService = DependencyResolver.Current.GetService<ISdk>();
            _shellView = DependencyResolver.Current.GetService<MainView>();
            _rtClientService = DependencyResolver.Current.GetService<IRtClientService>();
        }


        public void FinishStartingSdk(bool succeeded, string msg)
        {
            _shellView.DialogContent = msg;

            if (succeeded)
            {
                _shellView.IsDialogOpen = false;
                //shell.DialogContent = string.Empty;
            }
        }

        public void HideShell()
        {
            if (_shellView != null)
            {
                _shellView.IsDialogOpen = false;
                _shellView.DialogContent = string.Empty;
                _shellView.Visibility = Visibility.Collapsed;
            }
        }

        public async Task Logout()
        {
            TimerManager.Instance.StopTimer();
            if (GlobalData.Instance.Device.EnableLogin)
            {
                SscDialog dialog = new SscDialog(Messages.WarningYouAreSignedOut);
                dialog.ShowDialog();
                Application.Current.Shutdown();
            }
            else
            {
                Log.Logger.Debug($"【rt server connected】：{_rtClientService.IsConnected()}");
                Log.Logger.Debug($"【stop rt server begins】：");
                _rtClientService.Stop();
                Log.Logger.Debug($"【rt server connected】：{_rtClientService.IsConnected()}");

                _userInfo.IsLogouted = true;

                foreach (Window currentWindow in Application.Current.Windows)
                {
                    if (currentWindow is LoginView)
                    {
                        Log.Logger.Debug("【already in login view, do nothing】");
                        return;
                    }

                    if (currentWindow is MeetingView)
                    {
                        Log.Logger.Debug("【in meeting view, exit meeting】");
                        IExitMeeting exitMeetingService = currentWindow.DataContext as IExitMeeting;
                        if (exitMeetingService != null) await exitMeetingService.ExitAsync();
                    }
                }

                Log.Logger.Debug("【in main view】");
                HideShell();

                LoginView loginView = DependencyResolver.Current.GetService<LoginView>();
                loginView.Show();

                _sdkService.Stop();
                _sdkService.SetMeetingAgentStatus(false);
            }
        }

        public void ShowShell()
        {
            _shellView.Visibility = Visibility.Visible;
        }

        public void StartingSdk()
        {
            _shellView.IsDialogOpen = true;
            _shellView.DialogContent = Messages.InfoStartingMeetingSdk;
        }

        public void SetSelectedMenu(string menuName)
        {
            foreach (var item in _shellView.ListBoxMenu.Items)
            {
                if (item.GetType().Name == menuName)
                {
                    _shellView.ListBoxMenu.SelectedItem = item;
                }
            }
        }

        public void SetTopMost(bool isTopMost)
        {
            _shellView.Topmost = isTopMost;
        }
    }
}
