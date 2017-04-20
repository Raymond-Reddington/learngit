using ServiceStack.Redis;
using SharpSvn;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;


namespace svn_assist_client
{
    /// <summary>
    class Server
    {
        public string ServerID { set; get; }
        public string ServerName { set; get; }
        public string ServerPath { get; set; }
        public Server(string id)
        {
            RedisClient redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            ServerID = id;
            ServerName = redis.Get<string>(RedisKeyName.serverPrefix + id + ":name");
            ServerPath = redis.Get<string>(RedisKeyName.serverPrefix + id + ":path");
            redis.Quit();
        }
    }
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class CompileToolWindow : Window
    {
        RedisClient thisWindowRedis;
        List<string> taskProperities = new List<string>();

        JavaScriptSerializer js = new JavaScriptSerializer();

        const string svnSwitchShell = "svn_switch";
       
        // uri 集合
        ArrayList svnLogUris = new ArrayList();
        // 显示的 log 最大条数
        const int maxNumOfDisplay = 30;

        // 显示svn log/path/message 的集合
        ArrayList svnLogListViews = new ArrayList();
        ArrayList changedPathsLisView = new ArrayList();
        ArrayList messagesTextBox = new ArrayList();

        ArrayList svnLogTabItems = new ArrayList();

        Dictionary<string, int> currentBranchOfServer = new Dictionary<string, int>();
        Dictionary<string, int> preBranchOfServer = new Dictionary<string, int>();
        bool running;
        public CompileToolWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            this.Loaded += CompileToolWindow_Loaded;
        }
        private void setBtn(ArrayList btns, string redisSet, string redisCommandPrefix, string redisPrefix)
        {
            byte[][] allCommand = thisWindowRedis.SMembers(redisSet);
            for (int i = 0; i < btns.Count && i < allCommand.Length; i++)
            {
                ((Button)btns[i]).Visibility = System.Windows.Visibility.Visible;
                ((Button)btns[i]).Content = thisWindowRedis.Get<string>(
                    redisCommandPrefix +
                    Encoding.UTF8.GetString(allCommand[i]) +
                    ":name");
                ((Button)btns[i]).Tag = Encoding.UTF8.GetString(allCommand[i]);
                if (MainWindow.myAuthority == 3 || thisWindowRedis.Get<int>(
                    redisPrefix +
                    MainWindow.myUsername + ":" +
                    Encoding.UTF8.GetString(allCommand[i])) == 1)
                {
                    ((Button)btns[i]).IsEnabled = true;
                }
                else
                {
                    ((Button)btns[i]).IsEnabled = false;
                }
            }
        }
        private MenuItem FindChildByName(object fatherObj, string name)
        {
            ItemCollection items;
            if(fatherObj is ContextMenu)
            {
                items = ((ContextMenu)fatherObj).Items;
            }
            else if(fatherObj is MenuItem)
            {
                items = ((MenuItem)fatherObj).Items;
            }
            else
            { return null; }
            foreach (MenuItem item in items)
            {
                if (item.Name == name)
                {
                    return item;
                }
                if(item.HasItems)
                {
                    MenuItem findMenuItem = FindChildByName(item, name);
                    if(findMenuItem != null)
                    {
                        return findMenuItem;
                    }
                }
            }
            return null;
        }
        private void CompileToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            thisWindowRedis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            MyInitial();
            // 根据权限初始化按钮
            {
                ArrayList allTrunkCodeBtn = new ArrayList();
                {
                    allTrunkCodeBtn.Add(trunkCodeBtn1);
                    allTrunkCodeBtn.Add(trunkCodeBtn2);
                    allTrunkCodeBtn.Add(trunkCodeBtn3);
                    allTrunkCodeBtn.Add(trunkCodeBtn4);
                    setBtn(allTrunkCodeBtn, RedisKeyName.trunkCodeCommandSet, RedisKeyName.trunkCodeCommandPrefix, RedisKeyName.userAuthorityPrefix);
                }
                ArrayList allBranchCodeBtn = new ArrayList();
                {
                    allBranchCodeBtn.Add(branchCodeBtn1);
                    allBranchCodeBtn.Add(branchCodeBtn2);
                    allBranchCodeBtn.Add(branchCodeBtn3);
                    allBranchCodeBtn.Add(branchCodeBtn4);
                    setBtn(allBranchCodeBtn, RedisKeyName.branchCodeCommandSet, RedisKeyName.branchCodeCommandPrefix, RedisKeyName.userAuthorityPrefix);
                }
                {
                    byte[][] allCommand = thisWindowRedis.SMembers(RedisKeyName.serverCommandSet);
                    foreach (var item in allCommand)
                    {
                        MenuItem newMenuItem = new MenuItem();
                        newMenuItem.Header = thisWindowRedis.Get<string>(RedisKeyName.serverCommandPrefix +
                            Encoding.UTF8.GetString(item) + ":name");
                        newMenuItem.Name = Encoding.UTF8.GetString(item);
                        newMenuItem.Click += OnMenuItemClick;
                        if (MainWindow.myAuthority == 3 || thisWindowRedis.Get<int>(
                            RedisKeyName.userAuthorityPrefix +
                            MainWindow.myUsername + ":" +
                            Encoding.UTF8.GetString(item)) == 1)
                        {
                            newMenuItem.IsEnabled = true;
                        }
                        else
                        {
                            newMenuItem.IsEnabled = false;
                        }
                        SMenu.Items.Add(newMenuItem);
                    }
                    allCommand = thisWindowRedis.SMembers(RedisKeyName.tagIDSet);
                    foreach (var item in allCommand)
                    {
                        MenuItem newMenuItem = new MenuItem();
                        newMenuItem.Tag = Encoding.UTF8.GetString(item);
                        newMenuItem.Header = thisWindowRedis.Get<string>(
                            RedisKeyName.binTagPrefix +
                            Encoding.UTF8.GetString(item) + ":param").Replace(" ", "/").Replace("_", "__");
                        newMenuItem.Click += OnSwitchTo;
                        newMenuItem.Name = thisWindowRedis.Get<string>(
                            RedisKeyName.binTagPrefix +
                            Encoding.UTF8.GetString(item) + ":name");
                        string fatherName = thisWindowRedis.Get<string>(
                            RedisKeyName.binTagPrefix + 
                            Encoding.UTF8.GetString(item) + ":father"
                            );
                        newMenuItem.IsEnabled = true;
                        MenuItem fatherMenuItem = FindChildByName(SMenu, fatherName);
                        if(fatherMenuItem != null)
                        {
                            fatherMenuItem.Items.Add(newMenuItem);
                        }
                    }
                }
            }
            // 服务器列表渲染
            byte[][] allKeys = thisWindowRedis.SMembers(RedisKeyName.serverIDSet);
            foreach (var item in allKeys)
            {
                string serverID = Encoding.UTF8.GetString(item);
                serverListView.Items.Add(new Server(serverID));
                currentBranchOfServer.Add(serverID, thisWindowRedis.Get<int>(RedisKeyName.currentBranchOfServerPrefix + serverID));
            }

            // 等待任务队列渲染
            //list = redis.GetAllItemsFromList(Wtask_list);
            //foreach (var item in list)
            //{
            //    Task2 this_task = js.Deserialize<Task2>(item);
            //    Wtask_queue.Items.Add(this_task);
            //}
            // 等待任务队列更新线程
            Thread updateWaitingTask = new Thread(new ThreadStart(UpdateWaitingTaskQueueListView));
            updateWaitingTask.IsBackground = true;
            updateWaitingTask.Start();

            // 已完成队列渲染
            List<string> finishedTaskList = thisWindowRedis.GetAllItemsFromList(RedisKeyName.finishedTaskList);
            foreach (var taskID in finishedTaskList)
            {
                finishedTaskQueueListView.Items.Insert(0, new Task(taskID));
            }

            // 已完成队列更新线程
            Thread subscrbe = new Thread(new ThreadStart(OnSubscribe));
            subscrbe.IsBackground = true;
            subscrbe.Start();
            
            // svn日志查看是否需要输入密码
            {
                SvnClient svn = new SvnClient();
                SvnTarget target = null;
                try
                {
                    target = (SvnTarget)thisWindowRedis.Get<string>("svn_assist:config:svn_log:1:link");
                    svn.Authentication.UserNamePasswordHandlers += new EventHandler<SharpSvn.Security.SvnUserNamePasswordEventArgs>(
                    delegate(Object s, SharpSvn.Security.SvnUserNamePasswordEventArgs e1)
                    {
                        e1.UserName = GlobalVariable.svnUserName;
                        e1.Password = GlobalVariable.svnPassword;
                    });
                    Collection<SvnListEventArgs> list = new Collection<SvnListEventArgs>();
                    svn.GetList(target, out list);
                    DrawSvnLog();
                }
                catch(ArgumentNullException ex)
                {
                    MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                catch (SvnException)
                {
                    SvnLoginInWindow slw = new SvnLoginInWindow(target);
                    slw.Show();
                    slw.Owner = this;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                svn.Dispose();
            }

            // svn 高亮当前版本
            Thread highLightCurrentRevision = new Thread(new ThreadStart(HighLightCurrentRevision));
            highLightCurrentRevision.IsBackground = true;
            highLightCurrentRevision.Start();

            
        }
        // svn 日志的渲染及更新
        public void DrawSvnLog()
        {
            svnLogUris.Clear();
            {
                try
                {
                    svnLogUris.Add(new Uri(thisWindowRedis.Get<string>("svn_assist:config:svn_log:1:link")));
                    svnLogUris.Add(new Uri(thisWindowRedis.Get<string>("svn_assist:config:svn_log:3:link")));
                    svnLogUris.Add(new Uri(thisWindowRedis.Get<string>("svn_assist:config:svn_log:2:link")));
                    svnLogUris.Add(new Uri(thisWindowRedis.Get<string>("svn_assist:config:svn_log:4:link")));
                }
                catch (Exception)
                {

                    if (MessageBox.Show("svn的部分Uri无效，是否退出程序？", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                    {
                        running = false;
                        this.Close();
                    }
                    else
                    {
                        return;
                    }

                }
            }
            try
            {
                {
                    Thread firstDrawLog = new Thread(new ThreadStart(() => {
                        this.Dispatcher.BeginInvoke(
                            (ThreadStart)delegate()
                            {
                                SvnClient svn = new SvnClient();
                                svn.Authentication.UserNamePasswordHandlers += new EventHandler<SharpSvn.Security.SvnUserNamePasswordEventArgs>(
                                        delegate(Object s, SharpSvn.Security.SvnUserNamePasswordEventArgs e1)
                                        {
                                            e1.UserName = GlobalVariable.svnUserName;
                                            e1.Password = GlobalVariable.svnPassword;
                                        });
                                Collection<SvnLogEventArgs> svnLogs = new Collection<SvnLogEventArgs>();
                                SvnLogArgs logarg = new SvnLogArgs();
                                logarg.Limit = maxNumOfDisplay;
                                for (int i = 0; i < svnLogUris.Count; i++)
                                {
                                    if (svn.GetLog((Uri)svnLogUris[i], logarg, out svnLogs))
                                        UpdateListView(svnLogs, (ListView)svnLogListViews[i]);
                                }
                                svn.Dispose();
                            });
                        
                    }));
                    firstDrawLog.IsBackground = true;
                    firstDrawLog.Start();
                }
                StartUpdateSvnLog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Source, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        public void StartUpdateSvnLog()
        {
            Thread updateSvnLog = new Thread(new ThreadStart(UpdateLogListView));
            updateSvnLog.IsBackground = true;
            updateSvnLog.Start();
        }
        private void HighLightCurrentRevision()
        {
            RedisClient redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            while(running)
            {
                this.Dispatcher.BeginInvoke(
                    (ThreadStart)delegate()
                    {
                        try
                        {
                            trunkCodeLogListView.FontWeight = FontWeights.Normal;
                            branchCodeLogListView.FontWeight = FontWeights.Normal;
                            long currentTrunkRevision = 0;
                            long currentBranchRevision = 0;
                            try 
	                        {	        
                                currentTrunkRevision = redis.Get<long>(RedisKeyName.currentTrunkRevisionKey);
		                        currentBranchRevision = redis.Get<long>(RedisKeyName.currentBranchRevisionKey);
	                        }
	                        catch (FormatException ex)
	                        {
                                Console.WriteLine(ex.Message);
	                        }
                            currentTrunkRevisionLabel.Content = currentTrunkRevision.ToString();
                            currentBranchRevisionLabel.Content = currentBranchRevision.ToString();
                            foreach (var item in trunkCodeLogListView.Items)
                            {
                                Log thisLog = item as Log;
                                if(thisLog.Revision == currentTrunkRevision)
                                {
                                    ListViewItem lvi = trunkCodeLogListView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                                    lvi.FontWeight = FontWeights.Bold;
                                    break;
                                }
                            }
                            foreach (var item in branchCodeLogListView.Items)
                            {
                                Log thisLog = item as Log;
                                if (thisLog.Revision == currentBranchRevision)
                                {
                                    ListViewItem lvi = branchCodeLogListView.ItemContainerGenerator.ContainerFromItem(item) as ListViewItem;
                                    lvi.FontWeight = FontWeights.Bold;
                                    break;
                                }
                            }
                        }
                        catch (Exception)
                        {
                        }
                    });
                Thread.Sleep(500);
            }
            redis.Quit();
        }

        private void MyInitial()
        {
            {
                taskProperities.Add(":Author");
                taskProperities.Add(":Command");
                taskProperities.Add(":State");
                taskProperities.Add(":Time");
                taskProperities.Add(":Type");
                taskProperities.Add(":ServerID");
                taskProperities.Add(":TagID");
            }
            {
                int authority = MainWindow.myAuthority;
                if (authority <= 1)
                {
                    btnBack.Visibility = System.Windows.Visibility.Hidden;
                }
            }
            {
                svnLogListViews.Add(trunkCodeLogListView);
                svnLogListViews.Add(trunkCompileLogListView);
                svnLogListViews.Add(branchCodeLogListView);
                svnLogListViews.Add(branchCompileLogListView);
            }
            {
                changedPathsLisView.Add(trunkCodeLogPathListView);
                changedPathsLisView.Add(trunkCompileLogPathListView);
                changedPathsLisView.Add(branchCodeLogPathListView);
                changedPathsLisView.Add(branchCompileLogPathListView);
            }
            {
                messagesTextBox.Add(trunkCodeLogMessageTextBox);
                messagesTextBox.Add(trunkCompileLogMessageTextBox);
                messagesTextBox.Add(branchCodeLogMessageTextBox);
                messagesTextBox.Add(branchCompileLogMessageTextBox);
            }
            running = true;
        }


        private void OnSubscribe()
        {
            RedisClient newclient = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            newclient.Subscribe(RedisKeyName.subscribeKey);
            RedisSubscription sub = new RedisSubscription(newclient);
            sub.OnUnSubscribe += (obj) =>
            {
                Console.WriteLine();
            };
            sub.OnMessage = (sender, argcs) =>
            {
                string thisID = argcs;
                try
                {
                    Task thisTask = new Task(thisID);
                    this.Dispatcher.BeginInvoke(new Action(() => { finishedTaskQueueListView.Items.Insert(0, thisTask); }));
                    if(thisTask.Command == "svn_switch")
                    {
                        if(currentBranchOfServer.ContainsKey(thisTask.ServerID))
                        {
                            RedisClient redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
                            if (preBranchOfServer.ContainsKey(thisTask.ServerID))
                            {
                                preBranchOfServer[thisTask.ServerID] = currentBranchOfServer[thisTask.ServerID];
                            }
                            else
                            {
                                preBranchOfServer.Add(thisTask.ServerID, currentBranchOfServer[thisTask.ServerID]);
                            }
                            currentBranchOfServer[thisTask.ServerID] = int.Parse(thisTask.TagID) + 1;
                            redis.Set<int>(RedisKeyName.currentBranchOfServerPrefix + thisTask.ServerID, int.Parse(thisTask.TagID) + 1);
                            redis.Quit();
                        }
                        else
                        {
                            currentBranchOfServer.Add(thisTask.ServerID, int.Parse(thisTask.TagID) + 1);
                        }
                        this.Dispatcher.BeginInvoke(new Action(() => { UpdateCurrentBranchInServer(); }));
                    }
                }
                catch (Exception ex)
                {
                    
                    MessageBox.Show(ex.Message, ex.Source);
                    return;
                }
            };
            sub.SubscribeToChannels(RedisKeyName.subscribeKey);
            newclient.Quit();

        }

        private void UpdateLogListView()
        {
            RedisClient redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            SvnClient svn = new SvnClient();
            svn.Authentication.UserNamePasswordHandlers += new EventHandler<SharpSvn.Security.SvnUserNamePasswordEventArgs>(
                    delegate(Object s, SharpSvn.Security.SvnUserNamePasswordEventArgs e1)
                    {
                        e1.UserName = GlobalVariable.svnUserName;
                        e1.Password = GlobalVariable.svnPassword;
                    });
            while (running)
            {
                this.Dispatcher.BeginInvoke(
                    (ThreadStart)delegate()
                    {
                        Collection<SvnLogEventArgs> svnLogs = new Collection<SvnLogEventArgs>();
                        svnLogs.Clear();
                        {
                            SvnLogArgs logarg = new SvnLogArgs();
                            logarg.Limit = maxNumOfDisplay;
                            if(svnLogTabControl.SelectedIndex >=0 && svnLogTabControl.SelectedIndex <=3)
                            {
                                if (svn.GetLog((Uri)svnLogUris[svnLogTabControl.SelectedIndex], logarg, out svnLogs))
                                {
                                    UpdateListView(svnLogs, (ListView)svnLogListViews[svnLogTabControl.SelectedIndex]);
                                }
                            }
                            //for (int i = 0; i < svnLogUris.Count; i++)
                            //{
                            //    if (svn.GetLog((Uri)svnLogUris[i], logarg, out svnLogs))
                            //        UpdateListView(svnLogs, (ListView)svnLogListViews[i]);
                            //}
                        }
                    });
                Thread.Sleep(5000);
            }
            redis.Quit();
        }
       
        private void UpdateListView(Collection<SvnLogEventArgs> svnLogs, ListView logListView)
        {
            if (logListView.Items.Count > 0)
            {
                Log firstItemInListView = (Log)logListView.Items.GetItemAt(0);
                for(int i = 0; i < 30; i ++)
                {

                    if (firstItemInListView.Revision == svnLogs[i].Revision) break; ;
                    logListView.Items.Insert(i, new Log(svnLogs[i]));
                    if (logListView.Items.Count > 30)
                    {
                        logListView.Items.RemoveAt(logListView.Items.Count - 1);
                    }
                }
            }
            else
            {
                foreach (var item in svnLogs)
                {
                    logListView.Items.Add(new Log(item));
                }
            }
            
        }

        private void UpdateWaitingTaskQueueListView()
        {
            RedisClient redis = new RedisClient(MainWindow.myRedisIP, MainWindow.myRedisPort, null, MainWindow.database);
            // 每次刷新等待任务队列
            while (running)
            {
                this.Dispatcher.BeginInvoke(
                (ThreadStart)delegate()
                {
                    // 先记录刷新之前的选中状态
                    int selectID = -1;
                    if (waitingTaskQueueListView.SelectedIndex != -1)
                    {
                        Task selectTask = (Task)waitingTaskQueueListView.SelectedItem;
                        if (selectTask != null)
                            selectID = selectTask.ID;
                    }
                    List<string> allWaitingTaskID = new List<string>();
                    allWaitingTaskID = redis.GetRangeFromList(RedisKeyName.waitingTaskList, 0, -1);
                    waitingTaskQueueListView.Items.Clear();
                    int index = 0;
                    foreach (var ID in allWaitingTaskID)
                    {
                        waitingTaskQueueListView.Items.Add(new Task(ID));
                        if (selectID == int.Parse(ID))
                        {
                            waitingTaskQueueListView.SelectedIndex = index;
                        }
                        index++;
                    }
                });
                Thread.Sleep(500);
            }
            redis.Quit();
        }

        // preBtn 和 preTime 记录上次点击的按钮及时间
        Button preBtn = null;
        int preTime = 0;
        // taskID 是redis中用来生成每条指令的ID
        const string taskID = "svn_assist:task:unique_id";
        private void OnBtnClick(object sender, RoutedEventArgs e)
        {
            Button thisBtn = (Button)sender;
            int nowTime = int.Parse(TimeStyle.ConvertDateTimeInt(thisWindowRedis.GetServerTime()).ToString());
            if (preBtn != null && thisBtn == preBtn && (nowTime - preTime < 3))
            {
                string warningMessage = "此命令与上一条命令重复，请确认是否添加！";
                MessageBoxResult rc = MessageBox.Show(warningMessage, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (rc == MessageBoxResult.No)
                {
                    return;
                }
            }
            preBtn = thisBtn;
            preTime = nowTime;
            string thisID = thisWindowRedis.Incr(taskID).ToString();

            List<string> thisTaskProperities = new List<string>();
            {
                thisTaskProperities.Add(MainWindow.myUsername);
                thisTaskProperities.Add((string)thisBtn.Tag);
                thisTaskProperities.Add("Waiting");
                thisTaskProperities.Add(TimeStyle.ConvertDateTimeInt(thisWindowRedis.GetServerTime().AddHours(8)).ToString());
                thisTaskProperities.Add("1");
            }
            for (int i = 0; i < thisTaskProperities.Count; i++)
            {
                thisWindowRedis.Set<string>(RedisKeyName.taskPrefix + thisID + taskProperities[i], thisTaskProperities[i]);
            }
            using (IRedisTransaction IRT = thisWindowRedis.CreateTransaction())
            {
                IRT.QueueCommand(r => r.AddItemToList(RedisKeyName.waitingTaskList, thisID));
                IRT.QueueCommand(r => r.AddItemToList(RedisKeyName.waitingTaskListCopy, thisID));
                IRT.Commit();
            }
        }

        private void OnCancleThisTask(object sender, RoutedEventArgs e)
        {
            if (waitingTaskQueueListView.SelectedItem != null)
            {
                Task selectedTask = (Task)waitingTaskQueueListView.SelectedItem;
                selectedTask.SetStateCancel();
                if (selectedTask == null) return;
                string thisID = selectedTask.ID.ToString();
                thisWindowRedis.Set<string>(RedisKeyName.taskPrefix + thisID + ":State", "Cancel");
            }


        }
        private void OnDisplayResult(object sender, RoutedEventArgs e)
        {
            if (finishedTaskQueueListView.SelectedItem != null)
            {
                Task selectedTask = (Task)finishedTaskQueueListView.SelectedItem;
                string time = TimeStyle.ConvertIntDateTime(double.Parse(selectedTask.Time.ToString())).ToString();
                string comInfo = thisWindowRedis.Get<string>(RedisKeyName.taskPrefix + selectedTask.ID.ToString() + ":ComInfo");
                ComInfoWindow ciw = new ComInfoWindow(selectedTask.Command, time, comInfo);
                ciw.Owner = this;
                ciw.Show();
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            my_close();
            base.OnClosed(e);
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            my_close();
            base.OnClosing(e);
        }

        private void my_close()
        {
            running = false;
        }

        private void OnSwitchBranch(object sender, RoutedEventArgs e)
        {
            //             WindowFile pw = new WindowFile();
            //             pw.PassValuesEvent += new WindowFile.PassValuesHandler(ReceiveValue);
            //             pw.Show();
            //             pw.Owner = this;
            //             this.IsEnabled = false;
            //             this.WindowStyle = WindowStyle.None;
            //             this.ResizeMode = ResizeMode.NoResize;

        }

        private void ReceiveValue(object sender, string message)
        {
            if (message == null) return;
            svnLogUris[2] = new Uri(message);
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView currentListView = sender as ListView;
            Log selectedLog = currentListView.SelectedItem as Log;
            if (selectedLog == null) return;
            
            ListView changedPaths = new ListView();
            TextBox message = new TextBox();
            for (int i = 0; i < svnLogListViews.Count; i++)
            {
                if (currentListView == svnLogListViews[i])
                {
                    changedPaths = changedPathsLisView[i] as ListView;
                    message = messagesTextBox[i] as TextBox;
                    break;
                }
            }
            if (changedPaths == null) return;
            message.Text = selectedLog.Message;
            changedPaths.Items.Clear();
            Collection<SvnChangeItem> allPaths = selectedLog.Paths;
            for (int i = 0; i < allPaths.Count; i++)
            {
                changedPaths.Items.Add(new ChangedPath(allPaths[i]));
            }
        }

        private void OnBack(object sender, RoutedEventArgs e)
        {
            SelectWindow select = new SelectWindow();
            select.Show();
            this.Close();
        }

        private void OnLoginOut(object sender, RoutedEventArgs e)
        {
            MainWindow main = new MainWindow();
            main.Show();
            this.Close();
        }

        private void waitingTaskQueueListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (waitingTaskQueueListView.SelectedIndex == -1 || waitingTaskQueueListView.SelectedItem == null)
            {
                CancleBtn.IsEnabled = false;
                return;
            }
            Task selectedTask = waitingTaskQueueListView.SelectedItem as Task;

            if (selectedTask == null)
            {
                CancleBtn.IsEnabled = false;
                return;
            }
            if (selectedTask.State != "Waiting")
            {
                CancleBtn.IsEnabled = false;
                return;
            }
            User selectUser = new User(selectedTask.Author);
            if (MainWindow.myAuthority > selectUser.Authority)
            {
                CancleBtn.IsEnabled = true;
                return;
            }
            if (selectedTask.Author == MainWindow.myUsername)
            {
                CancleBtn.IsEnabled = true;
                return;
            }
            CancleBtn.IsEnabled = false;
        }

        private void OnSwitchTo(object sender, RoutedEventArgs e)
        {
            string newID = thisWindowRedis.Incr(taskID).ToString();
            
            MenuItem selectMenuItem = sender as MenuItem;

            List<string> newTaskProperities = new List<string>();
            {
                newTaskProperities.Add(MainWindow.myUsername);
                newTaskProperities.Add(((MenuItem)selectMenuItem.Parent).Name);
                newTaskProperities.Add("Waiting");
                newTaskProperities.Add(TimeStyle.ConvertDateTimeInt(thisWindowRedis.GetServerTime().AddHours(8)).ToString());
                newTaskProperities.Add("2");
                newTaskProperities.Add(((Server)serverListView.SelectedItem).ServerID);
                newTaskProperities.Add(selectMenuItem.Tag.ToString());
            }
            for (int j = 0; j < newTaskProperities.Count; j++)
            {
                thisWindowRedis.Set<string>(RedisKeyName.taskPrefix + newID + taskProperities[j], newTaskProperities[j]);
            }
            using (IRedisTransaction IRT = thisWindowRedis.CreateTransaction())
            {
                IRT.QueueCommand(r => r.AddItemToList(RedisKeyName.waitingTaskList, newID));
                IRT.QueueCommand(r => r.AddItemToList(RedisKeyName.waitingTaskListCopy, newID));
                IRT.Commit();
            }
            
        }
        private void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            if(((MenuItem)sender).HasItems)
            {
                return;
            }
            if (serverListView.SelectedIndex == -1 || serverListView.SelectedItem == null) return;
            MenuItem selectMenuItem = sender as MenuItem;
            List<string> newTaskProperities = new List<string>();
            {
                newTaskProperities.Add(MainWindow.myUsername);
                newTaskProperities.Add(selectMenuItem.Name);
                newTaskProperities.Add("Waiting");
                DateTime nowTime = thisWindowRedis.GetServerTime();
                nowTime = nowTime.AddHours(8);
                long t = TimeStyle.ConvertDateTimeInt(nowTime);
                string t_ = t.ToString();
                newTaskProperities.Add(t_);
                
                newTaskProperities.Add("2");
                newTaskProperities.Add(((Server)serverListView.SelectedItem).ServerID);
                newTaskProperities.Add("");
            }

            string newID = thisWindowRedis.Incr(taskID).ToString();
            for (int j = 0; j < newTaskProperities.Count; j++)
            {
                thisWindowRedis.Set<string>(RedisKeyName.taskPrefix + newID + taskProperities[j], newTaskProperities[j]);
            }
            using (IRedisTransaction IRT = thisWindowRedis.CreateTransaction())
            {
                IRT.QueueCommand(r => r.AddItemToList(RedisKeyName.waitingTaskList, newID));
                IRT.QueueCommand(r => r.AddItemToList(RedisKeyName.waitingTaskListCopy, newID));
                IRT.Commit();
            }
        }

        private void serverListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCurrentBranchInServer();
        }
        private void UpdateCurrentBranchInServer()
        {
            string thisServerID = ((Server)serverListView.SelectedItem).ServerID;

            if (currentBranchOfServer.ContainsKey(thisServerID) &&
                currentBranchOfServer[thisServerID] >= 1 &&
                currentBranchOfServer[thisServerID] <= 4)
            {
                string currentSelect = thisWindowRedis.Get<string>(
                    RedisKeyName.binTagPrefix +
                    (currentBranchOfServer[thisServerID] - 1).ToString() + ":name");

                MenuItem currentSelectBranch = FindChildByName(SMenu, currentSelect);
                if (currentSelectBranch != null)
                {
                    foreach (MenuItem item in ((MenuItem)currentSelectBranch.Parent).Items)
                    {
                        item.IsEnabled = true;
                    }
                    currentSelectBranch.IsEnabled = false;
                }
            }
        }

    }
}
