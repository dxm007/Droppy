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
// NOTES: This file contains primitive types for working with 2D coordinate systems.  These are
// redefined rather than using .NET Framework's Rect, Size and Point types because those are based
// on doubles which are not very friendly when we need work with discrete locations such as those
// that represent widget container sizes and locations.
//
//=================================================================================================
//=================================================================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace Droppy.Data
{
    /// <summary>
    /// When a matrix is being iterated, this enum defines direction in which the matrix cells are
    /// interated in
    /// </summary>
    enum ScanDirection
    {
        LeftToRight,
        TopToBottom,
        RightToLeft,
        BottomToTop
    }


    /// <summary>
    /// Describes the width, height and location of a rectangle in a discrete 2D coordinate system
    /// </summary>
    [Serializable]
    struct MatrixRect : IEnumerable< MatrixLoc >
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="row">Location of rectangles top edge</param>
        /// <param name="column">Location of rectangle left edge</param>
        /// <param name="rowCount">Number of rows in a rectangle</param>
        /// <param name="columnCount">Number of columns in a rectangle</param>
        public MatrixRect( int row, int column, int rowCount, int columnCount )
        {
            _location = new MatrixLoc( row, column );
            _size = new MatrixSize( rowCount, columnCount );
        }

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="location">Location of top-left corner of a rectangle</param>
        /// <param name="size">Size of the rectangle</param>
        public MatrixRect( MatrixLoc location, MatrixSize size )
        {
            _location = location;
            _size = size;
        }

        /// <summary>
        /// Initializing constructor. This constructor creates a rectangle whose top-left
        /// corner is set to location (0,0)
        /// </summary>
        /// <param name="size">size of the rectangle</param>
        public MatrixRect( MatrixSize size )
        {
            _location = new MatrixLoc();
            _size = size;
        }

        #region - - - - - - - Properties  - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// Gets/sets the location of the top edge of the rectangle
        /// </summary>
        public int Row
        {
            get { return _location.Row; }
            set { _location.Row = value; }
        }

        /// <summary>
        /// Gets/sets the location of the left edge of the rectangle
        /// </summary>
        public int Column
        { 
            get { return _location.Column; }
            set { _location.Column = value; }
        }

        /// <summary>
        /// Gets/sets the number of rows in a rectangle
        /// </summary>
        public int RowCount
        {
            get { return _size.RowCount; }
            set { _size.RowCount = value; }
        }

        /// <summary>
        /// Gets/sets the number of columns in a rectangle
        /// </summary>
        public int ColumnCount
        {
            get { return _size.ColumnCount; }
            set { _size.ColumnCount = value; }
        }

        /// <summary>
        /// Gets the location of a row right past the bottom row which belongs to the rectangle
        /// </summary>
        public int LastRow
        {
            get { return this.Row + this.RowCount; }
        }

        /// <summary>
        /// Gets the location of a column right past the right column which belongs to the rectangle
        /// </summary>
        public int LastColumn
        {
            get { return this.Column + this.ColumnCount; }
        }

        /// <summary>
        /// Gets/sets the location of top-left corner of the rectangle
        /// </summary>
        public MatrixLoc Location
        {
            get { return _location; }
            set { _location = value; }
        }

        /// <summary>
        /// Gets/sets the size of the rectangle
        /// </summary>
        public MatrixSize Size
        {
            get { return _size; }
            set { _size = value; }
        }

        #endregion

        /// <summary>
        /// Returns a new rectangle which represents the intersection between current rectangle
        /// and the second one which is passed in
        /// </summary>
        /// <param name="other">The other rectangle to test against</param>
        /// <returns>A rectangle which represents the intersection</returns>
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

        /// <summary>
        /// Converts absolute location within the rectangle into 0-based index with origin at
        /// top-left of the rectangle.  So if rectangle origin is at (2,4), a point at (3,6) will
        /// be converted to an index location of (1,2).
        /// </summary>
        /// <param name="location">Location within the rectangle to convert into 0-based index</param>
        /// <returns>0-based index of the location with origin at top-left of the rectangle</returns>
        public MatrixLoc ToIndex( MatrixLoc location )
        {
            return new MatrixLoc( location.Row - _location.Row,
                                  location.Column - _location.Column );
        }

        /// <summary>
        /// Converts 0-based index with origin at top-left of the rectangle to an absolute location
        /// within the rectangle.  So if rectangle origin is at (2,4), an index location of (1,2) will
        /// be translated into an absolute location of (3,6).
        /// </summary>
        /// <param name="index">0-based index into the rectangle</param>
        /// <returns>Absolute location of the indexed point</returns>
        public MatrixLoc ToLocation( MatrixLoc index )
        {
            return new MatrixLoc( _location.Row + index.Row,
                                  _location.Column + index.Column );
        }

        /// <summary>
        /// Calculates a rectangle that contains the specified location
        /// </summary>
        /// <param name="location">The location to test against</param>
        /// <returns>If the specified location is enclosed within the rectangle, returned rectangle is
        /// identical to the current one. Otherwise, returned rectangle's edges are extended to ensure
        /// the location is enclosed by the returned rectangle</returns>
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

        /// <summary>
        /// Tests the specified location to see if its within the current rectangle
        /// </summary>
        /// <param name="location">Location to test against</param>
        /// <returns>A boolean flag indicating whether or not the specified location is in
        /// the rectangle</returns>
        public bool Contains( MatrixLoc location )
        {
            return location.Row    >= this.Row     &&
                   location.Row    <  this.LastRow &&
                   location.Column >= this.Column  &&
                   location.Column <  this.LastColumn;
        }

        /// <summary>
        /// Calculates the distance from current rectangle to the specified location
        /// </summary>
        /// <param name="location">Location to test against</param>
        /// <returns>Size, which in this case is treated as a vector. If the point is within
        /// the rectangle, returned distance is 0, otherwise positive number indicates distance
        /// past right/bottom edge and negative number indicates distance before left/top edge</returns>
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

        /// <summary>
        /// Creates an enumerable object which enumerates all locations within the rectangle
        /// </summary>
        /// <param name="direction">Specifies from which edge the scan should start</param>
        /// <returns>Newly created enumerable object</returns>
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

        #region - - - - - - - IEnumerable< MatrixLoc > Interface  - - - - - - -

        public IEnumerator<MatrixLoc> GetEnumerator()
        {
            return AsEnumerable( ScanDirection.TopToBottom ).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion
        #endregion

        #region ----------------------- Private Members -----------------------

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

        private MatrixLoc   _location;
        private MatrixSize  _size;

        #endregion
    }


    /// <summary>
    /// Describes a location within a discrete 2D coordinate system.
    /// </summary>
    [Serializable]
    struct MatrixLoc : IEquatable< MatrixLoc >
    {
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="row">Row coordinate of the location</param>
        /// <param name="column">Column coordinate of the location</param>
        public MatrixLoc( int row, int column ) : this()
        {
            this.Row = row;
            this.Column = column;
        }

        #region - - - - - - - Properties  - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// Gets/sets row coordinate of the location
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// Gets/sets column coordinate of the location
        /// </summary>
        public int Column { get; set; }

        #endregion

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="a">First location</param>
        /// <param name="b">Second location</param>
        /// <returns>Returns a flag indicating if the two locations are equal</returns>
        public static bool operator ==( MatrixLoc a, MatrixLoc b )
        {
            return a.Row == b.Row && a.Column == b.Column;
        }

        /// <summary>
        /// Inequality operator
        /// </summary>
        /// <param name="a">First location</param>
        /// <param name="b">Second location</param>
        /// <returns>Returns a flag indicating if the two locations are different</returns>
        public static bool operator !=( MatrixLoc a, MatrixLoc b )
        {
            return !( a == b );
        }

        /// <summary>
        /// Addition operator
        /// </summary>
        /// <param name="loc">Original location</param>
        /// <param name="vector">vector to add to the location</param>
        /// <returns>Location after the vector has been applied to original location</returns>
        public static MatrixLoc operator +( MatrixLoc loc, MatrixSize vector )
        {
            return new MatrixLoc( loc.Row + vector.RowCount, loc.Column + vector.ColumnCount );
        }

        /// <summary>
        /// Subtraction operator
        /// </summary>
        /// <param name="loc">Original location</param>
        /// <param name="vector">vector to subtract from the location</param>
        /// <returns>Location after the vector has been applied to original location</returns>
        public static MatrixLoc operator -( MatrixLoc loc, MatrixSize vector )
        {
            return new MatrixLoc( loc.Row - vector.RowCount, loc.Column - vector.ColumnCount );
        }

        /// <summary>
        /// Subtraction operator
        /// </summary>
        /// <param name="a">location on the left side of the subtraction operator</param>
        /// <param name="b">location on the right side of the subtraction operator</param>
        /// <returns>The difference, as a vector, between the two locations. The direction of the
        /// vector is expressed as (b) --> (a)</returns>
        public static MatrixSize operator -( MatrixLoc a, MatrixLoc b )
        {
            return new MatrixSize( a.Row - b.Row, a.Column - b.Column );
        }

        #region ----------------------- Object Overrides ----------------------

        /// <inheritdoc/>
        public override bool Equals( object obj )
        {
            return obj is MatrixLoc ? this == (MatrixLoc)obj : false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Row ^ this.Column;
        }

        #endregion

        #region ----------------------- IEquatable<T> Interface ---------------

        /// <inheritdoc/>
        public bool Equals( MatrixLoc other )
        {
            return this == other;
        }

        #endregion
    }


    /// <summary>
    /// Describes a size of an object within a discrete 2D coordinate system. This type is
    /// sometimes also used as a vector (i.e. returned as difference between 2 points or
    /// added to one point in order to calculate another one).
    /// </summary>
    [Serializable]
    struct MatrixSize
    {
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="rows">Number of rows</param>
        /// <param name="columns">Number of columns</param>
        public MatrixSize( int rows, int columns ) : this()
        {
            this.RowCount = rows;
            this.ColumnCount = columns;
        }

        #region - - - - - - - Properties  - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// Gets/sets the number of rows
        /// </summary>
        public int RowCount { get; set; }

        /// <summary>
        /// Gets/sets the number of columns
        /// </summary>
        public int ColumnCount { get; set; }

        /// <summary>
        /// Gets a flag which indicates whether or not this object represents an empty region (i.e either
        /// number of rows or number of columns is 0)
        /// </summary>
        public bool IsEmpty { get { return this.RowCount == 0 || this.ColumnCount == 0; } }

        #endregion

        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="a">First size struct</param>
        /// <param name="b">Second size struct</param>
        /// <returns>Returns a flag indicating whether or not two sizes passed in are equal</returns>
        public static bool operator ==( MatrixSize a, MatrixSize b )
        {
            return a.RowCount == b.RowCount && a.ColumnCount == b.ColumnCount;
        }

        /// <summary>
        /// Inequality operator
        /// </summary>
        /// <param name="a">First size struct</param>
        /// <param name="b">Second size struct</param>
        /// <returns>Returns a flag indicating whether or not two sizes passed in are different</returns>
        public static bool operator !=( MatrixSize a, MatrixSize b )
        {
            return !( a == b );
        }

        /// <summary>
        /// Subtraction operator
        /// </summary>
        /// <param name="a">First size struct</param>
        /// <param name="b">Second size struct</param>
        /// <returns>A new size struct which represents the difference of the two passed in sizes</returns>
        public static MatrixSize operator -( MatrixSize a, MatrixSize b )
        {
            return new MatrixSize( a.RowCount - b.RowCount,
                                   a.ColumnCount - b.ColumnCount );
        }

        /// <summary>
        /// Addition operator
        /// </summary>
        /// <param name="a">First size struct</param>
        /// <param name="b">Second size struct</param>
        /// <returns>A new size struct which represents the sum of the two passed in sizes</returns>
        public static MatrixSize operator +( MatrixSize a, MatrixSize b )
        {
            return new MatrixSize( a.RowCount + b.RowCount,
                                   a.ColumnCount + b.ColumnCount );
        }

        #region ----------------------- Object Overrides ----------------------

        /// <inheritdoc/>
        public override bool Equals( object obj )
        {
            return obj is MatrixSize ? this == (MatrixSize)obj : false;
        }

        /// <inheritdoc/>
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


    /// <summary>
    /// Generic two-dimensional array which can work with any element of type T. This array
    /// wraps around regular C# array but is designed to work specifically with matrix data types 
    /// (MatrixLoc and MatrixSize). This makes the use of the array much cleaner and simpler in
    /// the code that works with 2D matricees of elements.
    /// </summary>
    /// <typeparam name="T">Element type to be stored in the array</typeparam>
    [Serializable]
    class Array2D< T >
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Initializing constructor. Creates pre-allocated two dimensional array of T elements
        /// </summary>
        /// <param name="rows">Number of rows to allocate</param>
        /// <param name="columns">Number of columns to allocate</param>
        public Array2D( int rows, int columns )
        {
            _arrayData = new T[ rows, columns ];
        }

        /// <summary>
        /// Initializing constructor. Creates pre-allocated two dimensional array of T elements
        /// </summary>
        /// <param name="size">Size of the array to initialize</param>
        public Array2D( MatrixSize size ) : this( size.RowCount, size.ColumnCount )
        {
        }

        /// <summary>
        /// Gets/sets an element at a specified location in the array
        /// </summary>
        /// <param name="index">Identifies the location</param>
        public T this[ MatrixLoc index ]
        {
            get { return _arrayData[ index.Row, index.Column ]; }
            set { _arrayData[ index.Row, index.Column ] = value; }
        }

        /// <summary>
        /// Gets the size of the array
        /// </summary>
        public MatrixSize Size
        {
            get { return new MatrixSize( _arrayData.GetLength( 0 ),
                                         _arrayData.GetLength( 1 )  ); }
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        private T[,]    _arrayData;

        #endregion
    }
}