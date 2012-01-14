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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Droppy
{
    enum AutoHideMode
    {
        None,
        CollapseLeft,
        CollapseTop,
        CollapseRight,
        CollapseBottom
    }


    static class AutoHideModeExtensionsMethods
    {
        public static bool IsHorizontalCollapse( this AutoHideMode mode )
        {
            return mode == AutoHideMode.CollapseLeft || mode == AutoHideMode.CollapseRight;
        }
    }


    enum AutoHideState
    {
        Visible,
        Collapsed,
        Opening,
        Closing
    }


    interface IWindowAutoHider
    {
        AutoHideMode Mode { get; set; }

        AutoHideState State { get; }

        void Freeze();

        void Unfreeze();

        void ShowWindow();
    }


    class WindowAutoHiderParams
    {
        public WindowAutoHiderParams()
        {
            this.CollapsedHeight = 5.0;
            this.CollapsedWidth = 5.0;
            this.AutoHideTimeout = TimeSpan.FromSeconds( 1 );
            
            _openCloseSpeed = TimeSpan.FromSeconds( 0.2 );
        }

        public double CollapsedWidth { get; set; }

        public double CollapsedHeight { get; set; }

        public TimeSpan AutoHideTimeout { get; set; }

        public TimeSpan OpenCloseSpeed { get { return _openCloseSpeed; } }

        private TimeSpan    _openCloseSpeed;
    }


    class WindowAutoHider : IWindowAutoHider
    {
        public WindowAutoHider( Window parent )
                : this( parent, new WindowAutoHiderParams() )
        {
        }

        public WindowAutoHider( Window parent, WindowAutoHiderParams parameters )
        {
            _parent = parent;
            _mode = AutoHideMode.None;
            _currentState = AutoHideState.Visible;
            _params = parameters;

            _dropHelper = new DropHelper( parent );

            // This code may appear silly to an untrained eye, but if it's not here, even though 'parent.Left' has
            // a valid value, when we try animate it we might get an exception because DoubleAnimation might read it 
            // as NaN. It's fine everywhere else, only the animation is screwed up.
            parent.Left = parent.Left;
            parent.Top = parent.Top;
        }

        public AutoHideMode Mode
        {
            get { return _mode; }
            
            set
            {
                AutoHideMode previousMode = _mode;
                _mode = value;
                if( previousMode != _mode ) OnModeUpdated( previousMode );
            }
        }

        public AutoHideState State
        {
            get { return _currentState; }
        }

        public void Freeze()
        {
            _freezeCount++;
        }

        public void Unfreeze()
        {
            System.Diagnostics.Debug.Assert( _freezeCount > 0 );

            if( --_freezeCount == 0 )
            {
                StartAutoHideTimerIfNeeded();
            }
        }

        public void ShowWindow()
        {
            ExpandParentWindow();
        }

        private void OnModeUpdated( AutoHideMode previousMode )
        {
            if( this.Mode == AutoHideMode.None )
            {
                Deactivate();
            }
            else if( previousMode == AutoHideMode.None )
            {
                Activate();
            }
        }

        private void Activate()
        {
            _parent.MouseEnter += new System.Windows.Input.MouseEventHandler( OnMouseEnter );
            _parent.MouseLeave += new System.Windows.Input.MouseEventHandler( OnMouseLeave );
            _parent.Activated += new EventHandler( OnParentActivated );
            _parent.Deactivated += new EventHandler( OnParentDeactivated );

            DropHelper.AddDragStartedHandler( _parent, new RoutedEventHandler( OnDragStarted ) );
            DropHelper.AddDragStoppedHandler( _parent, new RoutedEventHandler( OnDragStopped ) );

            StartAutoHideTimerIfNeeded();
        }

        private void Deactivate()
        {
            _parent.MouseEnter -= new System.Windows.Input.MouseEventHandler( OnMouseEnter );
            _parent.MouseLeave -= new System.Windows.Input.MouseEventHandler( OnMouseLeave );
            _parent.Activated -= new EventHandler( OnParentActivated );
            _parent.Deactivated -= new EventHandler( OnParentDeactivated );

            DropHelper.RemoveDragStartedHandler( _parent, new RoutedEventHandler( OnDragStarted ) );
            DropHelper.RemoveDragStoppedHandler( _parent, new RoutedEventHandler( OnDragStopped ) );

            TerminateAutoHideTimer();
        }

        private void OnMouseEnter( object sender, System.Windows.Input.MouseEventArgs e )
        {
            TerminateAutoHideTimer();

            ExpandParentWindow();
        }

        private void OnMouseLeave( object sender, System.Windows.Input.MouseEventArgs e )
        {
            StartAutoHideTimerIfNeeded();
        }

        private void OnParentActivated( object sender, EventArgs e )
        {
            _parent.AllowDrop = false;
        }

        private void OnParentDeactivated( object sender, EventArgs e )
        {
            _parent.AllowDrop = true;

            StartAutoHideTimerIfNeeded();
        }

        private void OnDragStarted( object sender, RoutedEventArgs e )
        {
            _dragCount++;

            ExpandParentWindow();
        }

        private void OnDragStopped( object sender, RoutedEventArgs e )
        {
            System.Diagnostics.Debug.Assert( _dragCount > 0 );

            if( --_dragCount == 0 )
            {
                StartAutoHideTimerIfNeeded();
            }
        }

        private void StartAutoHideTimerIfNeeded()
        {
            bool isAutoHideFrozen = _freezeCount > 0;
            bool isDragDropInProgress = _dragCount > 0;
            bool isAutoHideEnabled = _mode != AutoHideMode.None;

            if( _currentState == AutoHideState.Visible &&
                isAutoHideEnabled && !isAutoHideFrozen && !isDragDropInProgress &&
                !_parent.IsMouseOver && !_parent.IsActive                          )
            {
                _autoHideTimer = new DispatcherTimer( DispatcherPriority.Normal, _parent.Dispatcher );

                _autoHideTimer.Interval = _params.AutoHideTimeout;
                _autoHideTimer.Tick += new EventHandler( OnAutoHideTimer );
                _autoHideTimer.IsEnabled = true;
            }
        }

        private void TerminateAutoHideTimer()
        {
            if( _autoHideTimer != null )
            {
                _autoHideTimer.Stop();
                _autoHideTimer = null;
            }
        }

        private void OnAutoHideTimer( object sender, EventArgs e )
        {
            if( _autoHideTimer == null ) return;

            CollapseParentWindow();

            TerminateAutoHideTimer();
        }

        private void CollapseParentWindow()
        {
            if( _currentState != AutoHideState.Visible ) return;

            _originalParentSize = _parent.ActualSize();
            _currentState = AutoHideState.Closing;

            if( _mode.IsHorizontalCollapse() )
            {
                AnimateTransition( Window.LeftProperty,
                                   _mode == AutoHideMode.CollapseRight ? 
                                        _parent.Width - _params.CollapsedWidth : 0,
                                   Window.WidthProperty,
                                   _params.CollapsedWidth,
                                   () => _currentState = AutoHideState.Collapsed    );
            }
            else
            {
                AnimateTransition( Window.TopProperty,
                                   _mode == AutoHideMode.CollapseRight ?
                                        _parent.Height - _params.CollapsedHeight : 0,
                                   Window.HeightProperty,
                                   _params.CollapsedHeight,
                                   () => _currentState = AutoHideState.Collapsed );
            }
        }

        private void ExpandParentWindow()
        {
            if( _currentState != AutoHideState.Collapsed ) return;

            _currentState = AutoHideState.Opening;

            if( _mode.IsHorizontalCollapse() )
            {
                AnimateTransition( Window.LeftProperty,
                                   _mode == AutoHideMode.CollapseRight ?
                                        _params.CollapsedWidth - _originalParentSize.Width : 0,
                                   Window.WidthProperty,
                                   _originalParentSize.Width,
                                   () => _currentState = AutoHideState.Visible                  );
            }
            else
            {
                AnimateTransition( Window.TopProperty,
                                   _mode == AutoHideMode.CollapseRight ?
                                        _params.CollapsedHeight - _originalParentSize.Height : 0,
                                   Window.HeightProperty,
                                   _originalParentSize.Height,
                                   () => _currentState = AutoHideState.Visible                    );
            }
        }

        private void AnimateTransition( DependencyProperty      placementProperty,
                                        double                  placementDelta,
                                        DependencyProperty      sizeProperty,
                                        double                  targetSize,
                                        Action                  completionAction   )
        {
            AnimateParentProperty( sizeProperty, targetSize, completionAction );

            if( placementDelta != 0 )
            {
                double startPlacement = (double)_parent.GetValue( placementProperty );

                AnimateParentProperty( placementProperty, startPlacement + placementDelta );
            }
        }

        private void AnimateParentProperty( DependencyProperty property,
                                            double              toValue   )
        {
            AnimateParentProperty( property, toValue, null );
        }

        private void AnimateParentProperty( DependencyProperty property,
                                            double              toValue,
                                            Action              completionAction )
        {
            var animation = new DoubleAnimation( toValue, _params.OpenCloseSpeed );

            animation.Completed += ( o, e ) =>
            {
                _parent.SetValue( property, toValue );
                _parent.BeginAnimation( property, null );
                if( completionAction != null ) completionAction();
            };

            _parent.BeginAnimation( property, animation );
        }

        private Window                  _parent;
        private AutoHideMode            _mode;
        private AutoHideState           _currentState;
        private WindowAutoHiderParams   _params;
        private DispatcherTimer         _autoHideTimer;
        private Size                    _originalParentSize;
        private int                     _freezeCount;
        private int                     _dragCount;
        private DropHelper              _dropHelper;
    }
}
