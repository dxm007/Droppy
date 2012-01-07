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
    public class WindowMoverEventArgs : EventArgs
    {
        public WindowMoverEventArgs()
        {
        }

        public bool IsMoveCancelled
        {
            get { return _isMoveCancelled; }
            set { _isMoveCancelled = true; }
        }

        private bool    _isMoveCancelled;
    }

    public class WindowMoverMovingEventArgs : WindowMoverEventArgs
    {
        public WindowMoverMovingEventArgs( Point newPosition )
        {
            _newPosition = newPosition;
        }

        public Point NewPosition
        {
            get { return _newPosition; }
            set { _newPosition = value; }
        }

        private Point   _newPosition;
    }

    public interface IWindowMover
    {
        event EventHandler< WindowMoverEventArgs > MoveStarted;

        event EventHandler< WindowMoverMovingEventArgs > Moving;

        event EventHandler< WindowMoverEventArgs > MoveFinished;
    }

    public class WindowMover : IWindowMover
    {
        public WindowMover( Window wnd )
        {
            _wnd = wnd;

            SetupEventCallbacks();
        }

        public event EventHandler< WindowMoverEventArgs > MoveStarted;

        public event EventHandler< WindowMoverMovingEventArgs > Moving;

        public event EventHandler< WindowMoverEventArgs > MoveFinished;

        protected virtual bool OnMoveStarted()
        {
            if( MoveStarted == null ) return true;

            var eventArgs = new WindowMoverEventArgs();

            MoveStarted( this, eventArgs );

            return !eventArgs.IsMoveCancelled;
        }

        protected virtual bool OnMoving( ref Point newPosition )
        {
            if( Moving == null ) return true;

            var eventArgs = new WindowMoverMovingEventArgs( newPosition );

            Moving( this, eventArgs );

            newPosition = eventArgs.NewPosition;

            return !eventArgs.IsMoveCancelled;
        }

        protected virtual void OnMoveFinished()
        {
            if( MoveFinished != null )
            {
                MoveFinished( this, new WindowMoverEventArgs() );
            }
        }

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
    }

}
