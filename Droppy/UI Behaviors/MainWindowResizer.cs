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
using System.Windows.Media.Animation;

using Droppy.Data;


namespace Droppy
{
    class MainWindowResizer
    {
        public MainWindowResizer( MainWindow parent )
        {
            _parent = parent;

            // Parent window starts off being auto-sized based on its content. We must make this call 
            // in order to enable resizing of the parent. However, we want to change the attribute only
            // after the window initialization is complete.  Otherwise, instead of the center of the
            // screen, the window shows up in the corner.
            _parent.Dispatcher.BeginInvoke( new Action( () =>
            {
                SetManualSizingOnParent();
            } ) );

            ResizeBarControl resizer = (ResizeBarControl)_parent.FindName( "Resizer" );

            resizer.Resize += OnResize;
            resizer.ResizeComplete += OnResizeComplete;

            _siteCellSize = CalculateSiteCellSize();
        }

        public void SizeParentToContent()
        {
            SetAutoSizingOnParent();

            _parent.widgetContainer.MinHeight = 0;
            _parent.widgetContainer.MinWidth = 0;

            _parent.UpdateLayout();

            SetManualSizingOnParent();
        }

        private void OnResizeStarted( ResizeBarEventArgs e )
        {
            _isResizing = true;
            _currentSize = new Size( _parent.ActualWidth, _parent.ActualHeight );

            // We set min width/height on widget container element so that it remains static when the user
            // sizes the main window to be smaller than what's needed to display the entire widget container.
            _parent.widgetContainer.MinHeight = _parent.widgetContainer.ActualHeight;
            _parent.widgetContainer.MinWidth = _parent.widgetContainer.ActualWidth;

            _parent.widgetContainer.HorizontalAlignment =
                e.ThumbId == ThumbId.Left ? HorizontalAlignment.Right : HorizontalAlignment.Left;

            _minimumSize = CalculateMinimumSize( e.ThumbId == ThumbId.Left );
        }

        private void OnResize( object sender, ResizeBarEventArgs e )
        {
            if( !_isResizing ) OnResizeStarted( e );

            UpdateWindowSize( e );

            int rows, columns;
            Size originalSize = _currentSize;

            CalculateMatrixDimensions( out rows, out columns );

            if( originalSize != _currentSize )
            {
                _parent.widgetContainer.Source.Resize(
                            rows, columns,
                            e.ThumbId == ThumbId.Left ? Data.WidgetContainerResizeJustify.Right :
                                                        Data.WidgetContainerResizeJustify.Left );

                _parent.widgetContainer.MinHeight += _currentSize.Height - originalSize.Height;
                _parent.widgetContainer.MinWidth += _currentSize.Width - originalSize.Width;
            }
        }

        private void OnResizeComplete( object sender, EventArgs e )
        {
            if( _parent.widgetContainer.HorizontalAlignment == HorizontalAlignment.Right )
            {
                BeginResizeAnimation( Window.LeftProperty, _parent.Left + _parent.Width - _currentSize.Width );
            }

            BeginResizeAnimation( Window.WidthProperty, _currentSize.Width );
            BeginResizeAnimation( Window.HeightProperty, _currentSize.Height );

            _isResizing = false;
        }

        private void UpdateWindowSize( ResizeBarEventArgs e )
        {
            double newWidth = _parent.ActualWidth + e.WidthDelta;
            double newHeight = _parent.ActualHeight + e.HeightDelta;
            double newLeft = _parent.Left + e.LeftDelta;

            if( newHeight < _minimumSize.Height ) newHeight = _minimumSize.Height;

            if( newWidth < _minimumSize.Width )
            {
                if( e.LeftDelta != 0.0 ) newLeft -= _minimumSize.Width - newWidth;

                newWidth = _minimumSize.Width;
            }

            _parent.Left = newLeft;
            _parent.Width = newWidth;
            _parent.Height = newHeight;
        }

        private void SetAutoSizingOnParent()
        {
            _parent.SizeToContent = System.Windows.SizeToContent.WidthAndHeight;

            _parent.ClearValue( Window.WidthProperty );
            _parent.ClearValue( Window.HeightProperty );
        }

        private void SetManualSizingOnParent()
        {
            _parent.Width = _parent.ActualWidth;
            _parent.Height = _parent.ActualHeight;

            _parent.SizeToContent = SizeToContent.Manual;
        }

        private Size CalculateSiteCellSize()
        {
            // This function calculates how much space actual cell in the widget matrix takes. For this calculation:
            // {-----------||---------[cell][cell][cell][cell]-----------||-------------}
            //   margin       padding                           padding      magrin
            //
            // '{' and '}' = bounding box of the widget container control
            // '||' = visible boundary of the widget container control
            //
            // cell_size = ( actual_size - total_padding - total_margin ) / number_of_cells

            return new Size( ( _parent.widgetContainer.ActualWidth -
                                    _parent.widgetContainer.Margin.Width() -
                                    _parent.widgetContainer.Padding.Width() ) / _parent.widgetContainer.Columns,
                             ( _parent.widgetContainer.ActualHeight -
                                    _parent.widgetContainer.Margin.Height() -
                                    _parent.widgetContainer.Padding.Height() ) / _parent.widgetContainer.Rows );
        }

        private Size CalculateMinimumSize( bool isLeftSideDrag )
        {
            Size            minimumSize = _currentSize;
            var             dataSource = _parent.widgetContainer.Source;

            foreach( MatrixLoc loc in dataSource.Bounds.AsEnumerable( ScanDirection.BottomToTop ) )
            {
                if( dataSource[ loc ] != null || loc.Row == dataSource.Bounds.Row )
                {
                    minimumSize.Height -= _siteCellSize.Height *
                                            ( dataSource.Bounds.LastRow - loc.Row - 1 );
                    break;
                }
            }

            var scanParameters = isLeftSideDrag ?
                new { dir = ScanDirection.LeftToRight, stopCol = dataSource.Bounds.LastColumn - 1 } :
                new { dir = ScanDirection.RightToLeft, stopCol = dataSource.Bounds.Column };

            foreach( MatrixLoc loc in dataSource.Bounds.AsEnumerable( scanParameters.dir ) )
            {
                if( dataSource[ loc ] != null || loc.Column == scanParameters.stopCol )
                {
                    int removableCellCount = isLeftSideDrag ?
                                ( loc.Column - dataSource.Bounds.Column ) :
                                ( dataSource.Bounds.LastColumn - loc.Column - 1 );

                    minimumSize.Width -= _siteCellSize.Width * removableCellCount;

                    break;
                }
            }

            return minimumSize;
        }

        private void CalculateMatrixDimensions( out int rows, out int columns )
        {
            rows = _parent.widgetContainer.Rows;
            columns = _parent.widgetContainer.Columns;

            if( _currentSize.Width > _parent.ActualWidth + 45 )
            {
                while( columns > 1 && _currentSize.Width > _parent.ActualWidth + 45 )
                {
                    columns--;
                    _currentSize.Width -= _siteCellSize.Width;
                }
            }
            else
            {
                while( _currentSize.Width + _siteCellSize.Width - 25 <= _parent.ActualWidth )
                {
                    columns++;
                    _currentSize.Width += _siteCellSize.Width;
                }
            }

            if( _currentSize.Height > _parent.ActualHeight + 15 )
            {
                while( rows > 1 && _currentSize.Height > _parent.ActualHeight + 15 )
                {
                    rows--;
                    _currentSize.Height -= _siteCellSize.Height;
                }
            }
            else
            {
                while( _currentSize.Height + _siteCellSize.Height - 10 <= _parent.ActualHeight )
                {
                    rows++;
                    _currentSize.Height += _siteCellSize.Height;
                }
            }
        }

        private void BeginResizeAnimation( DependencyProperty property, double toValue )
        {
            DoubleAnimation animation = new DoubleAnimation();

            animation.To = toValue;
            animation.Duration = new TimeSpan( 1000000 );

            // We want to clear the animation because as long as that object is alive it will
            // no longer allow us to resize the window as its value will take precedence
            // over the local one
            animation.Completed += ( o, e ) =>
            {
                _parent.SetValue( property, toValue );
                _parent.BeginAnimation( property, null );
            };

            _parent.BeginAnimation( property, animation );
        }

        private MainWindow      _parent;
        private bool            _isResizing;
        private Size            _currentSize;
        private Size            _minimumSize;
        private Size            _siteCellSize;
    }
}
