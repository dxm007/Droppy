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
using System.Windows.Input;

namespace Droppy
{
    /// <summary>
    /// Defines an event data object for window mover events
    /// </summary>
    class WindowMoverEventArgs : EventArgs
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public WindowMoverEventArgs()
        {
        }

        /// <summary>
        /// Gets/sets a flag that indicates whether to cancel window move operation. This
        /// property allows any event listener to elect to cancel the move operation
        /// </summary>
        public bool IsMoveCancelled
        {
            get { return _isMoveCancelled; }
            set { _isMoveCancelled = true; }
        }

        private bool    _isMoveCancelled;
    }


    /// <summary>
    /// Defines an event data for window mover object's Moving event
    /// </summary>
    class WindowMoverMovingEventArgs : WindowMoverEventArgs
    {
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="newPosition">New location of the window</param>
        public WindowMoverMovingEventArgs( Point newPosition )
        {
            _newPosition = newPosition;
        }

        /// <summary>
        /// Gets/sets the location the window is about to be moved to. 
        /// </summary>
        /// <remarks>
        /// This event is fired before window location is updated. A listener listening for
        /// Moving event has an option to cancelling the move, in which case this new location
        /// will be ignored. A listener can also modify this property and the window mover will
        /// move the window correspondingly.
        /// </remarks>
        public Point NewPosition
        {
            get { return _newPosition; }
            set { _newPosition = value; }
        }

        private Point   _newPosition;
    }


    /// <summary>
    /// Implemented by a window mover behavior object
    /// </summary>
    interface IWindowMover
    {
        /// <summary>
        /// Gets fired when window move is initiated
        /// </summary>
        event EventHandler< WindowMoverEventArgs > MoveStarted;

        /// <summary>
        /// Gets fired while window is being moved. This event reports the next position
        /// the window will assume after the event is processed
        /// </summary>
        event EventHandler< WindowMoverMovingEventArgs > Moving;

        /// <summary>
        /// Gets fired when window move is terminated
        /// </summary>
        event EventHandler< WindowMoverEventArgs > MoveFinished;
    }


    /// <summary>
    /// Implements window moving behavior which can be attached to any window
    /// </summary>
    class WindowMover : IWindowMover
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="wnd">Reference to a parent window</param>
        public WindowMover( Window wnd )
        {
            _wnd = wnd;

            SetupEventCallbacks();
        }

        #region - - - - - - - IWindowMover Interface  - - - - - - - - - - - - -

        public event EventHandler< WindowMoverEventArgs > MoveStarted;

        public event EventHandler< WindowMoverMovingEventArgs > Moving;

        public event EventHandler< WindowMoverEventArgs > MoveFinished;

        #endregion
        #endregion

        #region ----------------------- Protected Members ---------------------

        /// <summary>
        /// Gets invoked when window move is initiated. Deriving class must ensure to call the
        /// base implementation of this function to ensure correct operation of the window
        /// mover object.
        /// </summary>
        /// <returns></returns>
        protected virtual bool OnMoveStarted()
        {
            if( MoveStarted == null ) return true;

            var eventArgs = new WindowMoverEventArgs();

            MoveStarted( this, eventArgs );

            return !eventArgs.IsMoveCancelled;
        }

        /// <summary>
        /// Gets invoked while the window is being moved to report new window position and give
        /// event listeners or deriving classes to cancel the move or adjust the window position.
        /// Deriving class must ensure to call the base implementation of this function to 
        /// ensure correct operation of the window mover object
        /// </summary>
        /// <param name="newPosition"></param>
        /// <returns></returns>
        protected virtual bool OnMoving( ref Point newPosition )
        {
            if( Moving == null ) return true;

            var eventArgs = new WindowMoverMovingEventArgs( newPosition );

            Moving( this, eventArgs );

            newPosition = eventArgs.NewPosition;

            return !eventArgs.IsMoveCancelled;
        }

        /// <summary>
        /// Gets invoked when window move operation is terminated. Deriving class must ensure to
        /// call the base implementation of this function to ensure correct operation of the 
        /// window mover object
        /// </summary>
        protected virtual void OnMoveFinished()
        {
            if( MoveFinished != null )
            {
                MoveFinished( this, new WindowMoverEventArgs() );
            }
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        private void SetupEventCallbacks()
        {
            _wnd.MouseLeftButtonDown += OnMouseLeftButtonDown;
            _wnd.MouseLeftButtonUp += OnMouseLeftButtonUp;
            _wnd.MouseMove += OnMouseMove;
            _wnd.Closed += OnWindowClosed;
        }

        private void RevokeEventCallbacks()
        {
            _wnd.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            _wnd.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            _wnd.MouseMove -= OnMouseMove;
            _wnd.Closed -= OnWindowClosed;
        }

        private void OnWindowClosed( object sender, EventArgs e )
        {
            RevokeEventCallbacks();
        }

        private void OnMouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            if( _wnd.CaptureMouse() )
            {
                _isMouseDown = true;
                _anchorScreenClickPos = _wnd.PointToScreen( e.GetPosition( _wnd ) );
                _anchorWindowPosition = new Point( _wnd.Left, _wnd.Top );
            }
        }
        
        private void OnMouseLeftButtonUp( object sender, MouseButtonEventArgs e )
        {
            if( _isMouseDown )
            {
                _isMouseDown = false;
                _wnd.ReleaseMouseCapture();

                FinishWindowMove();
            }
        }

        private void OnMouseMove( object sender, MouseEventArgs e )
        {
            if( !_isMouseDown ) return;

            Point mousePos = _wnd.PointToScreen( e.GetPosition( _wnd ) );
            Vector moveVector = Point.Subtract( mousePos, _anchorScreenClickPos );

            if( !_isMoving )
            {
                // don't move the window if the user clicked the mouse and moved it less than 5 pixels
                // in any direction
                if( moveVector.LengthSquared < 25 ) return;

                // Signal window move started giving any event listener a chance to block the move
                if( !OnMoveStarted() ) return;

                _isMoving = true;
            }

            Point windowPosition = _anchorWindowPosition + moveVector;

            if( OnMoving( ref windowPosition ) )
            {
                _wnd.Left = windowPosition.X;
                _wnd.Top = windowPosition.Y;
            }
            else
            {
                FinishWindowMove();
            }
        }

        private void FinishWindowMove()
        {
            if( _isMoving )
            {
                _isMoving = false;
                OnMoveFinished();
            }
        }

        private Window                  _wnd;
        private Point                   _anchorScreenClickPos;
        private Point                   _anchorWindowPosition;
        private bool                    _isMouseDown;
        private bool                    _isMoving;

        #endregion
    }

}
