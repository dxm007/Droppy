using System;
using System.Runtime.InteropServices;

namespace Droppy {


    public static class Win32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
          public Int32 X;
          public Int32 Y;
        };

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Point pt);
    }
}