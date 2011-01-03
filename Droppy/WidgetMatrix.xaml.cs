using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Droppy
{
    class WidgetMatrixData
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public Data.WidgetContainerData Source { get; set; }
        public WidgetSiteControl[,] SiteGrid { get; set; }
    }

    /// <summary>
    /// Interaction logic for WidgetMatrix.xaml
    /// </summary>
    public partial class WidgetMatrix : UserControl
    {
        public WidgetMatrix()
        {
            InitializeComponent();

            _controlData = new WidgetMatrixData();

            new WidgetMatrixDropHelper( this, _controlData );
        }

        public int Columns
        {
            get { return _controlData.Columns; }
            set { Resize( Rows, value ); }
        }

        public int Rows
        {
            get { return _controlData.Rows; }
            set { Resize( value, Columns ); }
        }

        public Data.WidgetContainerData Source
        {
            get { return _controlData.Source; }
            set { if( _controlData.Source != value ) UpdateSource( value ); }
        }

        public void Resize(int rows, int columns)
        {
            int r, c;

            ResizeRowOrColumn( matrixPanel.RowDefinitions, rows );
            ResizeRowOrColumn( matrixPanel.ColumnDefinitions, columns );

            _controlData.Rows = rows;
            _controlData.Columns = columns;

            _controlData.SiteGrid = new WidgetSiteControl[ rows, columns];

            foreach( var child in matrixPanel.Children )
            {
                WidgetSiteControl site = child as WidgetSiteControl;

                if( site == null ) continue;

                if( site.ContainerRow >= rows ||
                    site.ContainerColumn >= columns )
                {
                    matrixPanel.Children.Remove( site );
                }
                else
                {
                    _controlData.SiteGrid[ site.ContainerRow, site.ContainerColumn ] = site;
                }
            }

            for( r = 0; r < rows; r++ )
            {
                for( c = 0; c < columns; c++ )
                {
                    if( _controlData.SiteGrid[ r, c ] == null )
                    {
                        InitWidgetSite( r, c );
                    }

                    _controlData.SiteGrid[ r, c ].Content = _controlData.Source[ r, c ];
                }
            }
        }

        private void InitWidgetSite( int row, int column )
        {
            var site = new WidgetSiteControl();

            site.SetValue( Grid.RowProperty, row );
            site.SetValue( Grid.ColumnProperty, column );
            site.RenderTransform = new TranslateTransform();

            matrixPanel.Children.Add( site );

            _controlData.SiteGrid[ row, column ] = site;
        }

        private void ResizeRowOrColumn<T>( IList<T> col,
                                           int      newSize ) where T : DefinitionBase, new()
        {
            if( col.Count > newSize )
            {
                while( col.Count > newSize )
                {
                    col.RemoveAt( col.Count - 1 );
                }
            }
            else if( col.Count < newSize )
            {
                while( col.Count < newSize )
                {
                    col.Add( new T() );
                }
            }
        }

        private void UpdateSource( Data.WidgetContainerData source )
        {
            if( _controlData.Source != null )
            {
                _controlData.Source.ContainerChanged -= OnWidgetContainerChanged;
            }

            _controlData.Source = source;

            if( _controlData.Source != null )
            {
                Resize( _controlData.Source.RowCount, _controlData.Source.ColumnCount );

                _controlData.Source.ContainerChanged += OnWidgetContainerChanged;
            }
            else
            {
                Resize( 0, 0 );
            }
        }

        private void OnWidgetContainerChanged( object sender, Data.WidgetContainerChangedEventArgs e )
        {
            foreach( var child in matrixPanel.Children )
            {
                var site = child as WidgetSiteControl;

                if( site != null && site.ContainerRow == e.Row && site.ContainerColumn == e.Column )
                {
                    site.Content = e.NewWidget;
                }
            }
        }


        private WidgetMatrixData    _controlData;
    }




    class WidgetMatrixDropHelper : DropHelper
    {
        public WidgetMatrixDropHelper( WidgetMatrix parent, WidgetMatrixData data ) : base( parent )
        {
            _parent = parent;
            _controlData = data;
            _prevRelocatedSites = new List<WidgetSiteControl>();
        }

        protected override void OnQueryDragDataValid( object sender, DragEventArgs e )
        {
            base.OnQueryDragDataValid( sender, e );

            PreprocessDataObject( e, UpdateMatrixUI );
        }

        protected override void OnTargetDrop(object sender, DragEventArgs e)
        {
            base.OnTargetDrop( sender, e );

            PreprocessDataObject( e, CommitMove );
        }

        protected override void OnRealTargetDragLeave(object sender, DragEventArgs e)
        {
            CancelMove();
        }

        private delegate void ProcessDataObjectDelegate( int insertRow, int insertCol, WidgetSiteDragDropData data );

        private void PreprocessDataObject( DragEventArgs eventArgs, ProcessDataObjectDelegate callback )
        {
            WidgetSiteDragDropData data = eventArgs.Data.GetData( "Droppy.WidgetSiteDragDropData" ) as WidgetSiteDragDropData;

            if( data == null )
            {
                eventArgs.Effects = DragDropEffects.None;
            }
            else
            {
                Point   pt = eventArgs.GetPosition( this.Target ) - 
                                        new Vector( data.DraggableOffset.X, data.DraggableOffset.Y );
                Rect    dragRect =  new Rect( pt, new Size( data.Site.ActualWidth + data.Site.Margin.Width(),
                                                            data.Site.ActualHeight + data.Site.Margin.Height() ) );
                Point   dragCenter = new Point( dragRect.X + dragRect.Width / 2,
                                                dragRect.Y + dragRect.Height / 2 );
                int     insertRow = (int)( ( dragCenter.Y + dragRect.Height ) / dragRect.Height ) - 1;
                int     insertColumn = (int)( ( dragCenter.X + dragRect.Width ) / dragRect.Width ) - 1;

                if( insertRow < 0 || insertColumn < 0 ||
                    insertRow >= _controlData.Rows || insertColumn >= _controlData.Columns )
                {
                    eventArgs.Effects = DragDropEffects.None;
                }
                else
                {
                    eventArgs.Effects = DragDropEffects.Move;

                    callback( insertRow, insertColumn, data );
                }
            }
        }

        private void UpdateMatrixUI( int insertRow, int insertCol, WidgetSiteDragDropData data )
        {
            List< WidgetSiteControl >   relocatedSites;
            double                      translateDistance = data.Site.ActualHeight + data.Site.Margin.Height();
                
            if( insertRow > data.Site.ContainerRow ) translateDistance = -translateDistance;

            relocatedSites = GetShiftedSiteList( insertRow, insertCol, data );

            var sitesToMove = relocatedSites.Except( _prevRelocatedSites ).ToList();
            var sitesToReverse = _prevRelocatedSites.Except( relocatedSites ).ToList();

            foreach( var site in sitesToMove )
            {
                site.RenderTransform.BeginAnimation( TranslateTransform.YProperty, 
                                                     BuildAnimation2( translateDistance ) );
            }

            foreach( var site in sitesToReverse )
            {
                site.RenderTransform.BeginAnimation( TranslateTransform.YProperty, 
                                                     BuildAnimation2( 0 )           );
            }

            _prevRelocatedSites = relocatedSites;
        }

        private void CommitMove( int insertRow, int insertCol, WidgetSiteDragDropData data )
        {
            List< WidgetSiteControl >   relocatedSites;

            relocatedSites = GetShiftedSiteList( insertRow, insertCol, data );

            if( relocatedSites.Count > 0 )
            {
                int shiftDirection = insertRow < data.Site.ContainerRow ? 1 : -1;

                var sites = from a in relocatedSites
                            select new { site = a,
                                         pos = a.ContainerRow + shiftDirection,
                                         widget = (Data.WidgetData)a.Content    };

                var sitesList = sites.ToList();

                foreach( var siteInfo in sitesList )
                {
                    _controlData.Source[ siteInfo.pos, 0 ] = siteInfo.widget;
                    siteInfo.site.RenderTransform.BeginAnimation( TranslateTransform.YProperty, null );
                }

                _controlData.Source[ insertRow, insertCol ] = data.Widget;
            }

            // If there are any controls left out of original position, return them now.
            _prevRelocatedSites = _prevRelocatedSites.Except( relocatedSites ).ToList();
            CancelMove();
        }

        private void CancelMove()
        {
            foreach( var site in _prevRelocatedSites )
            {
                site.RenderTransform.BeginAnimation( TranslateTransform.YProperty, 
                                                        BuildAnimation2( 0 )           );
            }

            _prevRelocatedSites.Clear();
        }

        private List< WidgetSiteControl > GetShiftedSiteList( int insertRow, int insertCol, WidgetSiteDragDropData data )
        {
            int                         sourceRow = data.Site.ContainerRow;
            List< WidgetSiteControl >   relocatedSites = new List<WidgetSiteControl>( _controlData.Rows + _controlData.Columns );
            int                         i, step;

            if( insertRow != sourceRow )
            {
                if( insertRow < sourceRow )
                {
                    step = 1;
                }
                else
                {
                    step = -1;
                }

                for( i = insertRow; i != sourceRow; i += step )
                {
                    relocatedSites.Add( _controlData.SiteGrid[ i, insertCol ] );
                }
            }

            return relocatedSites;
        }

        private AnimationTimeline BuildAnimation2( double distance )
        {
            DoubleAnimationUsingKeyFrames animation = new DoubleAnimationUsingKeyFrames();

            animation.Duration = new TimeSpan( 4000000 );
            animation.KeyFrames.Add( 
                new SplineDoubleKeyFrame( distance,
                                          KeyTime.FromPercent( 1 ),
                                          new KeySpline( 0.4, 0, 0, 0.75 ) ) );
            animation.FillBehavior = ( distance == 0.0 ? FillBehavior.Stop : FillBehavior.HoldEnd );

            return animation;
        }

        private AnimationTimeline BuildAnimation( double distance )
        {
            DoubleAnimation animation = new DoubleAnimation();

            animation.Duration = new TimeSpan( 2500000 );
            animation.To = distance;
            animation.FillBehavior = ( distance == 0.0 ? FillBehavior.Stop : FillBehavior.HoldEnd );

            return animation;
        }

        private WidgetMatrix                    _parent;
        private WidgetMatrixData                _controlData;
        private List< WidgetSiteControl >       _prevRelocatedSites;
    }




    public class DropHelperEventArgs : EventArgs
    {
        public DropHelperEventArgs( object originalSender, DragEventArgs eventArgs )
        {
            _sender = originalSender;
            _eventArgs = eventArgs;
        }

        public object OriginalSender { get { return _sender; } }
        public DragEventArgs EventArgs { get { return _eventArgs; } }

        private object          _sender;
        private DragEventArgs   _eventArgs;
    }



    public class DropHelper
    {
        public DropHelper( UIElement target )
        {
            _target = target;

            _target.DragEnter += OnTargetDragEnter;
            _target.DragLeave += OnTargetDragLeave;
            _target.DragOver += OnTargetDragOver;
            _target.Drop += OnTargetDrop;
        }

        public UIElement Target { get { return _target; } }

        #region - - - - - - - - - IDragOver Attached Property - - - - - - - - - - - - - - - -

        public static readonly DependencyProperty IsDragOverProperty = 
                    DependencyProperty.RegisterAttached( "IsDragOver", typeof( bool ), typeof( DropHelper ),
                                                         new FrameworkPropertyMetadata(  false )             );

        public static void SetIsDragOver( UIElement element, bool value )
        {
            element.SetValue( IsDragOverProperty, value );
        }

        public static bool GetIsDragOver( UIElement element )
        {
            return (bool)element.GetValue( IsDragOverProperty );
        }

        #endregion


        public event EventHandler< DropHelperEventArgs > QueryDragDataValid;
        public event EventHandler< DropHelperEventArgs > TargetDrop;
        public event EventHandler< DropHelperEventArgs > RealTargetDragLeave;

        protected virtual void OnTargetDragEnter(object sender, DragEventArgs e)
        {
            _dragInProgress = true;

            OnQueryDragDataValid( sender, e );

            if( e.Effects != DragDropEffects.None && !_isDragOverSignalled )
            {
                SetIsDragOver( _target, true );
                _isDragOverSignalled = true;
            }
        }

        protected virtual void OnTargetDragLeave(object sender, DragEventArgs e)
        {
            _dragInProgress = false;

            // It appears there's a quirk in the drag/drop system.  While the user is dragging the object
            // over our control it appears the system will send us (quite frequently) DragLeave followed 
            // immediately by DragEnter events.  So when we get DragLeave, we can't be sure that the 
            // drag/drop operation was actually terminated.  Therefore, instead of doing cleanup
            // immediately, we schedule the cleanup to execute later and if during that time we receive
            // another DragEnter or DragOver event, then we don't do the cleanup.
            _target.Dispatcher.BeginInvoke( new Action( ()=> {
                                    if( _dragInProgress == false ) OnRealTargetDragLeave( sender, e ); } ) );
        }

        protected virtual void OnTargetDragOver(object sender, DragEventArgs e)
        {
            _dragInProgress = true;

            OnQueryDragDataValid( sender, e );
        }

        protected virtual void OnTargetDrop(object sender, DragEventArgs e)
        {
            if( TargetDrop != null )
            {
                TargetDrop( this, new DropHelperEventArgs( sender, e ) );
            }

            if( _isDragOverSignalled )
            {
                _isDragOverSignalled = false;
                SetIsDragOver( _target, false );
            }
        }

        protected virtual void OnQueryDragDataValid( object sender, DragEventArgs eventArgs )
        {
            eventArgs.Handled = true;

            if( QueryDragDataValid != null )
            {
                QueryDragDataValid( this, new DropHelperEventArgs( sender, eventArgs ) );
            }
        }

        protected virtual void OnRealTargetDragLeave( object sender, DragEventArgs eventArgs )
        {
            if( RealTargetDragLeave != null )
            {
                RealTargetDragLeave( this, new DropHelperEventArgs( sender, eventArgs ) );
            }

            if( _isDragOverSignalled )
            {
                _isDragOverSignalled = false;
                SetIsDragOver( _target, false );
            }
        }


        private UIElement           _target;
        private bool                _dragInProgress;
        private bool                _isDragOverSignalled;
    }
}
