using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Collections;
using ServiceStack.Redis;

namespace svn_assist_client
{
    /// <summary>
    /// EditUser.xaml 的交互逻辑
    /// </summary>
    public partial class EditUser : Window
    {
        RedisClient redis;

        User oldUser = new User();
        User newUser = new User();
        ArrayList allCheckBox = new ArrayList();
        ArrayList allCommand = new ArrayList();
        public EditUser()
        {
            InitializeComponent();
            oldUser = null;
            this.Loaded += EditUser_Loaded;
        }

        public EditUser(User u)
        {
            InitializeComponent();
            oldUser = u;
            usernameTextBox.Text = oldUser.UserName;
            passwordTextBox.Text = oldUser.Password;

            this.Loaded += EditUser_Loaded;
        }

        void EditUser_Loaded(object sender, RoutedEventArgs e)
        {
            redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            {
                byte[][] thisCommand = redis.SMembers(RedisKeyName.trunkCodeCommandSet);
                foreach (var item in thisCommand)
                {
                    allCommand.Add(item);
                }
                thisCommand = redis.SMembers(RedisKeyName.branchCodeCommandSet);
                foreach (var item in thisCommand)
                {
                    allCommand.Add(item);
                }
                thisCommand = redis.SMembers(RedisKeyName.serverCommandSet);
                foreach (var item in thisCommand)
                {
                    allCommand.Add(item);
                }
            }
            {
                allCheckBox.Add(checkBox1);
                allCheckBox.Add(checkBox2);
                allCheckBox.Add(checkBox3);
                allCheckBox.Add(checkBox4);
                allCheckBox.Add(checkBox5);
                allCheckBox.Add(checkBox6);
            }
            for (int i = 0; i < allCommand.Count && i < allCheckBox.Count; i++)
            {
                ((CheckBox)allCheckBox[i]).Visibility = System.Windows.Visibility.Visible;
                ((CheckBox)allCheckBox[i]).Content = redis.Get<string>(
                    RedisKeyName.commandPrefix + Encoding.UTF8.GetString((byte[])allCommand[i]) + ":name");
                if(oldUser != null && oldUser.Authority == 3)
                {
                    ((CheckBox)allCheckBox[i]).IsChecked = true;
                    ((CheckBox)allCheckBox[i]).IsEnabled = false;
                }
                else if(oldUser != null && redis.Get<int>(RedisKeyName.userAuthorityPrefix + oldUser.UserName + ":" + Encoding.UTF8.GetString((byte[])allCommand[i])) == 1)
                {
                    ((CheckBox)allCheckBox[i]).IsChecked = true;
                }
            }
        }
        // 取消按钮
        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        // 确认更改用户信息按钮
        private void OnEditUser(object sender, RoutedEventArgs e)
        {
            string name = usernameTextBox.Text;
            string pwd =passwordTextBox.Text;
            if (name == null || pwd == null || name == "" || pwd == "")
            {
                MessageBox.Show("用户名和密码不可为空", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (name.Contains(" ") || pwd.Contains(" "))
            {
                MessageBox.Show("用户名或密码不可包含空格", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (oldUser != null && oldUser.UserName != name && VerifyHelper.IsExist(name))
            {
                MessageBox.Show("用户名已存在", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if(oldUser == null && VerifyHelper.IsExist(name))
            {
                MessageBox.Show("用户名已存在", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            for (int i = 0; i < allCommand.Count && i < allCheckBox.Count; i++)
            {
                if (((CheckBox)allCheckBox[i]).IsChecked == true)
                {
                    redis.Set<int>(RedisKeyName.userAuthorityPrefix + name + ":" + Encoding.UTF8.GetString((byte[])allCommand[i]), 1);
                }
                else
                {
                    redis.Set<int>(RedisKeyName.userAuthorityPrefix + name + ":" + Encoding.UTF8.GetString((byte[])allCommand[i]), 0);
                }
            }
            if (oldUser != null && oldUser.UserName == name && oldUser.Password == pwd)
            {
                this.Close();
                return;
            }
            newUser.UserName = name;
            newUser.Password = pwd;
            if(oldUser != null)
            {
                newUser.Authority = oldUser.Authority;
            }
            else
            {
                newUser.Authority = 1;
            }
            ManageWindow manage = (ManageWindow)this.Owner;
            if(oldUser != null)
            {
                manage.EditUser(oldUser, newUser);
            }
            else
            {
                manage.AddNewUser(newUser);
            }
            this.Close();
        }
        protected override void OnClosed(System.EventArgs e)
        {
            redis.Quit();
            base.OnClosed(e);
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            redis.Quit();
            base.OnClosing(e);
        }
    }
}
