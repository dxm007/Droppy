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
    class InPlaceEditBoxPopup : Control
    {
        static InPlaceEditBoxPopup()
        {
            DefaultStyleKeyProperty.OverrideMetadata( typeof( InPlaceEditBoxPopup ),
                                                      new FrameworkPropertyMetadata( typeof( InPlaceEditBoxPopup ) ) );
        }

        public InPlaceEditBoxPopup()
        {
        }

        #region - - - - - - - - - - - CornorRadius Dependency Property - - - - - - - -

        public static readonly DependencyProperty CornerRadiusProperty =
                DependencyProperty.Register( "CornerRadius", typeof( CornerRadius ), typeof( InPlaceEditBoxPopup ),
                                             new FrameworkPropertyMetadata( new CornerRadius( 0 ) ) );

        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue( CornerRadiusProperty ); }
            set { SetValue( CornerRadiusProperty, value ); }
        }

        #endregion

        #region - - - - - - - - - - - IsOpen Dependency Property - - - - - - - - - - -

        public static readonly DependencyProperty IsOpenProperty =
                DependencyProperty.Register( 
                    "IsOpen", typeof( bool ), typeof( InPlaceEditBoxPopup ),
                    new FrameworkPropertyMetadata( false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                                                   OnIsOpenPropertyChanged ) );

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

        public static readonly DependencyProperty PlacementTargetProperty =
                DependencyProperty.Register( "PlacementTarget", typeof( UIElement ), typeof( InPlaceEditBoxPopup ),
                                             new FrameworkPropertyMetadata( null ) );

        public UIElement PlacementTarget
        {
            get { return (UIElement)GetValue( PlacementTargetProperty ); }
            set { SetValue( PlacementTargetProperty, value ); }
        }

        #endregion

        #region - - - - - - - - - - - Text Dependency Property - - - - - - - - - - - -

        public static readonly DependencyProperty TextProperty =
                DependencyProperty.Register( "Text", typeof( string ), typeof( InPlaceEditBoxPopup ),
                                             new FrameworkPropertyMetadata( null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault ) );

        public string Text
        {
            get { return (string)GetValue( TextProperty ); }
            set { SetValue( TextProperty, value ); }
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            new ControlInitHelper( this ).Element( "PART_TextBox" ).Get( out _textBox );
        }

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
    }
}
