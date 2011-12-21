using System;
using System.Runtime.InteropServices;

namespace Droppy {


    public static partial class Win32
    {
        #region ------------------ user32.dll ----------------------------------
        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
          public Int32 X;
          public Int32 Y;
        };

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(ref Point pt);

        #endregion


        #region ----------------- shell32.dll ----------------------------------

        #endregion
    }
}