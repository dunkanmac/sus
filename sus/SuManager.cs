using Microsoft.Win32;
using System;
using System.Collections;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;

namespace SUS
{
    public class SuManager
    {
        private const int BYTES_TO_READ = sizeof(Int64);
        private const string APP_NAME = "Switch User Tool";
        private const string REG_AUTORUN = @"Software\Microsoft\Windows\CurrentVersion\Run";

        public const string SERVICE_NAME = "SUS";
        public const string SERVICE_DISPLAY_NAME = "Switch User Service";
        public const int SWITCH_USER_COMMAND = 193;
        public const uint WM_HOTKEY = 0x312;
        public const uint ERROR_HOTKEY_ALREADY_REGISTERED = 1409;

        private enum WTS_CONNECTSTATE_CLASS
        {
            WTSActive,
            WTSConnected,
            WTSConnectQuery,
            WTSShadow,
            WTSDisconnected,
            WTSIdle,
            WTSListen,
            WTSReset,
            WTSDown,
            WTSInit
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct WTS_SESSION_INFO
        {
            public Int32 SessionId;
            [MarshalAs(UnmanagedType.LPStr)]
            public string pWinStationName;
            public WTS_CONNECTSTATE_CLASS State;
        }

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto)]
        private static extern bool WTSEnumerateSessions(IntPtr hServer,
            [MarshalAs(UnmanagedType.U4)]
            Int32 Reserved,
            [MarshalAs(UnmanagedType.U4)]
            Int32 Version,
            ref IntPtr ppSessionInfo,
            [MarshalAs(UnmanagedType.U4)]
            ref Int32 pCount);

        [DllImport("wtsapi32.dll")]
        private static extern void WTSFreeMemory(IntPtr pMemory);

        [DllImport("wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSDisconnectSession(IntPtr hServer, int sessionId, bool bWait);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int WTSGetActiveConsoleSessionId();

        [DllImport("wtsapi32.dll", CharSet = CharSet.Auto)]
        private static extern bool WTSConnectSession(UInt64 TargetSessionId, UInt64 SessionId, string pPassword, bool bWait);

        [DllImport("user32", SetLastError = true)]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, Keys vk);

        [DllImport("user32", SetLastError = true)]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private static readonly IntPtr WTS_CURRENT_SERVER_HANDLE = IntPtr.Zero;

        public static bool IsAutoStart
        {
            get
            {
                bool autoStart = false;

                RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).OpenSubKey(REG_AUTORUN, false);

                if (key != null)
                {
                    object value = key.GetValue(APP_NAME, null);//%PROGRAMDATA%
                    autoStart = (value != null && StringComparer.OrdinalIgnoreCase.Compare(value.ToString(), ServiceAppPath) == 0);
                }

                return autoStart;
            }
        }

        private static bool IsAdministrator
        {
            get
            {
                bool isAdmin;

                try
                {
                    using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                    {
                        WindowsPrincipal principal = new WindowsPrincipal(identity);
                        isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    isAdmin = false;
                }
                catch (Exception)
                {
                    isAdmin = false;
                }
                return isAdmin;
            }
        }

        private static bool IsRestartNeeded
        {
            get
            {
                return StringComparer.OrdinalIgnoreCase.Compare(Application.ExecutablePath, ServiceAppPath) != 0;
            }
        }

        private static string ServiceAppPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),  $"SUS\\SUS.exe");
            }
        }

        private static bool FilesAreEqual(string firstPath, string secondPath)
        {
            FileInfo first = new FileInfo(firstPath);
            FileInfo second = new FileInfo(secondPath);

            if (first.Length != second.Length)
                return false;

            int iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[BYTES_TO_READ];
                byte[] two = new byte[BYTES_TO_READ];

                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BYTES_TO_READ);
                    fs2.Read(two, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return false;
                }
            }

            return true;
        }

        private static string getHashMD5(string value, byte[] salt)
        {
            using (MD5CryptoServiceProvider algoritm = new MD5CryptoServiceProvider())
            {
                return getHash(value, salt, algoritm);
            }
        }

        private static string getHash(string value, byte[] salt, HashAlgorithm algoritm)
        {
            return String.Join(string.Empty, algoritm.ComputeHash(salt.Concat(Encoding.UTF8.GetBytes(value + "HASH")).ToArray()).Select(item => item.ToString("X2")));
        }

        public static bool IsExistService(bool throwEx = false)
        {
            ServiceController[] services = ServiceController.GetServices();
            var service = services.FirstOrDefault(s => s.ServiceName == SERVICE_NAME);
            bool exist = service != null;

            if (throwEx && !exist)
            {
                using (ServiceController sc = new ServiceController(SERVICE_NAME))
                {
                    string name = sc.DisplayName;
                }

                throw new Exception("Service not exist");
            }

            return exist;

            //try
            //{
            //    using (ServiceController sc = new ServiceController(SERVICE_NAME))
            //    {
            //        string name = sc.DisplayName;
            //    }
            //    return true;
            //}
            //catch (Exception)
            //{
            //    if (throwEx)
            //        throw;

            //    return false;
            //}
        }

        public static bool StartService()
        {
            try
            {
                using (ServiceController sc = new ServiceController(SERVICE_NAME))
                {

                    if (sc.Status == ServiceControllerStatus.Stopped)
                        sc.Start();

                    sc.WaitForStatus(ServiceControllerStatus.Running);
                }
            }
            catch (Exception ex)
            {
                if (!IsAdministrator)
                    return StartApp(false, "-start");

                MessageBox.Show(ex.Message, "Switch User Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            MessageBox.Show("Service start successfuly", "Switch User Application", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return true;
        }

        public static bool StopService()
        {
            try
            {
                using (ServiceController sc = new ServiceController(SERVICE_NAME))
                {

                    if (sc.Status == ServiceControllerStatus.Running)
                        sc.Stop();

                    sc.WaitForStatus(ServiceControllerStatus.Stopped);
                }
            }
            catch (Exception ex)
            {
                if (!IsAdministrator)
                    return StartApp(false, "-stop");

                MessageBox.Show(ex.Message, "Switch User Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            MessageBox.Show("Service stop successfuly", "Switch User Application", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return true;
        }

        public static bool StartApp(bool isServicePath, params string[] args)
        {
            try
            {
                ProcessStartInfo proc = new ProcessStartInfo();
                proc.UseShellExecute = true;

                if (isServicePath)
                {
                    bool copy = true;

                    if (File.Exists(ServiceAppPath))
                    {
                        if (FilesAreEqual(ServiceAppPath, Application.ExecutablePath))
                            copy = false;
                        else
                            File.Delete(ServiceAppPath);
                    }
                    else
                    {
                        string dir = Path.GetDirectoryName(ServiceAppPath);

                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                    }

                    if (copy)
                        File.Copy(Application.ExecutablePath, ServiceAppPath);
                }
                else
                {
                    proc.WorkingDirectory = Environment.CurrentDirectory;
                    proc.FileName = Application.ExecutablePath;
                }

                proc.WorkingDirectory = Path.GetDirectoryName(ServiceAppPath);
                proc.FileName = ServiceAppPath;

                foreach (string arg in args)
                {
                    proc.Arguments += String.Format("\"{0}\" ", arg);
                }

                proc.Verb = "runas";

                var process = Process.Start(proc);
                process.WaitForExit(5000);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Admin rights needed");
                MessageBox.Show(ex.Message, "Switch User Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool InstallService()
        {
            try
            {
                if (!IsAdministrator || IsRestartNeeded)
                    return StartApp(true, "-i");

                if (IsExistService(false))
                    return false;

                using (AssemblyInstaller inst = new AssemblyInstaller(typeof(Program).Assembly, new string[] { }))
                {
                    IDictionary state = new Hashtable();
                    inst.UseNewContext = true;
                    try
                    {
                        inst.Install(state);
                        inst.Commit(state);
                    }
                    catch
                    {
                        try
                        {
                            inst.Rollback(state);
                        }
                        catch { }
                        throw;
                    }
                }

                MessageBox.Show("Service installed successfuly", "Switch User Application", MessageBoxButtons.OK, MessageBoxIcon.Information);

                StartService();

                SetAutoStart(true);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Switch User Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool UninstallService()
        {
            try
            {
                if (!IsAdministrator || IsRestartNeeded)
                    return StartApp(true, "-u");

                SetAutoStart(false);

                if (!IsExistService(true))
                    return false;

                StopService();

                using (AssemblyInstaller inst = new AssemblyInstaller(typeof(Program).Assembly, new string[] { }))
                {
                    IDictionary state = new Hashtable();
                    inst.UseNewContext = true;
                    try
                    {
                        inst.Uninstall(state);
                    }
                    catch
                    {
                        try
                        {
                            inst.Rollback(state);
                        }
                        catch { }
                        throw;
                    }
                }

                MessageBox.Show("Service uninstalled successfuly", "Switch User Application", MessageBoxButtons.OK, MessageBoxIcon.Information);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Switch User Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool ServiceSwitchUser()
        {
            try
            {
                using (ServiceController sc = new ServiceController(SERVICE_NAME))
                {
                    sc.ExecuteCommand(SuManager.SWITCH_USER_COMMAND);
                    return true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Switch User Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static bool SetAutoStart(bool autoStart)
        {
            try
            {
                if (!IsAdministrator || IsRestartNeeded)
                    return StartApp(true, autoStart ? "+auto" : "-auto");

                RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default).OpenSubKey(REG_AUTORUN, true);

                if (key != null)
                {
                    if (autoStart)
                        key.SetValue(APP_NAME, ServiceAppPath);
                    else
                        key.DeleteValue(APP_NAME, false);
                }

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Switch User Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public static void SwitchUser()
        {
            IntPtr buffer = IntPtr.Zero;

            try
            {
                Int32 sessionCount = 0;
                Int32 dataSize = Marshal.SizeOf(typeof(WTS_SESSION_INFO));

                if (WTSEnumerateSessions(WTS_CURRENT_SERVER_HANDLE, 0, 1, ref buffer, ref sessionCount))
                {
                    WTS_SESSION_INFO[] sessionInfo = new WTS_SESSION_INFO[sessionCount];
                    IntPtr currentSession = buffer;

                    for (var index = 0; index < sessionCount; index++)
                    {
                        sessionInfo[index] = (WTS_SESSION_INFO)Marshal.PtrToStructure(currentSession, typeof(WTS_SESSION_INFO));
                        currentSession += dataSize;
                    }

                    int activeSessId = -1;
                    int targetSessId = -1;

                    for (var i = 1; i < sessionCount; i++)
                    {
                        if (sessionInfo[i].State == WTS_CONNECTSTATE_CLASS.WTSDisconnected)
                            targetSessId = sessionInfo[i].SessionId;
                        else if (sessionInfo[i].State == WTS_CONNECTSTATE_CLASS.WTSActive)
                            activeSessId = sessionInfo[i].SessionId;
                    }

                    if ((activeSessId > 0) && (targetSessId > 0))
                    {
                        WTSConnectSession(Convert.ToUInt64(targetSessId), Convert.ToUInt64(activeSessId), "", false);
                    }
                    else
                    {
                        WTSDisconnectSession(WTS_CURRENT_SERVER_HANDLE, activeSessId, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Switch User Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                    WTSFreeMemory(buffer);
            }
        }
    }
}
