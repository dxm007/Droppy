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
        }

        public int Rows
        {
            get { return _controlData.Rows; }
        }

        public Data.WidgetContainerData Source
        {
            get { return _controlData.Source; }
            set { if( _controlData.Source != value ) UpdateSource( value ); }
        }

        public void UpdateGrid( bool withAnimation )
        {
            int firstRow, lastRow, firstColumn, lastColumn, rowCount, columnCount;
            TimeSpan animationTime = new TimeSpan();
            int r, c;
            List< WidgetSiteControl > sitesToRemove = new List<WidgetSiteControl>( 256 );

            firstRow = _controlData.Source.FirstRow;
            rowCount = _controlData.Source.RowCount;
            lastRow = firstRow + rowCount;
            firstColumn = _controlData.Source.FirstColumn;
            columnCount = _controlData.Source.ColumnCount;
            lastColumn = firstColumn + columnCount;

            ResizeRowOrColumn( matrixPanel.RowDefinitions, rowCount );
            ResizeRowOrColumn( matrixPanel.ColumnDefinitions, columnCount );

            _controlData.Rows = rowCount;
            _controlData.Columns = columnCount;

            _controlData.SiteGrid = new WidgetSiteControl[ rowCount, columnCount ];

            foreach( var child in matrixPanel.Children )
            {
                WidgetSiteControl site = child as WidgetSiteControl;

                if( site == null ) continue;

                if( site.ContainerRow >= lastRow || site.ContainerRow < firstRow ||
                    site.ContainerColumn >= lastColumn || site.ContainerColumn < firstColumn )
                {
                    if( withAnimation )
                    {
                        TranslateTransform  transform = (TranslateTransform)( (TransformGroup)site.RenderTransform ).Children[1];

                        transform.Y = ( site.ActualHeight + site.Margin.Height() ) *
                                      ( site.ContainerRow >= lastRow ? ( site.ContainerRow - lastRow + 1 ) :
                                        site.ContainerRow < firstRow ? ( site.ContainerRow - firstRow )    : 0.0 );
                        transform.X = ( site.ActualWidth + site.Margin.Width() ) *
                                      ( site.ContainerColumn >= lastColumn ? ( site.ContainerColumn - lastColumn + 1 ) :
                                        site.ContainerColumn < firstColumn ? ( site.ContainerColumn - firstColumn )    : 0.0 );

                        AnimateSiteScaleTransform( site, ref animationTime, 1, 0,
                                                   new EventHandler( (o,e) => {
                                                       matrixPanel.Children.Remove( site );
                                                   } ) );
                    }
                    else
                    {
                        sitesToRemove.Add( site );
                    }
                }
                else
                {
                    _controlData.SiteGrid[ site.ContainerRow - firstRow,
                                           site.ContainerColumn - firstColumn ] = site;
                }
            }

            foreach( var site in sitesToRemove )
            {
                matrixPanel.Children.Remove( site );
            }

            for( r = 0; r < rowCount; r++ )
            {
                for( c = 0; c < columnCount; c++ )
                {
                    if( _controlData.SiteGrid[ r, c ] == null )
                    {
                        var site = InitWidgetSite( r, c, withAnimation, ref animationTime );
                    }

                    _controlData.SiteGrid[ r, c ].Content = _controlData.Source[ r + firstRow, c + firstColumn ];
                }
            }
        }

        private WidgetSiteControl InitWidgetSite( int row, int column, bool withAnimation, ref TimeSpan animationTime )
        {
            var site = new WidgetSiteControl();
            var transform = new TransformGroup();

            transform.Children.Add( withAnimation ? new ScaleTransform( 0, 0 ) : new ScaleTransform( 1, 1 ) );
            transform.Children.Add( new TranslateTransform() );

            site.SetValue( Grid.RowProperty, row );
            site.SetValue( Grid.ColumnProperty, column );
            
            site.RenderTransform = transform;
            site.RenderTransformOrigin = new Point( 0.5, 0.5 );

            if( withAnimation )
            {
                AnimateSiteScaleTransform( site, ref animationTime, 0, 1, null );
            }

            matrixPanel.Children.Add( site );

            _controlData.SiteGrid[ row, column ] = site;

            return site;
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
                _controlData.Source.ContainerResized -= OnWidgetContainerResized;
            }

            _controlData.Source = source;

            if( _controlData.Source != null )
            {
                _controlData.Source.ContainerChanged += OnWidgetContainerChanged;
                _controlData.Source.ContainerResized += OnWidgetContainerResized;
            }

            UpdateGrid( false );
        }

        private void AnimateSiteScaleTransform( WidgetSiteControl site, ref TimeSpan beginTime, 
                                                double fromScale, double toScale, EventHandler completedCallback )
        {
            ScaleTransform transform = (ScaleTransform)( (TransformGroup)site.RenderTransform ).Children[0];

            if( fromScale == double.NaN )
            {
                transform.BeginAnimation( ScaleTransform.ScaleXProperty, null );
                transform.BeginAnimation( ScaleTransform.ScaleYProperty, null );
            }
            else
            {
                AnimationTimeline animation = BuildSiteScaleAnimation( beginTime, fromScale, toScale );

                if( completedCallback != null )
                {
                    animation.Completed += completedCallback;
                }

                transform.BeginAnimation( ScaleTransform.ScaleXProperty, animation );
                transform.BeginAnimation( ScaleTransform.ScaleYProperty,
                                          BuildSiteScaleAnimation( beginTime, fromScale, toScale ) );

                //beginTime += new TimeSpan( 500000 );
            }            
        }

        private AnimationTimeline BuildSiteScaleAnimation( TimeSpan beginTime, double fromScale, double toScale )
        {
            var animation = new DoubleAnimation();

            animation.BeginTime = beginTime;
            animation.From = fromScale;
            animation.To = toScale;
            animation.Duration = new TimeSpan( 1500000 );
            
            return animation;
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

        private void OnWidgetContainerResized( object sender, EventArgs e )
        {
            UpdateGrid( true );
        }


        private WidgetMatrixData    _controlData;
    }




    class WidgetMatrixDropHelper : DropHelper
    {
        public WidgetMatrixDropHelper( WidgetMatrix parent, WidgetMatrixData data ) : base( parent )
        {
            _parent = parent;
            _controlData = data;
            _prevRelocatedSites = new List<SiteShiftInfo>();
        }


        protected override void OnQueryDragDataValid( object sender, DragEventArgs e )
        {
            base.OnQueryDragDataValid( sender, e );

            ProcessDataObject( e, UpdateMatrixUI );
        }

        protected override void OnTargetDrop(object sender, DragEventArgs e)
        {
            base.OnTargetDrop( sender, e );

            ProcessDataObject( e, CommitMove );
        }

        protected override void OnRealTargetDragLeave(object sender, DragEventArgs e)
        {
            CancelMove();
        }


        private delegate void ProcessDataObjectDelegate( int insertRow, int insertCol, WidgetSiteDragDropData data );

        private void ProcessDataObject( DragEventArgs eventArgs, ProcessDataObjectDelegate callback )
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

                    callback( insertRow + _controlData.Source.FirstRow, insertColumn + _controlData.Source.FirstColumn, data );
                }
            }
        }



        class SiteShiftInfo
        {
            public WidgetSiteControl    site;
            public Data.WidgetData      widget;
            public double               translateX;
            public double               translateY;
            public int                  newRow;
            public int                  newColumn;
        }

        class SiteShiftInfoComparer : IEqualityComparer< SiteShiftInfo >
        {
            public bool Equals( SiteShiftInfo x, SiteShiftInfo y )
            {
                return x.site == y.site;
            }

            public int GetHashCode( SiteShiftInfo obj )
            {
                return obj.site.GetHashCode();
            }

            public static IEqualityComparer< SiteShiftInfo > Comparer { get { return _comparer; } }

            private static SiteShiftInfoComparer _comparer = new SiteShiftInfoComparer();
        }


        private void UpdateMatrixUI( int insertRow, int insertCol, WidgetSiteDragDropData data )
        {
            List< SiteShiftInfo >       relocatedSites;

            relocatedSites = GetShiftedSiteList( insertRow, insertCol, data );

            var sitesToMove = relocatedSites.Except( _prevRelocatedSites, SiteShiftInfoComparer.Comparer ).ToList();
            var sitesToReverse = _prevRelocatedSites.Except( relocatedSites, SiteShiftInfoComparer.Comparer ).ToList();

            foreach( var s in sitesToMove )
            {
                AnimateSiteTranslate( s.site, s.translateX, s.translateY );
            }

            foreach( var s in sitesToReverse )
            {
                AnimateSiteTranslate( s.site, 0.0, 0.0 );
            }

            _prevRelocatedSites = relocatedSites;
        }

        private void CommitMove( int insertRow, int insertCol, WidgetSiteDragDropData data )
        {
            List< SiteShiftInfo >   relocatedSites;

            relocatedSites = GetShiftedSiteList( insertRow, insertCol, data );

            if( relocatedSites.Count > 0 )
            {
                foreach( var s in relocatedSites )
                {
                    _controlData.Source[ s.newRow, s.newColumn ] = s.widget;

                    AnimateTranslate( s.site, TranslateTransform.XProperty, null );
                    AnimateTranslate( s.site, TranslateTransform.YProperty, null );
                }

                _controlData.Source[ insertRow, insertCol ] = data.Widget;
            }

            // If there are any controls left out of original position, return them now.
            _prevRelocatedSites = _prevRelocatedSites.Except( relocatedSites, SiteShiftInfoComparer.Comparer ).ToList();

            CancelMove();
        }

        private void CancelMove()
        {
            foreach( var s in _prevRelocatedSites )
            {
                AnimateSiteTranslate( s.site, 0, 0 );
            }

            _prevRelocatedSites.Clear();
        }

        private List< SiteShiftInfo > GetShiftedSiteList( int insertRow, int insertCol, WidgetSiteDragDropData data )
        {
            int                         sourceRow = data.Site.ContainerRow;
            int                         sourceCol = data.Site.ContainerColumn;
            int                         firstCol = _controlData.Source.FirstColumn;
            int                         firstRow = _controlData.Source.FirstRow;
            List< SiteShiftInfo >       relocatedSites;
            int                         i, step;
            double                      translateBy;

            // preallocate enough so we don't need to worry about reallocations
            relocatedSites = new List<SiteShiftInfo>( _controlData.Rows + _controlData.Columns + 10 );

            if( insertCol != sourceCol )
            {
                translateBy = data.Site.ActualWidth + data.Site.Margin.Width();

                if( insertCol < sourceCol )
                {
                    step = 1;
                }
                else
                {
                    step = -1;
                    translateBy = -translateBy;
                }

                for( i = insertCol; i != sourceCol; i += step )
                {
                    WidgetSiteControl ctrl = _controlData.SiteGrid[ sourceRow - firstRow, i - firstCol ];

                    relocatedSites.Add( new SiteShiftInfo() { site = ctrl, widget = (Data.WidgetData)ctrl.Content,
                                                              translateX = translateBy, translateY = 0.0,
                                                              newColumn = i + step, newRow = sourceRow             } );
                }
            }

            if( insertRow != sourceRow )
            {
                translateBy = data.Site.ActualHeight + data.Site.Margin.Height();

                if( insertRow < sourceRow )
                {
                    step = 1;
                }
                else
                {
                    step = -1;
                    translateBy = -translateBy;
                }

                for( i = insertRow; i != sourceRow; i += step )
                {
                    WidgetSiteControl ctrl = _controlData.SiteGrid[ i - firstRow, insertCol - firstCol ];

                    relocatedSites.Add( new SiteShiftInfo() { site = ctrl, widget = (Data.WidgetData)ctrl.Content,
                                                              translateY = translateBy, translateX = 0.0,
                                                              newColumn = insertCol, newRow = i + step             } );
                }
            }

            return relocatedSites;
        }

        private void AnimateSiteTranslate( WidgetSiteControl site, double toX, double toY )
        {
            AnimateTranslate( site, TranslateTransform.XProperty, BuildAnimation( toX ) );
            AnimateTranslate( site, TranslateTransform.YProperty, BuildAnimation( toY ) );
        }

        private void AnimateTranslate( WidgetSiteControl site, DependencyProperty property, AnimationTimeline animation )
        {
            ( (TransformGroup)site.RenderTransform ).Children[1].BeginAnimation( property, animation );
        }

        private AnimationTimeline BuildAnimation( double distance )
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


        private WidgetMatrix                    _parent;
        private WidgetMatrixData                _controlData;
        private List< SiteShiftInfo >           _prevRelocatedSites;
    }

}
