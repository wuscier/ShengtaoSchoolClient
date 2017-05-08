using System.Windows;
using System;

namespace St.Meeting
{
    /// <summary>
    /// MeetingView.xaml 的交互逻辑
    /// </summary>
    public partial class MeetingView
    {
        public MeetingView(Action<bool, string> startMeetingCallback, Action<bool, string> exitMeetingCallback)
        {
            InitializeComponent();

            //Rect rect = SystemParameters.WorkArea;
            //Left = 0;
            //Top = 0;
            //Width = rect.Width;
            //Height = rect.Height;

            MeetingViewModel mvm = new MeetingViewModel(this, startMeetingCallback, exitMeetingCallback);
            LayoutMenu.SubmenuOpened += mvm.RefreshLayoutMenu;
            DataContext = mvm;
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Window win = sender as Window;
            win.DragMove();
        }
    }
}
