using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Droppy.Data;

namespace Droppy
{
    public class EmptySiteClass
    {
        public static EmptySiteClass Instance { get { return _instance; } }

        private static EmptySiteClass _instance = new EmptySiteClass();
    }


    public class WidgetSiteContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value ?? EmptySiteClass.Instance;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value.GetType() == typeof( EmptySiteClass ) ? null : value;
        }
    }


    public class WidgetSiteDragDropData
    {
        public Data.WidgetData Widget { get; set; }
        public WidgetSiteControl Site { get; set; }
        public Point DraggableOffset { get; set; }
    }





    /// <summary>
    /// </summary>
    public class WidgetSiteControl : ContentControl
    {
        static WidgetSiteControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata( typeof( WidgetSiteControl ), new FrameworkPropertyMetadata( typeof( WidgetSiteControl ) ) );
        }

        public WidgetSiteControl()
        {
            new WidgetSiteDragHelper( this );
        }

        public MatrixLoc Location
        {
            get { return _location; }
            set { _location = value; UpdateGridPosition(); }
        }

        public WidgetMatrix ParentContainer
        {
            get
            {
                if( _parentContainer == null ) FindParent();

                return _parentContainer;
            }
        }

        public double HeightWithMargin
        {
            get { return this.ActualHeight + this.Margin.Height(); }
        }

        public double WidthWithMargin
        {
            get { return this.ActualWidth + this.Margin.Width(); }
        }

        public void SetWidget( Data.WidgetData widget )
        {
            ParentContainer.Source[ this.Location ] = widget;
        }

        public void UpdateGridPosition()
        {
            MatrixLoc  gridPosition = ParentContainer.Source.Bounds.ToIndex( _location );

            SetValue( Grid.RowProperty, gridPosition.Row );
            SetValue( Grid.ColumnProperty, gridPosition.Column );
        }

        private static void OnSourceChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            ( (WidgetSiteControl)d ).OnSourceChanged( e );
        }

        private void OnSourceChanged( DependencyPropertyChangedEventArgs e )
        {
        }

        private void FindParent()
        {
            DependencyObject obj = LogicalTreeHelper.GetParent( this );

            while( obj != null )
            {
                _parentContainer = obj as WidgetMatrix;

                if( _parentContainer != null ) break;

                obj = LogicalTreeHelper.GetParent( obj );
            }
        }

        private WidgetMatrix            _parentContainer;
        private MatrixLoc               _location;
    }



    public class WidgetSiteDragHelper : DragHelper
    {
        public WidgetSiteDragHelper( WidgetSiteControl parentSite ) : base( parentSite )
        {
        }

        public WidgetSiteControl Parent { get { return (WidgetSiteControl)DragSource; } }

        protected override void OnQueryDragData( QueryDragDataEventArgs e )
        {
            base.OnQueryDragData( e );

            var data = new WidgetSiteDragDropData() { DraggableOffset = e.DraggableOffset,
                                                      Site = Parent,
                                                      Widget = (Data.WidgetData)Parent.Content };

            var dataObject = new DataObject( data ); 

            e.DragData = dataObject;
        }
    }
}
