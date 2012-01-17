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
    /// <summary>
    /// Specifies the mode of the auto-hide behavior
    /// </summary>
    enum AutoHideMode
    {
        None,
        CollapseLeft,
        CollapseTop,
        CollapseRight,
        CollapseBottom
    }


    /// <summary>
    /// Extension methods for AutoHideMode enumeration type
    /// </summary>
    static class AutoHideModeExtensionsMethods
    {
        /// <summary>
        /// Tests if the auto collapse mode is horizontal
        /// </summary>
        /// <param name="mode">Auto-hide mode to test</param>
        /// <returns>True of auto-hide mode is either collapse left or collapse right</returns>
        public static bool IsHorizontalCollapse( this AutoHideMode mode )
        {
            return mode == AutoHideMode.CollapseLeft || mode == AutoHideMode.CollapseRight;
        }
    }


    /// <summary>
    /// Specifies the current state of the window that uses auto-hide
    /// window behavior
    /// </summary>
    enum AutoHideState
    {
        Visible,
        Collapsed,
        Opening,
        Closing
    }


    /// <summary>
    /// Implemented by auto-hide window behavior object
    /// </summary>
    interface IWindowAutoHider
    {
        /// <summary>
        /// Gets/sets auto-hide mode
        /// </summary>
        AutoHideMode Mode { get; set; }

        /// <summary>
        /// Gets the current auto-hide state
        /// </summary>
        AutoHideState State { get; }

        /// <summary>
        /// Disables auto-hide behavior until Unfreeze() is called
        /// </summary>
        void Freeze();

        /// <summary>
        /// Restores auto-hide behavior. This method must be called as
        /// many times as Freeze() was called for auto-hide behavior to become
        /// enabled
        /// </summary>
        void Unfreeze();

        /// <summary>
        /// If the window is currently in a hidden state, calling this method will
        /// force it to transition to a visible state.
        /// </summary>
        void ShowWindow();
    }


    /// <summary>
    /// WindowAutoHider parameter class. This class is used to pass in a set of
    /// parameters during the initialization
    /// </summary>
    class WindowAutoHiderParams
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public WindowAutoHiderParams()
        {
            this.CollapsedHeight = 5.0;
            this.CollapsedWidth = 5.0;
            this.AutoHideTimeout = TimeSpan.FromSeconds( 1 );
            this.OpenCloseSpeed = TimeSpan.FromSeconds( 0.2 );
        }

        /// <summary>
        /// Gets/sets the width of the parent window in the collapsed state
        /// </summary>
        public double CollapsedWidth { get; set; }

        /// <summary>
        /// Gets/sets the height of the parent window in the collapsed state
        /// </summary>
        public double CollapsedHeight { get; set; }

        /// <summary>
        /// Gets/sets the time interval the window will remain visible after becoming
        /// inactive. When that time expires, auto-hider will collapse the window
        /// </summary>
        public TimeSpan AutoHideTimeout { get; set; }

        /// <summary>
        /// Gets/sets the time interval the transition from visible to collapsed and
        /// vice-a-versa will take.
        /// </summary>
        public TimeSpan OpenCloseSpeed { get; set; }
    }


    /// <summary>
    /// Defines auto-hide window behavior
    /// </summary>
    class WindowAutoHider : IWindowAutoHider
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="parent">Reference to the parent window</param>
        public WindowAutoHider( Window parent )
                : this( parent, new WindowAutoHiderParams() )
        {
        }

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="parent">Reference to the parent window</param>
        /// <param name="parameters">Auto-hider configuration settings</param>
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

        #region - - - - - - - IWindowAutoHider Interface  - - - - - - - - - - -

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

        #endregion
        #endregion

        #region ----------------------- Private Members -----------------------

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

        #endregion
    }
}
