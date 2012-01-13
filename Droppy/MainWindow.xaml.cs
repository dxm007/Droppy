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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using Droppy.Data;


namespace Droppy
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            _windowData = new MainWindowData();

            _windowData.Document = new Data.WidgetDocument();
            _windowData.Document.IsDirtyChanged += OnDocumentDirtyFlagChanged;
            _windowData.Document.Load();

            widgetContainer.Source = _windowData.Document.Root;

            this.Loaded += new RoutedEventHandler(MainWindow_Loaded);
            this.Closing += OnClosing;
        }

        public static new MainWindow GetWindow( DependencyObject dependencyObject )
        {
            return (MainWindow)Window.GetWindow( dependencyObject );
        }

        public void FreezeAutoHide()
        {
            _windowAutoHider.Freeze();
        }

        public void UnfreezeAutoHide()
        {
            _windowAutoHider.Unfreeze();
        }

        public new void Show()
        {
            base.Show();
            _windowAutoHider.ShowWindow();
        }

        private void OnClosing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            if( _windowData.Document.IsDirty )
            {
                SaveDocument();
            }
        }

        private void MainWindow_Loaded( object sender, RoutedEventArgs e )
        {
            new TrashCanPopupManager( this, _windowData );

            IWindowMover windowMover = new WindowMover( this );

            _windowResizer = new MainWindowResizer( this, _windowData );

            IWindowDocker docker = new WindowDocker( this, windowMover );

            _windowAutoHider = new WindowAutoHider( this );

            new WindowDockMediator( this, _windowAutoHider, docker );
        }

        private void OnToolsBtnClick( object sender, RoutedEventArgs e )
        {
            ToolsMenu.PlacementTarget = ToolsHdrButton;
            ToolsMenu.IsOpen = true;
        }

        private void OnCloseBtnClick( object sender, RoutedEventArgs e )
        {
            Close();
        }

        private void OnExportMenuItemClick( object sender, RoutedEventArgs e )
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            dlg.Title = "Export Configuration";
            
            AssignCommonFileDialogProps( dlg );

            if( dlg.ShowDialog() != true ) return;

            try
            {
                _windowData.Document.Save( dlg.OpenFile(), WidgetDocSaveFormat.Xml );
            }
            catch( Exception ex )
            {
                ReportToUserApplicationError( ex, "There were issues exporting configuration." );
            }
        }

        private void OnImportMenuItemClick( object sender, RoutedEventArgs e )
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Title = "Import Configuration";

            AssignCommonFileDialogProps( dlg );

            if( dlg.ShowDialog() != true ) return;

            try
            {
                _windowData.Document.Load( dlg.OpenFile(), WidgetDocSaveFormat.Xml );

                widgetContainer.Source = _windowData.Document.Root;

                _windowResizer.SizeParentToContent();
            }
            catch( Exception ex )
            {
                ReportToUserApplicationError( ex, "There were issues importing configuration." );
            }
        }

        private static void AssignCommonFileDialogProps( Microsoft.Win32.FileDialog dlg )
        {
            dlg.DefaultExt = "*.droppy";
            dlg.Filter = "Droppy Configuration Files (*.droppy)|*.droppy";
        }

        private void OnDocumentDirtyFlagChanged( object sender, EventArgs e )
        {
            if( _windowData.Document.IsDirty )
            {
                DispatcherTimer timer = new DispatcherTimer( DispatcherPriority.Background );

                timer.Interval = new TimeSpan( 0, 0, 5 );
                timer.Tick += (o,e2)=> {
                       ( (DispatcherTimer)o ).Stop();
                       SaveDocument();
                    };

                timer.Start();
            }
        }

        private void SaveDocument()
        {
            try
            {
                _windowData.Document.Save();
                _windowData.Document.IsDirty = false;
            }
            catch( Exception ex )
            {
                ReportToUserApplicationError( ex, "There were issues saving changes" );
            }
        }

        private void ReportToUserApplicationError( string format, params object[] args )
        {
            MessageBox.Show( string.Format( format, args ),
                             "Application Error", MessageBoxButton.OK, MessageBoxImage.Warning );
        }

        private void ReportToUserApplicationError( Exception ex, string format, params object[] args )
        {
            object[] additionalArgs = new object[] { ex.GetType().Name, ex.Message };

            ReportToUserApplicationError(
                string.Format( "{0}\n\nException Type: {{{1}}}\nException Text: {{{2}}}",
                               format, args.Length, args.Length + 1 ),
                args.Concat( additionalArgs ).ToArray()                                   );
        }

        private MainWindowData      _windowData;
        private MainWindowResizer   _windowResizer;
        private IWindowAutoHider    _windowAutoHider;
    }


    class MainWindowData
    {
        public Data.WidgetDocument Document { get; set; }
    }


    class TrashCanPopupManager
    {
        public TrashCanPopupManager( MainWindow parent, MainWindowData parentData )
        {
            _parent = parent;
            _parentData = parentData;
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
                    System.Diagnostics.Debug.Print( "Deleting widget..." );
                    dragData.Widget.Parent.Remove( dragData.Widget );
                }
                else
                {
                    System.Diagnostics.Debug.Print( "Deleted widget is deleted again!!" );
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
                                                               BuildPopupAnimation( 1.0, 0.0, (o,e2) => _trashCan.IsOpen = false ) );
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
            var animation = new DoubleAnimation( );

            animation.Duration = new TimeSpan( 2000000 );
            animation.From = fromValue;
            animation.To = toValue;

            if( completedHandler != null )
            {
                animation.Completed += completedHandler;
            }

            return animation;
        }

        private MainWindow          _parent;
        private MainWindowData      _parentData;
        private Popup               _trashCan;
        private FrameworkElement    _trashIcon;
    }


    class MainWindowResizer
    {
        public MainWindowResizer( MainWindow parent, MainWindowData windowData )
        {
            _parent = parent;
            _windowData = windowData;

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
                            e.ThumbId == ThumbId.Left ? Data.WidgetContainerResizeJustify.Right:
                                                        Data.WidgetContainerResizeJustify.Left   );

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
            animation.Completed += (o,e) => {
                _parent.SetValue( property, toValue );
                _parent.BeginAnimation( property, null );
            };

            _parent.BeginAnimation( property, animation );
        }

        private MainWindow      _parent;
        private MainWindowData  _windowData;
        private bool            _isResizing;
        private Size            _currentSize;
        private Size            _minimumSize;
        private Size            _siteCellSize;
    }

}
