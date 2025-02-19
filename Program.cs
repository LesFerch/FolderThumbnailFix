using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading;

namespace FolderThumbnailFix
{
    class Program
    {
        static string myName = typeof(Program).Namespace;
        static string myPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        static string myExe = System.Reflection.Assembly.GetExecutingAssembly().Location;
        static string appParts = $@"{myPath}\AppParts";
        static string Icons = $@"{myPath}\AppParts\Icons";
        static string myIcon = $@"{Icons}\{myName}.ico";
        static string iconFull = $@"{Icons}\Transparent.ico";
        static string iconHalf = $@"{Icons}\HalfMask.ico";
        static string HandleExe = $@"{appParts}\Handle.exe";
        static string ResourceHacker = $@"{appParts}\ResourceHacker\ResourceHacker.exe";
        static string munFile = @"C:\Windows\SystemResources\imageres.dll.mun";
        static string Option = "";

        static string sSetup = "Select thumbnail style:";
        static string sOK = "OK";
        static string sBuildError = "Windows 11 required";

        static float ScaleFactor = GetScale();
        static bool Dark = isDark();
        static bool ctrlKey = false;

        static string helpPage = "what-it-does";

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                Directory.SetCurrentDirectory(@"C:\");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                UnblockPath(myPath);

                ctrlKey = (GetAsyncKeyState(0x11) & 0x8000) != 0;

                string NTkey = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion";
                int buildNumber = int.Parse(Registry.GetValue(NTkey, "CurrentBuild", "").ToString());

                if (!ctrlKey && buildNumber < 21996)
                {
                    CustomMessageBox.Show(sBuildError, myName);
                    return;
                }

                if (args.Length == 0) { InstallRemove(); return; }

                Option = args[0];

                switch (Option.ToLower())
                {
                    case "/install":
                        RunUAC(myExe, "/installTrusted");
                        break;

                    case "/remove":
                        RunUAC(myExe, "/removeTrusted");
                        break;

                    case "/installtrusted":
                        KillExplorer();
                        Thread.Sleep(1000);
                        //AllocConsole();
                        CloseHandles();
                        Thread.Sleep(1000);
                        ReplaceIcon(iconFull);
                        Thread.Sleep(1000);
                        ResetThumbCache();
                        break;

                    case "/removetrusted":
                        KillExplorer();
                        Thread.Sleep(1000);
                        //AllocConsole();
                        CloseHandles();
                        Thread.Sleep(1000);
                        ReplaceIcon(iconHalf);
                        Thread.Sleep(1000);
                        ResetThumbCache();
                        break;

                    default:
                        return;
                }
            }
            catch (Exception ex)
            {
                Process.Start("explorer.exe");
                AllocConsole();
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine("Press Enter to exit...");
                Console.ReadLine();
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteFile(string name);

        public static void UnblockPath(string path)
        {
            string[] files = Directory.GetFiles(path);
            string[] dirs = Directory.GetDirectories(path);
            foreach (string file in files)
            {
                UnblockFile(file);
            }
            foreach (string dir in dirs)
            {
                UnblockPath(dir);
            }
        }

        public static bool UnblockFile(string fileName)
        {
            return DeleteFile(fileName + ":Zone.Identifier");
        }
        static void InstallRemove()
        {
            DialogResult result = TwoChoiceBox.Show(sSetup, myName, "", "");

            if (result == DialogResult.Yes)
            {
                RunUAC(myExe, "/installTrusted");
            }
            if (result == DialogResult.No)
            {
                RunUAC(myExe, "/removeTrusted");
            }
        }

        static void RunUAC(string fileName, string CommandLine)
        {
            Process p = new Process();
            p.StartInfo.FileName = fileName;
            p.StartInfo.Arguments = CommandLine;
            p.StartInfo.UseShellExecute = true;
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.Verb = "runas";
            p.Start();
            p.WaitForExit();
        }

        static void KillExplorer()
        {
            using (Process p = new Process())
            {
                p.StartInfo = new ProcessStartInfo
                {
                    FileName = "taskkill.exe",
                    Arguments = "/f /im explorer.exe",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true,
                    CreateNoWindow = true,
                };
                p.Start();
                p.WaitForExit();
            }
        }
        static void ResetThumbCache()
        {
            DeleteCacheFiles("thumbcache_*.db");
            Process.Start("explorer.exe");
        }

        static void DeleteCacheFiles(string searchPattern)
        {
            string targetDirectory = $@"{Environment.GetEnvironmentVariable("LocalAppData")}\Microsoft\Windows\Explorer";

            try
            {
                string[] files = Directory.GetFiles(targetDirectory, searchPattern, SearchOption.TopDirectoryOnly);

                foreach (string file in files)
                {
                    try { File.Delete(file); }
                    catch { }
                }
            }
            catch { }
        }

        static void ReplaceIcon(string icon)
        {
            ServiceController sc = new ServiceController
            {
                ServiceName = "TrustedInstaller",
            };

            if (sc.Status != ServiceControllerStatus.Running) sc.Start();

            Process[] proc = Process.GetProcessesByName("TrustedInstaller");

            Thread.Sleep(100);
            proc = Process.GetProcessesByName("TrustedInstaller");
            TrustedInstaller.Run(proc[0].Id, $"\"{ResourceHacker}\" -open {munFile} -save {munFile} -resource \"{icon}\" -action addoverwrite -mask icongroup,6,1033");
        }

        // Get current screen scaling factor
        static float GetScale()
        {
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                float dpiX = graphics.DpiX;
                return dpiX / 96;
            }
        }

        // Determine if dark colors (theme) are being used
        public static bool isDark()
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            const string valueName = "AppsUseLightTheme";

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath))
            {
                if (key != null)
                {
                    object value = key.GetValue(valueName);
                    if (value is int intValue)
                    {
                        return intValue == 0;
                    }
                }
            }
            return false; // Return false if the key or value is missing
        }

        // Make dialog title bar black
        public enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        }

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE attribute, ref int pvAttribute, uint cbAttribute);

        static void DarkTitleBar(IntPtr hWnd)
        {
            var preference = Convert.ToInt32(true);
            DwmSetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref preference, sizeof(uint));

        }
        public static void CloseHandles()
        {
            string tempFile = Path.GetTempFileName();

            HandleEULA();

            // Run Handle.exe to get handles
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = HandleExe,
                Arguments = $"-nobanner {munFile}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            Process p = Process.Start(psi);

            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();

            File.WriteAllText(tempFile, output);

            // Split the output into lines
            string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Split the line into parts
                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length > 5)
                {
                    // Extract the PID and handle ID
                    string pid = parts[2];
                    string handleId = parts[5];
                    handleId = handleId.TrimEnd(':');

                    Console.WriteLine($"Closing handle {handleId} for process {pid}");

                    // Run Handle.exe to close each handle
                    ProcessStartInfo closeHandlePsi = new ProcessStartInfo
                    {
                        FileName = HandleExe,
                        Arguments = $"-nobanner -p {pid} -c {handleId} -y",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    Process closeHandleProcess = Process.Start(closeHandlePsi);
                    closeHandleProcess.WaitForExit();
                }
            }
            File.Delete(tempFile);
        }

        static void HandleEULA()
        {
            const string subkey = @"Software\Sysinternals\Handle";

            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(subkey))
                {
                    if (key != null)
                    {
                        key.SetValue("EulaAccepted", 1, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        // Dialog for simple OK messages
        public class CustomMessageBox : Form
        {
            private Label messageLabel;
            private Label buttonHelp;
            private Button buttonOK;

            public CustomMessageBox(string message, string caption)
            {
                message = $"\n{message}";

                Icon = new Icon(myIcon);
                StartPosition = FormStartPosition.Manual;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                Text = caption;
                Width = (int)(300 * ScaleFactor);
                Height = (int)(150 * ScaleFactor);
                MaximizeBox = false;
                MinimizeBox = false;

                messageLabel = new Label();
                messageLabel.Text = message;
                messageLabel.Font = new Font("Segoe UI", 10);
                messageLabel.TextAlign = ContentAlignment.TopCenter;
                messageLabel.Dock = DockStyle.Fill;

                using (Graphics g = CreateGraphics())
                {
                    SizeF size = g.MeasureString(message, new Font("Segoe UI", 10), Width);
                    Height = Math.Max(Height, (int)(size.Height + (int)(100 * ScaleFactor)));
                }

                buttonHelp = new Label();
                Image image = Image.FromFile($@"{appParts}\Icons\Question.png");
                Bitmap scaledImage = new Bitmap((int)(26 * ScaleFactor), (int)(26 * ScaleFactor));
                using (Graphics g = Graphics.FromImage(scaledImage))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, 0, 0, (int)(26 * ScaleFactor), (int)(26 * ScaleFactor));
                }
                buttonHelp.BackgroundImage = scaledImage;
                buttonHelp.BackgroundImageLayout = ImageLayout.Stretch;
                buttonHelp.Size = new Size((int)(26 * ScaleFactor), (int)(26 * ScaleFactor));
                buttonHelp.FlatStyle = FlatStyle.Flat;
                buttonHelp.Left = ClientSize.Width - (int)(30 * ScaleFactor);
                buttonHelp.Top = (int)(4 * ScaleFactor);
                buttonHelp.Click += ButtonHelp_Click;

                messageLabel.Padding = new Padding(0, 0, (int)(26 * ScaleFactor), 0);

                buttonOK = new Button();
                buttonOK.Text = sOK;
                buttonOK.DialogResult = DialogResult.OK;
                buttonOK.Font = new Font("Segoe UI", 9);
                buttonOK.Width = (int)(75 * ScaleFactor);
                buttonOK.Height = (int)(26 * ScaleFactor);
                buttonOK.Left = (ClientSize.Width - buttonOK.Width) / 2;
                buttonOK.Top = ClientSize.Height - buttonOK.Height - (int)(10 * ScaleFactor);
                if (Dark)
                {
                    buttonOK.FlatStyle = FlatStyle.Flat;
                    buttonOK.FlatAppearance.BorderColor = SystemColors.Highlight;
                    buttonOK.FlatAppearance.BorderSize = 1;
                    buttonOK.BackColor = Color.FromArgb(60, 60, 60);
                    buttonOK.FlatAppearance.MouseOverBackColor = Color.Black;
                    DarkTitleBar(Handle);
                    BackColor = Color.FromArgb(32, 32, 32);
                    ForeColor = Color.White;
                }

                if (Dark)
                {
                    DarkTitleBar(Handle);
                    BackColor = Color.FromArgb(32, 32, 32);
                    ForeColor = Color.White;
                }

                Controls.Add(buttonHelp);
                Controls.Add(buttonOK);
                Controls.Add(messageLabel);

                Point cursorPosition = Cursor.Position;
                int dialogX = Cursor.Position.X - Width / 2;
                int dialogY = Cursor.Position.Y - Height / 2 - (int)(50 * ScaleFactor);
                Screen screen = Screen.FromPoint(cursorPosition);
                int screenWidth = screen.WorkingArea.Width;
                int screenHeight = screen.WorkingArea.Height;
                int baseX = screen.Bounds.X;
                int baseY = screen.Bounds.Y;
                dialogX = Math.Max(baseX, Math.Min(baseX + screenWidth - Width, dialogX));
                dialogY = Math.Max(baseY, Math.Min(baseY + screenHeight - Height, dialogY));
                Location = new Point(dialogX, dialogY);
            }
            public static DialogResult Show(string message, string caption)
            {
                using (var customMessageBox = new CustomMessageBox(message, caption))
                {
                    return customMessageBox.ShowDialog();
                }
            }

        }

        // Dialog for install/Remove
        public class TwoChoiceBox : Form
        {
            private Label messageLabel;
            private Label buttonHelp;
            private Label buttonYes;
            private Label buttonNo;
            Bitmap imageHelp;
            Bitmap imageHelpHover;
            Bitmap imageYes;
            Bitmap imageYesHover;
            Bitmap imageNo;
            Bitmap imageNoHover;

            public TwoChoiceBox(string message, string caption, string button1, string button2)
            {
                message = $"\n{message}";

                Icon = new Icon(myIcon);
                StartPosition = FormStartPosition.Manual;
                Text = caption;
                Width = (int)(300 * ScaleFactor);
                Height = (int)(192 * ScaleFactor);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;

                messageLabel = new Label();
                messageLabel.Text = message;
                messageLabel.Font = new Font("Segoe UI", 10);
                messageLabel.TextAlign = ContentAlignment.TopCenter;
                messageLabel.Dock = DockStyle.Fill;
                messageLabel.Padding = new Padding(0, 0, (int)(26 * ScaleFactor), 0);

                using (Graphics g = CreateGraphics())
                {
                    SizeF size = g.MeasureString(message, new Font("Segoe UI", 10), Width);
                    Height = Math.Max(Height, (int)(size.Height + (int)(100 * ScaleFactor)));
                }

                buttonHelp = new Label();
				
                Image image = Image.FromFile($@"{appParts}\Icons\Question.png");
                imageHelp = new Bitmap((int)(26 * ScaleFactor), (int)(26 * ScaleFactor));
                using (Graphics g = Graphics.FromImage(imageHelp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, 0, 0, (int)(26 * ScaleFactor), (int)(26 * ScaleFactor));
                }

                string hoverImage = "QuestionL.png";
                if (Dark) hoverImage = "QuestionD.png";
                image = Image.FromFile($@"{appParts}\Icons\{hoverImage}");
                imageHelpHover = new Bitmap((int)(26 * ScaleFactor), (int)(26 * ScaleFactor));
                using (Graphics g = Graphics.FromImage(imageHelpHover))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, 0, 0, (int)(26 * ScaleFactor), (int)(26 * ScaleFactor));
                }

                buttonHelp.MouseEnter += buttonHelp_MouseEnter;
                buttonHelp.MouseLeave += buttonHelp_MouseLeave;

                buttonHelp.BackgroundImage = imageHelp;
                buttonHelp.BackgroundImageLayout = ImageLayout.Stretch;
                buttonHelp.Size = new Size((int)(26 * ScaleFactor), (int)(26 * ScaleFactor));
                buttonHelp.FlatStyle = FlatStyle.Flat;
                buttonHelp.Left = ClientSize.Width - (int)(30 * ScaleFactor);
                buttonHelp.Top = (int)(4 * ScaleFactor);
                buttonHelp.Click += ButtonHelp_Click;

                buttonYes = new Label();
				
                image = Image.FromFile($@"{appParts}\Icons\ThumbFull.png");
                imageYes = new Bitmap((int)(96 * ScaleFactor), (int)(96 * ScaleFactor));
                using (Graphics g = Graphics.FromImage(imageYes))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, 0, 0, (int)(96 * ScaleFactor), (int)(96 * ScaleFactor));
                }

                hoverImage = "ThumbFullL.png";
                if (Dark) hoverImage = "ThumbFullD.png";
                image = Image.FromFile($@"{appParts}\Icons\{hoverImage}");
                imageYesHover = new Bitmap((int)(96 * ScaleFactor), (int)(96 * ScaleFactor));
                using (Graphics g = Graphics.FromImage(imageYesHover))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, 0, 0, (int)(96 * ScaleFactor), (int)(96 * ScaleFactor));
                }

                buttonYes.MouseEnter += buttonYes_MouseEnter;
                buttonYes.MouseLeave += buttonYes_MouseLeave;

                buttonYes.BackgroundImage = imageYes;
                buttonYes.BackgroundImageLayout = ImageLayout.Stretch;
                buttonYes.Size = new Size((int)(96 * ScaleFactor), (int)(96 * ScaleFactor));
                buttonYes.FlatStyle = FlatStyle.Flat;

                buttonYes.Left = (int)(10 * ScaleFactor);
                buttonYes.Top = ClientSize.Height - buttonYes.Height;
                buttonYes.Click += (s, e) => { this.DialogResult = DialogResult.Yes; this.Close(); };

                buttonNo = new Label();
				
                image = Image.FromFile($@"{appParts}\Icons\ThumbHalf.png");
                imageNo = new Bitmap((int)(96 * ScaleFactor), (int)(96 * ScaleFactor));
                using (Graphics g = Graphics.FromImage(imageNo))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, 0, 0, (int)(96 * ScaleFactor), (int)(96 * ScaleFactor));
                }

                hoverImage = "ThumbHalfL.png";
                if (Dark) hoverImage = "ThumbHalfD.png";
                image = Image.FromFile($@"{appParts}\Icons\{hoverImage}");
                imageNoHover = new Bitmap((int)(96 * ScaleFactor), (int)(96 * ScaleFactor));
                using (Graphics g = Graphics.FromImage(imageNoHover))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(image, 0, 0, (int)(96 * ScaleFactor), (int)(96 * ScaleFactor));
                }

                buttonNo.MouseEnter += buttonNo_MouseEnter;
                buttonNo.MouseLeave += buttonNo_MouseLeave;

                buttonNo.BackgroundImage = imageNo;
                buttonNo.BackgroundImageLayout = ImageLayout.Stretch;
                buttonNo.Size = new Size((int)(96 * ScaleFactor), (int)(96 * ScaleFactor));
                buttonNo.FlatStyle = FlatStyle.Flat;

                buttonNo.Left = ClientSize.Width - (int)(108 * ScaleFactor);
                buttonNo.Top = ClientSize.Height - buttonYes.Height;
                buttonNo.Click += (s, e) => { this.DialogResult = DialogResult.No; this.Close(); };


                if (Dark)
                {
                    DarkTitleBar(Handle);
                    BackColor = Color.FromArgb(32, 32, 32);
                    ForeColor = Color.White;
                }

                Controls.Add(buttonHelp);
                Controls.Add(buttonYes);
                Controls.Add(buttonNo);
                Controls.Add(messageLabel);

                Point cursorPosition = Cursor.Position;
                int dialogX = Cursor.Position.X - Width / 2;
                int x = 50;
                int dialogY = Cursor.Position.Y - Height / 2 - (int)(x * ScaleFactor);
                Screen screen = Screen.FromPoint(cursorPosition);
                int screenWidth = screen.WorkingArea.Width;
                int screenHeight = screen.WorkingArea.Height;
                int baseX = screen.Bounds.X;
                int baseY = screen.Bounds.Y;
                dialogX = Math.Max(baseX, Math.Min(baseX + screenWidth - Width, dialogX));
                dialogY = Math.Max(baseY, Math.Min(baseY + screenHeight - Height, dialogY));
                Location = new Point(dialogX, dialogY);
            }

            private void buttonHelp_MouseEnter(object sender, EventArgs e)
            {
                buttonHelp.BackgroundImage = imageHelpHover;
            }
            private void buttonHelp_MouseLeave(object sender, EventArgs e)
            {
                buttonHelp.BackgroundImage = imageHelp;
            }
            private void buttonYes_MouseEnter(object sender, EventArgs e)
            {
                buttonYes.BackgroundImage = imageYesHover;
            }
            private void buttonYes_MouseLeave(object sender, EventArgs e)
            {
                buttonYes.BackgroundImage = imageYes;
            }
            private void buttonNo_MouseEnter(object sender, EventArgs e)
            {
                buttonNo.BackgroundImage = imageNoHover;
            }
            private void buttonNo_MouseLeave(object sender, EventArgs e)
            {
                buttonNo.BackgroundImage = imageNo;
            }
            public static DialogResult Show(string message, string caption, string button1, string button2)
            {
                using (var TwoChoiceBox = new TwoChoiceBox(message, caption, button1, button2))
                {
                    return TwoChoiceBox.ShowDialog();
                }
            }
        }

        static void ButtonHelp_Click(object sender, EventArgs e)
        {
            Process.Start("https://lesferch.github.io/FolderThumbnailFix#" + helpPage);
        }
    }

    //Credit for the following TrustedInstaller code: https://github.com/rara64/GetTrustedInstaller
    class TrustedInstaller
    {
        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes, ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 WaitForSingleObject(IntPtr handle, UInt32 milliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr Attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetHandleInformation(IntPtr hObject, HANDLE_FLAGS dwMask, HANDLE_FLAGS dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, ref IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

        public static void Run(int parentProcessId, string binaryPath)
        {
            const int PROC_THREAD_ATTRIBUTE_PARENT_PROCESS = 0x00020000;

            const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
            const uint CREATE_NEW_CONSOLE = 0x00000010;

            var pInfo = new PROCESS_INFORMATION();
            var siEx = new STARTUPINFOEX();

            IntPtr lpValueProc = IntPtr.Zero;
            IntPtr hSourceProcessHandle = IntPtr.Zero;
            var lpSize = IntPtr.Zero;

            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
            siEx.lpAttributeList = Marshal.AllocHGlobal(lpSize);
            InitializeProcThreadAttributeList(siEx.lpAttributeList, 1, 0, ref lpSize);

            IntPtr parentHandle = OpenProcess(ProcessAccessFlags.CreateProcess | ProcessAccessFlags.DuplicateHandle, false, parentProcessId);

            lpValueProc = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(lpValueProc, parentHandle);

            UpdateProcThreadAttribute(siEx.lpAttributeList, 0, (IntPtr)PROC_THREAD_ATTRIBUTE_PARENT_PROCESS, lpValueProc, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero);

            var ps = new SECURITY_ATTRIBUTES();
            var ts = new SECURITY_ATTRIBUTES();
            ps.nLength = Marshal.SizeOf(ps);
            ts.nLength = Marshal.SizeOf(ts);

            // lpCommandLine was used instead of lpApplicationName to allow for arguments to be passed
            bool ret = CreateProcess(null, binaryPath, ref ps, ref ts, true, EXTENDED_STARTUPINFO_PRESENT | CREATE_NEW_CONSOLE, IntPtr.Zero, null, ref siEx, out pInfo);

            String stringPid = pInfo.dwProcessId.ToString();

        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct STARTUPINFO
        {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            [MarshalAs(UnmanagedType.Bool)]
            public bool bInheritHandle;
        }

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        [Flags]
        enum HANDLE_FLAGS : uint
        {
            None = 0,
            INHERIT = 1,
            PROTECT_FROM_CLOSE = 2
        }
    }

}
