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
using System.Windows;
using System.Windows.Controls;

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

        public static Size ActualSize( this FrameworkElement obj )
        {
            return new Size( obj.ActualWidth, obj.ActualHeight );
        }

        public static Point Location( this Window obj )
        {
            return new Point( obj.Left, obj.Top );
        }
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

    public class ControlInitHelper
    {
        public ControlInitHelper( Control control )
        {
            _element = null;
            _control = control;
        }

        public ControlInitHelper Element( string elementName )
        {
            _element = (UIElement)_control.Template.FindName( elementName, _control );
            return this;
        }

        public ControlInitHelper Get<TElem>( out TElem element )
                where TElem : UIElement
        {
            element = (TElem)_element;
            return this;
        }

        public ControlInitHelper Add( RoutedEvent   routedEvent,
                                      Delegate      eventHandler ) 
        {
            if( _element != null )
            {
                _element.AddHandler( routedEvent, eventHandler );
            }
            else
            {
                _control.AddHandler( routedEvent, eventHandler );
            }

            return this;
        }

        private Control     _control;
        private UIElement   _element;
    }
}