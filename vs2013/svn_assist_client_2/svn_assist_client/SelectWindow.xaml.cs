using System.Windows;

// 管理员功能选择界面
namespace svn_assist_client
{
    /// <summary>
    /// SelectWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SelectWindow : Window
    {
        public SelectWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        }
        // 退出登录
        private void OnLoginOut(object sender, RoutedEventArgs e)
        {
            
            MainWindow main = new MainWindow();
            
            main.Show();
            this.Close();
            
        }
        // 选择用户管理界面
        private void OnJumpToManageWindow(object sender, RoutedEventArgs e)
        {
            ManageWindow userManage = new ManageWindow();
            userManage.Show();
            this.Close();
        }
        // 选择配置界面
        private void OnJumpToConfigWindow(object sender, RoutedEventArgs e)
        {
            ConfigWindow config = new ConfigWindow();
            
            config.Show();
            this.Close();
        }
        // 选择编译工具界面
        private void OnJumpToCompileToolWindow(object sender, RoutedEventArgs e)
        {
            CompileToolWindow tool = new CompileToolWindow();

            tool.Show();
            this.Close();
        }
    }
}
