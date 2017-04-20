using System.Text;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ServiceStack.Redis;

// 用户管理界面
namespace svn_assist_client
{
    /// <summary>
    /// ManageWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ManageWindow : Window
    {
        RedisClient redis;
        JavaScriptSerializer js = new JavaScriptSerializer();
        const string hashID = "svn_assist:users:hash_table";
        public ManageWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            this.Loaded += ManageWindow_Loaded;
        }

        private void ManageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            // 读取所有用户信息并显示
            byte[][] keys = redis.SMembers(RedisKeyName.usernameSet);
            foreach (var item in keys)
            {
                allUserListView.Items.Add(new User(Encoding.UTF8.GetString(item)));
            }
        }
        // 返回功能
        private void OnBack(object sender, RoutedEventArgs e)
        {
            SelectWindow select = new SelectWindow();
            select.Show();
            this.Close();
        }

        
        // 添加用户按钮
        private void OnAddUser(object sender, RoutedEventArgs e)
        {
            //AddUser add = new AddUser();
            //add.Owner = this;
            //add.Show();
            EditUser add = new EditUser();
            add.Owner = this;
            add.Title = "添加新用户";
            add.Show();
        }
        // 添加用户
        public void AddNewUser(User newUser)
        {
            AddNewUserToRedis(newUser);
            allUserListView.Items.Add(newUser);
        }
        // 添加用户值数据库
        private void AddNewUserToRedis(User newUser)
        {
            redis.SAdd(RedisKeyName.usernameSet, Encoding.UTF8.GetBytes(newUser.UserName));
            redis.Set<string>(RedisKeyName.userInfoPrefix + newUser.UserName + ":password", newUser.Password);
            redis.Set<int>(RedisKeyName.userInfoPrefix + newUser.UserName + ":authority", newUser.Authority);
        }
        // 从数据库删除用户
        private void DelOldUserFromRedis(User oldUser)
        {
            redis.SRem(RedisKeyName.usernameSet, Encoding.UTF8.GetBytes(oldUser.UserName));
        }

        /*
         * Function: FindVisualChild
         * Description: 在父亲控件中查询特定类型的子控件
         * Parameter:
         *   obj: 父亲控件
         * Return: 找到时返回子控件，否则返回空
         */
        private ChildType FindVisualChild<ChildType>(DependencyObject obj) where ChildType : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is ChildType)
                {
                    return child as ChildType;
                }
                else
                {
                    ChildType childOfChildren = FindVisualChild<ChildType>(child);
                    if (childOfChildren != null)
                    {
                        return childOfChildren;
                    }
                }
            }
            return null;

        }

        // 删除用户按钮
        private void OnDeleteUser(object sender, RoutedEventArgs e)
        {
            bool hasChecked = false;
            for (int i = allUserListView.Items.Count - 1; i >= 0; i--)
            {
                ListViewItem lvi = allUserListView.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;

                var cb = FindVisualChild<CheckBox>(lvi);
                if (cb.IsChecked == true)
                {
                    User thisUser = (User)allUserListView.Items.GetItemAt(i);
                    if (MainWindow.myAuthority <= thisUser.Authority)
                    {
                        MessageBox.Show("权限不够！", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    DelOldUserFromRedis(thisUser);
                    allUserListView.Items.RemoveAt(i);
                    hasChecked = true;
                }
            }
            if(hasChecked)
            {
                MessageBox.Show("删除成功！", "Result", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("未选中数据！", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // 更改用户信息按钮
        private void OnEditUser(object sender, RoutedEventArgs e)
        {
            if (allUserListView.SelectedItem == null)
            {
                MessageBox.Show("请选中要修改的数据行！", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            User thisUser = (User)allUserListView.SelectedItem;
            if(thisUser == null)
            {
                MessageBox.Show("请选中要修改的数据行！", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if(MainWindow.myAuthority <= thisUser.Authority && MainWindow.myUsername != thisUser.UserName)
            {
                MessageBox.Show("权限不够！", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditUser edit = new EditUser(thisUser);
            edit.Owner = this;
            edit.Title = "修改用户信息";
            edit.Show();
        }
        // 更新用户信息
        public void EditUser(User oldUser, User newUser)
        {
            DelOldUserFromRedis(oldUser);
            AddNewUserToRedis(newUser);
            allUserListView.Items.Insert(allUserListView.SelectedIndex, newUser);
            allUserListView.Items.RemoveAt(allUserListView.SelectedIndex);
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
