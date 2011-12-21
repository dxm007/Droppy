
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

}