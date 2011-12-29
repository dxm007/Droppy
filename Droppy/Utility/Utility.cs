
using System;
using System.Windows;


namespace Droppy
{
    public static class GeneralHelperExtensions
    {
        public static bool HasFlag<T>( this Enum obj, T value )
        {
            return ( Convert.ToInt32( obj ) & Convert.ToInt32( value ) ) != 0;
        }
    }

    public static class WpfHelperExtensions
    {
        public static double Width( this Thickness obj ) { return obj.Left + obj.Right; }
        public static double Height( this Thickness obj ) { return obj.Top + obj.Bottom; }
    }

    public class Win32Window : System.Windows.Forms.IWin32Window
    {
        public Win32Window( DependencyObject dependencyObject )
            : this( Window.GetWindow( dependencyObject ) )
        {
        }

        public Win32Window( Window window )
        {
            _handle = new System.Windows.Interop.WindowInteropHelper( window ).Handle;
        }

        public IntPtr Handle { get { return _handle; } }

        private IntPtr      _handle;
    }
}