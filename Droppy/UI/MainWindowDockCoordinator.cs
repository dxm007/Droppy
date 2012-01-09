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

namespace Droppy
{
    class MainWindowDockCoordinator
    {
        public MainWindowDockCoordinator( Window            parent,            
                                          IWindowAutoHider  windowAutoHider,
                                          IWindowDocker     windowDocker     )
        {
            _parent = parent;
            _autoHider = windowAutoHider;
            _docker = windowDocker;

            SetupSubscriptions();

            UpdateMainWindowState();
        }

        private void SetupSubscriptions()
        {
            _docker.StateChanged += new EventHandler<DockStateChangedEventArgs>( OnDockStateChanged );
        }

        private void OnDockStateChanged( object sender, DockStateChangedEventArgs e )
        {
            UpdateMainWindowState();
        }

        private void UpdateMainWindowState()
        {
            _autoHider.Mode = MapDockStateToAutoHideMode( _docker.State );

            if( _docker.State == DockState.Floating )
            {
                _parent.Topmost = false;
                _parent.ShowInTaskbar = true;
            }
            else
            {
                _parent.Topmost = true;
                _parent.ShowInTaskbar = false;
            }
        }

        private AutoHideMode MapDockStateToAutoHideMode( DockState state )
        {
            switch( state )
            {
            case DockState.Floating:
                return AutoHideMode.None;
            case DockState.LeftDock:
                return AutoHideMode.CollapseLeft;
            case DockState.TopDock:
                return AutoHideMode.CollapseTop;
            case DockState.RightDock:
                return AutoHideMode.CollapseRight;
            case DockState.BottomDock:
                return AutoHideMode.CollapseBottom;
            default:
                throw new InvalidOperationException( string.Format( 
                                "Invalid DockState value: {0}", _docker.State ) );
            }
        }

        private Window              _parent;
        private IWindowAutoHider    _autoHider;
        private IWindowDocker       _docker;
    }
}
