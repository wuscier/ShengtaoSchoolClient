namespace St.Common
{
    /// <summary>
    /// UpdateConfirmView.xaml 的交互逻辑
    /// </summary>
    public partial class SscDialogWithoutButton
    {
        public SscDialogWithoutButton(string msg) : this()
        {
            TbMsg.Text = msg;
        }

        public SscDialogWithoutButton()
        {
            InitializeComponent();
        }
    }
}
