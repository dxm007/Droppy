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
// NOTES: This file contains random classes and declarations, which are used in the Droppy project
//        but which were not large or significant enough to warrant a dedicated source file.
//
//=================================================================================================
//=================================================================================================

using System;
using System.Windows;
using System.Windows.Controls;

namespace Droppy
{
    /// <summary>
    /// Unaffiliated, general extension methods which didn't belong anywhere else
    /// </summary>
    static class GeneralHelperExtensions
    {
        /// <summary>
        /// Provides a user-friendly test to see if an enumration has a specified flag. This function
        /// already exists in .NET Framework 4.0, but in prior versions, this extension method is it.
        /// </summary>
        /// <typeparam name="T">Any enumration type that has [Flags] attribute</typeparam>
        /// <param name="obj">An enumeration value being tested</param>
        /// <param name="value">Flag value being tested</param>
        /// <returns>True if 'obj' contains 'value' flag. Otherwise, returns false.</returns>
        public static bool HasFlag<T>( this Enum obj, T value )
        {
            return ( Convert.ToInt32( obj ) & Convert.ToInt32( value ) ) != 0;
        }
    }


    /// <summary>
    /// WPF-related helper extension methods used in Droppy
    /// </summary>
    static class WpfHelperExtensions
    {
        /// <summary>
        /// Calculates total width of a thickness object
        /// </summary>
        /// <param name="obj">Source thickness object</param>
        /// <returns>The sum of left and right thinkness properties</returns>
        public static double Width( this Thickness obj ) { return obj.Left + obj.Right; }

        /// <summary>
        /// Calculates total width of a thickness object
        /// </summary>
        /// <param name="obj">Source thickness object</param>
        /// <returns>The sum of top and bottom thickness properties</returns>
        public static double Height( this Thickness obj ) { return obj.Top + obj.Bottom; }

        /// <summary>
        /// Adds actual size function to a framework element 
        /// </summary>
        /// <param name="obj">Source framework element</param>
        /// <returns>A Size struct which is built from ActualWidth and ActualHeight of the 
        /// framework element</returns>
        public static Size ActualSize( this FrameworkElement obj )
        {
            return new Size( obj.ActualWidth, obj.ActualHeight );
        }

        /// <summary>
        /// Adds location function to a window
        /// </summary>
        /// <param name="obj">source window</param>
        /// <returns>A Point struct which is build from Width and Height of the window</returns>
        public static Point Location( this Window obj )
        {
            return new Point( obj.Left, obj.Top );
        }
    }


    /// <summary>
    /// Implements IWin32Window interface which is needed for interoperability with certain
    /// Windows Forms features.
    /// </summary>
    class Win32Window : System.Windows.Forms.IWin32Window
    {
        /// <summary>
        /// Initializing constructor. 
        /// </summary>
        /// <param name="dependencyObject">Any WPF object which is part of a visual tree the root of
        /// which leads to a Window. That window's handle will then be returned via IWin32Window interface</param>
        public Win32Window( DependencyObject dependencyObject )
            : this( Window.GetWindow( dependencyObject ) )
        {
        }

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="window">Window whose handle is to be returned via IWin32Window interface</param>
        public Win32Window( Window window )
        {
            _handle = new System.Windows.Interop.WindowInteropHelper( window ).Handle;
        }

        #region - - - - - - - IWin32Window Interface  - - - - - - - - - - - - -

        public IntPtr Handle { get { return _handle; } }

        #endregion

        private IntPtr      _handle;
    }

    /// <summary>
    /// Helper class which provides a short-hand syntax for initializing custom controls
    /// </summary>
    /// <remarks>
    /// This class was written as a result of refactoring of various custom control classes
    /// who all had a common task of initializing their member variables and setting up events
    /// during a call to OnApplyTemplate() method.
    /// 
    /// This class makes it simpler to write those operations by taking out redundant work
    /// that has to take place.
    /// 
    /// Example usage of this class:
    /// <code>
    ///     new ControlInitHelper( this ).
    ///            Element( "PART_Paste" ).
    ///                Get( out pasteMenuItem ).
    ///                Add( MenuItem.ClickEvent, new RoutedEventHandler( OnPaste ) ).
    ///            Element( "PART_ChangeLabel" ).
    ///                Add( MenuItem.ClickEvent, new RoutedEventHandler( 
    ///                                          ( o, e ) => IsLabelEditPopupOpen = true ) );
    ///
    /// </code>
    /// 
    /// Once a ControlInitHelper instance is created passing in custom control reference, any
    /// element can be looked up in the control template. Then a reference to that element can
    /// be assigned to an external variable or event subscriptions can be setup against that element.
    /// 
    /// Note that each of the member functions returns a reference back to ControlInitHelper which 
    /// is what allows multiple calls to be chained together as in the example above.
    /// </remarks>
    class ControlInitHelper
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="control">Custom control being initialized</param>
        public ControlInitHelper( Control control )
        {
            _element = null;
            _control = control;
        }

        /// <summary>
        /// Selects an element out of control's template 
        /// </summary>
        /// <param name="elementName">Name of the element to select</param>
        public ControlInitHelper Element( string elementName )
        {
            _element = (UIElement)_control.Template.FindName( elementName, _control );
            return this;
        }

        /// <summary>
        /// Returns a reference to an element which was selected with Element() call
        /// </summary>
        /// <typeparam name="TElem">Any type to which selected element reference can be cast to</typeparam>
        /// <param name="element">Output parameter which is to receive element reference</param>
        public ControlInitHelper Get<TElem>( out TElem element )
                where TElem : UIElement
        {
            element = (TElem)_element;
            return this;
        }

        /// <summary>
        /// Adds a routed event handler to an element which was selected with Element() call
        /// </summary>
        /// <param name="routedEvent">Identifies routed event for which to subscribe</param>
        /// <param name="eventHandler">Event handler to be invoked when an event is received</param>
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

        #endregion

        #region ----------------------- Private Members -----------------------

        private Control     _control;
        private UIElement   _element;

        #endregion
    }
}

