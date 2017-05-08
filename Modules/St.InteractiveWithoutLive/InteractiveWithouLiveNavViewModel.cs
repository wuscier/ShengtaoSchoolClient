using Prism.Commands;
using Prism.Regions;
using St.Common;
using System;
using System.Windows.Input;

namespace St.InteractiveWithouLive
{
    public class InteractiveWithouLiveNavViewModel
    {
        public InteractiveWithouLiveNavViewModel()
        {
            _regionManager = DependencyResolver.Current.GetService<IRegionManager>();
            _groupManager = DependencyResolver.Current.GetService<IGroupManager>();
            NavToContentCommand = new DelegateCommand(NavToContentHandler);
        }

        //private fields
        private readonly IGroupManager _groupManager;
        private readonly IRegionManager _regionManager;
		
        private static readonly Uri ContentViewUri = new Uri(GlobalResources.InteractiveWithouLiveContentView, UriKind.Relative);

        //commands
        public ICommand NavToContentCommand { get; set; }

        private void NavToContentHandler()
        {
            _regionManager.RequestNavigate(RegionNames.ContentRegion, ContentViewUri, NavigationCallback);
        }

        private void NavigationCallback(NavigationResult navigationResult)
        {
            _groupManager.GotoNewView(navigationResult.Context.Uri.OriginalString);
        }
    }
}
