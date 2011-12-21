using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Droppy
{
    public class WindowMover
    {
        public WindowMover( Window wnd )
        {
            _wnd = wnd;

            SetupEventCallbacks();
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

        void OnWindowClosed(object sender, EventArgs e)
        {
            RevokeEventCallbacks();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if( _wnd.CaptureMouse() )
            {
                _isMouseDown = true;
                _lastScreenPoint = _wnd.PointToScreen( e.GetPosition( _wnd ) );
            }
        }
        
        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if( _isMouseDown )
            {
                _isMouseDown = false;
                _isMoving = false;
                _wnd.ReleaseMouseCapture();
            }
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if( !_isMouseDown ) return;

            Point pt = _wnd.PointToScreen( e.GetPosition( _wnd ) );
            Vector vect = Point.Subtract( pt, _lastScreenPoint );

            if( !_isMoving )
            {
                // don't move the window if the user clicked the mouse and moved it less than 5 pixels
                // in any direction
                if( vect.LengthSquared < 25 ) return;

                _isMoving = true;
            }

            _wnd.Left += vect.X;
            _wnd.Top += vect.Y;
            _lastScreenPoint = pt;
        }

        private Window                  _wnd;
        private Point                   _lastScreenPoint;
        private bool                    _isMouseDown;
        private bool                    _isMoving;
    }

}
