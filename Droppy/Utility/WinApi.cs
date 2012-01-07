//=================================================================================================
//=================================================================================================
//
// Copyright (c) 2012 Dennis Mnuskin
//
// This file is part of Droppy application.
//
// This source code is distributed under the MIT license.  For full text, see
// http://www.opensource.org/licenses/mit-license.php Same text is found in LICENSE file which
// is located in root directory of the project.
//
//=================================================================================================
//=================================================================================================

using System;
using System.Runtime.InteropServices;

namespace Droppy {


    public static partial class Win32
    {
        #region ------------------ user32.dll ----------------------------------

        #region ---- Graphics > Legacy Graphics > Windows GDI -----
        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public Int32 X;
            public Int32 Y;

            public static implicit operator System.Windows.Point( Point pt )
            {
                return new System.Windows.Point( pt.X, pt.Y );
            }
        };

        [StructLayout( LayoutKind.Sequential )]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public static implicit operator System.Windows.Rect( Rect r )
            {
                return new System.Windows.Rect( r.left, r.top, r.right - r.left, r.bottom - r.top );
            }
        }

        #region ---- Multiple Display Monitors ----

        [Flags]
        public enum MonitorInfoFlags : int
        {
            None        = 0x00,
            Primary     = 0x01
        }

        [StructLayout( LayoutKind.Sequential )]
        public struct MonitorInfo
        {
            public MonitorInfo( bool init ) : this()
            {
                cbSize = Marshal.SizeOf( typeof( MonitorInfo ) );
            }

            public int                  cbSize;
            public Rect                 rcMonitor;
            public Rect                 rcWork;
            public MonitorInfoFlags     flags;
        }

        public delegate bool MonitorEnumDelegate( IntPtr hMonitor, IntPtr hdcMonitor,
                                                  ref Rect lprcMonitor, IntPtr dwData );

        [DllImport( "user32.dll", SetLastError = true )]
        public static extern bool EnumDisplayMonitors( IntPtr hdc, IntPtr lprcClip,
                                                       MonitorEnumDelegate lpfnEnum, IntPtr dwData );

        [DllImport( "user32.dll", SetLastError = true )]
        public static extern bool GetMonitorInfo( IntPtr hMonitor, ref MonitorInfo lpmi );

        #endregion

        #endregion

        #region ---- Windows App UI ----
        
        #region ---- Menus and Other Resources > Cursors ----

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(ref Point pt);

        #endregion

        #region ---- Windows and Messages > Messages and Message Queues -----

        [DllImport( "user32.dll", SetLastError=true )]
        public static extern bool PostMessage( IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam );

        [DllImport( "user32.dll", SetLastError=true )]
        public static extern int RegisterWindowMessage( string message );

        [DllImport( "user32.dll", SetLastError = true )]
        public static extern bool SendNotifyMessage( IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam );

        #endregion

        #endregion

        #endregion


        #region ----------------- shell32.dll ----------------------------------

        #endregion
    }
}