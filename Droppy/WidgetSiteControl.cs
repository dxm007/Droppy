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

        #region - - - - - - - - Undraggable Attached Routed Event - - - - - - -

        public static readonly RoutedEvent UndraggableEvent = 
                EventManager.RegisterRoutedEvent( "Undraggable", RoutingStrategy.Bubble,
                                                  typeof( RoutedEventHandler ), typeof( WidgetSiteControl ) );

        public static void AddUndraggableHandler( DependencyObject dependencyObject,
                                                  RoutedEventHandler eventHandler )
        {
            if( dependencyObject is UIElement )
            {
                ( (UIElement)dependencyObject ).AddHandler( UndraggableEvent, eventHandler );
            }
            else if( dependencyObject is ContentElement )
            {
                ( (ContentElement)dependencyObject ).AddHandler( UndraggableEvent, eventHandler );
            }
        }

        public static void RemoveUndraggableHandler( DependencyObject dependencyObject,
                                                     RoutedEventHandler eventHandler )
        {
            if( dependencyObject is UIElement )
            {
                ( (UIElement)dependencyObject ).RemoveHandler( UndraggableEvent, eventHandler );
            }
            else if( dependencyObject is ContentElement )
            {
                ( (ContentElement)dependencyObject ).RemoveHandler( UndraggableEvent, eventHandler );
            }
        }

        #endregion

        #region - - - - - - - - Draggable Attached Routed Event - - - - - - - -

        public static readonly RoutedEvent DraggableEvent = 
                EventManager.RegisterRoutedEvent( "Draggable", RoutingStrategy.Bubble,
                                                  typeof( RoutedEventHandler ), typeof( WidgetSiteControl ) );

        public static void AddDraggableHandler( DependencyObject dependencyObject,
                                                RoutedEventHandler eventHandler )
        {
            if( dependencyObject is UIElement )
            {
                ( (UIElement)dependencyObject ).AddHandler( DraggableEvent, eventHandler );
            }
            else if( dependencyObject is ContentElement )
            {
                ( (ContentElement)dependencyObject ).AddHandler( DraggableEvent, eventHandler );
            }
        }

        public static void RemoveDraggableHandler( DependencyObject dependencyObject,
                                                   RoutedEventHandler eventHandler )
        {
            if( dependencyObject is UIElement )
            {
                ( (UIElement)dependencyObject ).RemoveHandler( DraggableEvent, eventHandler );
            }
            else if( dependencyObject is ContentElement )
            {
                ( (ContentElement)dependencyObject ).RemoveHandler( DraggableEvent, eventHandler );
            }
        }

        #endregion

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
            WidgetSiteControl.AddUndraggableHandler( parentSite, OnWidgetUndraggable );
            WidgetSiteControl.AddDraggableHandler( parentSite, OnWidgetDraggable );
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

        protected override bool ValidateDragEventSource( MouseButtonEventArgs e )
        {
            return _dragFrozenCount == 0;
        }

        private void OnWidgetUndraggable( object sender, RoutedEventArgs e )
        {
            _dragFrozenCount++;
        }

        private void OnWidgetDraggable( object sender, RoutedEventArgs e )
        {
            System.Diagnostics.Debug.Assert( _dragFrozenCount > 0 );

            _dragFrozenCount--;
        }

        private int _dragFrozenCount;
    }
}
