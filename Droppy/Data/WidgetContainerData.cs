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
    public struct WidgetContainerKey
    {
        public WidgetContainerKey( int r, int c )
        {
            row = r;
            column = c;
        }

        public int row;
        public int column;
    }


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
        public WidgetContainerChangedEventArgs( NotifyCollectionChangedAction action, int row,
                                                int column, WidgetData oldWidget, WidgetData newWidget )
        {
            _row = row;
            _column = column;
            _action = action;
            _oldWidget = oldWidget;
            _newWidget = newWidget;
        }

        public NotifyCollectionChangedAction Action { get { return _action; } }
        public WidgetData OldWidget { get { return _oldWidget; } }
        public WidgetData NewWidget { get { return _newWidget; } }
        public int Row { get { return _row; } }
        public int Column { get { return _column; } }

        private NotifyCollectionChangedAction   _action;
        private int                             _row;
        private int                             _column;
        private WidgetData                      _oldWidget;
        private WidgetData                      _newWidget;
    }

    public delegate void WidgetContainerChangedEventHandler( object sender, WidgetContainerChangedEventArgs e );

    public interface INotifyWidgetContainerChanged
    {
        event WidgetContainerChangedEventHandler ContainerChanged;
    }




    [Serializable]
    public class WidgetContainerData : WidgetData,
                                       INotifyWidgetContainerChanged
    {
        public WidgetContainerData()
        {
        }

        public WidgetContainerData( int numRows, int numColumns )
        {
            _widgetArray = new WidgetData[ numRows, numColumns ];
        }

        public int ColumnCount { get { return _widgetArray != null ? _widgetArray.GetLength( 1 ) : 0; } }

        public int RowCount { get { return _widgetArray != null ? _widgetArray.GetLength( 0 ) : 0; } }

        public WidgetData this[ int row, int column ]
        {
            get { return _widgetArray[ row - _firstRowIndex, column - _firstColumnIndex ]; }
            set { SetWidget( row, column, value ); }
        }

        public event WidgetContainerChangedEventHandler  ContainerChanged
        {
            add { _containerChangedEvent += value; }
            remove { _containerChangedEvent -= value; }
        }

        public void Resize( int numRows, int numColumns, WidgetContainerResizeJustify justify )
        {
            if( numRows > 256 || numColumns > 256 )
            {
                throw new ApplicationException( string.Format( "Attemp to allocate too much ({0} x {1})", numRows, numColumns ) );
            }

            int currentNumRows = this.RowCount;
            int currentNumColumns = this.ColumnCount;
            int shiftRows = ( justify.HasFlag( WidgetContainerResizeJustify.Bottom ) ? numRows - currentNumRows : 0 );
            int shiftColumns = ( justify.HasFlag( WidgetContainerResizeJustify.Right ) ? numColumns - currentNumColumns : 0 );
            int r, rMax, c, cMax;

            if( shiftRows < 0 )
            {
                r = -shiftRows;
                rMax = currentNumRows;
            }
            else
            {
                r = 0;
                rMax = Math.Min( currentNumRows, numRows );
            }

            if( shiftColumns < 0 )
            {
                c = -shiftColumns;
                cMax = currentNumColumns;
            }
            else
            {
                c = 0;
                cMax = Math.Min( currentNumColumns, numColumns );
            }

            WidgetData[,] newArray = new WidgetData[ numRows, numColumns ];

            for( ; r < rMax; r++ )
            {
                for( ; c < cMax; c++ )
                {
                    newArray[ r + shiftRows, c + shiftColumns ] = _widgetArray[ r, c ];
                }
            }

            _widgetArray = newArray;
            _firstRowIndex += shiftRows;
            _firstColumnIndex += shiftColumns;
        }

        public void Remove( WidgetData widget )
        {
            if( widget.Parent == this )
            {
                this[ widget.Row, widget.Column ] = null;
            }
        }

        public override void ClearDirtyFlag( bool includeChildren )
        {
            base.ClearDirtyFlag(includeChildren);

            if( includeChildren )
            {
                IterateChildren( null, (r,c,w,o) => { if( w != null ) w.ClearDirtyFlag( true ); } ); 
            }
        }

        public override void PostDeserialize()
        {
            IterateChildren( null, (r,c,w,o) => {
                if( w != null )
                {
                    w.PostDeserialize();

                    // just in case the subscription is already set up, let's try to kill it.
                    // if it is not setup, the "-=" won't have any effect
                    w.IsDirtyChanged -= OnChildIsDirtyChanged;
                    w.IsDirtyChanged += OnChildIsDirtyChanged;
                }
            } );
        }


        protected virtual void OnWidgetAdded( int row, int column, WidgetData widget )
        {
            if( _containerChangedEvent != null )
            {
                _containerChangedEvent( this, new WidgetContainerChangedEventArgs(
                                        NotifyCollectionChangedAction.Add, row, column, null, widget ) );
            }
        }

        protected virtual void OnWidgetReplaced( int row, int column, WidgetData oldWidget, WidgetData newWidget )
        {
            if( _containerChangedEvent != null )
            {
                _containerChangedEvent( this, new WidgetContainerChangedEventArgs(
                                        NotifyCollectionChangedAction.Replace, row, column, oldWidget, newWidget ) );
            }
        }

        protected virtual void OnWidgetRemoved( int row, int column, WidgetData widget )
        {
            if( _containerChangedEvent != null )
            {
                _containerChangedEvent( this, new WidgetContainerChangedEventArgs(
                                        NotifyCollectionChangedAction.Remove, row, column, widget, null ) );
            }
        }

        protected override void SerializeToXml( XmlWriter writer )
        {
            bool    bChildrenWritten = false;

            writer.WriteAttributeString( "firstRow", _firstRowIndex.ToString() );
            writer.WriteAttributeString( "firstColumn", _firstColumnIndex.ToString() );
            writer.WriteAttributeString( "rowCount", RowCount.ToString() );
            writer.WriteAttributeString( "columnCount", ColumnCount.ToString() );

            IterateChildren( null, (r,c,w,o) => {
                if( w != null )
                {
                    if( !bChildrenWritten )
                    {
                        writer.WriteStartElement( "Children" );
                        bChildrenWritten = true;
                    }

                    ( (IXmlSerializable)w ).WriteXml( writer );
                }
            } );

            if( bChildrenWritten )
            {
                writer.WriteEndElement();
            }
        }

        protected override void DeserializeFromXml( XmlReader reader )
        {
            int rows, columns;

            _firstRowIndex = XmlConvert.ToInt32( reader.GetAttribute( "firstRow" ) );
            _firstColumnIndex = XmlConvert.ToInt32( reader.GetAttribute( "firstColumn" ) );
            rows = XmlConvert.ToInt32( reader.GetAttribute( "rowCount" ) );
            columns = XmlConvert.ToInt32( reader.GetAttribute( "columnCount" ) );

            Resize( rows, columns, 0 );

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
                                this[ widget.Row, widget.Column ] = widget;
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


        private WidgetData ClearWidget( int row, int column )
        {
            WidgetData widget = _widgetArray[ row, column ];

            _widgetArray[ row, column ] = null;

            if( widget != null )
            {
                widget.ClearOwner();
                widget.IsDirtyChanged -= OnChildIsDirtyChanged;
            }

            return widget;
        }

        private void SetWidget( int row, int column, WidgetData widget )
        {
            int                             numRows = this.RowCount;
            int                             numColumns = this.ColumnCount;
            WidgetContainerResizeJustify    resizeJustify = 0;
            WidgetData                      removedWidget = null;

            // the same widget is being inserted at the same spot where it is already located,
            // let not do anything and just exit
            if( widget != null && widget.Parent == this && widget.Row == row && widget.Column == column ) return;

            if( row < _firstRowIndex )
            {
                numRows += _firstRowIndex - row;
                resizeJustify |= WidgetContainerResizeJustify.Bottom;
            }
            else if( row > _firstRowIndex + numRows )
            {
                numRows = row - _firstRowIndex;
            }

            if( column < _firstColumnIndex )
            {
                numColumns += _firstColumnIndex - column;
                resizeJustify |= WidgetContainerResizeJustify.Right;
            }
            else if( column > _firstColumnIndex + numColumns )
            {
                numColumns = column - _firstColumnIndex;
            }

            // If the cell being modified within the bounds of the current array, let's make sure
            // the existing cell is empty.  Otherwise, we need to reallocate the array before 
            // we can assign the widget to it.
            if( numRows == this.RowCount && numColumns == this.ColumnCount )
            {
                removedWidget = ClearWidget( row, column );
            }
            else if( widget != null )
            {
                Resize( numRows, numColumns, resizeJustify );
            }

            if( widget != null )
            {
                _widgetArray[ row, column ] = widget;

                widget.SetOwner( this, row, column );
                widget.IsDirtyChanged += OnChildIsDirtyChanged;

                if( removedWidget != null )
                {
                    OnWidgetReplaced( row, column, removedWidget, widget );
                    removedWidget = null;
                }
                else
                {
                    OnWidgetAdded( row, column, widget );
                }
            }

            if( removedWidget != null )
            {
                OnWidgetRemoved( row, column, removedWidget );
            }

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

        private void IterateChildren( object userData, IterateChildenDelegate callback )
        {
            int rows = RowCount;
            int columns = ColumnCount;

            for( int r = 0; r < rows; r++ )
            {
                for( int c = 0; c < columns; c++ )
                {
                    callback( r, c, _widgetArray[ r, c ], userData );
                }
            }
        }

        private delegate void IterateChildenDelegate( int row, int col, WidgetData widget, object userData );

        private int             _firstRowIndex;
        private int             _firstColumnIndex;
        private WidgetData[,]   _widgetArray;

        [NonSerialized]
        private WidgetContainerChangedEventHandler  _containerChangedEvent;
    }
}
