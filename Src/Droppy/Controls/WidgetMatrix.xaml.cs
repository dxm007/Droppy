﻿//=================================================================================================
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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Droppy.Data;


namespace Droppy
{
    /// <summary>
    /// A user control which contains two-dimensional grid of widget sites. Depending on the data
    /// binding of a particular cell, each site presents one of the WidgetControl-derived controls.
    /// </summary>
    /// <remarks>
    /// This control supports data binding through Source property to a Data.WidgetContainerData
    /// object which holds the data presented by the control. Widget container data includes the
    /// size of the matrix to be displayed as well as data elements to be presented in each of
    /// the cells.  If a cell has no data (i.e. null value), EmptyWidgetControl is placed into the
    /// site for that matrix cell.
    /// </remarks>
    partial class WidgetMatrix : UserControl
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Default constructor 
        /// </summary>
        public WidgetMatrix()
        {
            InitializeComponent();

            _controlData = new WidgetMatrixData();

            new WidgetMatrixDropHelper( this, _controlData );
        }

        /// <summary>
        /// Gets/sets a widget container data object which this control is to be bound to. The
        /// data object fully defines what is being presented by this matrix control
        /// </summary>
        public Data.WidgetContainerData Source
        {
            get { return _controlData.Source; }
            set { if( _controlData.Source != value ) UpdateSource( value ); }
        }

        /// <summary>
        /// Returns the number of rows being displayed
        /// </summary>
        public int Rows
        {
            get
            {
                var source = _controlData.Source;
                return source != null ? source.Bounds.RowCount : 0;
            }
        }

        /// <summary>
        /// Returns the number of columns being displayed
        /// </summary>
        public int Columns
        {
            get
            {
                var source = _controlData.Source;
                return source != null ? source.Bounds.ColumnCount : 0;
            }
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        private void UpdateGrid( bool withAnimation )
        {
            TimeSpan animationTime = new TimeSpan();
            List< WidgetSiteControl > sitesToRemove = new List<WidgetSiteControl>( 256 );

            int rowCount = _controlData.Source.Bounds.RowCount;
            int columnCount = _controlData.Source.Bounds.ColumnCount;

            ResizeRowOrColumn( matrixPanel.RowDefinitions, rowCount );
            ResizeRowOrColumn( matrixPanel.ColumnDefinitions, columnCount );

            _controlData.SiteGrid = new Array2D<WidgetSiteControl>( rowCount, columnCount );

            foreach( var child in matrixPanel.Children )
            {
                WidgetSiteControl site = child as WidgetSiteControl;

                if( site == null ) continue;

                if( !_controlData.Source.Bounds.Contains( site.Location ) )
                {
                    if( withAnimation )
                    {
                        MatrixSize siteDistance = _controlData.Source.Bounds.Distance( site.Location );

                        TranslateTransform transform = (TranslateTransform)( (TransformGroup)site.RenderTransform ).Children[1];

                        transform.Y = site.HeightWithMargin * siteDistance.RowCount;
                        transform.X = site.WidthWithMargin * siteDistance.ColumnCount;

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
                    MatrixLoc arrayIndex = _controlData.Source.Bounds.ToIndex( site.Location );

                    _controlData.SiteGrid[ arrayIndex ] = site;

                    site.UpdateGridPosition();
                }
            }

            foreach( var site in sitesToRemove )
            {
                matrixPanel.Children.Remove( site );
            }

            foreach( MatrixLoc loc in _controlData.Source.Bounds )
            {
                MatrixLoc arrayIndex = _controlData.Source.Bounds.ToIndex( loc );

                if( _controlData.SiteGrid[ arrayIndex ] == null )
                {
                    var site = CreateWidgetSite( loc, withAnimation, ref animationTime );

                    _controlData.SiteGrid[ arrayIndex ] = site;
                }

                _controlData.SiteGrid[ arrayIndex ].Content = _controlData.Source[ loc ];
            }
        }

        private WidgetSiteControl CreateWidgetSite( MatrixLoc location, bool withAnimation, ref TimeSpan animationTime )
        {
            var site = new WidgetSiteControl();
            var transform = new TransformGroup();

            transform.Children.Add( withAnimation ? new ScaleTransform( 0, 0 ) : new ScaleTransform( 1, 1 ) );
            transform.Children.Add( new TranslateTransform() );
            
            site.RenderTransform = transform;
            site.RenderTransformOrigin = new Point( 0.5, 0.5 );

            if( withAnimation )
            {
                AnimateSiteScaleTransform( site, ref animationTime, 0, 1, null );
            }

            matrixPanel.Children.Add( site );

            site.Location = location;

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
                    DefinitionBase def = new T();

                    if( def is ColumnDefinition )
                    {
                        ( (ColumnDefinition)def ).Width = new GridLength( 0, GridUnitType.Auto );
                    }
                    else
                    {
                        ( (RowDefinition)def ).Height = new GridLength( 0, GridUnitType.Auto );
                    }

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
            _controlData.SiteGrid[ e.Location ].Content = e.NewWidget;
        }

        private void OnWidgetContainerResized( object sender, EventArgs e )
        {
            UpdateGrid( true );
        }


        private WidgetMatrixData    _controlData;

        #endregion
    }


    /// <summary>
    /// Data object for WidgetMatrix control. This class is used to preserve hierarchical dependency
    /// relationship between WidgetMatrix and the behavior classes it internally instantiates
    /// </summary>
    class WidgetMatrixData
    {
        public int Rows { get; set; }
        public int Columns { get; set; }
        public Data.WidgetContainerData Source { get; set; }
        public Array2D<WidgetSiteControl> SiteGrid { get; set; }
    }


    /// <summary>
    /// Extends DropHelper to provide specific drag-drop behavior for the WidgetMatrix control
    /// </summary>
    /// <remarks>
    /// The custom behavior this class adds is to monitor the location of the mouse cursor and apply 
    /// TranslateTransform to other elements in the WidgetMatrix so that they appear to move out of the
    /// way of the widget that is being dropped.
    /// </remarks>
    class WidgetMatrixDropHelper : DropHelper
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="parent">Parent widget matrix control which is to support drop operations</param>
        /// <param name="data">Widget matrix control data</param>
        public WidgetMatrixDropHelper( WidgetMatrix parent, WidgetMatrixData data ) : base( parent )
        {
            _parent = parent;
            _controlData = data;
            _prevRelocatedSites = new List<SiteShiftInfo>();
        }

        #endregion

        #region ----------------------- Protected Base Overrides --------------

        /// <inheritdoc/>
        protected override void OnQueryDragDataValid( object sender, DragEventArgs e )
        {
            base.OnQueryDragDataValid( sender, e );

            if( ProcessDataObject( e, UpdateMatrixUI ) )
            {
                e.Handled = true;
            }
        }

        /// <inheritdoc/>
        protected override void OnTargetDrop( object sender, DragEventArgs e )
        {
            base.OnTargetDrop( sender, e );

            ProcessDataObject( e, CommitMove );
        }

        /// <inheritdoc/>
        protected override void OnRealTargetDragLeave( object sender, DragEventArgs e )
        {
            base.OnRealTargetDragLeave( sender, e );

            CancelMove();
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        class SiteShiftInfo
        {
            public WidgetSiteControl    site;
            public Data.WidgetData      widget;
            public double               translateX;
            public double               translateY;
            public MatrixLoc            newLocation;
        }

        class SiteShiftInfoComparer : IEqualityComparer<SiteShiftInfo>
        {
            public bool Equals( SiteShiftInfo x, SiteShiftInfo y )
            {
                return x.site == y.site;
            }

            public int GetHashCode( SiteShiftInfo obj )
            {
                return obj.site.GetHashCode();
            }

            public static IEqualityComparer<SiteShiftInfo> Comparer { get { return _comparer; } }

            private static SiteShiftInfoComparer _comparer = new SiteShiftInfoComparer();
        }


        private delegate void ProcessDataObjectDelegate( MatrixLoc insertLoc, WidgetSiteDragDropData data );

        private bool ProcessDataObject( DragEventArgs eventArgs, ProcessDataObjectDelegate callback )
        {
            WidgetSiteDragDropData data = eventArgs.Data.GetData( "Droppy.WidgetSiteDragDropData" ) as WidgetSiteDragDropData;

            if( data == null )
            {
                eventArgs.Effects = DragDropEffects.None;

                return false;
            }

            Point   pt = eventArgs.GetPosition( this.Target ) - 
                                    new Vector( data.DraggableOffset.X, data.DraggableOffset.Y );
            Rect    dragRect =  new Rect( pt, new Size( data.Site.ActualWidth + data.Site.Margin.Width(),
                                                        data.Site.ActualHeight + data.Site.Margin.Height() ) );
            Point   dragCenter = new Point( dragRect.X + dragRect.Width / 2,
                                            dragRect.Y + dragRect.Height / 2 );

            MatrixLoc insertIndex = new MatrixLoc(
                        (int)( ( dragCenter.Y + dragRect.Height ) / dragRect.Height ) - 1,
                        (int)( ( dragCenter.X + dragRect.Width  ) / dragRect.Width  ) - 1  );

            MatrixLoc insertLoc = _controlData.Source.Bounds.ToLocation( insertIndex );

            if( _controlData.Source.Bounds.Contains( insertLoc ) )
            {
                eventArgs.Effects = DragDropEffects.Move;

                callback( insertLoc, data );
            }
            else
            {
                eventArgs.Effects = DragDropEffects.None;
            }

            return true;
        }

        private void UpdateMatrixUI( MatrixLoc insertLoc, WidgetSiteDragDropData data )
        {
            List< SiteShiftInfo >       relocatedSites;

            relocatedSites = CalculateWhichSitesToShift( insertLoc, data );

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

        private void CommitMove( MatrixLoc insertLoc, WidgetSiteDragDropData data )
        {
            List< SiteShiftInfo >   relocatedSites;

            relocatedSites = CalculateWhichSitesToShift( insertLoc, data );

            if( relocatedSites.Count > 0 )
            {
                foreach( var s in relocatedSites )
                {
                    _controlData.Source[ s.newLocation ] = s.widget;

                    AnimateTranslate( s.site, TranslateTransform.XProperty, null );
                    AnimateTranslate( s.site, TranslateTransform.YProperty, null );
                }

                _controlData.Source[ insertLoc ] = data.Widget;
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

        private List< SiteShiftInfo > CalculateWhichSitesToShift( MatrixLoc insertLoc, WidgetSiteDragDropData data )
        {
            MatrixLoc                   sourceLoc = data.Site.Location;
            List< SiteShiftInfo >       relocatedSites;
            int                         i, step;
            double                      translateBy;

            // preallocate enough so we don't need to worry about reallocations
            relocatedSites = new List<SiteShiftInfo>( _controlData.Source.Bounds.RowCount +
                                                      _controlData.Source.Bounds.ColumnCount + 10 );

            if( insertLoc.Column != sourceLoc.Column )
            {
                translateBy = data.Site.ActualWidth + data.Site.Margin.Width();

                if( insertLoc.Column < sourceLoc.Column )
                {
                    step = 1;
                }
                else
                {
                    step = -1;
                    translateBy = -translateBy;
                }

                for( i = insertLoc.Column; i != sourceLoc.Column; i += step )
                {
                    MatrixLoc arrayIndex = _controlData.Source.Bounds.ToIndex( new MatrixLoc( sourceLoc.Row, i ) );

                    WidgetSiteControl ctrl = _controlData.SiteGrid[ arrayIndex ];

                    relocatedSites.Add( new SiteShiftInfo() { site = ctrl, widget = (Data.WidgetData)ctrl.Content,
                                                              translateX = translateBy, translateY = 0.0,
                                                              newLocation = new MatrixLoc( sourceLoc.Row, i + step ) } );
                }
            }

            if( insertLoc.Row != sourceLoc.Row )
            {
                translateBy = data.Site.ActualHeight + data.Site.Margin.Height();

                if( insertLoc.Row < sourceLoc.Row )
                {
                    step = 1;
                }
                else
                {
                    step = -1;
                    translateBy = -translateBy;
                }

                for( i = insertLoc.Row; i != sourceLoc.Row; i += step )
                {
                    MatrixLoc arrayIndex = _controlData.Source.Bounds.ToIndex( new MatrixLoc( i, insertLoc.Column ) );

                    WidgetSiteControl ctrl = _controlData.SiteGrid[ arrayIndex ];

                    relocatedSites.Add( new SiteShiftInfo() { site = ctrl, widget = (Data.WidgetData)ctrl.Content,
                                                              translateY = translateBy, translateX = 0.0,
                                                              newLocation = new MatrixLoc( i + step, insertLoc.Column ) } );
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

        #endregion
    }

}
