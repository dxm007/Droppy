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
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;



namespace Droppy
{
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


    public class DropHelperEventArgs : EventArgs
    {
        public DropHelperEventArgs( object originalSender, DragEventArgs eventArgs )
        {
            _sender = originalSender;
            _eventArgs = eventArgs;
        }

        public object OriginalSender { get { return _sender; } }
        public DragEventArgs EventArgs { get { return _eventArgs; } }

        private object          _sender;
        private DragEventArgs   _eventArgs;
    }


    public class DropHelper
    {
        public DropHelper( UIElement target )
        {
            _target = target;

            _target.DragEnter += OnTargetDragEnter;
            _target.DragLeave += OnTargetDragLeave;
            _target.DragOver += OnTargetDragOver;
            _target.Drop += OnTargetDrop;
        }

        public UIElement Target { get { return _target; } }

        #region - - - - - - - - - IDragOver Attached Property - - - - - - - - - - - - - - - -

        public static readonly DependencyProperty IsDragOverProperty = 
                    DependencyProperty.RegisterAttached( "IsDragOver", typeof( bool ), typeof( DropHelper ),
                                                         new FrameworkPropertyMetadata( false )              );

        public static void SetIsDragOver( UIElement element, bool value )
        {
            element.SetValue( IsDragOverProperty, value );
        }

        public static bool GetIsDragOver( UIElement element )
        {
            return (bool)element.GetValue( IsDragOverProperty );
        }

        #endregion

        #region - - - - - - - - - DragStarted Attached Event  - - - - - - - - - - - - - - - -

        public static readonly RoutedEvent DragStartedEvent =
                    EventManager.RegisterRoutedEvent( "DragStarted", RoutingStrategy.Bubble,
                                                      typeof( RoutedEventHandler ), typeof( DropHelper ) );

        public static void AddDragStartedHandler( DependencyObject      dependencyObject,
                                                  RoutedEventHandler    eventHandler      )
        {
            if( dependencyObject is UIElement )
            {
                ( (UIElement)dependencyObject ).AddHandler( DragStartedEvent, eventHandler );
            }
            else if( dependencyObject is ContentElement )
            {
                ( (ContentElement)dependencyObject ).AddHandler( DragStartedEvent, eventHandler );
            }
        }

        public static void RemoveDragStartedHandler( DependencyObject       dependencyObject,
                                                     RoutedEventHandler     eventHandler      )
        {
            if( dependencyObject is UIElement )
            {
                ( (UIElement)dependencyObject ).RemoveHandler( DragStartedEvent, eventHandler );
            }
            else if( dependencyObject is ContentElement )
            {
                ( (ContentElement)dependencyObject ).RemoveHandler( DragStartedEvent, eventHandler );
            }
        }

        #endregion

        #region - - - - - - - - - DragStopped Attached Event  - - - - - - - - - - - - - - - -

        public static readonly RoutedEvent DragStoppedEvent =
                    EventManager.RegisterRoutedEvent( "DragStopped", RoutingStrategy.Bubble,
                                                      typeof( RoutedEventHandler ), typeof( DropHelper ) );

        public static void AddDragStoppedHandler( DependencyObject      dependencyObject,
                                                  RoutedEventHandler    eventHandler      )
        {
            if( dependencyObject is UIElement )
            {
                ( (UIElement)dependencyObject ).AddHandler( DragStoppedEvent, eventHandler );
            }
            else if( dependencyObject is ContentElement )
            {
                ( (ContentElement)dependencyObject ).AddHandler( DragStoppedEvent, eventHandler );
            }
        }

        public static void RemoveDragStoppedHandler( DependencyObject dependencyObject,
                                                     RoutedEventHandler eventHandler )
        {
            if( dependencyObject is UIElement )
            {
                ( (UIElement)dependencyObject ).RemoveHandler( DragStoppedEvent, eventHandler );
            }
            else if( dependencyObject is ContentElement )
            {
                ( (ContentElement)dependencyObject ).RemoveHandler( DragStoppedEvent, eventHandler );
            }
        }

        #endregion

        public event EventHandler< DropHelperEventArgs >    QueryDragDataValid;
        public event EventHandler< DropHelperEventArgs >    TargetDrop;
        public event EventHandler< DropHelperEventArgs >    RealTargetDragLeave;

        protected virtual void OnTargetDragEnter(object sender, DragEventArgs e)
        {
            _dragInProgress = true;

            OnQueryDragDataValid( sender, e );

            if( !_isDragStartedSignalled )
            {
                _target.RaiseEvent( new RoutedEventArgs( DragStartedEvent ) );
                _isDragStartedSignalled = true;
            }

            if( e.Effects != DragDropEffects.None )
            {
                e.Handled = true;

                if( !_isDragOverSignalled )
                {
                    SetIsDragOver( _target, true );
                    _isDragOverSignalled = true;
                }
            }
        }

        protected virtual void OnTargetDragLeave(object sender, DragEventArgs e)
        {
            if( _isDragOverSignalled ) e.Handled = true; 
            
            _dragInProgress = false;

            // It appears there's a quirk in the drag/drop system.  While the user is dragging the object
            // over our control it appears the system will send us (quite frequently) DragLeave followed 
            // immediately by DragEnter events.  So when we get DragLeave, we can't be sure that the 
            // drag/drop operation was actually terminated.  Therefore, instead of doing cleanup
            // immediately, we schedule the cleanup to execute later and if during that time we receive
            // another DragEnter or DragOver event, then we don't do the cleanup.
            _target.Dispatcher.BeginInvoke( new Action( ()=> {
                                    if( _dragInProgress == false ) OnRealTargetDragLeave( sender, e ); } ) );
        }

        protected virtual void OnTargetDragOver(object sender, DragEventArgs e)
        {
            if( _isDragOverSignalled ) e.Handled = true;

            _dragInProgress = true;

            OnQueryDragDataValid( sender, e );
        }

        protected virtual void OnTargetDrop(object sender, DragEventArgs e)
        {
            if( _isDragOverSignalled )
            {
                e.Handled = true;

                if( TargetDrop != null )
                {
                    TargetDrop( this, new DropHelperEventArgs( sender, e ) );
                }
            }

            FinalizeTargetDrag();
        }

        protected virtual void OnQueryDragDataValid( object sender, DragEventArgs eventArgs )
        {
            if( QueryDragDataValid != null )
            {
                QueryDragDataValid( this, new DropHelperEventArgs( sender, eventArgs ) );
            }
        }

        protected virtual void OnRealTargetDragLeave( object sender, DragEventArgs eventArgs )
        {
            if( RealTargetDragLeave != null )
            {
                RealTargetDragLeave( this, new DropHelperEventArgs( sender, eventArgs ) );
            }

            FinalizeTargetDrag();
        }

        private void FinalizeTargetDrag()
        {
            if( _isDragStartedSignalled )
            {
                _isDragStartedSignalled = false;
                _target.RaiseEvent( new RoutedEventArgs( DragStoppedEvent ) );
            }

            if( _isDragOverSignalled )
            {
                _isDragOverSignalled = false;
                SetIsDragOver( _target, false );
            }
        }


        private UIElement           _target;
        private bool                _dragInProgress;
        private bool                _isDragOverSignalled;
        private bool                _isDragStartedSignalled;
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

        protected virtual bool ValidateDragEventSource( MouseButtonEventArgs e )
        {
            return true;
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If a child control (such as a button) already has mouse capture, we don't want to take
            // it away from it.  Since it is a child, the events we need will still bubble up to us.
            // If there's no capture, make sure to get one here so there's no quirks listening to the
            // mouse
            if( ValidateDragEventSource( e ) &&
                ( Mouse.Captured != null ||
                  ( _isMouseCaptured = _dragSource.CaptureMouse() ) == true ) )
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
                if( _isMouseCaptured )
                {
                    _dragSource.ReleaseMouseCapture();
                    _isMouseCaptured = false;
                }

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

                if( _isMouseCaptured )
                {
                    _dragSource.ReleaseMouseCapture();
                    _isMouseCaptured = false;
                }

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
        private bool                _isMouseCaptured;
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

