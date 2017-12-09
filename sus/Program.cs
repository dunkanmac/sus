using System;
using System.ServiceProcess;
using System.Windows.Forms;

namespace SUS
{
    static class Program
    {
        static void Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    if (Environment.UserInteractive)
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        Application.Run(new SuApp());
                    }
                    else
                    {
                        ServiceBase.Run(new SuService());
                    }
                    return;
                }
                else if (args.Length == 1)
                {
                    string arg = args[0].ToLower();

                    if (arg == "-start")
                    {
                        SuManager.StartService();
                        return;
                    }
                    else if (arg == "-stop")
                    {
                        SuManager.StopService();
                        return;
                    }
                    else if (arg == "-i")
                    {
                        SuManager.InstallService();
                        return;
                    }
                    else if (arg == "-u")
                    {
                        SuManager.UninstallService();
                        return;
                    }
                    else if (arg == "-auto")
                    {
                        SuManager.SetAutoStart(false);
                        return;
                    }
                    else if (arg == "+auto")
                    {
                        SuManager.SetAutoStart(true);
                        return;
                    }
                }

                MessageBox.Show($"Command aruments:\n-help : Show help\n-start : Start service\n-stop : Stop service\n-i : Install service\n-u : Uninstall service",
                    "Switch User Application",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Switch User Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
