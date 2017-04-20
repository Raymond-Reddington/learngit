using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using SharpSvn;
using System.Collections.ObjectModel;

namespace svn_assist_client
{
    /// <summary>
    /// SvnLoginInWindow.xaml 的交互逻辑
    /// </summary>
    public partial class SvnLoginInWindow : Window
    {
        SvnTarget target;
        public SvnLoginInWindow(SvnTarget uri)
        {
            InitializeComponent();
            this.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            target = uri;
        }

        private void OnSvnVerify(object sender, RoutedEventArgs e)
        {
            Collection<SvnListEventArgs> list = new Collection<SvnListEventArgs>();
            try
            {
                SvnClient svn = new SvnClient();
                svn.Authentication.UserNamePasswordHandlers += new EventHandler<SharpSvn.Security.SvnUserNamePasswordEventArgs>(
                    delegate(Object s, SharpSvn.Security.SvnUserNamePasswordEventArgs e1)
                    {
                        e1.UserName = usernameTextBox.Text;
                        e1.Password = passwordPasswordBox.Password;
                    });
                svn.GetList(target, out list);
                svn.Dispose();
                GlobalVariable.svnUserName = usernameTextBox.Text;
                GlobalVariable.svnPassword =passwordPasswordBox.Password;
                if(this.Owner is WindowFile)
                {
                    ((WindowFile)this.Owner).My_Load();
                }
                else if(this.Owner is CompileToolWindow)
                {
                    ((CompileToolWindow)this.Owner).DrawSvnLog();
                }
                this.Close();
            }
            catch (Exception)
            {
                MessageBox.Show("用户名或密码错误", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
