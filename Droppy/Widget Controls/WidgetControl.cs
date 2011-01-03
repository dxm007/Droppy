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
    public class WidgetControl : Control
    {
        public WidgetSiteControl Site
        {
            get
            {
                if( _parentSite == null ) FindParent();

                return _parentSite;
            }
        }

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
