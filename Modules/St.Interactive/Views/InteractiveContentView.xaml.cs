
using System.Windows.Controls;

namespace St.Interactive
{
    /// <summary>
    /// InteractiveContentView.xaml 的交互逻辑
    /// </summary>
    public partial class InteractiveContentView : UserControl
    {
        public InteractiveContentView()
        {
            InitializeComponent();
            DataContext = new InteractiveContentViewModel(this);
        }
    }
}