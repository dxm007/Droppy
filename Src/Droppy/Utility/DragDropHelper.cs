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
    /// <summary>
    /// Event data object for DragHelper's QueryDragData event
    /// </summary>
    class QueryDragDataEventArgs : EventArgs
    {
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="draggableOffset">Difference between the location where mouse was clicked on a 
        /// draggable control and the top-left corner of that control.</param>
        public QueryDragDataEventArgs( Point draggableOffset )
        {
            _draggableOffset = draggableOffset;
            IsCancelled = false;
        }

        /// <summary>
        /// Gets the difference between the location where the mouse was clicked on a draggable control
        /// and the top-left corner of that control
        /// </summary>
        public Point DraggableOffset { get { return _draggableOffset; } }

        /// <summary>
        /// Gets/sets a flag that indicates whether or not drag operation should be cancelled. This property
        /// allows any event listener to cancel a drag operation
        /// </summary>
        public bool IsCancelled { get; set; }

        /// <summary>
        /// Gets/sets data object that is associated with the drag operation. Since base DragHelper class
        /// has no application-specific logic, this property is null unless an event handler fills it in.
        /// </summary>
        public IDataObject DragData { get; set; }

        private Point       _draggableOffset;
    }


    /// <summary>
    /// Event data object for DragHelper's DragStarted/DragStopped events 
    /// </summary>
    class DragHelperEventArgs : EventArgs
    {
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="source">Control being dragged</param>
        /// <param name="data">Data object associated with a drag operation</param>
        /// <param name="effects">Drag-drop effects associated with current drag operation</param>
        public DragHelperEventArgs( FrameworkElement source, IDataObject data, DragDropEffects effects )
        {
            _source = source;
            _data = data;
            _effects = effects;
        }

        /// <summary>
        /// Gets a control that is being dragged
        /// </summary>
        public FrameworkElement DragSource { get { return _source; } }

        /// <summary>
        /// Gets a data object that is associated with the drag operation
        /// </summary>
        public IDataObject Data { get { return _data; } }

        /// <summary>
        /// Gets drag-drop effects associated with the current drag operation
        /// </summary>
        public DragDropEffects Effects { get { return _effects; } }

        private FrameworkElement    _source;
        private IDataObject         _data;
        private DragDropEffects     _effects;
    }


    /// <summary>
    /// Event data object for events fired by DropHelper. 
    /// </summary>
    public class DropHelperEventArgs : EventArgs
    {
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="originalSender">Original sender that received drop event</param>
        /// <param name="eventArgs">Event data from original drop event</param>
        public DropHelperEventArgs( object originalSender, DragEventArgs eventArgs )
        {
            _sender = originalSender;
            _eventArgs = eventArgs;
        }

        /// <summary>
        /// Gets original sender that received the drop event.
        /// </summary>
        public object OriginalSender { get { return _sender; } }

        /// <summary>
        /// Gets event data object that was associated with original drop event
        /// </summary>
        public DragEventArgs EventArgs { get { return _eventArgs; } }

        private object          _sender;
        private DragEventArgs   _eventArgs;
    }


    /// <summary>
    /// Helper behavior class for UI elements that wish to act as drop targets for drag-drop
    /// operations
    /// </summary>
    /// <remarks>
    /// This class is built on WPF's standard drag-drop events (DragEnter, DragOver,
    /// DragLeave and Drop) but this class adds certain "convenience" functionality that
    /// makes implementing a drop target much easier.  This includes:
    ///     1) Work around for a bug in WPF where many enter/leave events are generated while
    ///        an object is being dragged over the control.  This class exposes RealTargetDragLeave
    ///        event which only gets fired when the dragged object truly leaves the target area
    ///     2) Defines an IsDragOver boolean attached property that gets set to true whenever
    ///        drag operation enters the drop target area AND object being dragged is valid for
    ///        being dropped.
    ///     3) Defines DragStarted/DragStopped attached events which get fired whenever ANY drag
    ///        operation is detected over the control. This includes objects which may not be 
    ///        valid for the current drop target
    /// </remarks>
    public class DropHelper
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="target">Parent element that wishes to act as a drop target</param>
        public DropHelper( UIElement target )
        {
            _target = target;

            _target.DragEnter += OnTargetDragEnter;
            _target.DragLeave += OnTargetDragLeave;
            _target.DragOver += OnTargetDragOver;
            _target.Drop += OnTargetDrop;
        }

        /// <summary>
        /// Gets the parent element that is acting as the drop target
        /// </summary>
        public UIElement Target { get { return _target; } }

        #region - - - - - - - - - IDragOver Attached Property - - - - - - - - - - - - - - - -

        /// <summary>
        /// Identifies IsDragOver attached property. This property indicates when a valid object
        /// for a drop operation is hoving over the drop target control
        /// </summary>
        public static readonly DependencyProperty IsDragOverProperty = 
                    DependencyProperty.RegisterAttached( "IsDragOver", typeof( bool ), typeof( DropHelper ),
                                                         new FrameworkPropertyMetadata( false )              );

        private static void SetIsDragOver( UIElement element, bool value )
        {
            element.SetValue( IsDragOverProperty, value );
        }

        private static bool GetIsDragOver( UIElement element )
        {
            return (bool)element.GetValue( IsDragOverProperty );
        }

        #endregion

        #region - - - - - - - - - DragStarted Attached Event  - - - - - - - - - - - - - - - -

        /// <summary>
        /// Identifies DragStarted attached routed event. This event gets fired whenever drop target
        /// control detects a drag operation entering its boundary. This event is fired regardless of
        /// whether or not drop target is able to accept the drop
        /// </summary>
        public static readonly RoutedEvent DragStartedEvent =
                    EventManager.RegisterRoutedEvent( "DragStarted", RoutingStrategy.Bubble,
                                                      typeof( RoutedEventHandler ), typeof( DropHelper ) );

        /// <summary>
        /// Adds a handler for DragStarted attached event
        /// </summary>
        /// <param name="dependencyObject">The UIElement or ContentElement that listens for the event</param>
        /// <param name="eventHandler">The event handler</param>
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

        /// <summary>
        /// Removes a handler for DragStarted attached event
        /// </summary>
        /// <param name="dependencyObject">The UIElement or ContentElement that listens for the event</param>
        /// <param name="eventHandler">The event handler</param>
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

        /// <summary>
        /// Identifies DragStopped attached routed event. This event gets fired whenever drop target
        /// control detects a drag operation leaving its boundary.
        /// </summary>
        public static readonly RoutedEvent DragStoppedEvent =
                    EventManager.RegisterRoutedEvent( "DragStopped", RoutingStrategy.Bubble,
                                                      typeof( RoutedEventHandler ), typeof( DropHelper ) );

        /// <summary>
        /// Adds a handler for DragStopped attached event
        /// </summary>
        /// <param name="dependencyObject">The UIElement or ContentElement that listens for the event</param>
        /// <param name="eventHandler">The event handler</param>
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

        /// <summary>
        /// Adds a handler for DragStopped attached event
        /// </summary>
        /// <param name="dependencyObject">The UIElement or ContentElement that listens for the event</param>
        /// <param name="eventHandler">The event handler</param>
        public static void RemoveDragStoppedHandler( DependencyObject   dependencyObject,
                                                     RoutedEventHandler eventHandler      )
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

        /// <summary>
        /// Gets fired when DropHelper needs to determine if drop target is able to accept an
        /// object that is dragged over it. UI control acting as a drop target must either
        /// subscribe for this event or define a class deriving from DropHelper where
        /// OnQueryDragDataValid is overriden.
        /// </summary>
        public event EventHandler< DropHelperEventArgs >    QueryDragDataValid;

        /// <summary>
        /// Gets fired whenever an object is dropped within the drop target control
        /// </summary>
        public event EventHandler< DropHelperEventArgs >    TargetDrop;

        /// <summary>
        /// Gets fired whenever a dragged object leaves drop target control without being dropped
        /// This event is similar to WPF's DragLeave but it is provided as a workaround because a
        /// client listening for this one will only be notified once when the event really happens,
        /// not a whole bunch of times while the object is dragged over the target.
        /// </summary>
        public event EventHandler< DropHelperEventArgs >    RealTargetDragLeave;

        #endregion

        #region ----------------------- Protected Members ---------------------

        /// <summary>
        /// Gets invoked when drop helper object receives target control's DragEnter event. Deriving
        /// class should make sure to call the base implementation of this method
        /// </summary>
        /// <param name="sender">Sender of DragEnter event</param>
        /// <param name="e">Event data associated with DragEnter event</param>
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

        /// <summary>
        /// Gets invoked when drop helper object receives target control's DragLeave event. Deriving
        /// class should make sure to call the base implementation of this method
        /// </summary>
        /// <param name="sender">Sender of DragLeave event</param>
        /// <param name="e">Event data associated with DragLeave event</param>
        protected virtual void OnTargetDragLeave( object sender, DragEventArgs e )
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

        /// <summary>
        /// Gets invoked when drop helper object receives target control's DragOver event. Deriving
        /// class should make sure to call the base implementation of this method
        /// </summary>
        /// <param name="sender">Sender of DragOver event</param>
        /// <param name="e">Event data associated with DragOver event</param>
        protected virtual void OnTargetDragOver( object sender, DragEventArgs e )
        {
            if( _isDragOverSignalled ) e.Handled = true;

            _dragInProgress = true;

            OnQueryDragDataValid( sender, e );
        }

        /// <summary>
        /// Gets invoked when drop helper object receives target control's Drop event. Deriving
        /// class should make sure to call the base implementation of this method
        /// </summary>
        /// <param name="sender">Sender of Drop event</param>
        /// <param name="e">Event data associated with Drop event</param>
        protected virtual void OnTargetDrop( object sender, DragEventArgs e )
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

        /// <summary>
        /// Gets invoked when drop helper wishes to determine what to do with the object that is being
        /// dragged over the drop target. This function gets called both when the object first enters the
        /// target's boundaries as well as the object is dragged over the target.
        /// </summary>
        /// <remarks>
        /// In order for drop target to be able to accept the object being dragged, eventArgs that is being
        /// passed into this method must be modified. Base implementation of this method will fire the event,
        /// giving drop target control a chance to subscribe for that event and modify 'eventArgs'. The other
        /// option is to provide a deriving class with an override of this method.
        /// </remarks>
        /// <param name="sender">Original sender that fired DragEnter/DragOver event that resulted in this call</param>
        /// <param name="eventArgs">Event data object which is associated with DragEnter/DragOver event. This object
        /// contains 'Effects' property which must be modified in order to signal that drop is possible.</param>
        protected virtual void OnQueryDragDataValid( object sender, DragEventArgs eventArgs )
        {
            if( QueryDragDataValid != null )
            {
                QueryDragDataValid( this, new DropHelperEventArgs( sender, eventArgs ) );
            }
        }

        /// <summary>
        /// Gets invoked when dragged object actually leaves the drop target control's boundaries without being
        /// dropped. This is similar to DragLeave event but won't give the listener false positives.
        /// </summary>
        /// <param name="sender">Original sender that fired DragLeave event</param>
        /// <param name="eventArgs">Event data object which is associated with DragLeave event</param>
        protected virtual void OnRealTargetDragLeave( object sender, DragEventArgs eventArgs )
        {
            if( RealTargetDragLeave != null )
            {
                RealTargetDragLeave( this, new DropHelperEventArgs( sender, eventArgs ) );
            }

            FinalizeTargetDrag();
        }

        #endregion

        #region ----------------------- Private Members -----------------------

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

        #endregion
    }


    /// <summary>
    /// Helper behavior class for UI elements that wish to act as the drag source for drag-drop
    /// operation
    /// </summary>
    /// <remarks>
    /// This class monitors mouse events on drag source UI element. When a left button is clicked and
    /// then the mouse is moved a certain distance, drag operation is initiated.
    /// 
    /// In order for drag operation to start, data object must be provided. This can be done by either
    /// subscribing to QueryDragData event or by overriding OnQueryDragData in a deriving class. If the
    /// data object is not provided, drag operation is aborted.
    /// 
    /// When drag operation starts, this class creates DragDropAdorner which uses a VisualBrush to
    /// duplicate what drag source UI element looks like. It then hides the original UI element for the
    /// duration of the drag operation so to the user it appears as if the drag source element was
    /// picked up and is being dragged with the mouse.
    /// </remarks>
    class DragHelper
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="dragSource">Element to be the source of a drag operation</param>
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

        #region - - - - - - - Events  - - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// Gets fired when drag is first initiated by the source.
        /// </summary>
        /// <remarks>QueryDragDataEventArgs contains a property, DragData, which must be filled in
        /// with a valid data object in order for the drag operation to begin. This data object must
        /// be filled in either by the listener of this event or by a deriving class that provides 
        /// an override for OnQueryDragData method.
        /// </remarks>
        public event EventHandler< QueryDragDataEventArgs >     QueryDragData;

        /// <summary>
        /// Gets fired when drag source successfully initiates a drag operation
        /// </summary>
        public static event EventHandler< DragHelperEventArgs > DragStarted;

        /// <summary>
        /// Gets fired when drag source completes a drag operation
        /// </summary>
        public static event EventHandler< DragHelperEventArgs > DragComplete;

        #endregion
        #endregion

        #region ----------------------- Protected Members ---------------------

        /// <summary>
        /// Gets drag source UI element
        /// </summary>
        protected FrameworkElement DragSource { get { return _dragSource; } }
        
        /// <summary>
        /// Gets invoked when drag helper first attempts to initiate a drag operation.
        /// </summary>
        /// <param name="e">Event data object</param>
        /// <remarks>The event data contains DragData field which must be filled in with a valid data
        /// object. The base implementation of this method fires an event giving any listener a chance
        /// to provide the data object. Alternatively, a deriving class can provide it's own override
        /// of this method and fill in data object there. Without the data object, drag operation will
        /// not be started. It is advisable for derived class to make sure base implementation of this
        /// method is called.</remarks>
        protected virtual void OnQueryDragData( QueryDragDataEventArgs e )
        {
            if( QueryDragData != null )
            {
                QueryDragData( this, e );
            }
        }

        /// <summary>
        /// Gets invoked when a drag operation is first started
        /// </summary>
        /// <param name="data">Data object which was constructed in QueryDragData.</param>
        protected virtual void OnDragStarted( IDataObject data )
        {
            if( DragStarted != null )
            {
                DragStarted( this, new DragHelperEventArgs( _dragSource, data, 0 ) );
            }
        }

        /// <summary>
        /// Gets invoked when a drag operation is completed
        /// </summary>
        /// <param name="data">Data object which was constructed in QueryDragData.</param>
        /// <param name="effects">The result of drag-drop operation as returned from DoDragDrop()</param>
        protected virtual void OnDragComplete( IDataObject data, DragDropEffects effects )
        {
            if( DragComplete != null )
            {
                DragComplete( this, new DragHelperEventArgs( _dragSource, data, effects ) );
            }
        }

        /// <summary>
        /// Gets invoked when drag helper detects that a drag source has been dragged far enough
        /// for drag operation to initiate. If this method is overriden, deriving class must ensure
        /// that base implementation is invoked.
        /// </summary>
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

        #endregion

        #region ----------------------- Private Members -----------------------

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If a child control (such as a button) already has mouse capture, we don't want to take
            // it away from it.  Since it is a child, the events we need will still bubble up to us.
            // If there's no capture, make sure to get one here so there's no quirks listening to the
            // mouse
            if( Mouse.Captured != null ||
                ( _isMouseCaptured = _dragSource.CaptureMouse() ) == true )
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

        #endregion
    }


    /// <summary>
    /// Used by DragHelper class to create an adorner which appears just like the UI element that is acting as
    /// the drag source. This adorner is used to make it look like original element is being dragged by the mouse
    /// While the adorner is active, it makes the original element invisible.
    /// </summary>
    class DragDropAdorner : Adorner
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="adornedElement">Element whose UI is to be converted into an adorner</param>
        /// <param name="offset">Location within 'adornedElement' where mouse cursor was actually clicked</param>
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

        /// <summary>
        /// Updates the position of the adorner which displays the draggable element
        /// </summary>
        /// <param name="pt">Current mouse position in screen coordinates</param>
        public void SetPosition( Point pt )
        {
            _translateTransform.X = pt.X - _offset.X;
            _translateTransform.Y = pt.Y - _offset.Y;
        }

        /// <summary>
        /// Deactivates the adorner and restores visibility of the original UI element
        /// </summary>
        public void RemoveAdorner()
        {
            _layer.Remove( this );

            AdornedElement.Visibility = Visibility.Visible;
        }

        #endregion

        #region ----------------------- Protected Members ---------------------

        /// <inheritdoc/>
        protected override int VisualChildrenCount { get { return 1; } }

        /// <inheritdoc/>
        protected override Visual GetVisualChild( int index )
        {
            if( index != 0 ) throw new ArgumentOutOfRangeException();
                
            return _childVisual;
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        private AdornerLayer        _layer;
        private Visual              _childVisual;
        private TranslateTransform  _translateTransform;
        private Point               _offset;

        #endregion
    }

}

