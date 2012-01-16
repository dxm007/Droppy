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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace Droppy
{
    /// <summary>
    /// Base class for a control which is to be loaded into the widget site to present a UI for 
    /// one of WidgetData-derived classes from Droppy.Data namespace
    /// </summary>
    abstract class WidgetControl : Control
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Default constructor
        /// </summary>
        public WidgetControl()
        {
            AddHandler( Button.ClickEvent, new RoutedEventHandler( OnClick ) );
        }

        /// <summary>
        /// Gets a reference to the parent widget site control
        /// </summary>
        public WidgetSiteControl Site
        {
            get
            {
                if( _parentSite == null ) FindParent();

                return _parentSite;
            }
        }

        #endregion

        #region ----------------------- Protected Members ---------------------

        /// <summary>
        /// Gets invoked when a mouse click is detected in the widget.
        /// </summary>
        /// <param name="sender">Sender of the original button click event</param>
        /// <param name="e">Data object of the original button click event</param>
        /// <remarks> Since all widgets are essentially buttons which are designed to be clickable,
        /// this is just a small optimization that moved few lines of code into a base class</remarks>
        protected abstract void OnClick( object sender, RoutedEventArgs e );

        #endregion

        #region ----------------------- Private Members -----------------------

        private void FindParent()
        {
            DependencyObject obj = VisualTreeHelper.GetParent( this );

            while( obj != null )
            {
                _parentSite = obj as WidgetSiteControl;

                if( _parentSite != null ) break;

                obj = VisualTreeHelper.GetParent( obj );
            }
        }

        private WidgetSiteControl _parentSite;

        #endregion
    }
}
