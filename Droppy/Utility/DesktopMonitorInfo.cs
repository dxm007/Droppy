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
    public class MonitorInfo
    {
        public MonitorInfo( Win32.MonitorInfo info )
        {
            _isPrimary = info.flags.HasFlag( Win32.MonitorInfoFlags.Primary );
            _monitorArea = info.rcMonitor;
            _workArea = info.rcWork;
        }

        public void Update( Win32.MonitorInfo info )
        {
            _isPrimary = info.flags.HasFlag( Win32.MonitorInfoFlags.Primary );
            _monitorArea = info.rcMonitor;
            _workArea = info.rcWork;
            _isStale = false;
        }

        public bool IsPrimary { get { return _isPrimary; } }

        public bool IsStale { get { return _isStale; } 
                              set { _isStale = value; } }

        public Rect MonitorArea { get { return _monitorArea; } }

        public Rect WorkArea { get { return _workArea; } }

        public bool HasLeftTaskbar { get { return _monitorArea.Left != _workArea.Left; } }

        public bool HasTopTaskbar { get { return _monitorArea.Top != _workArea.Top; } }

        public bool HasRightTaskbar { get { return _monitorArea.Right != _workArea.Right; } }

        public bool HasBottomTaskbar { get { return _monitorArea.Bottom != _workArea.Bottom; } }

        private Rect        _monitorArea;
        private Rect        _workArea;
        private bool        _isPrimary;
        private bool        _isStale;
    }

    public class DesktopMonitorInfo
    {
        public DesktopMonitorInfo( Dispatcher dispatcher )
        {
            _dispatcher = dispatcher;
            _monitors = new Dictionary<IntPtr, MonitorInfo>();

            QueryMonitorInfo();
        }

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

        public MonitorInfo FindMonitor( Rect windowRect )
        {
            return _monitors.Values.FirstOrDefault(
                        ( m ) => m.MonitorArea.IntersectsWith( windowRect ) );
        }

        public MonitorInfo FindPrimaryMonitor()
        {
            return _monitors.Values.First( ( m ) => m.IsPrimary );
        }

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
    }
}
