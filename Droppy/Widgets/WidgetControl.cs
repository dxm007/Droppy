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
    abstract class WidgetControl : Control
    {
        public WidgetSiteControl Site
        {
            get
            {
                if( _parentSite == null ) FindParent();

                return _parentSite;
            }
        }

        public WidgetControl()
        {
            AddHandler( Button.ClickEvent, new RoutedEventHandler( OnClick ) );
        }

        protected abstract void OnClick( object sender, RoutedEventArgs e );

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
    }
}
