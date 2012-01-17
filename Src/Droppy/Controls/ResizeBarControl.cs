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

namespace Droppy
{
    /// <summary>
    /// Identifies the portion of the resize bar that is being dragged by the mouse
    /// </summary>
    enum ThumbId : int
    {
        /// <summary>Identifies left portion of the bar</summary>
        Left,

        /// <summary>Identifies center portion of the bar</summary>
        Center,

        /// <summary>Identifies right portion of the bar</summary>
        Right,

        /// <summary>Identifies the count of possible, valid ThumbId enumeration values</summary>
        NUM_THUMBS
    };


    /// <summary>
    /// Event arguments type used by Resize event which is raised by the ResizeBarControl
    /// </summary>
    class ResizeBarEventArgs : EventArgs
    {
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="leftDelta">Difference in X location of a window since the time the last
        /// event has fired.</param>
        /// <param name="widthDelta">Difference in width of a window since the time the last
        /// event has fired.</param>
        /// <param name="heightDelta">Difference in height of a window since the time the last
        /// event has fired.</param>
        /// <param name="thumbId">Identifies which portion of the resize bar is being dragged</param>
        public ResizeBarEventArgs( double leftDelta, double widthDelta, double heightDelta, ThumbId thumbId )
        {
            _leftDelta = leftDelta;
            _widthDelta = widthDelta;
            _heightDelta = heightDelta;
            _thumbId = thumbId;
        }

        /// <summary>
        /// Gets the difference in X location of a window since the time the last event has fired.
        /// </summary>
        public double LeftDelta { get { return _leftDelta; } }

        /// <summary>
        /// Gets the difference in width of a window since the time the last event has fired.
        /// </summary>
        public double WidthDelta { get { return _widthDelta; } }

        /// <summary>
        /// Gets the difference in height of a window since the time the last event has fired.
        /// </summary>
        public double HeightDelta { get { return _heightDelta; } }

        /// <summary>
        /// Gets the value which identifies the portion of the resize bar that is being dragged.
        /// </summary>
        public ThumbId ThumbId { get { return _thumbId; } }

        private double _leftDelta;
        private double _widthDelta;
        private double _heightDelta;
        private ThumbId _thumbId;
    }


    /// <summary>
    /// Resize bar custom control
    /// </summary>
    /// <remarks>
    /// This control is designed specifically to go onto the bottom edge of the window. It expects to operate
    /// with a control template which consists of 3 thumb areas:
    ///     * Left - When dragged, the change in window X location as well as window width are reported
    ///     * Center - When dragged, the change in window height is reported
    ///     * Right - When dragged, the change in window width is reported
    ///     
    /// It is important to note that this control only reports what window location and size should be updated to
    /// based on mouse movements, but it does not actually resize the window. External listener must be attached 
    /// to Resize and/or ResizeComplete events to carry out window resizing/repositioning
    /// </remarks>
    class ResizeBarControl : Control
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Default constructor
        /// </summary>
        public ResizeBarControl()
        {
            _thumbControls = new Thumb[ (int)ThumbId.NUM_THUMBS ];
        }

        #region - - - - - - - Events - - - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// An event that gets fired whenever a user clicks and drags on any draggable part of the resize
        /// bar control. It will continue to be generated as the drag operation continues
        /// </summary>
        public event EventHandler< ResizeBarEventArgs > Resize;

        /// <summary>
        /// An event that gets fired whenever a user releases left mouse button and resize operation is
        /// terminated.
        /// </summary>
        public event EventHandler ResizeComplete;

        #endregion

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            UnsubscribeThumbEvents();

            _thumbControls[ (int)ThumbId.Left ] =   Template.FindName( "PART_LeftThumb", this ) as Thumb;
            _thumbControls[ (int)ThumbId.Center ] = Template.FindName( "PART_CenterThumb", this ) as Thumb;
            _thumbControls[ (int)ThumbId.Right ] =  Template.FindName( "PART_RightThumb", this ) as Thumb;

            SubscribeThumbEvents();
        }

        #endregion

        #region ----------------------- Protected Members ---------------------

        /// <summary>
        /// Gets invoked whenever one of draggable parts of the resize bar control is being dragged by
        /// the mouse. If this function is overridden, the deriving class should make sure to invoke base 
        /// implementation.
        /// </summary>
        /// <param name="leftDelta">Specifies by how much left edge of the window should move</param>
        /// <param name="widthDelta">Specifies by how much window width should change</param>
        /// <param name="heightDelta">Specifies by how much window height should change</param>
        protected virtual void OnResize( double leftDelta, double widthDelta, double heightDelta )
        {
            if( Resize != null )
            {
                Resize( this, new ResizeBarEventArgs( leftDelta, widthDelta, heightDelta, _currentThumb ) );
            }
        }

        /// <summary>
        /// Gets invoked whenever a user terminates resize operation by releasing the mouse button
        /// If this function is overridden, the deriving class should make sure to invoke the base implementation.
        /// </summary>
        protected virtual void OnResizeComplete()
        {
            if( ResizeComplete != null )
            {
                ResizeComplete( this, new EventArgs() );
            }
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        static ResizeBarControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata( typeof( ResizeBarControl ), new FrameworkPropertyMetadata( typeof( ResizeBarControl ) ) );
        }

        private void SubscribeThumbEvents()
        {
            foreach( var t in _thumbControls.Select( (x,i)=>new{ ctrl=x, idx=i} ) )
            {
                if( t.ctrl != null )
                {
                    t.ctrl.DragDelta += OnDragDelta;
                    t.ctrl.DragCompleted += OnDragCompleted;

                    t.ctrl.Tag = (ThumbId)t.idx; // LATER: do we need this?
                }
            }
        }

        private void UnsubscribeThumbEvents()
        {
            foreach( var t in _thumbControls )
            {
                if( t != null )
                {
                    t.DragDelta -= OnDragDelta;
                    t.DragCompleted -= OnDragCompleted;
                }
            }
        }

        private void OnDragDelta( object sender, DragDeltaEventArgs e )
        {
            if( sender != _currentSender )
            {
                if( sender == _thumbControls[ (int)ThumbId.Center ] )
                {
                    _currentThumb = ThumbId.Center;
                    _leftMultiplier = 0.0;
                    _widthMultplier = 0.0;
                    _heightMultplier = 1.0;
                }
                else if( sender == _thumbControls[ (int)ThumbId.Left ] )
                {
                    _currentThumb = ThumbId.Left;
                    _leftMultiplier = 1.0;
                    _widthMultplier = -1.0;
                    _heightMultplier = 1.0;
                }
                else if( sender == _thumbControls[ (int)ThumbId.Right ] )
                {
                    _currentThumb = ThumbId.Right;
                    _leftMultiplier = 0.0;
                    _widthMultplier = 1.0;
                    _heightMultplier = 1.0;
                }
                else
                {
                    return;
                }
            }

            OnResize( e.HorizontalChange * _leftMultiplier,
                      e.HorizontalChange * _widthMultplier,
                      e.VerticalChange   * _heightMultplier );
        }

        private void OnDragCompleted( object sender, DragCompletedEventArgs e )
        {
            _currentSender = null;

            OnResizeComplete();
        }
        

        private object      _currentSender;
        private ThumbId     _currentThumb;
        private Thumb[]     _thumbControls;
        private double      _leftMultiplier;
        private double      _widthMultplier;
        private double      _heightMultplier;

        #endregion
    }
}
