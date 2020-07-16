using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Office.Interop.Excel;
using System.Diagnostics;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace Anoth
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        [DllImport("user32.dll")]
        static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, uint dwExtraInfo);

        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string lclassName, string windowTitle);

        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        public static IntPtr excelHandle = FindWindow("EXCEL7", "Excel 2016");

        /* uint LEFTDOWN = 0x00000002;
         uint LEFTUP = 0x00000004;
         uint MIDDLEDOWN = 0x00000020;
         uint MIDDLEUP = 0x00000040;
         uint MOVE = 0x00000001;
         uint ABSOLUTE = 0x00008000;
         uint RIGHTDOWN = 0x00000008;
         uint RIGHTUP = 0x00000010;*/

        int hwnd = 0;
        IntPtr hwndChild = IntPtr.Zero;

        private delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        private static extern IntPtr FindWindowByCaption(IntPtr zeroOnly, string lpWindowName);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, [Out] StringBuilder lParam);

        // Callback method used to collect a list of child windows we need to capture text from.
        private static bool EnumChildWindowsCallback(IntPtr handle, IntPtr pointer)
        {
            // Creates a managed GCHandle object from the pointer representing a handle to the list created in GetChildWindows.
            var gcHandle = GCHandle.FromIntPtr(pointer);

            // Casts the handle back back to a List<IntPtr>
            var list = gcHandle.Target as List<IntPtr>;

            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }

            // Adds the handle to the list.
            list.Add(handle);

            return true;
        }

        // Returns an IEnumerable<IntPtr> containing the handles of all child windows of the parent window.
        private static IEnumerable<IntPtr> GetChildWindows(IntPtr parent)
        {
            // Create list to store child window handles.
            var result = new List<IntPtr>();

            // Allocate list handle to pass to EnumChildWindows.
            var listHandle = GCHandle.Alloc(result);

            try
            {
                // Enumerates though all the child windows of the parent represented by IntPtr parent, executing EnumChildWindowsCallback for each. 
                EnumChildWindows(parent, EnumChildWindowsCallback, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                // Free the list handle.
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }

            // Return the list of child window handles.
            return result;
        }

        // Gets text text from a control by it's handle.
        private static string GetText(IntPtr handle)
        {
            const uint WM_GETTEXTLENGTH = 0x000E;
            const uint WM_GETTEXT = 0x000D;

            // Gets the text length.
            var length = (int)SendMessage(handle, WM_GETTEXTLENGTH, IntPtr.Zero, null);

            // Init the string builder to hold the text.
            var sb = new StringBuilder(length + 1);

            // Writes the text from the handle into the StringBuilder
            SendMessage(handle, WM_GETTEXT, (IntPtr)sb.Capacity, sb);

            // Return the text as a string.
            return sb.ToString();
        }

        // Wraps everything together. Will accept a window title and return all text in the window that matches that window title.
        private static string GetAllTextFromWindowByTitle(string windowTitle)
        {
            var sb = new StringBuilder();

            try
            {
                // Find the main window's handle by the title.
                var windowHWnd = FindWindowByCaption(IntPtr.Zero, windowTitle);

                // Loop though the child windows, and execute the EnumChildWindowsCallback method
                var childWindows = GetChildWindows(windowHWnd);

                // For each child handle, run GetText
                foreach (var childWindowText in childWindows.Select(GetText))
                {
                    // Append the text to the string builder.
                    sb.Append(childWindowText);
                }

                

                // Return the windows full text.
                return sb.ToString();
            }
            catch (Exception e)
            {
                Console.Write(e.Message);
            }

            return string.Empty;

        }


        private void button1_Click(object sender, EventArgs e)
        {
            /*Process.Start("excel");
            Thread.Sleep(1000);
            SetForegroundWindow(excelHandle);
            this.WindowState = FormWindowState.Minimized;
            SendKeys.SendWait("{ENTER}");

            Thread.Sleep(1000);

            //SendKeys.SendWait("{ENTER}");
            SendKeys.SendWait("1st Column");
            SendKeys.SendWait("{RIGHT}");
            SendKeys.SendWait("2nd Column");
            SendKeys.SendWait("{RIGHT}");
            SendKeys.SendWait("3rd Column");
            SendKeys.SendWait("{RIGHT}");
            SendKeys.SendWait("4th Column");
            SendKeys.SendWait("{RIGHT}");

            Thread.Sleep(2000);

            hwndChild = FindWindowEx((IntPtr)excelHandle, IntPtr.Zero, "Button", "Cut");

            //hwndChild = FindWindowEx((IntPtr)hwnd, IntPtr.Zero, "Button", "1");

            Thread.Sleep(1000);

            SendKeys.SendWait("+{LEFT} 4");

            Thread.Sleep(1000);

            SendKeys.SendWait("^(b)");

            Thread.Sleep(1000);

            SendKeys.SendWait("{DOWN}");
            /*Microsoft.Office.Interop.Excel._Application objExcelApplication = new Microsoft.Office.Interop.Excel.Application();

            Microsoft.Office.Interop.Excel._Workbook objWorkBooks = objExcelApplication.Workbooks.Open(@"F:\Book1.xlsx", 0,true, 1, "", "", true, Microsoft.Office.Interop.Excel.XlPlatform.xlWindows,"\t", false, false, Type.Missing, Type.Missing);

            Microsoft.Office.Interop.Excel._Worksheet excelSheet = (Microsoft.Office.Interop.Excel.Worksheet)objExcelApplication.Worksheets.get_Item(1);

            Microsoft.Office.Interop.Excel.Range range1 = excelSheet.get_Range("A1", "A8");
            Microsoft.Office.Interop.Excel.Range range2 = excelSheet.get_Range("A1", "A1");*/
            var allText = GetAllTextFromWindowByTitle("Untitled - Notepad");
            textBox1.Text = allText;
        }
        static class KeyboardSend
        {
            [DllImport("user32.dll")]
            private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);

            private const int KEYEVENTF_EXTENDEDKEY = 1;
            private const int KEYEVENTF_KEYUP = 2;

            public static void KeyDown(Keys vKey)
            {
                keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY, 0);
            }

            public static void KeyUp(Keys vKey)
            {
                keybd_event((byte)vKey, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, 0);
            }
        }
        public class MouseSend
        {
            // The WM_COMMAND message is sent when the user selects a command item from 
            // a menu, when a control sends a notification message to its parent window, 
            // or when an accelerator keystroke is translated.
            public const int WM_KEYDOWN = 0x100;
            public const int WM_KEYUP = 0x101;
            public const int WM_COMMAND = 0x111;
            public const int WM_LBUTTONDOWN = 0x201;
            public const int WM_LBUTTONUP = 0x202;
            public const int WM_LBUTTONDBLCLK = 0x203;
            public const int WM_RBUTTONDOWN = 0x204;
            public const int WM_RBUTTONUP = 0x205;
            public const int WM_RBUTTONDBLCLK = 0x206;

            // The FindWindow function retrieves a handle to the top-level window whose
            // class name and window name match the specified strings.
            // This function does not search child windows.
            // This function does not perform a case-sensitive search.
            [DllImport("User32.dll")]
            public static extern int FindWindow(string strClassName, string strWindowName);

            // The FindWindowEx function retrieves a handle to a window whose class name 
            // and window name match the specified strings.
            // The function searches child windows, beginning with the one following the
            // specified child window.
            // This function does not perform a case-sensitive search.
            [DllImport("User32.dll")]
            public static extern int FindWindowEx(
                int hwndParent,
                int hwndChildAfter,
                string strClassName,
                string strWindowName);


            // The SendMessage function sends the specified message to a window or windows. 
            // It calls the window procedure for the specified window and does not return
            // until the window procedure has processed the message. 
            [DllImport("User32.dll")]
            public static extern Int32 SendMessage(
                int hWnd,               // handle to destination window
                int Msg,                // message
                int wParam,             // first message parameter
                [MarshalAs(UnmanagedType.LPStr)] string lParam); // second message parameter

            [DllImport("User32.dll")]
            public static extern Int32 SendMessage(
                int hWnd,               // handle to destination window
                int Msg,                // message
                int wParam,             // first message parameter
                int lParam);            // second message parameter
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Anoth
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }
        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.Dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static public extern IntPtr GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            list.Add(handle);
            return true;
        }

        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                Win32Callback childProc = new Win32Callback(EnumWindow);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        public static string GetWinClass(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero)
                return null;
            StringBuilder classname = new StringBuilder(100);
            IntPtr result = GetClassName(hwnd, classname, classname.Capacity);
            if (result != IntPtr.Zero)
                return classname.ToString();
            return null;
        }

        public static IEnumerable<IntPtr> EnumAllWindows(IntPtr hwnd, string childClassName)
        {
            List<IntPtr> children = GetChildWindows(hwnd);
            if (children == null)
                yield break;
            foreach (IntPtr child in children)
            {
                if (GetWinClass(child) == childClassName)
                    yield return child;
                foreach (var childchild in EnumAllWindows(child, childClassName))
                    yield return childchild;
            }
        }


        public static IntPtr excelHandle = Form1.FindWindow("EXCEL7", "Excel 2016");
        private void button1_Click(object sender, EventArgs e)
        {

            var hwndChild = EnumAllWindows(excelHandle, "EXCEL7").FirstOrDefault();
            textBox1.Text = hwndChild.ToString() ;
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Anoth
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var processes = Process.GetProcessesByName("excel");
            StringBuilder className = new StringBuilder(256);
            GetClassName(processes[0].MainWindowHandle, className, className.Capacity);
            _windowLookupMap[processes[0].MainWindowHandle] = new WindowInformation(processes[0].MainWindowHandle, IntPtr.Zero, className.ToString());
            EnumChildWindows(processes[0].MainWindowHandle, EnumChildWindowsCallback, processes[0].MainWindowHandle);
            List<WindowInformation> matchingWindows = new List<WindowInformation>();
            FindWindowsByClass("Insert", _windowLookupMap.Single(window => window.Value._parent == IntPtr.Zero).Value, ref matchingWindows);
            textBox1.Text = matchingWindows.Count.ToString();
        }
        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public static bool EnumChildWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            StringBuilder className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            var windowInformation = new WindowInformation(hWnd, lParam, className.ToString());
            _windowLookupMap[hWnd] = windowInformation;
            if (lParam != IntPtr.Zero)
            {
                _windowLookupMap[lParam]._children.Add(windowInformation);
            }
            EnumChildWindows(hWnd, EnumChildWindowsCallback, hWnd);
            return true;
        }

        class WindowInformation
        {
            public IntPtr _parent;

            public IntPtr _hWnd;

            public string _className;

            public List<WindowInformation> _children = new List<WindowInformation>();

            public WindowInformation(IntPtr hWnd, IntPtr parent, string className)
            {
                _hWnd = hWnd;
                _parent = parent;
                _className = className;
            }
        }

        static Dictionary<IntPtr, WindowInformation> _windowLookupMap = new Dictionary<IntPtr, WindowInformation>();

        static void FindWindowsByClass(string className, WindowInformation root, ref List<WindowInformation> matchingWindows)
        {
            if (root._className == className)
            {
                matchingWindows.Add(root);
            }
            foreach (var child in root._children)
            {
                FindWindowsByClass(className, child, ref matchingWindows);
            }
        }

    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows;


namespace Anoth
{
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();

        }
        [DllImport("user32")]

        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr i);

        /// <summary>
        /// Returns a list of child windows
        /// </summary>
        /// <param name="parent">Parent of the windows to return</param>
        /// <returns>List of child windows</returns>
        public static List<IntPtr> GetChildWindows(IntPtr parent)
        {
            List<IntPtr> result = new List<IntPtr>();
            GCHandle listHandle = GCHandle.Alloc(result);
            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(parent, childProc, GCHandle.ToIntPtr(listHandle));
            }
            finally
            {
                if (listHandle.IsAllocated)
                    listHandle.Free();
            }
            return result;
        }

        /// <summary>
        /// Callback method to be used when enumerating windows.
        /// </summary>
        /// <param name="handle">Handle of the next window</param>
        /// <param name="pointer">Pointer to a GCHandle that holds a reference to the list to fill</param>
        /// <returns>True to continue the enumeration, false to bail</returns>
        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            List<IntPtr> list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            //  You can modify this to check to see if you want to cancel the operation, then return a null here
            return true;
        }


        /// <summary>
        /// Delegate for the EnumChildWindows method
        /// </summary>
        /// <param name="hWnd">Window handle</param>
        /// <param name="parameter">Caller-defined variable; we use it for a pointer to our list</param>
        /// <returns>True to continue enumerating, false to bail.</returns>
        public delegate bool EnumWindowProc(IntPtr hWnd, IntPtr parameter);

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Anoth
{
    public partial class Form5 : Form
    {
        public Form5()
        {
            InitializeComponent();
        }

        private static IntPtr[] GetWindowHandlesForThread(int threadHandle)
        {
            _results.Clear();
            EnumWindows(WindowEnum, threadHandle);
            return _results.ToArray();
        }

        // enum windows

        private delegate int EnumWindowsProc(IntPtr hwnd, int lParam);

        [DllImport("user32.Dll")]
        private static extern int EnumWindows(EnumWindowsProc x, int y);
        [DllImport("user32")]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowsProc callback, int lParam);
        [DllImport("user32.dll")]
        public static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        private static List<IntPtr> _results = new List<IntPtr>();

        private static int WindowEnum(IntPtr hWnd, int lParam)
        {
            int processID = 0;
            int threadID = GetWindowThreadProcessId(hWnd, out processID);
            if (threadID == lParam)
            {
                _results.Add(hWnd);
                EnumChildWindows(hWnd, WindowEnum, threadID);
            }
            return 1;
        }

        // get window text

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        private static string GetText(IntPtr hWnd)
        {
            int length = GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            GetWindowText(hWnd, sb, sb.Capacity);
            return sb.ToString();
        }

        // get richedit text 

        public const int GWL_ID = -12;
        public const int WM_GETTEXT = 0x000D;

        [DllImport("User32.dll")]
        public static extern int GetWindowLong(IntPtr hWnd, int index);
        [DllImport("User32.dll")]
        public static extern IntPtr SendDlgItemMessage(IntPtr hWnd, int IDDlgItem, int uMsg, int nMaxCount, StringBuilder lpString);
        [DllImport("User32.dll")]
        public static extern IntPtr GetParent(IntPtr hWnd);

        private static StringBuilder GetEditText(IntPtr hWnd)
        {
            Int32 dwID = GetWindowLong(hWnd, GWL_ID);
            IntPtr hWndParent = GetParent(hWnd);
            StringBuilder title = new StringBuilder(128);
            SendDlgItemMessage(hWndParent, dwID, WM_GETTEXT, 128, title);
            return title;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (Process procesInfo in Process.GetProcesses())
            {
                textBox1.Text+= procesInfo.ProcessName + procesInfo.Id;
                foreach (ProcessThread threadInfo in procesInfo.Threads)
                {
                    // uncomment to dump thread handles
                    //Console.WriteLine("\tthread {0:x}", threadInfo.Id);
                    IntPtr[] windows = GetWindowHandlesForThread(threadInfo.Id);
                    if (windows != null && windows.Length > 0)
                        foreach (IntPtr hWnd in windows)
                            textBox1.Text+= hWnd.ToInt32() + GetText(hWnd) + GetEditText(hWnd);
                }
            }
            var txt = textBox1.Text;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Anoth
{
    public partial class Form6 : Form
    {
        public Form6()
        {
            InitializeComponent();
        }
        [DllImport("user32.dll")]
        static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, IntPtr wParam, string lParam);

        private uint msgId = RegisterWindowMessage("my_message_type");

        // Transmitter
        private void Send(IntPtr targetWindowHandle, string msgText)
        {
            SendMessage(targetWindowHandle, (int)msgId, IntPtr.Zero, msgText);
        }

        // Receiver
        protected override void WndProc(ref Message msg)
        {
            if (msg.Msg == msgId)
            {
                string msgText = Marshal.PtrToStringAnsi(msg.LParam);
                MessageBox.Show(msgText);
            }
            base.WndProc(ref msg);
        }

    }
}
public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(IntPtr hwndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public static bool EnumChildWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            StringBuilder className = new StringBuilder(256);
            GetClassName(hWnd, className, className.Capacity);
            var windowInformation = new WindowInformation(hWnd, lParam, className.ToString());
            _windowLookupMap[hWnd] = windowInformation;
            if (lParam != IntPtr.Zero)
            {
                _windowLookupMap[lParam]._children.Add(windowInformation);
            }
            EnumChildWindows(hWnd, EnumChildWindowsCallback, hWnd);
            return true;
        }

        class WindowInformation
        {
            public IntPtr _parent;

            public IntPtr _hWnd;

            public string _className;

            public List<WindowInformation> _children = new List<WindowInformation>();

            public WindowInformation(IntPtr hWnd, IntPtr parent, string className)
            {
                _hWnd = hWnd;
                _parent = parent;
                _className = className;
            }
        }

        static Dictionary<IntPtr, WindowInformation> _windowLookupMap = new Dictionary<IntPtr, WindowInformation>();

        static void FindWindowsByClass(string className, WindowInformation root, ref List<WindowInformation> matchingWindows)
        {
            if (root._className == className)
            {
                matchingWindows.Add(root);
            }
            foreach (var child in root._children)
            {
                FindWindowsByClass(className, child, ref matchingWindows);
            }
        }

        static void Main(string[] args)
        {
            var processes = Process.GetProcessesByName("notepad");
            StringBuilder className = new StringBuilder(256);
            GetClassName(processes[0].MainWindowHandle, className, className.Capacity);
            _windowLookupMap[processes[0].MainWindowHandle] = new WindowInformation(processes[0].MainWindowHandle, IntPtr.Zero, className.ToString());
            EnumChildWindows(processes[0].MainWindowHandle, EnumChildWindowsCallback, processes[0].MainWindowHandle);
            List<WindowInformation> matchingWindows = new List<WindowInformation>();
            FindWindowsByClass("Edit", _windowLookupMap.Single(window => window.Value._parent == IntPtr.Zero).Value, ref matchingWindows);
            Console.WriteLine("Found {0} matching window handles", matchingWindows.Count);
        }