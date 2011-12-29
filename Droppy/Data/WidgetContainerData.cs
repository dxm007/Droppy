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
    [Flags]
    public enum WidgetContainerResizeJustify
    {
        Left    = 0x00,
        Right   = 0x01,
        Top     = 0x00,
        Bottom  = 0x02
    }

    public class WidgetContainerChangedEventArgs : EventArgs
    {
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

        public NotifyCollectionChangedAction Action { get { return _action; } }
        public WidgetData OldWidget { get { return _oldWidget; } }
        public WidgetData NewWidget { get { return _newWidget; } }
        public MatrixLoc Location { get { return _location; } }

        private NotifyCollectionChangedAction   _action;
        private MatrixLoc                       _location;
        private WidgetData                      _oldWidget;
        private WidgetData                      _newWidget;
    }

    public delegate void WidgetContainerChangedEventHandler( object sender, WidgetContainerChangedEventArgs e );


    [Serializable]
    public class WidgetContainerData : WidgetData
    {
        public WidgetContainerData()
        {
        }

        public WidgetContainerData( int numRows, int numColumns )
        {
            _widgetArray = new  Array2D<WidgetData>( numRows, numColumns );
            _containerBounds.Size = _widgetArray.Size;
        }

        public MatrixRect Bounds { get { return _containerBounds; } }

        public MatrixSize Size { get { return this.Bounds.Size; } }

        //public WidgetData this[ int row, int column ]
        //{
        //    get { return this[ new MatrixLoc( row, column ) ]; }
        //    set { this[ new MatrixLoc( row, column ) ] = value; }
        //}

        public WidgetData this[ MatrixLoc location ]
        {
            get { return _widgetArray[ _containerBounds.ToIndex( location ) ]; }
            set { SetWidget( location, value ); }
        }

        public event WidgetContainerChangedEventHandler  ContainerChanged
        {
            add { _containerChangedEvent += value; }
            remove { _containerChangedEvent -= value; }
        }

        public event EventHandler ContainerResized
        {
            add { _containerResizedEvent += value; }
            remove { _containerResizedEvent -= value; }
        }

        public void Resize( int numRows, int numColumns, WidgetContainerResizeJustify justify )
        {
            Resize( new MatrixSize( numRows, numColumns ), justify );
        }

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

        public void Validate()
        {
            IterateChildren( ( loc, widget ) =>
            {
                System.Diagnostics.Debug.Assert( 
                    widget == null || ( widget.Parent == this && widget.Location == loc ) );
            } );
        }

        protected virtual void OnWidgetAdded( MatrixLoc location, WidgetData widget )
        {
            if( _containerChangedEvent != null )
            {
                _containerChangedEvent( this, new WidgetContainerChangedEventArgs(
                                        NotifyCollectionChangedAction.Add, location, null, widget ) );
            }
        }

        protected virtual void OnWidgetReplaced( MatrixLoc location, WidgetData oldWidget, WidgetData newWidget )
        {
            if( _containerChangedEvent != null )
            {
                _containerChangedEvent( this, new WidgetContainerChangedEventArgs(
                                        NotifyCollectionChangedAction.Replace, location, oldWidget, newWidget ) );
            }
        }

        protected virtual void OnWidgetRemoved( MatrixLoc location, WidgetData widget )
        {
            if( _containerChangedEvent != null )
            {
                _containerChangedEvent( this, new WidgetContainerChangedEventArgs(
                                        NotifyCollectionChangedAction.Remove, location, widget, null ) );
            }
        }

        protected virtual void OnContainerResized()
        {
            if( _containerResizedEvent != null )
            {
                _containerResizedEvent( this, new EventArgs() );
            }
        }

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
    }
}
