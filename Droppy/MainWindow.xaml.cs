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
    public partial class MainWindow : Window,
                                      IWindowMoverOwner
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
            new WindowMover( this, this );
            new TrashCanPopupManager( this, _windowData );
        }

        bool IWindowMoverOwner.IsMovable
        {
            get { return true; }
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
                dragData.Widget.Parent.Remove( dragData.Widget );
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



    public interface IWindowMoverOwner
    {
        bool IsMovable { get; }
    }

    public class WindowMover
    {
        public WindowMover( Window wnd, IWindowMoverOwner owner )
        {
            _wnd = wnd;
            _owner = owner;

            SetupEventCallbacks();
        }

        private void SetupEventCallbacks()
        {
            _wnd.MouseLeftButtonDown += OnMouseLeftButtonDown;
            _wnd.MouseLeftButtonUp += OnMouseLeftButtonUp;
            _wnd.MouseMove += OnMouseMove;
            _wnd.Closed += OnWindowClosed;
        }

        private void RevokeEventCallbacks()
        {
            _wnd.MouseLeftButtonDown -= OnMouseLeftButtonDown;
            _wnd.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            _wnd.MouseMove -= OnMouseMove;
            _wnd.Closed -= OnWindowClosed;
        }

        void OnWindowClosed(object sender, EventArgs e)
        {
            RevokeEventCallbacks();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if( _owner.IsMovable && _wnd.CaptureMouse() )
            {
                _isMouseDown = true;
                _lastScreenPoint = _wnd.PointToScreen( e.GetPosition( _wnd ) );
            }
        }
        
        void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if( _isMouseDown )
            {
                _isMouseDown = false;
                _isMoving = false;
                _wnd.ReleaseMouseCapture();
            }
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if( !_isMouseDown ) return;

            Point pt = _wnd.PointToScreen( e.GetPosition( _wnd ) );
            Vector vect = Point.Subtract( pt, _lastScreenPoint );

            if( !_isMoving )
            {
                // don't move the window if the user clicked the mouse and moved it less than 5 pixels
                // in any direction
                if( vect.LengthSquared < 25 ) return;

                _isMoving = true;
            }

            _wnd.Left += vect.X;
            _wnd.Top += vect.Y;
            _lastScreenPoint = pt;
        }

        private Window                  _wnd;
        private IWindowMoverOwner       _owner;
        private Point                   _lastScreenPoint;
        private bool                    _isMouseDown;
        private bool                    _isMoving;
    }
}
