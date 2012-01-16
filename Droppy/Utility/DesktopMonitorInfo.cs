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
using System.Windows.Threading;

namespace Droppy
{
    /// <summary>
    /// Describes one of the monitors which makes up the desktop area.
    /// </summary>
    class MonitorInfo
    {
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="info">Monitor information struct received from Win32 API</param>
        public MonitorInfo( Win32.MonitorInfo info )
        {
            _isPrimary = info.flags.HasFlag( Win32.MonitorInfoFlags.Primary );
            _monitorArea = info.rcMonitor;
            _workArea = info.rcWork;
        }

        /// <summary>
        /// Updates monitor information of a previously initialized object
        /// </summary>
        /// <param name="info">Monitor information struct received from Win32 API</param>
        public void Update( Win32.MonitorInfo info )
        {
            _isPrimary = info.flags.HasFlag( Win32.MonitorInfoFlags.Primary );
            _monitorArea = info.rcMonitor;
            _workArea = info.rcWork;
            _isStale = false;
        }

        /// <summary>
        /// Gets a flag which indicates whether or not current monitor is the primary one
        /// </summary>
        public bool IsPrimary { get { return _isPrimary; } }

        /// <summary>
        /// Gets/sets a flag which indicates whether or not this monitor information object
        /// contains outdated information. This might become the case if the parent
        /// DesktopMonitorInfo object updates itself and current monitor info object ends up
        /// being removed from its collection
        /// </summary>
        public bool IsStale { get { return _isStale; } 
                              set { _isStale = value; } }

        /// <summary>
        /// Gets a rectangle which defines total monitor area. Note that coordinates of this
        /// rectangle can be negative if the current monitor is to the left of the primary one
        /// </summary>
        public Rect MonitorArea { get { return _monitorArea; } }

        /// <summary>
        /// Gets a rectangle which defines working area of the monitor. Working area is defined
        /// as the portion of the monitor available for applications and that's what maximized
        /// applications are sized to. This exclused any taskbars or appbars.
        /// </summary>
        public Rect WorkArea { get { return _workArea; } }

        /// <summary>
        /// Gets a flag indicating if there's a taskbar/appbar on the left side of the monitor
        /// </summary>
        public bool HasLeftTaskbar { get { return _monitorArea.Left != _workArea.Left; } }

        /// <summary>
        /// Gets a flag indicating if there's a taskbar/appbar on the top side of the monitor
        /// </summary>
        public bool HasTopTaskbar { get { return _monitorArea.Top != _workArea.Top; } }

        /// <summary>
        /// Gets a flag indicating if there's a taskbar/appbar on the right side of the monitor
        /// </summary>
        public bool HasRightTaskbar { get { return _monitorArea.Right != _workArea.Right; } }

        /// <summary>
        /// Gets a flag indicating if there's a taskbar/appbar on the bottom side of the monitor
        /// </summary>
        public bool HasBottomTaskbar { get { return _monitorArea.Bottom != _workArea.Bottom; } }

        private Rect        _monitorArea;
        private Rect        _workArea;
        private bool        _isPrimary;
        private bool        _isStale;
    }


    /// <summary>
    /// Describes monitor information for the entire desktop
    /// </summary>
    class DesktopMonitorInfo
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Initializaing constructor
        /// </summary>
        /// <param name="dispatcher">Dispatcher object to be used by events fired by this class.
        /// This parameter allows this class to coexist peacefully in a WPF UI environment.</param>
        public DesktopMonitorInfo( Dispatcher dispatcher )
        {
            _dispatcher = dispatcher;
            _monitors = new Dictionary<IntPtr, MonitorInfo>();

            QueryMonitorInfo();
        }

        /// <summary>
        /// When a listener subscribes for this event, DesktopMonitorInfo in turn will set up a
        /// monitor for display settings changes and will update itself whenever a change is 
        /// detected. After the update, it will fire this event to let the listener know.
        /// </summary>
        public event EventHandler DesktopChanged
        {
            add
            {
                _desktopChangedEvent += value;

                if( !_isListeningForDisplayChanges )
                {
                    SubscribeForDesktopChanges();
                }
            }

            remove
            {
                _desktopChangedEvent -= value;

                if( _desktopChangedEvent == null && _isListeningForDisplayChanges )
                {
                    UnsubscribeFromDesktopChanges();
                }
            }
        }

        /// <summary>
        /// Returns the first monitor which contains even a portion of the passed in rectangle
        /// </summary>
        /// <param name="windowRect">Desktop rectangle to test against</param>
        public MonitorInfo FindMonitor( Rect windowRect )
        {
            return _monitors.Values.FirstOrDefault(
                        ( m ) => m.MonitorArea.IntersectsWith( windowRect ) );
        }

        /// <summary>
        /// Returns a reference to the primary monitor information
        /// </summary>
        public MonitorInfo FindPrimaryMonitor()
        {
            return _monitors.Values.First( ( m ) => m.IsPrimary );
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        private void QueryMonitorInfo()
        {
            MarkAllMonitorsStale();

            if( !Win32.EnumDisplayMonitors( IntPtr.Zero,
                                            IntPtr.Zero,
                                            new Win32.MonitorEnumDelegate( QueryMonitorInfoCallback ),
                                            IntPtr.Zero ) )
            {
                throw new System.ComponentModel.Win32Exception();
            }
        }

        private void MarkAllMonitorsStale()
        {
            foreach( var monitor in _monitors.Values )
            {
                monitor.IsStale = true;
            }
        }

        private bool QueryMonitorInfoCallback( IntPtr hMonitor, IntPtr hdcMonitor,
                                               ref Win32.Rect lprcMonitor, IntPtr dwData )
        {
            Win32.MonitorInfo       monitorInfo = new Win32.MonitorInfo( true );
            MonitorInfo             monitor;

            if( !Win32.GetMonitorInfo( hMonitor, ref monitorInfo ) )
            {
                throw new System.ComponentModel.Win32Exception();
            }

            if( _monitors.TryGetValue( hMonitor, out monitor ) )
            {
                monitor.Update( monitorInfo );
            }
            else
            {
                _monitors.Add( hMonitor, new MonitorInfo( monitorInfo ) );
            }

            return true;
        }

        private void SubscribeForDesktopChanges()
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += 
                                    new EventHandler( OnDisplaySettingsChanged );

            _isListeningForDisplayChanges = true;
        }

        private void UnsubscribeFromDesktopChanges()
        {
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -=
                                    new EventHandler( OnDisplaySettingsChanged );

            _isListeningForDisplayChanges = false;
        }

        private void OnDisplaySettingsChanged( object sender, EventArgs e )
        {
            _dispatcher.BeginInvoke( new Action( () =>
            {
                QueryMonitorInfo();

                if( _desktopChangedEvent != null )
                {
                    _desktopChangedEvent( this, new EventArgs() );
                }
            } ) );
        }

        private Dictionary< IntPtr, MonitorInfo >   _monitors;
        private Dispatcher                          _dispatcher;
        private EventHandler                        _desktopChangedEvent;
        private bool                                _isListeningForDisplayChanges;

        #endregion
    }
}
