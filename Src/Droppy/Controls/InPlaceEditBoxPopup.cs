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
using System.Windows.Input;
using System.Windows.Media;

namespace Droppy
{
    /// <summary>
    /// In-place edit box control similar to the one in Windows Explorer which appears when a user
    /// is modifying a file. The edit box is intended to appear on top of static text which is being
    /// edited
    /// </summary>
    /// <remarks>
    /// Similarly to System.Windows.Controls.Primitives.Popup, this control can be placed in any container
    /// (preferrably one which supports multiple children such as a Grid).  InPlaceEditBoxPopup itself
    /// remains invisible by reporting 0 width/height for itself.  However using PlacementTarget property
    /// it attaches an edit box popup which will appear whenever IsOpen property is set to 'true'. 
    /// </remarks>
    class InPlaceEditBoxPopup : Control
    {
        #region ----------------------- Public Members ------------------------

        #region - - - - - - - - - - - CornorRadius Dependency Property - - - - - - - -

        /// <summary>
        /// Identifies InPlaceEditBoxPopup.CornerRadius dependency property
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty =
                DependencyProperty.Register( "CornerRadius", typeof( CornerRadius ), typeof( InPlaceEditBoxPopup ),
                                             new FrameworkPropertyMetadata( new CornerRadius( 0 ) ) );

        /// <summary>
        /// Gets/sets corner radius of the popup, in-place edit box
        /// </summary>
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue( CornerRadiusProperty ); }
            set { SetValue( CornerRadiusProperty, value ); }
        }

        #endregion

        #region - - - - - - - - - - - IsOpen Dependency Property - - - - - - - - - - -

        /// <summary>
        /// Identifies InPlaceEditBoxPopup.IsOpen dependency property
        /// </summary>
        public static readonly DependencyProperty IsOpenProperty =
                DependencyProperty.Register( 
                    "IsOpen", typeof( bool ), typeof( InPlaceEditBoxPopup ),
                    new FrameworkPropertyMetadata( false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                                   OnIsOpenPropertyChanged ) );

        /// <summary>
        /// Gets/sets a flag which indicates whether or not in-place edit box is visible
        /// </summary>
        public bool IsOpen
        {
            get { return (bool)GetValue( IsOpenProperty ); }
            set { SetValue( IsOpenProperty, value ); }
        }

        private static void OnIsOpenPropertyChanged( object o, DependencyPropertyChangedEventArgs e )
        {
            ( (InPlaceEditBoxPopup)o ).OnIsOpenPropertyChanged();
        }

        #endregion

        #region - - - - - - - - - - - PlacementTarget Dependency Property  - - - - - -

        /// <summary>
        /// Identifies InPlaceEditBoxPopup.PlacementTarget dependency property
        /// </summary>
        public static readonly DependencyProperty PlacementTargetProperty =
                DependencyProperty.Register( "PlacementTarget", typeof( UIElement ), typeof( InPlaceEditBoxPopup ),
                                             new FrameworkPropertyMetadata( null ) );

        /// <summary>
        /// Gets/sets an element on top of which in-place edit box will appear when IsOpen property is set to 'true'.
        /// </summary>
        public UIElement PlacementTarget
        {
            get { return (UIElement)GetValue( PlacementTargetProperty ); }
            set { SetValue( PlacementTargetProperty, value ); }
        }

        #endregion

        #region - - - - - - - - - - - Text Dependency Property - - - - - - - - - - - -

        /// <summary>
        /// Identifies InPlaceEditBoxPopup.Text dependency property
        /// </summary>
        public static readonly DependencyProperty TextProperty =
                DependencyProperty.Register( "Text", typeof( string ), typeof( InPlaceEditBoxPopup ),
                                             new FrameworkPropertyMetadata( null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault ) );

        /// <summary>
        /// Gets/sets a string which appears in the edit box of the in-place edit popup
        /// </summary>
        public string Text
        {
            get { return (string)GetValue( TextProperty ); }
            set { SetValue( TextProperty, value ); }
        }

        #endregion

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            new ControlInitHelper( this ).Element( "PART_TextBox" ).Get( out _textBox );
        }

        #endregion

        #region ----------------------- Protected Base Overrides --------------

        /// <inheritdoc/>
        protected override void OnKeyDown( System.Windows.Input.KeyEventArgs e )
        {
            if( e.Key == Key.Enter )
            {
                CleanupPopup( true );
            }
            else if( e.Key == Key.Escape )
            {
                CleanupPopup( false );
            }
            else
            {
                base.OnKeyDown( e );
                return;
            }

            e.Handled = true;
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        static InPlaceEditBoxPopup()
        {
            DefaultStyleKeyProperty.OverrideMetadata( typeof( InPlaceEditBoxPopup ),
                                                      new FrameworkPropertyMetadata( typeof( InPlaceEditBoxPopup ) ) );
        }

        private void OnIsOpenPropertyChanged()
        {
            if( IsOpen )
            {
                InitPopup();
            }
            else
            {
                CleanupPopup( true );
            }
        }

        private void InitPopup()
        {
            _textBox.SelectAll();
            Dispatcher.BeginInvoke( new Action( () => _textBox.Focus() ) );
        }

        private void CleanupPopup( bool applyUpdate )
        {
            var bindingExpression = _textBox.GetBindingExpression( TextBox.TextProperty );

            IsOpen = false;

            if( bindingExpression == null ) return;

            if( applyUpdate )
            {
                bindingExpression.UpdateSource();
            }
            else
            {
                bindingExpression.UpdateTarget();
            }
        }

        private TextBox     _textBox;

        #endregion
    }
}
