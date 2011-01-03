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
            return value.GetType() == typeof( EmptySiteClass) ? null : value;
        }
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

        public int ContainerRow { get { return (int)GetValue( Grid.RowProperty ); } }
        public int ContainerColumn { get { return (int)GetValue( Grid.ColumnProperty ); } }

        public WidgetMatrix ParentContainer
        {
            get
            {
                if( _parentContainer == null ) FindParent();

                return _parentContainer;
            }
        }

        public void SetWidget( Data.WidgetData widget )
        {
            ParentContainer.Source[ ContainerRow, ContainerColumn ] = widget;
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
    }


    public static class WpfHelperExtensions
    {
        public static double Width( this Thickness obj ) { return obj.Left + obj.Right; }
        public static double Height( this Thickness obj ) { return obj.Top + obj.Bottom; }
    }


    public class WidgetSiteDragDropData
    {
        public Data.WidgetData Widget { get; set; }
        public WidgetSiteControl Site { get; set; }
        public Point DraggableOffset { get; set; }
    }

    public class QueryDragDataEventArgs : EventArgs
    {
        public QueryDragDataEventArgs( Point draggableOffset )
        {
            _draggableOffset = draggableOffset;
            IsCancelled = false;
        }

        public Point DraggableOffset { get { return _draggableOffset; } }
        public bool IsCancelled { get; set; }
        public IDataObject DragData { get; set; }

        private Point       _draggableOffset;
    }

    public class DragHelperEventArgs : EventArgs
    {
        public DragHelperEventArgs( FrameworkElement source, IDataObject data, DragDropEffects effects )
        {
            _source = source;
            _data = data;
            _effects = effects;
        }

        public FrameworkElement DragSource { get { return _source; } }
        public IDataObject Data { get { return _data; } }
        public DragDropEffects Effects { get { return _effects; } }

        private FrameworkElement    _source;
        private IDataObject         _data;
        private DragDropEffects     _effects;
    }



    public class WidgetSiteDragHelper : DragHelper
    {
        public WidgetSiteDragHelper( WidgetSiteControl parentSite ) : base( parentSite )
        {
            
        }

        public WidgetSiteControl Parent { get { return (WidgetSiteControl)DragSource; } }

        //protected override void DoDragDrop()
        //{
        //    Popup trashCan = Parent.Template.FindName( "PART_TrashCanPopup", Parent ) as Popup;
        //    var x = Parent.Template.FindName( "PART_TrashCanPopup", Parent );

        //    if( trashCan != null )
        //    {
        //        trashCan.IsOpen = true;
        //    }

        //    base.DoDragDrop();

        //    if( trashCan != null ) trashCan.IsOpen = false;
        //}

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




    public class DragHelper
    {
        public DragHelper( FrameworkElement dragSource )
        {
            _dragSource = dragSource;

            _dragSource.AddHandler( FrameworkElement.MouseLeftButtonDownEvent,
                                    new MouseButtonEventHandler( OnMouseLeftButtonDown ), true );
            _dragSource.AddHandler( FrameworkElement.MouseLeftButtonUpEvent,
                                    new MouseButtonEventHandler( OnMouseLeftButtonUp ), true );
            _dragSource.AddHandler( FrameworkElement.MouseMoveEvent,
                                    new MouseEventHandler( OnMouseMove ), true );

            _dragSource.GiveFeedback += OnGiveFeedback;
        }

        public event EventHandler< QueryDragDataEventArgs >     QueryDragData;

        public static event EventHandler< DragHelperEventArgs > DragStarted;
        public static event EventHandler< DragHelperEventArgs > DragComplete;

        protected FrameworkElement DragSource { get { return _dragSource; } }
        
        protected virtual void OnQueryDragData( QueryDragDataEventArgs e )
        {
            if( QueryDragData != null )
            {
                QueryDragData( this, e );
            }
        }

        protected virtual void OnDragStarted( IDataObject data )
        {
            if( DragStarted != null )
            {
                DragStarted( this, new DragHelperEventArgs( _dragSource, data, 0 ) );
            }
        }

        protected virtual void OnDragComplete( IDataObject data, DragDropEffects effects )
        {
            if( DragComplete != null )
            {
                DragComplete( this, new DragHelperEventArgs( _dragSource, data, effects ) );
            }
        }

        protected virtual void DoDragDrop()
        {
            var eventArgs = new QueryDragDataEventArgs( _clickPoint );

            OnQueryDragData( eventArgs );

            if( eventArgs.IsCancelled || eventArgs.DragData == null ) return;

            OnDragStarted( eventArgs.DragData );

            _adorner = new DragDropAdorner( _dragSource, _clickPoint );

            DragDropEffects result = DragDrop.DoDragDrop(
                    _dragSource, eventArgs.DragData, DragDropEffects.Move | DragDropEffects.Link );

            _adorner.RemoveAdorner();
            _adorner = null;

            OnDragComplete( eventArgs.DragData, result );
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if( /*_dragSource.CaptureMouse()*/ true )
            {
                _isMouseDown = true;
                _clickPoint = e.GetPosition( _dragSource );
            }

            e.Handled = true;
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if( _isMouseDown )
            {
                //_dragSource.ReleaseMouseCapture();
                _isMouseDown = false;
            }

            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if( !_isMouseDown ) return;

            Point pt = e.GetPosition( _dragSource );
            Vector vect = Point.Subtract( pt, _clickPoint );

            if( vect.LengthSquared > 100 )
            {
                // Apparently ReleaseMouseCapture will enter a message pump and if we don't set
                // _isMouseDown flag to false first, we'll end up with a second OnMouseMove call 
                // entering the stack while the first OnMouseMove is inside ReleaseMouseCapture()
                _isMouseDown = false;
                //_dragSource.ReleaseMouseCapture();

                DoDragDrop();
            }

            e.Handled = true;
        }

        private void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            if( _adorner != null )
            {
                _adorner.SetPosition( MouseEx.GetPosition( _dragSource ) );
            }
        }

        private FrameworkElement    _dragSource;
        private bool                _isMouseDown;
        private Point               _clickPoint;
        private DragDropAdorner     _adorner;
    }


    public class DragDropAdorner : Adorner
    {
        public DragDropAdorner( FrameworkElement adornedElement, Point offset ) : base( adornedElement )
        {
            DrawingVisual   dv = new DrawingVisual();
            Visual          childVisual = (Visual)VisualTreeHelper.GetChild( adornedElement, 0 );
            VisualBrush     brush = new VisualBrush( childVisual );
            DrawingContext  dc = dv.RenderOpen();

            _offset = offset;

            dc.DrawRectangle( brush, null, new Rect( 0, 0, adornedElement.ActualWidth, adornedElement.ActualHeight ) );
            dc.Close();

            _translateTransform = new TranslateTransform();
            dv.Transform = _translateTransform;
            _childVisual = dv;
            
            AddVisualChild( _childVisual );

            _layer = AdornerLayer.GetAdornerLayer( adornedElement );
            _layer.Add( this ); 
           
            IsHitTestVisible = false;

            adornedElement.Visibility = Visibility.Hidden;
        }

        public void SetPosition( Point pt )
        {
            _translateTransform.X = pt.X - _offset.X;
            _translateTransform.Y = pt.Y - _offset.Y;
        }

        public void RemoveAdorner()
        {
            _layer.Remove( this );

            AdornedElement.Visibility = Visibility.Visible;
        }

        protected override int VisualChildrenCount { get { return 1; } }

        protected override Visual GetVisualChild( int index )
        {
            if( index != 0 ) throw new ArgumentOutOfRangeException();
                
            return _childVisual;
        }

        private AdornerLayer        _layer;
        private Visual              _childVisual;
        private TranslateTransform  _translateTransform;
        private Point               _offset;
    }
}
