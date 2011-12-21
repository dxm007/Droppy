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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Droppy
{
    class MainWindowData
    {
        public Data.WidgetDocument  Document { get; set; }
    }


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

        void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if( _windowData.Document.IsDirty )
            {
                SaveDocument();
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            new WindowMover( this );
            new TrashCanPopupManager( this, _windowData );
            new MainWindowResizer( this, _windowData );
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
                MessageBox.Show( string.Format( "There were issues saving changes.\n\nException Type: {0}\nException Text: {1}",
                                                ex.GetType().Name, ex.Message ),
                                    "Application Error", MessageBoxButton.OK, MessageBoxImage.Warning );
            }
        }


        private MainWindowData      _windowData;
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

            // Change the attribute once window initialization is complete.  Otherwise, instead of the
            // center of the screen, the window shows up in the corner.
            _parent.Dispatcher.BeginInvoke( new Action( () => {
                _parent.SizeToContent = SizeToContent.Manual;
            } ) );


            ResizeBarControl resizer = _parent.FindName( "Resizer" ) as ResizeBarControl;

            if( resizer != null )
            {
                resizer.Resize += OnResize;
                resizer.ResizeComplete += OnResizeComplete;

                _siteHeight = ( _parent.widgetContainer.ActualHeight -
                                    _parent.widgetContainer.Margin.Height() -
                                    _parent.widgetContainer.Padding.Height()  ) / _parent.widgetContainer.Rows;
                _siteWidth = ( _parent.widgetContainer.ActualWidth -
                                    _parent.widgetContainer.Margin.Width() -
                                    _parent.widgetContainer.Padding.Width()  ) / _parent.widgetContainer.Columns;

                _parent.widgetContainer.MinHeight = _parent.widgetContainer.ActualHeight;
                _parent.widgetContainer.MinWidth = _parent.widgetContainer.ActualWidth;
            }
        }

        private void OnResize( object sender, ResizeBarEventArgs e )
        {
            if( !_isResizing )
            {
                _isResizing = true;
                _currentSize = new Size( _parent.ActualWidth, _parent.ActualHeight );

                _parent.widgetContainer.HorizontalAlignment = 
                    e.ThumbId == ThumbId.Left ? HorizontalAlignment.Right : HorizontalAlignment.Left;

                CalculateMinimumSize( e.ThumbId == ThumbId.Left );
            }

            UpdateWindowSize( e );

            int rows, columns;
            Size originalSize = _currentSize;

            if( CalculateMatrixDimensions( out rows, out columns ) )
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

        private void CalculateMinimumSize( bool isLeftSideDrag )
        {
            int numRows = _parent.widgetContainer.Rows;
            int numCols = _parent.widgetContainer.Columns;
            int r, c, firstColumn, lastColumn, colStep, firstRow, lastRow;
            int prelastRow, prelastColumn;

            if( isLeftSideDrag )
            {
                firstColumn = _parent.widgetContainer.Source.FirstColumn;
                lastColumn = firstColumn + numCols;
                prelastColumn = lastColumn - 1;
                colStep = 1;
            }
            else
            {
                prelastColumn = _parent.widgetContainer.Source.FirstColumn;
                lastColumn = prelastColumn - 1;
                firstColumn = lastColumn + numCols;
                colStep = -1;
            }

            prelastRow = _parent.widgetContainer.Source.FirstRow;
            lastRow = prelastRow - 1;
            firstRow = lastRow + numRows;

            _minimumSize = _currentSize;

            for( r = firstRow; r != prelastRow; r-- )
            {
                for( c = firstColumn; c != lastColumn; c += colStep )
                {
                    if( _parent.widgetContainer.Source[ r, c ] != null ) break;
                }

                if( c != lastColumn ) break;

                _minimumSize.Height -= _siteHeight;
            }

            for( c = firstColumn; c != prelastColumn; c += colStep )
            {
                for( r = firstRow; r != lastRow; r-- )
                {
                    if( _parent.widgetContainer.Source[ r, c ] != null ) break;
                }

                if( r != lastRow ) break;

                _minimumSize.Width -= _siteWidth;
            }
        }

        private bool CalculateMatrixDimensions( out int rows, out int columns )
        {
            Size sz = _currentSize;

            rows = _parent.widgetContainer.Rows;
            columns = _parent.widgetContainer.Columns;

            if( _currentSize.Width > _parent.ActualWidth + 45 )
            {
                while( columns > 1 && _currentSize.Width > _parent.ActualWidth + 45 )
                {
                    columns--;
                    _currentSize.Width -= _siteWidth;
                }
            }
            else
            {
                while( _currentSize.Width + _siteWidth - 25 <= _parent.ActualWidth )
                {
                    columns++;
                    _currentSize.Width += _siteWidth;
                }
            }

            if( _currentSize.Height > _parent.ActualHeight + 15 )
            {
                while( rows > 1 && _currentSize.Height > _parent.ActualHeight + 15 )
                {
                    rows--;
                    _currentSize.Height -= _siteHeight;
                }
            }
            else
            {
                while( _currentSize.Height + _siteHeight - 10 <= _parent.ActualHeight )
                {
                    rows++;
                    _currentSize.Height += _siteHeight;
                }
            }

            return sz != _currentSize;
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
        private double          _siteHeight;
        private double          _siteWidth;
        private bool            _isResizing;
        private Size            _currentSize;
        private Size            _minimumSize;
    }
}
