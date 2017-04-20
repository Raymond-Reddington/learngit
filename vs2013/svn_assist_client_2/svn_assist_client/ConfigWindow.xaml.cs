using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ServiceStack.Redis;

namespace svn_assist_client
{
    public class Config
    {
        public string ConfigKey { get; set; }
        public string ConfigValue { get; set; }
        public string BtnName { get; set; }
        public int ID { get; set; }
    }
    /// <summary>
    /// ConfigWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigWindow : Window
    {
        RedisClient redis;
        public ConfigWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            this.Loaded += ConfigWindow_Loaded;
        }
        
        private void SetDataGrid(string redisSet, string redisPrefix, DataGrid dg, string btnNamePostfix = null, string valuePostfix = null)
        {
            ObservableCollection<Config> memberData = new ObservableCollection<Config>();
            byte[][] allKeys = redis.SMembers(redisSet);
            if (btnNamePostfix == null)
            {
                foreach (var item in allKeys)
                {
                    string key = Encoding.UTF8.GetString(item);
                    memberData.Add(new Config() 
                    { 
                        ConfigKey = key,
                        ConfigValue = redis.Get<string>(redisPrefix + key + ":script"),
                        BtnName = redis.Get<string>(redisPrefix + key + ":name")
                    });
                }
            }
            else
            {
                foreach (var item in allKeys)
                {
                    string key = Encoding.UTF8.GetString(item);
                    memberData.Add(new Config()
                    {
                        BtnName = redis.Get<string>(redisPrefix + key + btnNamePostfix),
                        ConfigValue = redis.Get<string>(redisPrefix + key + valuePostfix),
                        ConfigKey = redis.Get<string>(redisPrefix + key + ":father")
                    });
                }
            }
            dg.DataContext = memberData;
        }
        private void ConfigWindow_Loaded(object sender, RoutedEventArgs e)
        {
            redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            ObservableCollection<Config> memberData1 = new ObservableCollection<Config>();
            ObservableCollection<Config> memberData2 = new ObservableCollection<Config>();
            ObservableCollection<Config> memberData3 = new ObservableCollection<Config>();
            ObservableCollection<Config> memberData4 = new ObservableCollection<Config>();
            ObservableCollection<Config> memberData5 = new ObservableCollection<Config>();
            // svn 基准目录设定
            svnBaseDirTextBox.Text = redis.Get<string>(RedisKeyName.svnBaseDirKey);
            // 工作目录设定
            workDirTextBox.Text = redis.Get<string>(RedisKeyName.workDirKey);

            trunkCodeRevesionShell.Text = redis.Get<string>(RedisKeyName.trunkCodeRevesionShellKey);
            branchCodeRevesionShell.Text = redis.Get<string>(RedisKeyName.branchCodeRevesionShellKey);
            

            // 指令及相应脚本配置
            {
                // 主干代码svn log 按钮
                SetDataGrid(RedisKeyName.trunkCodeCommandSet, RedisKeyName.trunkCodeCommandPrefix, trunkCodeCommandConfigDataGrid);
                // 分支代码svn log 按钮
                SetDataGrid(RedisKeyName.branchCodeCommandSet, RedisKeyName.branchCodeCommandPrefix, branchCodeCommandConfigDataGrid);
                // 服务器操作 按钮
                SetDataGrid(RedisKeyName.serverCommandSet, RedisKeyName.serverCommandPrefix, serverCommandConfigDataGrid);
            }
            // 切换服务器参数配置
            SetDataGrid(RedisKeyName.tagIDSet, RedisKeyName.binTagPrefix, tagConfigDataGrid, ":name", ":param");
            // 服务器id与名称对应表
            SetDataGrid(RedisKeyName.serverIDSet, RedisKeyName.serverPrefix, serverConfigDataGrid, ":name", ":path");
            
            // svn Uri 配置
            byte[][] allKeys = redis.SMembers(RedisKeyName.svnLogIDSet);
            foreach (var item in allKeys)
            {
                string key = Encoding.UTF8.GetString(item);
                svnLogUriListView.Items.Add(new Config()
                {
                    ID = int.Parse(key),
                    BtnName = redis.Get<string>(RedisKeyName.svnLogPrefix + key + ":name"),
                    ConfigValue = redis.Get<string>(RedisKeyName.svnLogPrefix + key + ":link")
                });
            }
        }

        /*配置：
         *   工作目录
         *   svn目录主
         *   按钮执行脚本
         *   服务器序列
         *   切换服务器参数列表
         *   svn子目录
         */
        private void OnBack(object sender, RoutedEventArgs e)
        {
            SelectWindow select = new SelectWindow();
            select.Show();
            this.Close();
        }
        private bool SubmitDataGrid(string redisSet, string redisPrefix, DataGrid dg, string btnNamePostfix = null, string valuePostfix = null)
        {

            if(redis.SMembers(redisSet) != null && redis.SMembers(redisSet).Length > 0)
            {
                redis.SRem(redisSet, redis.SMembers(redisSet));
            }
            if (btnNamePostfix == null)
            {
                foreach (var item in dg.Items)
                {
                    Config thisTag = item as Config;
                    if (thisTag == null ||
                        ((thisTag.ConfigKey == null || thisTag.ConfigKey.Trim() == "") ||
                        (thisTag.ConfigValue == null || thisTag.ConfigValue.Trim() == "")))
                    {
                        continue;
                    }
                    if (redis.SAdd(redisSet, Encoding.UTF8.GetBytes(thisTag.ConfigKey)) == 0)
                    {
                        return false;
                    }
                    if (!redis.Set<string>(
                        redisPrefix + thisTag.ConfigKey + ":name",
                        thisTag.BtnName))
                    {
                        return false;
                    }
                    if (!redis.Set<string>(
                        redisPrefix + thisTag.ConfigKey + ":script",
                        thisTag.ConfigValue))
                    {
                        return false;
                    }
                }
            }
            else 
            {
                int index = 0;
                foreach (var item in dg.Items)
                {
                    Config thisTag = item as Config;
                    if (thisTag == null ||
                        ((thisTag.BtnName == null || thisTag.BtnName.Trim() == "") ||
                        (thisTag.ConfigValue == null || thisTag.ConfigValue.Trim() == "")))
                    {
                        continue;
                    }
                    if (redis.SAdd(redisSet, Encoding.UTF8.GetBytes(index.ToString())) == 0)
                    {
                        return false;
                    }
                    if (!redis.Set<string>(
                        redisPrefix + index.ToString() + btnNamePostfix,
                        thisTag.BtnName))
                    {
                        return false;
                    }
                    if (!redis.Set<string>(
                        redisPrefix + index.ToString() + valuePostfix,
                        thisTag.ConfigValue))
                    {
                        return false;
                    }
                    if (!redis.Set<string>(
                        redisPrefix + index.ToString() + ":father",
                        thisTag.ConfigKey))
                    {
                        return false;
                    }
                    index++;
                }
            }
            return true;
        }
        private void OnSubmit(object sender, RoutedEventArgs e)
        {
            try
            {
                bool submitResult = true;
                submitResult &= redis.Set<string>(RedisKeyName.workDirKey, workDirTextBox.Text);
                submitResult &= redis.Set<string>(RedisKeyName.svnBaseDirKey, svnBaseDirTextBox.Text);
                submitResult &= redis.Set<string>(RedisKeyName.trunkCodeRevesionShellKey, trunkCodeRevesionShell.Text);
                submitResult &= redis.Set<string>(RedisKeyName.branchCodeRevesionShellKey, branchCodeRevesionShell.Text);
                // 提交 指令及相应脚本配置
                {
                    // 提交 主干代码svn log 按钮
                    submitResult &= SubmitDataGrid(RedisKeyName.trunkCodeCommandSet, RedisKeyName.trunkCodeCommandPrefix, trunkCodeCommandConfigDataGrid);
                    // 提交 分支代码svn log 按钮
                    submitResult &= SubmitDataGrid(RedisKeyName.branchCodeCommandSet, RedisKeyName.branchCodeCommandPrefix, branchCodeCommandConfigDataGrid);
                    // 提交 服务器操作 按钮
                    submitResult &= SubmitDataGrid(RedisKeyName.serverCommandSet, RedisKeyName.serverCommandPrefix, serverCommandConfigDataGrid);
                }
                // 提交 切换服务器参数配置
                submitResult &= SubmitDataGrid(RedisKeyName.tagIDSet, RedisKeyName.binTagPrefix, tagConfigDataGrid, ":name", ":param");
                // 提交 服务器id与名称对应表
                submitResult &= SubmitDataGrid(RedisKeyName.serverIDSet, RedisKeyName.serverPrefix, serverConfigDataGrid, ":name", ":path");
                // 提交 svn log 的 Uri
                foreach (var item in svnLogUriListView.Items)
                {
                    Config thisLogUri = item as Config;
                    string key = RedisKeyName.svnLogPrefix + thisLogUri.ID.ToString() + ":link";
                    if (!redis.Set<string>(key, thisLogUri.ConfigValue)) submitResult = false;
                }
                if(submitResult) MessageBox.Show("提交成功", "Result", MessageBoxButton.OK, MessageBoxImage.Information);
                else MessageBox.Show("提交失败", "Result", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            { 
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

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
        int svnIndex = 0;
        private void OnSwitchDir(object sender, RoutedEventArgs e)
        {
            Button thisBtn = sender as Button;
            for(int i = 0; i < svnLogUriListView.Items.Count; i++)
            {
                ListViewItem lvi = svnLogUriListView.ItemContainerGenerator.ContainerFromIndex(i) as ListViewItem;
                Button btn = FindVisualChild<Button>(lvi);
                if(btn == thisBtn)
                {
                    svnIndex = i;
                    break;
                }
            }
            WindowFile wf = new WindowFile();
            wf.PassValuesEvent += new WindowFile.PassValuesHandler(ReceiveValue);
            wf.Show();
            wf.Owner = this;
        }
        private void ReceiveValue(object sender, string message)
        {
            if (message == null) return;
            Config thisLogUri = (Config)svnLogUriListView.Items.GetItemAt(svnIndex);
            thisLogUri.ConfigValue = message;
            svnLogUriListView.Items.RemoveAt(svnIndex);
            svnLogUriListView.Items.Insert(svnIndex, thisLogUri);
            
        }
    }
}
