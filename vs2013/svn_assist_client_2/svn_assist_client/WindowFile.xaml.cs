using SharpSvn;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Threading;
using ServiceStack.Redis;

namespace svn_assist_client
{
    /// <summary>
    /// WindowFile.xaml 的交互逻辑
    /// </summary>
    public partial class WindowFile : Window
    {
        RedisClient redis;
        public delegate void PassValuesHandler(object sender, string message);
        public event PassValuesHandler PassValuesEvent;
        public string Path { get; set; }
        string dir = "http://svn.ids111.com/studios/nebula-art/x-rpg-server/";
        private readonly object _dummyNode = null;
        SvnClient svn = new SvnClient();
        public WindowFile()
        {
            InitializeComponent();
            this.Loaded += new RoutedEventHandler(WindowFile_Loaded);
        }

        private void WindowFile_Loaded(object sender, RoutedEventArgs e)
        {
            redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            dir = redis.Get<string>(RedisKeyName.svnBaseDirKey);
            if (dir == null || dir.Trim() == "")
            {
                MessageBox.Show("svn 地址为空！", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }
            //svn.Authentication.UserNamePasswordHandlers += new EventHandler<SharpSvn.Security.SvnUserNamePasswordEventArgs>(
            //    delegate(Object s, SharpSvn.Security.SvnUserNamePasswordEventArgs e1)
            //    {
            //        e1.UserName = "red.sun";
            //        e1.Password = "819499100@dsky";
            //    });

            try
            {
                Collection<SvnListEventArgs> list = new Collection<SvnListEventArgs>();
                SvnTarget repos = (SvnTarget)dir;
                {
                    svn.Authentication.UserNamePasswordHandlers += new EventHandler<SharpSvn.Security.SvnUserNamePasswordEventArgs>(
                    delegate(Object s, SharpSvn.Security.SvnUserNamePasswordEventArgs e1)
                    {
                        e1.UserName = GlobalVariable.svnUserName;
                        e1.Password = GlobalVariable.svnPassword;
                    });
                    try
                    {
                        svn.GetList(repos, out list);
                        My_Load();
                    }
                    catch (SvnException)
                    {
                        SvnLoginInWindow slw = new SvnLoginInWindow(repos);
                        slw.Owner = this;
                        slw.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }
        public void My_Load()
        {
            svn.Authentication.UserNamePasswordHandlers += new EventHandler<SharpSvn.Security.SvnUserNamePasswordEventArgs>(
                delegate(Object s, SharpSvn.Security.SvnUserNamePasswordEventArgs e1)
                {
                    e1.UserName = GlobalVariable.svnUserName;
                    e1.Password = GlobalVariable.svnPassword;
                });
            dir = redis.Get<string>(RedisKeyName.svnBaseDirKey);
            SvnTarget repos = (SvnTarget)dir;
            Collection<SvnListEventArgs> list = new Collection<SvnListEventArgs>();
            try
            {
                if (svn.GetList(repos, out list))
                {
                    list.RemoveAt(0);
                    foreach (var file in list)
                    {
                        TreeViewItem item = new TreeViewItem();
                        item.Header = file.Name;
                        item.Tag = dir + file.Name;
                        item.Items.Add(_dummyNode);
                        item.Expanded += folder_Expanded;
                        TreeViewItemProps.SetFileType(item, "folder");
                        folderTree.Items.Add(item);
                    }
                }
            }
            catch (SvnException ex)
            {
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close();
            }
        }
        private void File_Select(object sender, RoutedEventArgs e)
        {
            TreeView tree = sender as TreeView;
            if(tree.SelectedItem != null)
            {
                TreeViewItem item = (TreeViewItem)tree.SelectedItem;
                Path = (string)item.Tag;
            }
        }
        private void folder_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem item = (TreeViewItem)sender;
            if(item.Items.Count == 1 && item.Items[0] == _dummyNode)
            {
                item.Items.Clear();
                try
                { 
                    Collection<SvnListEventArgs> list = new Collection<SvnListEventArgs>();
                    string sub_dir = (string)item.Tag;
                    SvnTarget repos = (SvnTarget)sub_dir;
                    svn.GetList(repos, out list);
                    if(list.Count > 1 && !TreeViewItemProps.GetFileType(item).Equals("folder"))
                    {
                        TreeViewItemProps.SetFileType(item, "folder");
                    }
                    list.RemoveAt(0);
                    foreach (var file in list)
                    {
                        TreeViewItem sub_item = new TreeViewItem();
                        if(file.Name.Contains(".txt"))
                        {
                            TreeViewItemProps.SetFileType(sub_item, "txt");
                        }
                        else if(file.Name.Contains(".exe"))
                        {
                            TreeViewItemProps.SetFileType(sub_item, "exe");
                        }
                        else
                        {
                            if(file.Name.Contains(".c") ||
                                file.Name.Contains(".py") ||
                                file.Name.Contains(".sh") ||
                                file.Name.Contains(".h") ||
                                file.Name.Contains(".lua") ||
                                file.Name.Contains(".js"))
                            {
                                TreeViewItemProps.SetFileType(sub_item, "code");
                            }
                            else if(file.Name.Contains("."))
                            {
                                TreeViewItemProps.SetFileType(sub_item, "unknown");
                            }
                        }
                        sub_item.Header = file.Name;
                        sub_item.Tag = sub_dir + "/" + file.Name;
                        sub_item.Items.Add(_dummyNode);
                        sub_item.Expanded += folder_Expanded;
                        item.Items.Add(sub_item);
                    }
                }
                catch (Exception) { }
            }
        }

        private void OnOK(object sender, RoutedEventArgs e)
        {
            PassValuesEvent(this, Path);
            this.Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        protected override void OnClosed(EventArgs e)
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
