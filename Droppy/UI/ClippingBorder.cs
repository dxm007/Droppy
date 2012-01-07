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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Droppy.UI
{
    /// <summary>
    /// This control isn't being used currently.  I'm leaving it in for the time being, but if there's no need for it in the
    /// near future, it should just be deleted.
    /// </summary>
    public class ClippingBorder : Border
    {
        static ClippingBorder()
        {
            Border.CornerRadiusProperty.OverrideMetadata( 
                    typeof( ClippingBorder ),
                    new FrameworkPropertyMetadata( new CornerRadius( 0.0 ), new PropertyChangedCallback( OnCornerRadiusChanged ) ) );
        }

        protected static void OnCornerRadiusChanged( DependencyObject                      o,
                                                     DependencyPropertyChangedEventArgs    eventArgs )
        {
            ( (ClippingBorder)o ).UpdateClippingRect( (double)eventArgs.NewValue );
        }

        protected override void OnRenderSizeChanged( SizeChangedInfo sizeInfo )
        {
            base.OnRenderSizeChanged( sizeInfo );

            UpdateClippingRect( sizeInfo.NewSize );
        }

        protected override void OnRender( DrawingContext dc )
        {
            base.OnRender( dc );

            Child.Clip = _clippingRect;
        }

        private void UpdateClippingRect( Size renderSize )
        {
            _renderSize = renderSize;

            UpdateClippingRect();
        }

        private void UpdateClippingRect( double cornerRadius )
        {
            _cornerRadius = cornerRadius;

            UpdateClippingRect();
        }

        private void UpdateClippingRect()
        {
            _clippingRect = new RectangleGeometry( 
                    new Rect( _renderSize ), _cornerRadius, _cornerRadius );

            _clippingRect.Freeze();
        }

        private Size                    _renderSize;
        private Double                  _cornerRadius;
        private RectangleGeometry       _clippingRect;
    }
}
