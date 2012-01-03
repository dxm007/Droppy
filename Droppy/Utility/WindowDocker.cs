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

            _desktopInfo = new DesktopMonitorInfo( parent.Dispatcher );
            _desktopInfo.DesktopChanged += new EventHandler( OnDesktopChanged );

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
        }

        private void OnWindowMoving( object sender, WindowMoverMovingEventArgs e )
        {
            DockState dockability = DetermineDockability( 
                        new Rect( e.NewPosition, _parent.ActualSize() ), _snapDistance );

            e.NewPosition = CalculateParentDockedPosition( dockability, e.NewPosition );
        }

        private void OnWindowMoveFinished( object sender, WindowMoverEventArgs e )
        {
            DockState dockability = DetermineDockability( GetParentWindowRect(), 0.0 );

            UpdateState( dockability );
        }

        private void OnDesktopChanged( object sender, EventArgs e )
        {
            if( !IsDockStateValidForMonitor( _currentState, _currentMonitor ) )
            {
                _currentMonitor = null;
            }

            if( _currentMonitor != null )
            {
                Rect windowRect = GetParentWindowRect();

                windowRect.Location = CalculateParentDockedPosition( _currentState, windowRect.Location );

                EnsureWindowRectIsInMonitor( ref windowRect, _currentMonitor );

                _parent.Left = windowRect.X;
                _parent.Top = windowRect.Y;
            }
            else
            {
                UpdateState( DockState.Floating );
                MoveParentToPrimaryMonitor();
            }
        }

        private DockState DetermineDockability( Rect windowRect, double snapDistance )
        {
            UpdateCurrentMonitor( windowRect );

            Rect monitorRect = _currentMonitor.MonitorArea;

            if( _currentMonitor == null ||
                !monitorRect.Contains( windowRect ) )
            {
                return DockState.Floating;
            }

            monitorRect.Inflate( -snapDistance, -snapDistance );

            if( windowRect.Left <= monitorRect.Left )
            {
                if( !_currentMonitor.HasLeftTaskbar ) return DockState.LeftDock;
            }

            if( windowRect.Right >= monitorRect.Right )
            {
                if( !_currentMonitor.HasRightTaskbar ) return DockState.RightDock;                
            }

            if( windowRect.Top <= monitorRect.Top )
            {
                if( !_currentMonitor.HasTopTaskbar ) return DockState.TopDock;
            }
            else if( windowRect.Bottom >= monitorRect.Bottom )
            {
                if( !_currentMonitor.HasBottomTaskbar ) return DockState.BottomDock;
            }

            return DockState.Floating;
        }

        private Point CalculateParentDockedPosition( DockState dockState, Point parentPosition )
        {
            Point position = parentPosition;

            switch( dockState )
            {
            case DockState.LeftDock:
                position.X = _currentMonitor.MonitorArea.Left;
                break;
            case DockState.RightDock:
                position.X = _currentMonitor.MonitorArea.Right - _parent.ActualWidth;
                break;
            case DockState.TopDock:
                position.Y = _currentMonitor.MonitorArea.Top;
                break;
            case DockState.BottomDock:
                position.Y = _currentMonitor.MonitorArea.Bottom - _parent.ActualHeight;
                break;
            default:
                break;
            }

            return position;
        }

        private bool IsDockStateValidForMonitor( DockState state, MonitorInfo monitor )
        {
            if( monitor == null ) return false;

            if( monitor.IsStale ) return false;

            switch( _currentState )
            {
            case DockState.LeftDock:
                if( _currentMonitor.HasLeftTaskbar ) return false;
                break;
            case DockState.TopDock:
                if( _currentMonitor.HasTopTaskbar ) return false;
                break;
            case DockState.RightDock:
                if( _currentMonitor.HasRightTaskbar ) return false;
                break;
            case DockState.BottomDock:
                if( _currentMonitor.HasBottomTaskbar ) return false;
                break;
            default:
                break;
            }
             
            return true;
        }

        private void EnsureWindowRectIsInMonitor( ref Rect windowRect, MonitorInfo monitor )
        {
            if( windowRect.Left < monitor.MonitorArea.Left )
            {
                windowRect.X = monitor.MonitorArea.Left;
            }
            else if( windowRect.Right > monitor.MonitorArea.Right )
            {
                windowRect.X = monitor.MonitorArea.Right - windowRect.Width;
            }

            if( windowRect.Top < monitor.MonitorArea.Top )
            {
                windowRect.Y = monitor.MonitorArea.Top;
            }
            else if( windowRect.Bottom > monitor.MonitorArea.Bottom )
            {
                windowRect.Y = monitor.MonitorArea.Bottom - windowRect.Height;
            }
        }

        private void MoveParentToPrimaryMonitor()
        {
            MonitorInfo     primaryMonitor = _desktopInfo.FindPrimaryMonitor();

            _parent.Left = primaryMonitor.WorkArea.Left + 100;
            _parent.Top = primaryMonitor.WorkArea.Top + 100;
        }

        private void UpdateCurrentMonitor( Rect windowRect )
        {
            if( _currentMonitor == null ||
                !_currentMonitor.MonitorArea.IntersectsWith( windowRect ) )
            {
                _currentMonitor = _desktopInfo.FindMonitor( windowRect );
            }
        }

        private Rect GetParentWindowRect()
        {
            return new Rect( _parent.Location(), _parent.ActualSize() );
        }

        private Window              _parent;
        private IWindowMover        _windowMover;
        private DesktopMonitorInfo  _desktopInfo;
        private MonitorInfo         _currentMonitor;
        private DockState           _currentState;

        private const double    _snapDistance = 10.0;
    }
}
