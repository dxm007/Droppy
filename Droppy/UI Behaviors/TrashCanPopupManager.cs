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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;


namespace Droppy
{
    class TrashCanPopupManager
    {
        public TrashCanPopupManager( MainWindow parent )
        {
            _trashCan = parent.FindName( "TrashCanPopup" ) as Popup;
            _trashIcon = parent.FindName( "TrashIconRect" ) as FrameworkElement;

            if( _trashCan != null )
            {
                DragHelper.DragStarted += OnGlobalDragStarted;
                DragHelper.DragComplete += OnGlobalDragComplete;

                var dropHelper = new DropHelper( _trashCan );

                dropHelper.QueryDragDataValid += OnTrashCanDragQueryDataValid;
                dropHelper.TargetDrop += OnTrashCanDrop;
            }

            if( _trashIcon != null )
            {
                _trashIcon.RenderTransform = new ScaleTransform( 1.0, 1.0 );
                _trashIcon.RenderTransformOrigin = new Point( 0.5, 0.9 );
            }
        }

        private void OnTrashCanDragQueryDataValid( object sender, DropHelperEventArgs e )
        {
            if( e.EventArgs.Data.GetDataPresent( "Droppy.WidgetSiteDragDropData" ) )
            {
                e.EventArgs.Effects = DragDropEffects.Move;
            }
            else
            {
                e.EventArgs.Effects = DragDropEffects.None;
            }
        }

        private void OnTrashCanDrop( object sender, DropHelperEventArgs e )
        {
            var dragData = e.EventArgs.Data.GetData( "Droppy.WidgetSiteDragDropData" ) as WidgetSiteDragDropData;

            if( dragData.Widget != null )
            {
                if( dragData.Widget.HasOwner )
                {
                    dragData.Widget.Parent.Remove( dragData.Widget );
                }
            }
        }

        private void OnGlobalDragStarted( object sender, DragHelperEventArgs e )
        {
            var dragData = e.Data.GetData( "Droppy.WidgetSiteDragDropData" ) as WidgetSiteDragDropData;

            if( dragData != null && dragData.Widget != null )
            {
                _trashCan.Placement = PlacementMode.Left;
                _trashCan.PlacementTarget = dragData.Site;
                _trashCan.IsOpen = true;

                if( _trashIcon != null )
                {
                    _trashIcon.RenderTransform.BeginAnimation( ScaleTransform.ScaleXProperty, BuildPopupAnimation( 0.0, 1.0 ) );
                    _trashIcon.RenderTransform.BeginAnimation( ScaleTransform.ScaleYProperty, BuildPopupAnimation( 0.0, 1.0 ) );
                }
            }
        }

        private void OnGlobalDragComplete( object sender, DragHelperEventArgs e )
        {
            if( _trashCan.IsOpen )
            {
                if( _trashIcon != null )
                {
                    _trashIcon.RenderTransform.BeginAnimation( ScaleTransform.ScaleXProperty, BuildPopupAnimation( 1, 0 ) );
                    _trashIcon.RenderTransform.BeginAnimation( ScaleTransform.ScaleYProperty,
                                                               BuildPopupAnimation( 1.0, 0.0, ( o, e2 ) => _trashCan.IsOpen = false ) );
                }
                else
                {
                    _trashCan.IsOpen = false;
                }
            }
        }

        private AnimationTimeline BuildPopupAnimation( double fromValue, double toValue )
        {
            return BuildPopupAnimation( fromValue, toValue, null );
        }

        private AnimationTimeline BuildPopupAnimation( double fromValue, double toValue, EventHandler completedHandler )
        {
            var animation = new DoubleAnimation();

            animation.Duration = new TimeSpan( 2000000 );
            animation.From = fromValue;
            animation.To = toValue;

            if( completedHandler != null )
            {
                animation.Completed += completedHandler;
            }

            return animation;
        }

        private Popup               _trashCan;
        private FrameworkElement    _trashIcon;
    }

}
