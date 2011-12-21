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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Droppy
{
    public enum ThumbId : int { Left, Center, Right, NUM_THUMBS };


    public class ResizeBarEventArgs : EventArgs
    {
        public ResizeBarEventArgs( double leftDelta, double widthDelta, double heightDelta, ThumbId thumbId )
        {
            _leftDelta = leftDelta;
            _widthDelta = widthDelta;
            _heightDelta = heightDelta;
            _thumbId = thumbId;
        }

        public double LeftDelta { get { return _leftDelta; } }
        public double WidthDelta { get { return _widthDelta; } }
        public double HeightDelta { get { return _heightDelta; } }
        public ThumbId ThumbId { get { return _thumbId; } }

        private double _leftDelta;
        private double _widthDelta;
        private double _heightDelta;
        private ThumbId _thumbId;
    }


    /// <summary>
    /// </summary>
    public class ResizeBarControl : Control
    {
        static ResizeBarControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata( typeof( ResizeBarControl ), new FrameworkPropertyMetadata( typeof( ResizeBarControl ) ) );
        }

        public ResizeBarControl()
        {
            _thumbControls = new Thumb[ (int)ThumbId.NUM_THUMBS ];
        }

        public event EventHandler< ResizeBarEventArgs > Resize;
        public event EventHandler                       ResizeComplete;


        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            UnsubscribeThumbEvents();

            _thumbControls[ (int)ThumbId.Left ] =   Template.FindName( "PART_LeftThumb", this ) as Thumb;
            _thumbControls[ (int)ThumbId.Center ] = Template.FindName( "PART_CenterThumb", this ) as Thumb;
            _thumbControls[ (int)ThumbId.Right ] =  Template.FindName( "PART_RightThumb", this ) as Thumb;

            SubscribeThumbEvents();
        }

        protected virtual void OnResize( double leftDelta, double widthDelta, double heightDelta )
        {
            if( Resize != null )
            {
                Resize( this, new ResizeBarEventArgs( leftDelta, widthDelta, heightDelta, _currentThumb ) );
            }
        }

        protected virtual void OnResizeComplete()
        {
            if( ResizeComplete != null )
            {
                ResizeComplete( this, new EventArgs() );
            }
        }
        
        private void SubscribeThumbEvents()
        {
            foreach( var t in _thumbControls.Select( (x,i)=>new{ ctrl=x, idx=i} ) )
            {
                if( t.ctrl != null )
                {
                    t.ctrl.DragDelta += OnDragDelta;
                    t.ctrl.DragCompleted += OnDragCompleted;

                    t.ctrl.Tag = (ThumbId)t.idx; // LATER: do we need this?
                }
            }
        }

        private void UnsubscribeThumbEvents()
        {
            foreach( var t in _thumbControls )
            {
                if( t != null )
                {
                    t.DragDelta -= OnDragDelta;
                    t.DragCompleted -= OnDragCompleted;
                }
            }
        }

        private void OnDragDelta( object sender, DragDeltaEventArgs e )
        {
            if( sender != _currentSender )
            {
                if( sender == _thumbControls[ (int)ThumbId.Center ] )
                {
                    _currentThumb = ThumbId.Center;
                    _leftMultiplier = 0.0;
                    _widthMultplier = 0.0;
                    _heightMultplier = 1.0;
                }
                else if( sender == _thumbControls[ (int)ThumbId.Left ] )
                {
                    _currentThumb = ThumbId.Left;
                    _leftMultiplier = 1.0;
                    _widthMultplier = -1.0;
                    _heightMultplier = 1.0;
                }
                else if( sender == _thumbControls[ (int)ThumbId.Right ] )
                {
                    _currentThumb = ThumbId.Right;
                    _leftMultiplier = 0.0;
                    _widthMultplier = 1.0;
                    _heightMultplier = 1.0;
                }
                else
                {
                    return;
                }
            }

            OnResize( e.HorizontalChange * _leftMultiplier,
                      e.HorizontalChange * _widthMultplier,
                      e.VerticalChange   * _heightMultplier );
        }

        private void OnDragCompleted( object sender, DragCompletedEventArgs e )
        {
            _currentSender = null;

            OnResizeComplete();
        }
        

        private object      _currentSender;
        private ThumbId     _currentThumb;
        private Thumb[]     _thumbControls;
        private double      _leftMultiplier;
        private double      _widthMultplier;
        private double      _heightMultplier;
    }
}
