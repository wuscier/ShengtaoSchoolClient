using System.Windows.Controls;

namespace St.Discussion
{
    /// <summary>
    /// DiscussionContentView.xaml 的交互逻辑
    /// </summary>
    public partial class DiscussionContentView : UserControl
    {
        public DiscussionContentView()
        {
            InitializeComponent();
            DataContext = new DiscussionContentViewModel(this);
        }
    }
}
