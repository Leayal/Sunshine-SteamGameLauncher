using System;
using System.Collections.Generic;
using Windows.Win32;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Leayal.Sunshine.SteamGameLauncher
{
    static class NativeMethods
    {
        public sealed class ConsoleWindowHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public static readonly ConsoleWindowHandle Invalid = new ConsoleWindowHandle(IntPtr.Zero);

            internal ConsoleWindowHandle(nint handle) : base(true)
            {
                base.SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                return (PInvoke.CloseHandle(new Windows.Win32.Foundation.HANDLE(this.handle)).Value != 0);
            }
        }

        public static ConsoleWindowHandle GetConsoleWindow()
        {
            var handle = PInvoke.GetConsoleWindow();
            
            if (handle.IsNull) return ConsoleWindowHandle.Invalid;

            return new ConsoleWindowHandle(handle.Value);
        }

        public static bool IsPsuedoConsole(this ConsoleWindowHandle handle)
        {
            if (handle.IsInvalid) return true;

            bool isAddRefSuccess = false;
            handle.DangerousAddRef(ref isAddRefSuccess);

            if (isAddRefSuccess)
            {
                try
                {
                    return !PInvoke.IsWindow(new Windows.Win32.Foundation.HWND(handle.DangerousGetHandle()));
                }
                finally
                {
                    handle.DangerousRelease();
                }
            }
            else throw new InvalidProgramException();
        }

        public static void MinimizeWindow(this ConsoleWindowHandle handle)
        {
            if (!handle.IsInvalid)
            {
                bool isAddRefSuccess = false;
                handle.DangerousAddRef(ref isAddRefSuccess);

                if (isAddRefSuccess)
                {
                    try
                    {
                        PInvoke.PostMessage(new Windows.Win32.Foundation.HWND(handle.DangerousGetHandle()), PInvoke.WM_SYSCOMMAND, PInvoke.SC_MINIMIZE, 0);
                    }
                    finally
                    {
                        handle.DangerousRelease();
                    }
                }
            }
        }

        public static bool IsInPsuedoConsole()
        {
            using (var handle = GetConsoleWindow())
            {
                if (handle.IsInvalid) return true;

                bool isAddRefSuccess = false;
                handle.DangerousAddRef(ref isAddRefSuccess);

                if (isAddRefSuccess)
                {
                    try
                    {
                        return !PInvoke.IsWindow(new Windows.Win32.Foundation.HWND(handle.DangerousGetHandle()));
                    }
                    finally
                    {
                        handle.DangerousRelease();
                    }
                }
                else throw new InvalidProgramException();
            }
        }

        public static string? GetWindowTitle()
        {
            using (var handle = GetConsoleWindow())
            {
                return GetWindowTitle(handle);
            }
        }

        public static string? GetWindowTitle(ConsoleWindowHandle handle)
        {
            // var handle = GetConsoleWindow();
            if (handle == null || handle.IsInvalid) return null;

            bool isAddRefSuccess = false;
            handle.DangerousAddRef(ref isAddRefSuccess);

            if (!isAddRefSuccess) return null;

            try
            {
                var nativeHandle = new Windows.Win32.Foundation.HWND(handle.DangerousGetHandle());
                var textLength = PInvoke.GetWindowTextLength(nativeHandle);

                if (textLength == 0) return string.Empty;
                return string.Create(textLength, nativeHandle, (span, obj) =>
                {
                    unsafe
                    {
                        fixed (char* c = span)
                        {
                            PInvoke.GetWindowText(obj, new Windows.Win32.Foundation.PWSTR(c), span.Length + 1);
                        }
                    }
                });
            }
            finally
            {
                handle.DangerousRelease();
            }
        }
    }
}
