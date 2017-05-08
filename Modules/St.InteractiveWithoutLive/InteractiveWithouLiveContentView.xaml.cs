
using System.Windows.Controls;

namespace St.InteractiveWithouLive
{
    /// <summary>
    /// InteractiveWithouLiveContentView.xaml 的交互逻辑
    /// </summary>
    public partial class InteractiveWithouLiveContentView : UserControl
    {
        public InteractiveWithouLiveContentView()
        {
            InitializeComponent();
            DataContext = new InteractiveWithouLiveContentViewModel(this);
        }
    }
}