using OpenNGS.Windows;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;


namespace OpenNGS.Windows
{
    public class Native
    {
        static readonly bool kIsWow64 = Native.IsWow64();
        public struct WNDCLASSEXW
        {
            public int cbSize;
            public int style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
            public IntPtr hIconSm;
        }
        // 引入 Windows API 方法
        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int hookType, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetricsForDpi(int nIndex, uint dpi);

        public const int USHRT_MAX = 0xFFFF;

        public const int SM_XVIRTUALSCREEN = 76;
        public const int SM_YVIRTUALSCREEN = 77;
        public const int SM_CXVIRTUALSCREEN = 78;
        public const int SM_CYVIRTUALSCREEN = 79;

        public const int GWL_EXSTYLE = -20;
        public const uint WS_EX_LAYERED = 0x00080000;

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);



        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetModuleHandleW([MarshalAs(UnmanagedType.LPWStr)] string lpModuleName);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr SetForegroundWindow(IntPtr hWnd);

        [DllImport("USER32.DLL")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);


        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool IsWow64Process(IntPtr hProcess, out bool Wow64Process);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.U2)]
        public static extern ushort RegisterClassExW([In] ref WNDCLASSEXW lpwcx);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnregisterClassW(IntPtr windowClass, IntPtr hInstance);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr CreateWindowExW(uint dwExStyle, IntPtr windowClass, [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName,
            uint dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr pvParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("kernel32.dll")]
        public static extern IntPtr SetWindowPos(IntPtr hWnd, IntPtr hWndinsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("User32.dll")]
        public extern static bool ShowWindow(IntPtr hWnd, short State);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProcW(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);


        // 键盘钩子的事件类型
        public delegate int LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);


        public static IntPtr GetCurrentModuleHandle()
        {
            using (var currentProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var currentModule = currentProcess.MainModule)
            {
                return Native.GetModuleHandle(currentModule.ModuleName);
            }
        }

        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadWndProc lpfn, IntPtr lParam);

        public delegate bool EnumThreadWndProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("Kernel32.dll")]
        public static extern int GetCurrentThreadId();

        public static IntPtr GetWindowHandle()
        {
            IntPtr returnHwnd = IntPtr.Zero;
            var threadId = GetCurrentThreadId();
            EnumThreadWindows(threadId, (hWnd, lParam) =>
            {
                if (returnHwnd == IntPtr.Zero) returnHwnd = hWnd;
                return true;
            }, IntPtr.Zero);
            return returnHwnd;
        }

        public static bool IsWow64()
        {
            if (IntPtr.Size == 8)
                return false;

            if (!Native.IsWow64Process(Native.GetCurrentProcess(), out bool isWow64))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to figure out whether we're running under WOW64.");

            return isWow64;
        }


        public delegate IntPtr WndProcDelegate(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);

        public static IntPtr RegisterWindowClass(IntPtr hInstance, string className, WndProcDelegate proc)
        {
            var wndClass = new Native.WNDCLASSEXW();
            wndClass.cbSize = Marshal.SizeOf<Native.WNDCLASSEXW>();
            wndClass.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(proc);
            wndClass.hInstance = hInstance;
            wndClass.lpszClassName = className;

            var registeredClass = Native.RegisterClassExW(ref wndClass);
            if (registeredClass == 0)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to register the window class.");

            return new IntPtr(registeredClass);
        }

        const uint SWP_NOMOVE = 0x0001;
        const uint SWP_NOSIZE = 0x0002;
        const uint SWP_SHOWWINDOW = 0x0040;
        static IntPtr HWND_TOPMOST = new IntPtr(-1);
        static IntPtr HWND_NOTOPMOST = new IntPtr(-2);
        public static void SetMainWindowTopMost(bool topmost)
        {
            var unityWindowHandle = System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle;
            if (unityWindowHandle != IntPtr.Zero)
            {
                SetWindowTopMost(unityWindowHandle, topmost);
            }
            else
            {
                Debug.LogError("❌ 无法获取主窗口句柄");
            }
            var hWnd = GetProcessWnd();
            if (hWnd != IntPtr.Zero)
            {
                SetWindowTopMost(hWnd, topmost);
            }
        }

        public static void SetWindowTopMost(IntPtr hWnd, bool topmost)
        {
            if (hWnd != IntPtr.Zero)
            {
                if (topmost)
                {
                    //ShowWindow(hWnd, 1); // 确保窗口显示
                    SetForegroundWindow(hWnd); // 将窗口设为前台激活
                    SetWindowPos(hWnd, (IntPtr)HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);
                }
                else
                {
                    SetWindowPos(hWnd, (IntPtr)HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);
                }
            }
        }

        public delegate bool WNDENUMPROC(IntPtr hwnd, uint lParam);
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool EnumWindows(WNDENUMPROC lpEnumFunc, uint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr GetParent(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        public static extern void SetLastError(uint dwErrCode);

        public static IntPtr GetProcessWnd()
        {
            IntPtr ptrWnd = IntPtr.Zero;
            uint pid = (uint)System.Diagnostics.Process.GetCurrentProcess().Id; // 当前进程 ID  
            bool bResult = EnumWindows(new WNDENUMPROC(delegate (IntPtr hwnd, uint lParam)
            {
                uint id = 0;
                if (GetParent(hwnd) == IntPtr.Zero)
                {
                    GetWindowThreadProcessId(hwnd, ref id);
                    if (id == lParam)    // 找到进程对应的主窗口句柄  
                    {
                        ptrWnd = hwnd;   // 把句柄缓存起来  
                        SetLastError(0);    // 设置无错误  
                        return false;   // 返回 false 以终止枚举窗口  
                    }
                }
                return true;
            }), pid);
            return (!bResult && Marshal.GetLastWin32Error() == 0) ? ptrWnd : IntPtr.Zero;
        }
    }
}