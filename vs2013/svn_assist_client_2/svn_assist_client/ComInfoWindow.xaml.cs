using System.Windows;

namespace svn_assist_client
{
    /// <summary>
    /// ComInfoWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ComInfoWindow : Window
    {
        public ComInfoWindow(string cmd, string t, string rc)
        {
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            commandTextBox.Text = cmd;
            timeTextBox.Text = t;
            resultTextBox.Text = rc;
        }
        private void OnBack(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
