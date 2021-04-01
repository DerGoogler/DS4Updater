﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Windows;
using System.Windows.Shell;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Linq;

namespace Updater2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string CUSTOM_EXE_CONFIG_FILENAME = "custom_exe_name.txt";
        WebClient wc = new WebClient(), subwc = new WebClient();
        protected string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\DS4Windows";
        string exepath = Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName;
        string version = "0", newversion = "0";
        bool downloading = false;
        private int round = 1;
        public bool downloadLang = false;
        private bool backup;
        private string outputUpdatePath = "";
        private string updatesFolder = "";
        public bool autoLaunchDS4W = false;
        public bool forceLaunchDS4WUser = false;
        internal string arch = Environment.Is64BitProcess ? "x64" : "x86";
        private string custom_exe_name_path;
        public string CustomExeNamePath { get => custom_exe_name_path; }

        [DllImport("Shell32.dll")]
        private static extern int SHGetKnownFolderPath(
        [MarshalAs(UnmanagedType.LPStruct)]Guid rfid, uint dwFlags, IntPtr hToken,
        out IntPtr ppszPath);

        public bool AdminNeeded()
        {
            try
            {
                File.WriteAllText(exepath + "\\test.txt", "test");
                File.Delete(exepath + "\\test.txt");
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return true;
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            if (File.Exists(exepath + "\\DS4Windows.exe"))
                version = FileVersionInfo.GetVersionInfo(exepath + "\\DS4Windows.exe").FileVersion;

            if (AdminNeeded())
                label1.Content = "Please re-run with admin rights";
            else
            {
                custom_exe_name_path = Path.Combine(exepath, CUSTOM_EXE_CONFIG_FILENAME);

                try
                {
                    string[] files = Directory.GetFiles(exepath);

                    for (int i = 0, arlen = files.Length; i < arlen; i++)
                    {
                        string tempFile = Path.GetFileName(files[i]);
                        if (new Regex(@"DS4Windows_[\w.]+_\w+.zip").IsMatch(tempFile))
                        {
                            File.Delete(files[i]);
                        }
                    }

                    if (Directory.Exists(exepath + "\\Update Files"))
                        Directory.Delete(exepath + "\\Update Files", true);

                    if (!Directory.Exists(Path.Combine(exepath, "Updates")))
                        Directory.CreateDirectory(Path.Combine(exepath, "Updates"));

                    updatesFolder = Path.Combine(exepath, "Updates");
                }
                catch (IOException) { label1.Content = "Cannot save download at this time"; return; }

                if (File.Exists(exepath + "\\Profiles.xml"))
                    path = exepath;

                if (File.Exists(path + "\\version.txt"))
                {
                    newversion = File.ReadAllText(path + "\\version.txt");
                    newversion = newversion.Trim();
                }
                else if (File.Exists(exepath + "\\version.txt"))
                {
                    newversion = File.ReadAllText(exepath + "\\version.txt");
                    newversion = newversion.Trim();
                }
                else
                {
                    Uri urlv = new Uri("https://raw.githubusercontent.com/Ryochan7/DS4Windows/jay/DS4Windows/newest.txt");
                    //Sorry other devs, gonna have to find your own server
                    WebClient wc2 = new WebClient();
                    downloading = true;
                    subwc.DownloadFileAsync(urlv, exepath + "\\version.txt");
                    subwc.DownloadFileCompleted += subwc_DownloadFileCompleted;
                    label1.Content = "Getting Update info";
                }

                if (!downloading && version.Replace(',', '.').CompareTo(newversion) != 0)
                {
                    Uri url = new Uri($"https://github.com/Ryochan7/DS4Windows/releases/download/v{newversion}/DS4Windows_{newversion}_{arch}.zip");
                    sw.Start();
                    outputUpdatePath = Path.Combine(updatesFolder, $"DS4Windows_{newversion}_{arch}.zip");
                    try { wc.DownloadFileAsync(url, outputUpdatePath); }
                    catch (Exception e) { label1.Content = e.Message; }
                    wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                    wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                }
                else if (!downloading)
                {
                    label1.Content = "DS4Windows is up to date";
                    try
                    {
                        File.Delete(path + "\\version.txt");
                        File.Delete(exepath + "\\version.txt");
                    }
                    catch { }
                    btnOpenDS4.IsEnabled = true;
                }
            }
        }

        void subwc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            newversion = File.ReadAllText(exepath + "\\version.txt");
            newversion = newversion.Trim();
            File.Delete(exepath + "\\version.txt");
            if (version.Replace(',', '.').CompareTo(newversion) != 0)
            {
                Uri url = new Uri($"https://github.com/Ryochan7/DS4Windows/releases/download/v{newversion}/DS4Windows_{newversion}_{arch}.zip");
                sw.Start();
                outputUpdatePath = Path.Combine(updatesFolder, $"DS4Windows_{newversion}_{arch}.zip");
                try { wc.DownloadFileAsync(url, outputUpdatePath); }
                catch (Exception ec) { label1.Content = ec.Message; }
                wc.DownloadFileCompleted += wc_DownloadFileCompleted;
                wc.DownloadProgressChanged += wc_DownloadProgressChanged;
            }
            else
            {
                label1.Content = "DS4Windows is up to date";
                try
                {
                    File.Delete(path + "\\version.txt");
                    File.Delete(exepath + "\\version.txt");
                }
                catch { }

                if (autoLaunchDS4W)
                {
                    label1.Content = "Launching DS4Windows soon";
                    btnOpenDS4.IsEnabled = false;
                    Task.Delay(5000).ContinueWith((t) =>
                    {
                        PrepareAutoOpenDS4();
                    });
                }
                else
                {
                    btnOpenDS4.IsEnabled = true;
                }
            }
        }

        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        Stopwatch sw = new Stopwatch();

        private void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            label2.Opacity = 1;
            double speed = e.BytesReceived / sw.Elapsed.TotalSeconds;
            double timeleft = (e.TotalBytesToReceive - e.BytesReceived) / speed;
            if (timeleft > 3660)
                label2.Content = (int)timeleft / 3600 + "h left";
            else if (timeleft > 90)
                label2.Content = (int)timeleft / 60 + "m left";
            else
                label2.Content = (int)timeleft + "s left";

            UpdaterBar.Value = e.ProgressPercentage;
            TaskbarItemInfo.ProgressValue = UpdaterBar.Value / 106d;
            string convertedrev, convertedtotal;
            if (e.BytesReceived > 1024 * 1024 * 5) convertedrev = (int)(e.BytesReceived / 1024d / 1024d) + "MB";
            else convertedrev = (int)(e.BytesReceived / 1024d) + "kB";

            if (e.TotalBytesToReceive > 1024 * 1024 * 5) convertedtotal = (int)(e.TotalBytesToReceive / 1024d / 1024d) + "MB";
            else convertedtotal = (int)(e.TotalBytesToReceive / 1024d) + "kB";

            if (round == 1) label1.Content = "Downloading update: " + convertedrev + " / " + convertedtotal;
            else label1.Content = "Downloading Laugauge Pack: " + convertedrev + " / " + convertedtotal;
        }

        private void wc_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            sw.Reset();
            string lang = CultureInfo.CurrentCulture.ToString();

            if (new FileInfo(outputUpdatePath).Length > 0)
            {
                Process[] processes = Process.GetProcessesByName("DS4Windows");
                label1.Content = "Download Complete";
                if (processes.Length > 0)
                {
                    if (MessageBox.Show("It will be closed to continue this update.", "DS4Windows is still running", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation) == MessageBoxResult.OK)
                    {
                        label1.Content = "Terminating DS4Windows";
                        foreach (Process p in processes)
                        {
                            if (!p.HasExited)
                            {
                                try
                                {
                                    p.Kill();
                                }
                                catch { }
                            }
                        }

                        System.Threading.Thread.Sleep(5000);
                    }
                    else
                    {
                        this.Close();
                        return;
                    }
                }

                while (processes.Length > 0)
                {
                    label1.Content = "Waiting for DS4Windows to close";
                    processes = Process.GetProcessesByName("DS4Windows");
                    System.Threading.Thread.Sleep(10);
                }

                label2.Opacity = 0;
                label1.Content = "Deleting old files";
                UpdaterBar.Value = 102;
                TaskbarItemInfo.ProgressValue = UpdaterBar.Value / 106d;

                string libsPath = Path.Combine(exepath, "libs");
                string oldLibsPath = Path.Combine(exepath, "oldlibs");

                // Grab relative file paths to DLL files in the current install
                string[] oldDLLFiles = Directory.GetDirectories(exepath, "*.dll", SearchOption.AllDirectories);
                for (int i = oldDLLFiles.Length - 1; i >= 0; i--)
                {
                    oldDLLFiles[i] = oldDLLFiles[i].Replace($"{exepath}", "");
                }

                try
                {
                    // Temporarily move existing libs folder
                    if (Directory.Exists(libsPath))
                    {
                        Directory.Move(libsPath, oldLibsPath);
                    }

                    string[] checkFiles = new string[]
                    {
                        exepath + "\\DS4Windows.exe",
                        exepath + "\\DS4Tool.exe",
                        exepath + "\\DS4Control.dll",
                        exepath + "\\DS4Library.dll",
                        exepath + "\\HidLibrary.dll",
                    };

                    foreach(string checkFile in checkFiles)
                    {
                        if (File.Exists(checkFile))
                        {
                            File.Delete(checkFile);
                        }
                    }

                    string updateFilesDir = exepath + "\\Update Files";
                    if (Directory.Exists(updateFilesDir))
                    {
                        Directory.Delete(updateFilesDir);
                    }

                    string[] updatefiles = Directory.GetFiles(exepath);
                    for (int i = 0, arlen = updatefiles.Length; i < arlen; i++)
                    {
                        if (Path.GetExtension(updatefiles[i]) == ".ds4w")
                            File.Delete(updatefiles[i]);
                    }
                }
                catch { }

                label1.Content = "Installing new files";
                UpdaterBar.Value = 104;
                TaskbarItemInfo.ProgressValue = UpdaterBar.Value / 106d;

                try
                {
                    Directory.CreateDirectory(exepath + "\\Update Files");
                    ZipFile.ExtractToDirectory(outputUpdatePath, exepath + "\\Update Files");
                }
                catch (IOException) { }

                try
                {
                    File.Delete(exepath + "\\version.txt");
                    File.Delete(path + "\\version.txt");
                }
                catch { }

                string[] directories = Directory.GetDirectories(exepath + "\\Update Files\\DS4Windows", "*", SearchOption.AllDirectories);
                for (int i = directories.Length - 1; i >= 0; i--)
                {
                    string relativePath = directories[i].Replace($"{exepath}\\Update Files\\DS4Windows\\", "");
                    string tempDestPath = Path.Combine(exepath, relativePath);
                    if (!Directory.Exists(tempDestPath))
                    {
                        Directory.CreateDirectory(tempDestPath);
                    }
                }

                // Grab relative file paths to DLL files in the newer install
                string[] newDLLFiles = Directory.GetFiles(exepath + "\\Update Files\\DS4Windows", "*.dll", SearchOption.AllDirectories);
                for (int i = newDLLFiles.Length - 1; i >= 0; i--)
                {
                    newDLLFiles[i] = newDLLFiles[i].Replace($"{exepath}\\Update Files\\DS4Windows\\", "");
                }

                string[] files = Directory.GetFiles(exepath + "\\Update Files\\DS4Windows", "*", SearchOption.AllDirectories);
                for (int i = files.Length - 1; i >= 0; i--)
                {
                    if (Path.GetFileNameWithoutExtension(files[i]) != "DS4Updater")
                    {
                        string relativePath = files[i].Replace($"{exepath}\\Update Files\\DS4Windows\\", "");
                        string tempDestPath = Path.Combine(exepath, relativePath);
                        //string tempDestPath = $"{exepath}\\{Path.GetFileName(files[i])}";
                        if (File.Exists(tempDestPath))
                        {
                            File.Delete(tempDestPath);
                        }

                        File.Move(files[i], tempDestPath);
                    }
                }

                // Delete old libs folder
                if (Directory.Exists(oldLibsPath))
                {
                    Directory.Delete(oldLibsPath, true);
                }

                // Remove unused DLLs (in main app folder) from previous install
                string[] excludedDLLs = oldDLLFiles.Except(newDLLFiles).ToArray();
                foreach (string dllFile in excludedDLLs)
                {
                    if (File.Exists(dllFile))
                    {
                        File.Delete(dllFile);
                    }
                }

                string ds4winversion = FileVersionInfo.GetVersionInfo(exepath + "\\DS4Windows.exe").FileVersion;
                if ((File.Exists(exepath + "\\DS4Windows.exe") || File.Exists(exepath + "\\DS4Tool.exe")) &&
                    ds4winversion == newversion.Trim())
                {
                    //File.Delete(exepath + $"\\DS4Windows_{newversion}_{arch}.zip");
                    //File.Delete(exepath + "\\" + lang + ".zip");
                    label1.Content = $"DS4Windows has been updated to v{newversion}";
                }
                else if (File.Exists(exepath + "\\DS4Windows.exe") || File.Exists(exepath + "\\DS4Tool.exe"))
                {
                    label1.Content = "Could not replace DS4Windows, please manually unzip";
                }
                else
                    label1.Content = "Could not unpack zip, please manually unzip";

                // Check for custom exe name setting
                string custom_exe_name_path = Path.Combine(exepath, CUSTOM_EXE_CONFIG_FILENAME);
                bool fakeExeFileExists = File.Exists(custom_exe_name_path);
                if (fakeExeFileExists)
                {
                    string fake_exe_name = File.ReadAllText(custom_exe_name_path).Trim();
                    bool valid = !string.IsNullOrEmpty(fake_exe_name) && !(fake_exe_name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0);
                    // Attempt to copy executable and assembly config file
                    if (valid)
                    {
                        string current_exe_location = Path.Combine(exepath, "DS4Windows.exe");
                        string current_conf_file_path = Path.Combine(exepath, "DS4Windows.exe.config");

                        string fake_exe_file = Path.Combine(exepath, $"{fake_exe_name}.exe");
                        string fake_conf_file = Path.Combine(exepath, $"{fake_exe_name}.exe.config");

                        File.Copy(current_exe_location, fake_exe_file, true);
                        File.Copy(current_conf_file_path, fake_conf_file, true);
                    }
                }

                UpdaterBar.Value = 106;
                TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;

                if (autoLaunchDS4W)
                {
                    label1.Content = "Launching DS4Windows soon";
                    btnOpenDS4.IsEnabled = false;
                    Task.Delay(5000).ContinueWith((t) =>
                    {
                        PrepareAutoOpenDS4();
                    });
                }
                else
                {
                    btnOpenDS4.IsEnabled = true;
                }
            }
            else if (!backup)
            {
                Uri url = new Uri($"https://github.com/Ryochan7/DS4Windows/releases/download/v{newversion}/DS4Windows_{newversion}_{arch}.zip");

                sw.Start();
                outputUpdatePath = Path.Combine(updatesFolder, $"DS4Windows_{newversion}_{arch}.zip");
                try { wc.DownloadFileAsync(url, outputUpdatePath); }
                catch (Exception ex) { label1.Content = ex.Message; }
                backup = true;
            }
            else
            {
                label1.Content = "Could not download update";
                try
                {
                    File.Delete(exepath + "\\version.txt");
                    File.Delete(path + "\\version.txt");
                }
                catch { }
                btnOpenDS4.IsEnabled = true;
            }
        }

        private void BtnChangelog_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo("https://docs.google.com/document/d/1CovpH08fbPSXrC6TmEprzgPwCe0tTjQ_HTFfDotpmxk/edit?usp=sharing");
            startInfo.UseShellExecute = true;
            try
            {
                using (Process tempProc = Process.Start(startInfo))
                {
                }
            }
            catch { }
        }

        private void BtnOpenDS4_Click(object sender, RoutedEventArgs e)
        {
            if (File.Exists(exepath + "\\DS4Windows.exe"))
                Process.Start(exepath + "\\DS4Windows.exe");
            else
                Process.Start(exepath);

            App.openingDS4W = true;
            this.Close();
        }

        private void PrepareAutoOpenDS4()
        {
            App.openingDS4W = true;
            Dispatcher.BeginInvoke((Action)(() =>
            {
                this.Close();
            }));
        }
    }
}

