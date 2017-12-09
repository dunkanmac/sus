using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace SUS
{
    public class SuApp : ApplicationContext
    {
        //private HotkeyFilter hk;
        //private Form form;
        private NotifyIcon appIcon;
        private ToolStripMenuItem miAutoStart = null;

        public SuApp()
        {
            //string hotkey = Properties.Settings.Default.HotKey.ToUpper();

            //if (string.IsNullOrWhiteSpace(hotkey))
            //    throw new Exception("Hotkey is empty");

            //var m = Regex.Match(hotkey, @"[\w]+\+(?<key>[\w\W])");

            //if (!m.Success)
            //    throw new Exception("Hotkey is invalid: " + hotkey);

            //bool isAlt = hotkey.Contains("ALT");
            //bool isControl = hotkey.Contains("CTRL");
            //bool isShift = hotkey.Contains("SHIFT");
            //bool isWin = hotkey.Contains("WIN");

            //KeyModifiers modifiers = ((isAlt ? KeyModifiers.Alt : 0) | (isControl ? KeyModifiers.Control : 0) |
            //(isShift ? KeyModifiers.Shift : 0) | (isWin ? KeyModifiers.Windows : 0) /*| KeyModifiers.NoRepeat*/);

            //Keys k = (Keys)Enum.Parse(typeof(Keys), m.Groups["key"].Value, true);

            //HotKeyManager.RegisterHotKey(k, modifiers);

            try
            {
                HotKeyManager.RegisterHotKey(Keys.Z, KeyModifiers.Alt | KeyModifiers.Shift);
            }
            catch (Exception)
            {
            }

            HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_HotKeyPressed);

            ContextMenuStrip cm = new ContextMenuStrip();

            ToolStripMenuItem miSwitchUser = new ToolStripMenuItem("Switch user", null, miSwitchUser_Click);
            cm.Items.Add(miSwitchUser);

            cm.Items.Add(new ToolStripSeparator());

            miAutoStart = new ToolStripMenuItem("Auto start", null, miAutoStart_Click);

            ToolStripMenuItem miInstall = new ToolStripMenuItem("Install service", null, miInstall_Click);
            ToolStripMenuItem miUninstall = new ToolStripMenuItem("Uninstall service", null, miUninstall_Click);

            ToolStripMenuItem miStart = new ToolStripMenuItem("Start service", null, miStart_Click);
            ToolStripMenuItem miStop = new ToolStripMenuItem("Stop service", null, miStop_Click);

            ToolStripMenuItem miOptions = new ToolStripMenuItem("Options", null);
            miOptions.DropDownItems.Add(miAutoStart);
            miOptions.DropDownItems.Add(new ToolStripSeparator());
            miOptions.DropDownItems.Add(miInstall);
            miOptions.DropDownItems.Add(miUninstall);
            miOptions.DropDownItems.Add(new ToolStripSeparator());
            miOptions.DropDownItems.Add(miStart);
            miOptions.DropDownItems.Add(miStop);
            cm.Items.Add(miOptions);

            cm.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem miExit = new ToolStripMenuItem("Exit", null, miExit_Click);
            cm.Items.Add(miExit);

            this.appIcon = new NotifyIcon();
            this.appIcon.ContextMenuStrip = cm;
            this.appIcon.Icon = Properties.Resources.users;

            //var iconHandle = Properties.Resources.users1.GetHicon();
            //this.appIcon.Icon = System.Drawing.Icon.FromHandle(iconHandle);
            //FileStream fs = new FileStream("D:\\u.ico", FileMode.CreateNew);
            //this.appIcon.Icon.Save(fs);
            //fs.Close();

            this.appIcon.Text = $"Switch Users Application [{Environment.UserName}]";
            this.appIcon.Visible = true;
            this.appIcon.MouseClick += appIcon_MouseClick;
            this.appIcon.MouseDoubleClick += appIcon_MouseDoubleClick;

            //form = new Form();

            //hk.Pressed += SendSwitchCommand;

            //if (hk.GetCanRegister(form))
            //{
            //    hk.Register(form);
            //}

            //Application.ApplicationExit += Application_ApplicationExit;

            if (!SuManager.IsExistService(false))
            {
                Application.Idle += Application_Idle;
            }
        }

        private void Application_Idle(object sender, EventArgs e)
        {
            Application.Idle -= Application_Idle;

            if (MessageBox.Show("Switch User Service not installed. Do you want install service?", "Switch Users Application", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) == DialogResult.Cancel)
            {
                Application.Exit();
                return;
            }

            if (!SuManager.StartApp(true, "-i"))
            { 
                Application.Exit();
                return;
            }
        }

        protected override void ExitThreadCore()
        {
            if (appIcon != null)
            {
                appIcon.Visible = false;
                appIcon.Dispose();
                appIcon = null;
            }

            base.ExitThreadCore();
        }

        private void appIcon_MouseClick(object sender, MouseEventArgs e)
        {
            miAutoStart.Checked = SuManager.IsAutoStart;

            if (e.Button == MouseButtons.Left)
                SuManager.ServiceSwitchUser();
        }

        private void appIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            SuManager.ServiceSwitchUser();
        }

        private void miInstall_Click(object sender, EventArgs e)
        {
            SuManager.InstallService();
        }

        private void miUninstall_Click(object sender, EventArgs e)
        {
            SuManager.UninstallService();
        }

        private void miStart_Click(object sender, EventArgs e)
        {
            SuManager.StartService();
        }

        private void miStop_Click(object sender, EventArgs e)
        {
            SuManager.StopService();
        }

        private void miAutoStart_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            bool autoStart = SuManager.IsAutoStart;
            if (SuManager.SetAutoStart(!autoStart))
                item.Checked = SuManager.IsAutoStart;
        }
        
        private void miSwitchUser_Click(object sender, EventArgs e)
        {
            SuManager.ServiceSwitchUser();
        }

        private void miExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e)
        {
            SuManager.ServiceSwitchUser();
        }

        //private void SendSwitchCommand(object sender, HandledEventArgs e)
        //{
        //    if (!e.Handled)
        //        SuManager.ServiceSwitchUser();
        //}

        //void Application_ApplicationExit(object sender, EventArgs e)
        //{
        //    if (hk.Registered)
        //        hk.Unregister();
        //}
    }
}
