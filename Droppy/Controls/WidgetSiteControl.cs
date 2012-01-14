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
    /// <summary>
    /// This is a NULL OBJECT class which is used for data context of those matrix cells whose actual
    /// source is 'null'.  This object is used so that control data DataTemplate properly selects the UI
    /// for an empty cell.
    /// </summary>
    class EmptySiteClass
    {
        /// <summary>
        /// Returns a singleton instance of EmptySiteClass object.  Having this class be a singleton makes
        /// comparisons for a null source binding very easy (i.e. if( x == EmptySiteClass.Instance ) ... )
        /// </summary>
        public static EmptySiteClass Instance { get { return _instance; } }

        private static EmptySiteClass _instance = new EmptySiteClass();
    }


    /// <summary>
    /// Value converter class to ensure that if cell data binding is null, EmptySiteClass object instance is
    /// used instead of the actual 'null' value.
    /// </summary>
    class WidgetSiteContentConverter : IValueConverter
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


    /// <summary>
    /// Defines an object which gets attached as 'data' of a drag-drop operation. This object allows drop targets to
    /// determine what kind of data is being dragged over them.
    /// </summary>
    class WidgetSiteDragDropData
    {
        /// <summary>
        /// Gets/sets a reference to underlying widget data, which is the source of the site control that is
        /// being dragged
        /// </summary>
        public Data.WidgetData Widget { get; set; }

        /// <summary>
        /// Gets/sets a reference to the site control which is being dragged
        /// </summary>
        public WidgetSiteControl Site { get; set; }

        /// <summary>
        /// Gets/sets the offset by which site control was dragged from its original position. This value would be
        /// more accurately represented by a vector instead of a point, but since there's no Vector data type, point
        /// is the next best thing since it has the same storage foot print.
        /// </summary>
        public Point DraggableOffset { get; set; }
    }


    /// <summary>
    /// Immediate child of a WidgetMatrix control. This control is responsible for hosting widget UI which gets loaded
    /// using DataTemplate.  The template is selected based on the bound data type of the source associated with the 
    /// matrix cell in which this site is located.
    /// </summary>
    class WidgetSiteControl : ContentControl
    {
        #region ----------------------- Public Members ------------------------

        public WidgetSiteControl()
        {
            new WidgetSiteDragHelper( this );
        }

        #region - - - - - - - Properties  - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// Gets/sets the location of the site within the parent widget matrix control
        /// </summary>
        public MatrixLoc Location
        {
            get { return _location; }
            set { _location = value; UpdateGridPosition(); }
        }

        /// <summary>
        /// Gets a reference to the parent widget matrix control
        /// </summary>
        public WidgetMatrix ParentContainer
        {
            get
            {
                if( _parentContainer == null ) FindParent();

                return _parentContainer;
            }
        }

        /// <summary>
        /// Gets the total height, which includes margins, that the site takes
        /// up in the layout
        /// </summary>
        public double HeightWithMargin
        {
            get { return this.ActualHeight + this.Margin.Height(); }
        }

        /// <summary>
        /// Gets the total width, which includes margins, that the site takes
        /// up in the layout
        /// </summary>
        public double WidthWithMargin
        {
            get { return this.ActualWidth + this.Margin.Width(); }
        }

        #endregion

        #region - - - - - - - Events  - - - - - - - - - - - - - - - - - - - - -

        #region - - - - - - - - Undraggable Attached Routed Event - - - - - - -

        /// <summary>
        /// Undraggable event can be raised by any child control in the widget site's visual tree
        /// whenever it wishes to suspend draggable ability of the site control.  When the site receives
        /// such an event, it won't allow drag operation to stop until Draggable event is received.
        /// These events are implemented with a lock count so same number of Draggable events as the
        /// number of Undraggable events must be received before the site can become draggable.
        /// </summary>
        public static readonly RoutedEvent UndraggableEvent = 
                EventManager.RegisterRoutedEvent( "Undraggable", RoutingStrategy.Bubble,
                                                  typeof( RoutedEventHandler ), typeof( WidgetSiteControl ) );

        /// <summary>
        /// Adds a handler for Undraggable attached event
        /// </summary>
        /// <param name="dependencyObject">The UIElement or ContentElement that listens for the event</param>
        /// <param name="eventHandler">The event handler</param>
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

        /// <summary>
        /// Removes a handler for Undraggable attached event
        /// </summary>
        /// <param name="dependencyObject">The UIElement or ContentElement that listens for the event</param>
        /// <param name="eventHandler">The event handler</param>
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

        /// <summary>
        /// Draggable event can be raised by any child control in the widget site's visual tree
        /// whenever it wishes to restore draggable ability of the site control.  When the site receives
        /// same number of these events as the number of Undraggable events previously received,
        /// drag ability of the site will be restored
        /// </summary>
        public static readonly RoutedEvent DraggableEvent = 
                EventManager.RegisterRoutedEvent( "Draggable", RoutingStrategy.Bubble,
                                                  typeof( RoutedEventHandler ), typeof( WidgetSiteControl ) );

        /// <summary>
        /// Adds a handler for Draggable attached event
        /// </summary>
        /// <param name="dependencyObject">The UIElement or ContentElement that listens for the event</param>
        /// <param name="eventHandler">The event handler</param>
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

        /// <summary>
        /// Removes a handler for Draggable attached event
        /// </summary>
        /// <param name="dependencyObject">The UIElement or ContentElement that listens for the event</param>
        /// <param name="eventHandler">The event handler</param>
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

        #endregion

        /// <summary>
        /// Associates a widget with the current site
        /// </summary>
        /// <param name="widget">Data object representing the widget</param>
        public void SetWidget( Data.WidgetData widget )
        {
            ParentContainer.Source[ this.Location ] = widget;
        }

        /// <summary>
        /// Updates site controls UI position in parent Grid panel based on the
        /// location of data bound widget data object within its parent widget container.
        /// </summary>
        /// <remarks>
        /// Widget data model consists of widget data objects which represent individual widgets
        /// and widget container objects which represent a 2D matrix of widgets. On the UI side
        /// widget containers are bound to source properties of WidgetMatrix controls which in
        /// turn create children WidgetSiteControl controls whose source is then bound to 
        /// corresponding widget data objects.
        /// 
        /// If parent widget container is resized, locations of widgets in the UI may need to
        /// be updated. The purpose of this method is to perform such an update to ensure the UI
        /// remains consistent with its underlying data model.
        /// </remarks>
        public void UpdateGridPosition()
        {
            MatrixLoc  gridPosition = ParentContainer.Source.Bounds.ToIndex( _location );

            SetValue( Grid.RowProperty, gridPosition.Row );
            SetValue( Grid.ColumnProperty, gridPosition.Column );
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        static WidgetSiteControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata( typeof( WidgetSiteControl ), new FrameworkPropertyMetadata( typeof( WidgetSiteControl ) ) );
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

        #endregion
    }


    /// <summary>
    /// Allows WidgetSiteControl controls to become draggable.
    /// </summary>
    class WidgetSiteDragHelper : DragHelper
    {
        public WidgetSiteDragHelper( WidgetSiteControl parentSite ) : base( parentSite )
        {
            WidgetSiteControl.AddUndraggableHandler( parentSite, OnWidgetUndraggable );
            WidgetSiteControl.AddDraggableHandler( parentSite, OnWidgetDraggable );
        }

        public WidgetSiteControl Parent { get { return (WidgetSiteControl)DragSource; } }

        /// <inheritdoc/>
        protected override void OnQueryDragData( QueryDragDataEventArgs e )
        {
            base.OnQueryDragData( e );

            var data = new WidgetSiteDragDropData() { DraggableOffset = e.DraggableOffset,
                                                      Site = Parent,
                                                      Widget = (Data.WidgetData)Parent.Content };

            var dataObject = new DataObject( data ); 

            e.DragData = dataObject;
        }

        /// <inheritdoc/>
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
