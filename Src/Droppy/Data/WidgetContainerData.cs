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
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Droppy.Data
{
    /// <summary>
    /// Specifies the justification of existing widget container cells when the container is being resized
    /// </summary>
    [Flags]
    enum WidgetContainerResizeJustify
    {
        Left    = 0x00,
        Right   = 0x01,
        Top     = 0x00,
        Bottom  = 0x02
    }


    /// <summary>
    /// Event arguments for collection change events raised by the WidgetContainerData class
    /// </summary>
    class WidgetContainerChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="action">Specifies if a widget was added, removed or replaced</param>
        /// <param name="location">Location of the changed widget</param>
        /// <param name="oldWidget">Reference to the original widget, or null if there wasn't any</param>
        /// <param name="newWidget">Reference to the new widget, or null if there isn't any</param>
        public WidgetContainerChangedEventArgs( NotifyCollectionChangedAction   action,
                                                MatrixLoc                       location,
                                                WidgetData                      oldWidget,
                                                WidgetData                      newWidget )
        {
            _location = location;
            _action = action;
            _oldWidget = oldWidget;
            _newWidget = newWidget;
        }

        /// <summary>
        /// Specifies if a widget was added, removed or replaced
        /// </summary>
        public NotifyCollectionChangedAction Action { get { return _action; } }

        /// <summary>
        /// Location of the changed widget
        /// </summary>
        public WidgetData OldWidget { get { return _oldWidget; } }

        /// <summary>
        /// Reference to the new widget, or null if there isn't any
        /// </summary>
        public WidgetData NewWidget { get { return _newWidget; } }

        /// <summary>
        /// Reference to the original widget, or null if there wasn't any
        /// </summary>
        public MatrixLoc Location { get { return _location; } }

        private NotifyCollectionChangedAction   _action;
        private MatrixLoc                       _location;
        private WidgetData                      _oldWidget;
        private WidgetData                      _newWidget;
    }


    /// <summary>
    /// Event handler type for WidgetContainerData's collection change events
    /// </summary>
    /// <param name="sender">Reference to the object raising the event</param>
    /// <param name="e">Event data</param>
    delegate void WidgetContainerChangedEventHandler( object sender, WidgetContainerChangedEventArgs e );


    /// <summary>
    /// Data object for a widget container.  The container itself is a type of widget. This allows the possibility
    /// of a container being nested within another container creating a hierarchy of widgets.
    /// </summary>
    [Serializable]
    class WidgetContainerData : WidgetData
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Default constructor
        /// </summary>
        public WidgetContainerData()
        {
        }

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="numRows">Number of rows to allocate in the container</param>
        /// <param name="numColumns">Number of columns to allocate in the container</param>
        public WidgetContainerData( int numRows, int numColumns )
        {
            _widgetArray = new Array2D<WidgetData>( numRows, numColumns );
            _containerBounds.Size = _widgetArray.Size;
        }

        #region - - - - - - - Properties  - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// Gets the coordinates of the rectangle bounding the container. Note that the bounds include size as well
        /// as the location of top-left corner of the rectangle. This is necessary since its possible to resize the
        /// container with right/button justification which will make left/top edge of the container to change in
        /// position
        /// </summary>
        public MatrixRect Bounds { get { return _containerBounds; } }

        /// <summary>
        /// Gets the size of the widget container
        /// </summary>
        public MatrixSize Size { get { return this.Bounds.Size; } }

        /// <summary>
        /// Gets/sets a widget at a specified location in the container
        /// </summary>
        /// <param name="location">Location to access/modify</param>
        public WidgetData this[ MatrixLoc location ]
        {
            get { return _widgetArray[ _containerBounds.ToIndex( location ) ]; }
            set { SetWidget( location, value ); }
        }

        #endregion 

        #region - - - - - - - Events  - - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// Gets fired whenever a widget is added, removed or replaced in the container
        /// </summary>
        public event WidgetContainerChangedEventHandler  ContainerChanged
        {
            add { _containerChangedEvent += value; }
            remove { _containerChangedEvent -= value; }
        }

        /// <summary>
        /// Gets fired whenever container size changes
        /// </summary>
        public event EventHandler ContainerResized
        {
            add { _containerResizedEvent += value; }
            remove { _containerResizedEvent -= value; }
        }

        #endregion

        /// <summary>
        /// Resizes the container to the specified size
        /// </summary>
        /// <param name="numRows">Number of rows to resize to</param>
        /// <param name="numColumns">Number of columns to resize to</param>
        /// <param name="justify">Resize justification, which indicates the edge at which widgets are to
        /// remain in their current positions (i.e. right justification will make the left edge move left/right
        /// while right edge's location stays constant)</param>
        public void Resize( int numRows, int numColumns, WidgetContainerResizeJustify justify )
        {
            Resize( new MatrixSize( numRows, numColumns ), justify );
        }

        /// <summary>
        /// Resizes the container to the specified size
        /// </summary>
        /// <param name="newSize">New size to which the container is to be resized</param>
        /// <param name="justify">Resize justification, which indicates the edge at which widgets are to
        /// remain in their current positions (i.e. right justification will make the left edge move left/right
        /// while right edge's location stays constant)</param>
        public void Resize( MatrixSize newSize, WidgetContainerResizeJustify justify )
        {
            if( newSize.RowCount > 256 || newSize.ColumnCount > 256 )
            {
                throw new ApplicationException( string.Format( "Attemp to allocate too much ({0} x {1})",
                                                               newSize.RowCount, newSize.ColumnCount ) );
            }
            else if( newSize == this.Bounds.Size )
            {
                return;
            }

            Validate();

            MatrixSize currentSize = this.Size;
            MatrixSize originShift = currentSize - newSize;

            if( !justify.HasFlag( WidgetContainerResizeJustify.Bottom ) ) originShift.RowCount = 0;
            if( !justify.HasFlag( WidgetContainerResizeJustify.Right ) ) originShift.ColumnCount = 0;

            MatrixRect newContainerBounds = new MatrixRect( 
                            _containerBounds.Location + originShift, newSize );
            
            // Cells in the intersection of original bounding rectangle and the new bounding
            // rectangle are the ones that will survive the resize operation. Once we identify
            // the intersection of the two rectangles, we need to iterate through the original
            // array and copy those cells into the new array
            MatrixRect existingCellBounds = _containerBounds.Intersect( newContainerBounds );

            // normalize existing cell bounding box around 0-based arrays. 
            existingCellBounds.Location = 
                    new MatrixLoc( originShift.RowCount > 0 ? originShift.RowCount : 0,
                                   originShift.ColumnCount > 0 ? originShift.ColumnCount : 0 );

            Array2D< WidgetData > newArray = new Array2D<WidgetData>( newSize );

            foreach( var loc in existingCellBounds )
            {
                newArray[ loc - originShift ] = _widgetArray[ loc ];
            }
                
            _widgetArray = newArray;
            _containerBounds = newContainerBounds;

            this.IsDirty = true;

            Validate();

            OnContainerResized();

            Validate();
        }

        /// <summary>
        /// Removes specified widget from the container.
        /// </summary>
        /// <param name="widget">Widget to be removed. If the widget isn't in the current container, no action
        /// will take place</param>
        public void Remove( WidgetData widget )
        {
            if( widget.Parent == this )
            {
                if( this[ widget.Location ] == widget )
                {
                    this[ widget.Location ] = null;
                }
                else
                {
                    System.Diagnostics.Debug.Assert( false, "widget location doesn't match its parent!!" );
                    throw new InvalidProgramException();
                }
            }

            Validate();
        }

        /// <inheritdoc/>
        public override void ClearDirtyFlag( bool includeChildren )
        {
            base.ClearDirtyFlag( includeChildren );

            if( includeChildren )
            {
                IterateChildren( ( loc, widget ) =>
                { 
                    if( widget != null ) widget.ClearDirtyFlag( true );
                } ); 
            }
        }

        /// <inheritdoc/>
        public override void PostDeserialize()
        {
            var widgetArraySize = _widgetArray.Size;

            if( _containerBounds.Size != widgetArraySize )
            {
                _containerBounds.Location = new MatrixLoc();
                _containerBounds.Size = widgetArraySize;
            }

            IterateChildren( ( loc, widget ) =>
            {
                if( widget != null )
                {
                    // If something went horribly wrong and this object ended up with some child
                    // that doesn't actually belong to it, let's erase it now after deserialization
                    if( widget.Parent != this )
                    {
                        this[ loc ] = null;
                    }
                    else
                    {
                        widget.SetOwner( this, loc );

                        widget.PostDeserialize();

                        // just in case the subscription is already set up, let's try to kill it.
                        // if it is not setup, the "-=" won't have any effect
                        widget.IsDirtyChanged -= OnChildIsDirtyChanged;
                        widget.IsDirtyChanged += OnChildIsDirtyChanged;
                    }
                }
            } );
        }

        /// <summary>
        /// Validation function used for debugging. It ensures that each control returns its own location which
        /// matches its position in the container array.
        /// </summary>
        public void Validate()
        {
            IterateChildren( ( loc, widget ) =>
            {
                System.Diagnostics.Debug.Assert( 
                    widget == null || ( widget.Parent == this && widget.Location == loc ) );
            } );
        }

        #endregion

        #region ----------------------- Protected Members ---------------------

        /// <summary>
        /// This method is invoked whenever a widget is added
        /// </summary>
        /// <param name="location">Location where new widget was added</param>
        /// <param name="widget">Reference to a widget that was added</param>
        protected virtual void OnWidgetAdded( MatrixLoc location, WidgetData widget )
        {
            if( _containerChangedEvent != null )
            {
                _containerChangedEvent( this, new WidgetContainerChangedEventArgs(
                                        NotifyCollectionChangedAction.Add, location, null, widget ) );
            }
        }

        /// <summary>
        /// This method is invoked whenever a widget is replaced
        /// </summary>
        /// <param name="location">Location where a widget was replaced</param>
        /// <param name="oldWidget">Reference to an old widget that was replaced</param>
        /// <param name="newWidget">Reference to a new widget that is being inserted</param>
        protected virtual void OnWidgetReplaced( MatrixLoc location, WidgetData oldWidget, WidgetData newWidget )
        {
            if( _containerChangedEvent != null )
            {
                _containerChangedEvent( this, new WidgetContainerChangedEventArgs(
                                        NotifyCollectionChangedAction.Replace, location, oldWidget, newWidget ) );
            }
        }

        /// <summary>
        /// This method is invoked whenever a widget is removed
        /// </summary>
        /// <param name="location">Location where a widget is being removed</param>
        /// <param name="widget">Reference to a widget that was removed</param>
        protected virtual void OnWidgetRemoved( MatrixLoc location, WidgetData widget )
        {
            if( _containerChangedEvent != null )
            {
                _containerChangedEvent( this, new WidgetContainerChangedEventArgs(
                                        NotifyCollectionChangedAction.Remove, location, widget, null ) );
            }
        }

        /// <summary>
        /// This method is invoked whenever a widget container is resized
        /// </summary>
        protected virtual void OnContainerResized()
        {
            if( _containerResizedEvent != null )
            {
                _containerResizedEvent( this, new EventArgs() );
            }
        }

        /// <inheritdoc/>
        protected override void SerializeToXml( XmlWriter writer )
        {
            bool    bChildrenWritten = false;

            writer.WriteAttributeString( "firstRow", _containerBounds.Row.ToString() );
            writer.WriteAttributeString( "firstColumn", _containerBounds.Column.ToString() );
            writer.WriteAttributeString( "rowCount", _containerBounds.RowCount.ToString() );
            writer.WriteAttributeString( "columnCount", _containerBounds.ColumnCount.ToString() );

            IterateChildren( ( loc, widget ) =>
            {
                if( widget == null ) return;

                if( !bChildrenWritten )
                {
                    writer.WriteStartElement( "Children" );
                    bChildrenWritten = true;
                }

                ( (IXmlSerializable)widget ).WriteXml( writer );
            } );

            if( bChildrenWritten )
            {
                writer.WriteEndElement();
            }
        }

        /// <inheritdoc/>
        protected override void DeserializeFromXml( XmlReader reader )
        {
            _containerBounds.Row = XmlConvert.ToInt32( reader.GetAttribute( "firstRow" ) );
            _containerBounds.Column = XmlConvert.ToInt32( reader.GetAttribute( "firstColumn" ) );

            MatrixSize size = new MatrixSize( 
                                    XmlConvert.ToInt32( reader.GetAttribute( "rowCount" ) ),
                                    XmlConvert.ToInt32( reader.GetAttribute( "columnCount" ) ) );

            Resize( size, 0 );

            if( !reader.Read() ) return;

            while( reader.NodeType != XmlNodeType.EndElement )
            {
                if( reader.NodeType == XmlNodeType.Element )
                {
                    if( string.Compare( reader.Name, "Children", true ) == 0 )
                    {
                        reader.Read();

                        while( reader.NodeType != XmlNodeType.EndElement )
                        {
                            if( reader.NodeType == XmlNodeType.Element )
                            {
                                WidgetData widget = WidgetData.Create( reader );
                                this[ widget.Location ] = widget;
                            }
                            else
                            {
                                reader.Read();
                            }
                        }

                        reader.ReadEndElement();
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
                else
                {
                    reader.Read();
                }
            }
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        private WidgetData ClearWidget( MatrixLoc loc )
        {
            MatrixLoc   arrayIndex = _containerBounds.ToIndex( loc );
            WidgetData  widget = _widgetArray[ arrayIndex ];

            _widgetArray[ arrayIndex ] = null;

            if( widget != null )
            {
                widget.ClearOwner();
                widget.IsDirtyChanged -= OnChildIsDirtyChanged;
            }

            return widget;
        }

        private void SetWidget( MatrixLoc location, WidgetData widget )
        {
            WidgetData  removedWidget = null;

            Validate();

            // the same widget is being inserted at the same spot where it is already located,
            // let not do anything and just exit
            if( widget != null && widget.Parent == this && widget.Location == location ) return;

            MatrixRect neededBounds = _containerBounds.GrowTo( location );

            // If the cell being modified within the bounds of the current array, let's make sure
            // the existing cell is empty.  Otherwise, we need to reallocate the array before 
            // we can assign the widget to it.
            if( neededBounds.Size == _containerBounds.Size )
            {
                removedWidget = ClearWidget( location );
            }
            else if( widget != null )
            {
                WidgetContainerResizeJustify resizeJustify =
                    ( _containerBounds.Row == neededBounds.Row ?
                            WidgetContainerResizeJustify.Top : WidgetContainerResizeJustify.Bottom ) |
                    ( _containerBounds.Column == neededBounds.Column ?
                            WidgetContainerResizeJustify.Left : WidgetContainerResizeJustify.Right );

                Resize( neededBounds.Size, resizeJustify );
            }

            Validate();

            if( widget != null )
            {
                if( widget.HasOwner )
                {
                    widget.Parent.Remove( widget );
                }

                _widgetArray[ _containerBounds.ToIndex( location ) ] = widget;

                widget.SetOwner( this, location );
                widget.IsDirtyChanged += OnChildIsDirtyChanged;

                if( removedWidget != null )
                {
                    OnWidgetReplaced( location, removedWidget, widget );
                    removedWidget = null;
                }
                else
                {
                    OnWidgetAdded( location, widget );
                }
            }

            if( removedWidget != null )
            {
                OnWidgetRemoved( location, removedWidget );
            }

            Validate();

            IsDirty = true;
        }

        private void OnChildIsDirtyChanged( object sender, EventArgs e )
        {
            WidgetData child = (WidgetData)sender;

            if( child.IsDirty )
            {
                IsDirty = true;
            }
        }

        private void IterateChildren( IterateChildenDelegate callback )
        {
            foreach( var loc in _containerBounds )
            {
                callback( loc, this[ loc ] );
            }
        }

        private delegate void IterateChildenDelegate( MatrixLoc location, WidgetData widget );

        private MatrixRect              _containerBounds;
        private Array2D< WidgetData >   _widgetArray;

        [NonSerialized]
        private WidgetContainerChangedEventHandler  _containerChangedEvent;

        [NonSerialized]
        private EventHandler                        _containerResizedEvent;

        #endregion
    }
}
