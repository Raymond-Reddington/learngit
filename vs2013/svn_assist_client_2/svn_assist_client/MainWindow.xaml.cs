using ServiceStack.Redis;
using System;
using System.Windows;
using SharpSvn;

namespace svn_assist_client
{
    // 登录界面
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        // 静态全局变量记录登陆者的信息
        public static string myUsername;
        public static string myPassword;
        public static int myAuthority;
        public static string myRedisIP;
        public static int myRedisPort;
        public static long database = 0;
        
        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            // 从配置文件中读取默认redis连接
            redisIPTextBox.Text = ConfigHelper.GetAppConfig("host");
            redisPortTextBox.Text = ConfigHelper.GetAppConfig("port");
        }

        // 登录事件
        private void OnLoginIn(object sender, RoutedEventArgs e)
        {
            //{
            //    SelectWindow select = new SelectWindow();
            //    select.Show();
            //    this.Close();
            //}
            if (redisIPTextBox.Text == null)
            {
                MessageBox.Show("请输入数据库地址！", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            string ip = redisIPTextBox.Text;
            int port = int.Parse(redisPortTextBox.Text);
            if(!IsConnected(ip, port))
            {
                MessageBox.Show("连接失败！", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            myRedisIP = ip;
            myRedisPort = port;
            
            string name = usernameTextBox.Text;
            string pwd =passwordPasswordBox.Password;
            int authority = VerifyHelper.VerifyUser(name, pwd);
            if(authority == (int)UserAuthority.None)
            {
                MessageBox.Show("用户名或密码错误", "Warning!",MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if(authority == (int)UserAuthority.Alrealy)
            {
                MessageBox.Show("不允许多个用户使用同一账号！", "Warning!", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            myUsername = name;
            myPassword = pwd;
            myAuthority = authority;
            // 普通用户和管理员进入不同的界面
            if(authority == (int)UserAuthority.Normal)
            {
                CompileToolWindow tool = new CompileToolWindow();
                tool.Show();
            }
            else
            {
                SelectWindow select = new SelectWindow();
                select.Show();
            }
            this.Close();
        }
        // 判断redis连接是否有效
        private bool IsConnected(string ip, int port)
        {
            Console.WriteLine(ip + " " + port);
            RedisClient redis = new RedisClient(ip, port, null, database);
            try
            {
                redis.RetryTimeout = 500;
                redis.ConnectTimeout = 100;
                bool isOK = redis.Ping();
                redis.Quit();
                return isOK;
            }
            catch (RedisException e)
            {
                redis.Quit();
                Console.WriteLine(e.Message);
                return false;
            }
        }
        
    }
}
