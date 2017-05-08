using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Commands;
using Prism.Modularity;
using Prism.Regions;
using St.Common;
using System.Windows.Controls;

namespace St.Host.Views
{
    /// <summary>
    ///     MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainView : INotifyPropertyChanged
    {
        private const string FirstModuleName = "CollaborativeInfoModule";

        private static readonly Uri FirstViewUri = new Uri(Common.GlobalResources.CollaborativeInfoContentView,
            UriKind.Relative);

        private readonly IRegionManager _regionManager;
        private readonly IGroupManager _groupManager;

        public MainView(IRegionManager regionManager, IModuleManager moduleManager)
        {
            InitializeComponent();

            DataContext = this;

            _regionManager = regionManager;
            _groupManager = DependencyResolver.Current.GetService<IGroupManager>();

            moduleManager.LoadModuleCompleted +=
                (s, e) =>
                {
                    if (e.ModuleInfo.ModuleName == FirstModuleName)
                        _regionManager.RequestNavigate(RegionNames.ContentRegion, FirstViewUri);
                };

            TopMostTriggerCommand = new DelegateCommand(TriggerTopMost);
            ShowLogCommand = DelegateCommand.FromAsyncHandler(ShowLogAsync);
        }

        private void TriggerTopMost()
        {
            Topmost = !Topmost;
        }

        private bool _isDialogOpen;

        public bool IsDialogOpen
        {
            get { return _isDialogOpen; }
            set
            {
                Topmost = !value;
                SetProperty(ref _isDialogOpen, value);
            }
        }

        private string _dialogContent;

        public string DialogContent
        {
            get { return _dialogContent; }
            set
            {
                SetProperty(ref _dialogContent, value);
            }
        }

        public ICommand LoadCommand { get; set; }
        public ICommand TopMostTriggerCommand { get; set; }
        public ICommand ShowLogCommand { get; set; }

        private async Task ShowLogAsync()
        {
            await LogManager.ShowLogAsync();
        }

        #region Infrastructure Code

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);

            return true;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }



        #endregion

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listbox = sender as ListBox;
            object selectedObj = listbox.SelectedItem;
            string navViewName = selectedObj.GetType().Name;

            switch (navViewName)
            {
                case Common.GlobalResources.SettingNavView:
                    navViewName = Common.GlobalResources.SettingContentView;
                    break;
                case Common.GlobalResources.ProfileNavView:
                    navViewName = Common.GlobalResources.ProfileContentView;
                    break;
                case Common.GlobalResources.InstantMeetingNavView:
                    navViewName = Common.GlobalResources.InstantMeetingContentView;
                    break;
                case Common.GlobalResources.InteractiveNavView:
                    navViewName = Common.GlobalResources.InteractiveContentView;
                    break;
                case Common.GlobalResources.InteractiveWithouLiveNavView:
                    navViewName = Common.GlobalResources.InteractiveWithouLiveContentView;
                    break;
                case Common.GlobalResources.CollaborativeInfoNavView:
                    navViewName = Common.GlobalResources.CollaborativeInfoContentView;
                    break;
                case Common.GlobalResources.DiscussionNavView:
                    navViewName = Common.GlobalResources.DiscussionContentView;
                    break;
            }

            _regionManager.RequestNavigate(RegionNames.ContentRegion, new Uri(navViewName, UriKind.Relative),
                NavigationCallback);
        }

        private void NavigationCallback(NavigationResult navigationResult)
        {
            _groupManager.GotoNewView(navigationResult.Context.Uri.OriginalString);
        }
    }
}