using System.Windows.Controls;

namespace St.CollaborativeInfo
{
    /// <summary>
    /// CollaborativeInfoContentView.xaml 的交互逻辑
    /// </summary>
    public partial class CollaborativeInfoContentView : UserControl
    {
        public CollaborativeInfoContentView()
        {
            InitializeComponent();
            DataContext = new CollaborativeInfoContentViewModel(this);
        }
    }
}
