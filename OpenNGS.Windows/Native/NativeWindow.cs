using AOT;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

namespace OpenNGS.Windows
{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
    public class NativeWindow
    {
        static IntPtr s_HInstance = Native.GetModuleHandleW(null);

        IntPtr m_WindowClass;
        public IntPtr m_Hwnd { get; private set; }

        public delegate void WndMessageDelegate(uint message, IntPtr wParam, IntPtr lParam);

        public static event WndMessageDelegate OnMessage;

        public static NativeWindow CreateWindow(string className,string winName)
        {
            NativeWindow win = new NativeWindow();
            win.m_WindowClass = Native.RegisterWindowClass(s_HInstance, className, WndProc);
            win.m_Hwnd = Native.CreateWindowExW(0, win.m_WindowClass, winName, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, s_HInstance, IntPtr.Zero);
            if (win.m_Hwnd == null)
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"Failed to create window {className}[{winName}].");
            Native.ShowWindow(win.m_Hwnd, 0);
            Debug.Log($"NativeWindow CreateWindow({className},{winName}) == {win.m_Hwnd}");
            return win;
        }

        [MonoPInvokeCallback(typeof(Native.WndProcDelegate))]
        static IntPtr WndProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                OnMessage?.Invoke(message, wParam, lParam);
                return Native.DefWindowProcW(window, message, wParam, lParam);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return IntPtr.Zero;
            }
        }


        public void DestroyWindow()
        {
            if (!Native.DestroyWindow(m_Hwnd))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to destroy raw input redirection window class.");

            if (!Native.UnregisterClassW(m_WindowClass, s_HInstance))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Failed to unregister raw input redirection window class.");
        }
    }
#endif
}