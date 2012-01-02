using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Droppy
{
    enum DockState
    {
        Floating,
        LeftDock,
        TopDock,
        RightDock,
        BottomDock
    }

    class DockStateChangedEventArgs : EventArgs
    {
        public DockStateChangedEventArgs( DockState oldState, DockState newState )
        {
            _oldState = oldState;
            _newState = newState;
        }

        public DockState OldState { get { return _oldState; } }

        public DockState NewState { get { return _newState; } }

        private DockState   _oldState;
        private DockState   _newState;
    }


    interface IWindowDocker
    {
        DockState State { get; }

        event EventHandler< DockStateChangedEventArgs >  StateChanged;
    }


    class WindowDocker : IWindowDocker
    {
        public WindowDocker( Window parent, IWindowMover windowMover )
        {
            _parent = parent;
            _windowMover = windowMover;

            SetupEventSubscriptions();
        }

        public DockState State { get { return _currentState; } }

        public event EventHandler< DockStateChangedEventArgs >  StateChanged;

        protected virtual void OnStateChanged( DockState newState )
        {
            DockState oldState = _currentState;

            System.Diagnostics.Debug.Assert( oldState != newState );

            _currentState = newState;

            if( StateChanged != null )
            {
                StateChanged( this, new DockStateChangedEventArgs( oldState, newState ) );
            }
        }

        private void UpdateState( DockState state )
        {
            if( state != _currentState )
            {
                OnStateChanged( state );
            }
        }

        private void SetupEventSubscriptions()
        {
            _windowMover.Moving += new EventHandler<WindowMoverMovingEventArgs>( OnWindowMoving );
            _windowMover.MoveFinished += new EventHandler<WindowMoverEventArgs>( OnWindowMoveFinished );

            _screenRect = new Rect( new Size( SystemParameters.PrimaryScreenWidth,
                                              SystemParameters.PrimaryScreenHeight ) );
        }

        private void InitDockableBoundary()
        {
        }

        private void OnWindowMoving( object sender, WindowMoverMovingEventArgs e )
        {
            Point   position = e.NewPosition;

            DockState dockability = CalculateDockability( 
                        new Rect( e.NewPosition, _parent.ActualSize() ), _snapDistance );

            switch( dockability )
            {
            case DockState.LeftDock:
                position.X = _screenRect.Left;
                break;
            case DockState.RightDock:
                position.X = _screenRect.Right - _parent.ActualWidth;
                break;
            case DockState.TopDock:
                position.Y = _screenRect.Top;
                break;
            case DockState.BottomDock:
                position.Y = _screenRect.Bottom - _parent.ActualHeight;
                break;
            default:
                break;
            }

            e.NewPosition = position;
        }

        private void OnWindowMoveFinished( object sender, WindowMoverEventArgs e )
        {
            DockState dockability = CalculateDockability(
                        new Rect( _parent.Location(), _parent.ActualSize() ), 0.0 );

            UpdateState( dockability );
        }

        private DockState CalculateDockability( Rect windowRect, double snapDistance )
        {
            if( windowRect.Left <= snapDistance )
            {
                return DockState.LeftDock;
            }
            else if( windowRect.Right >= _screenRect.Right - snapDistance )
            {
                return DockState.RightDock;                
            }

            if( windowRect.Top <= snapDistance )
            {
                return DockState.TopDock;
            }
            else if( windowRect.Bottom >= _screenRect.Bottom - snapDistance )
            {
                return DockState.BottomDock;
            }

            return DockState.Floating;
        }

        private Window          _parent;
        private IWindowMover    _windowMover;
        private Rect            _screenRect;
        private DockState       _currentState;

        private const double    _snapDistance = 10.0;
    }
}
