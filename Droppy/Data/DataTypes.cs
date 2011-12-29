using System;
using System.Collections;
using System.Collections.Generic;


namespace Droppy.Data
{
    public enum ScanDirection
    {
        LeftToRight,
        TopToBottom,
        RightToLeft,
        BottomToTop
    }


    [Serializable]
    public struct MatrixRect : IEnumerable< MatrixLoc >
    {
        #region ----------------------- Public Properties ---------------------

        public int Row
        {
            get { return _location.Row; }
            set { _location.Row = value; }
        }

        public int Column
        { 
            get { return _location.Column; }
            set { _location.Column = value; }
        }

        public int RowCount
        {
            get { return _size.RowCount; }
            set { _size.RowCount = value; }
        }

        public int ColumnCount
        {
            get { return _size.ColumnCount; }
            set { _size.ColumnCount = value; }
        }

        public int LastRow
        {
            get { return this.Row + this.RowCount; }
        }

        public int LastColumn
        {
            get { return this.Column + this.ColumnCount; }
        }

        public MatrixLoc Location
        {
            get { return _location; }
            set { _location = value; }
        }

        public MatrixSize Size
        {
            get { return _size; }
            set { _size = value; }
        }

        #endregion

        #region ----------------------- Constructors --------------------------

        public MatrixRect( int row, int column, int rowCount, int columnCount )
        {
            _location = new MatrixLoc( row, column );
            _size = new MatrixSize( rowCount, columnCount );
        }

        public MatrixRect( MatrixLoc location, MatrixSize size )
        {
            _location = location;
            _size = size;
        }

        public MatrixRect( MatrixSize size )
        {
            _location = new MatrixLoc();
            _size = size;
        }

        #endregion

        #region ----------------------- Public Methods ------------------------

        public MatrixRect Intersect( MatrixRect other )
        {
            MatrixLoc topLeft     = new MatrixLoc( Math.Max( this.Row,        other.Row ),
                                                   Math.Max( this.Column,     other.Column ) );
            MatrixLoc bottomRight = new MatrixLoc( Math.Min( this.LastRow,    other.LastRow ),
                                                   Math.Min( this.LastColumn, other.LastColumn ) );
            MatrixSize size = bottomRight - topLeft;

            if( size.ColumnCount < 0 ) size.ColumnCount = 0;
            if( size.RowCount < 0 ) size.RowCount = 0;

            return new MatrixRect( topLeft, size );
        }

        public MatrixLoc ToIndex( MatrixLoc location )
        {
            return new MatrixLoc( location.Row - _location.Row,
                                  location.Column - _location.Column );
        }

        public MatrixLoc ToLocation( MatrixLoc index )
        {
            return new MatrixLoc( _location.Row + index.Row,
                                  _location.Column + index.Column );
        }

        public MatrixRect GrowTo( MatrixLoc location )
        {
            MatrixRect rect = this;

            if( location.Row < rect.Row )
            {
                rect.RowCount += rect.Row - location.Row;
                rect.Row = location.Row;
            }
            else if( location.Row >= rect.LastRow )
            {
                rect.RowCount += location.Row - rect.LastRow + 1;
            }

            if( location.Column < rect.Column )
            {
                rect.ColumnCount += rect.Column - location.Column;
                rect.Column = location.Column;
            }
            else if( location.Column >= rect.LastColumn )
            {
                rect.ColumnCount += location.Column - rect.LastColumn + 1;
            }

            return rect;
        }

        public bool Contains( MatrixLoc location )
        {
            return location.Row    >= this.Row     &&
                   location.Row    <  this.LastRow &&
                   location.Column >= this.Column  &&
                   location.Column <  this.LastColumn;
        }

        public MatrixSize Distance( MatrixLoc location )
        {
            return new MatrixSize(
                ( location.Row <  this.Row           ? location.Row - this.Row               :
                  location.Row >= this.LastRow       ? location.Row - this.LastRow + 1       :
                                                       0                                       ),
                ( location.Column <  this.Column     ? location.Column - this.Column         :
                  location.Column >= this.LastColumn ? location.Column - this.LastColumn + 1 :
                                                       0                                       ) );
        }

        #region ----------------------- IEnumeratable<> Interface -------------

        public IEnumerator<MatrixLoc> GetEnumerator()
        {
            return AsEnumerable( ScanDirection.TopToBottom ).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerable<MatrixLoc> AsEnumerable( ScanDirection direction )
        {
            switch( direction )
            {
            case ScanDirection.LeftToRight:
                return GetColumnFirstEnumerator( this.Column, this.LastColumn );

            case ScanDirection.RightToLeft:
                return GetColumnFirstEnumerator( this.LastColumn - 1, this.Column - 1 );

            case ScanDirection.TopToBottom:
                return GetRowFirstEnumerator( this.Row, this.LastRow );

            case ScanDirection.BottomToTop:
                return GetRowFirstEnumerator( this.LastRow - 1, this.Row - 1 );

            default:
                throw new InvalidOperationException();
            }
        }

        private IEnumerable<MatrixLoc> GetRowFirstEnumerator( int firstRow, int lastRow )
        {
            int step = firstRow < lastRow ? 1 : -1;
            int row, column, lastColumn = this.LastColumn;

            for( row = firstRow; row != lastRow; row += step )
            {
                for( column = this.Column; column != lastColumn; column++ )
                {
                    yield return new MatrixLoc( row, column );
                }
            }
        }

        private IEnumerable<MatrixLoc> GetColumnFirstEnumerator( int firstColumn, int lastColumn )
        {
            int step = firstColumn < lastColumn ? 1 : -1;
            int row, column, lastRow = this.LastRow;

            for( column = firstColumn; column != lastColumn; column += step )
            {
                for( row = this.Row; row != lastRow; row++ )
                {
                    yield return new MatrixLoc( row, column );
                }
            }
        }

        #endregion

        #endregion

        #region ----------------------- Private Fields ------------------------

        private MatrixLoc   _location;
        private MatrixSize  _size;

        #endregion
    }


    [Serializable]
    public struct MatrixLoc : IEquatable< MatrixLoc >
    {
        public int Row { get; set; }
        public int Column { get; set; }

        public MatrixLoc( int row, int column ) : this()
        {
            this.Row = row;
            this.Column = column;
        }

        public static bool operator ==( MatrixLoc a, MatrixLoc b )
        {
            return a.Row == b.Row && a.Column == b.Column;
        }

        public static bool operator !=( MatrixLoc a, MatrixLoc b )
        {
            return !( a == b );
        }

        public static MatrixLoc operator +( MatrixLoc loc, MatrixSize vector )
        {
            return new MatrixLoc( loc.Row + vector.RowCount, loc.Column + vector.ColumnCount );
        }

        public static MatrixLoc operator -( MatrixLoc loc, MatrixSize vector )
        {
            return new MatrixLoc( loc.Row - vector.RowCount, loc.Column - vector.ColumnCount );
        }

        public static MatrixSize operator -( MatrixLoc a, MatrixLoc b )
        {
            return new MatrixSize( a.Row - b.Row, a.Column - b.Column );
        }

        #region ----------------------- Object Overrides ----------------------

        public override bool Equals( object obj )
        {
            return obj is MatrixLoc ? this == (MatrixLoc)obj : false;
        }

        public override int GetHashCode()
        {
            return this.Row ^ this.Column;
        }

        #endregion

        #region ----------------------- IEquatable<T> Interface ---------------

        public bool Equals( MatrixLoc other )
        {
            return this == other;
        }

        #endregion
    }


    [Serializable]
    public struct MatrixSize
    {
        public int RowCount { get; set; }
        public int ColumnCount { get; set; }

        public MatrixSize( int rows, int columns ) : this()
        {
            this.RowCount = rows;
            this.ColumnCount = columns;
        }

        public bool IsEmpty()
        {
            return this.RowCount == 0 || this.ColumnCount == 0;
        }

        public static bool operator ==( MatrixSize a, MatrixSize b )
        {
            return a.RowCount == b.RowCount && a.ColumnCount == b.ColumnCount;
        }

        public static bool operator !=( MatrixSize a, MatrixSize b )
        {
            return !( a == b );
        }

        public static MatrixSize operator -( MatrixSize a, MatrixSize b )
        {
            return new MatrixSize( a.RowCount - b.RowCount,
                                   a.ColumnCount - b.ColumnCount );
        }

        public static MatrixSize operator +( MatrixSize a, MatrixSize b )
        {
            return new MatrixSize( a.RowCount + b.RowCount,
                                   a.ColumnCount + b.ColumnCount );
        }

        #region ----------------------- Object Overrides ----------------------

        public override bool Equals( object obj )
        {
            return obj is MatrixSize ? this == (MatrixSize)obj : false;
        }

        public override int GetHashCode()
        {
            return this.RowCount ^ this.ColumnCount;
        }

        #endregion

        #region ----------------------- IEquatable<T> Interface ---------------

        public bool Equals( MatrixSize other )
        {
            return this == other;
        }

        #endregion
    }


    [Serializable]
    public class Array2D< T >
    {
        public Array2D( int rows, int columns )
        {
            _arrayData = new T[ rows, columns ];
        }

        public Array2D( MatrixSize size ) : this( size.RowCount, size.ColumnCount )
        {
        }

        public T this[ MatrixLoc index ]
        {
            get { return _arrayData[ index.Row, index.Column ]; }
            set { _arrayData[ index.Row, index.Column ] = value; }
        }

        public MatrixSize Size
        {
            get { return new MatrixSize( _arrayData.GetLength( 0 ),
                                         _arrayData.GetLength( 1 )  ); }
        }

        private T[,]    _arrayData;
    }
}