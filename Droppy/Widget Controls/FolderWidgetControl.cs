using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Droppy
{
    /// <summary>
    /// </summary>
    public class FolderWidgetControl : WidgetControl
    {
        static FolderWidgetControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata( typeof( FolderWidgetControl ), new FrameworkPropertyMetadata( typeof( FolderWidgetControl ) ) );
        }

        public FolderWidgetControl()
        {
            new FileDropHelper( this, true ).FileDrop += OnFileDrop;

            ContextMenuOpening += OnContextMenuOpening;
        }

        #region - - - - - - - - IsPasteEnabled (read-only) Dependency Property - - - - - - - - - -

        private static readonly DependencyPropertyKey IsPasteEnabledPropertyKey =
                DependencyProperty.RegisterReadOnly( "IsPasteEnabled", typeof( bool ), typeof( FolderWidgetControl ),
                                                     new FrameworkPropertyMetadata( false )                           );
        public static readonly DependencyProperty IsPasteEnabledProperty = IsPasteEnabledPropertyKey.DependencyProperty;

        public bool IsPasteEnabled
        {
            get { return (bool)GetValue( IsPasteEnabledPropertyKey.DependencyProperty ); }
            private set { SetValue( IsPasteEnabledPropertyKey, value ); }
        }

        #endregion

        #region - - - - - - - - IsLabelEditPopupOpen Dependency Property - - - - - - - - - - - - -

        public static readonly DependencyProperty IsLabelEditPopupOpenProperty =
                DependencyProperty.Register( "IsLabelEditPopupOpen", typeof( bool ), typeof( FolderWidgetControl ),
                                             new FrameworkPropertyMetadata( OnIsLabelEditPopupOpenChanged ) );

        public bool IsLabelEditPopupOpen
        {
            get { return (bool)GetValue( IsLabelEditPopupOpenProperty ); }
            set { SetValue( IsLabelEditPopupOpenProperty, value ); }
        }

        public static void OnIsLabelEditPopupOpenChanged( DependencyObject o,
                                                          DependencyPropertyChangedEventArgs e )
        {
            ( (FolderWidgetControl)o ).OnIsLabelEditPopupChanged();
        }

        #endregion

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            new ControlInitHelper( this ).
                Element( "PART_Paste" ).
                    Add( MenuItem.ClickEvent, new RoutedEventHandler( OnPaste ) ).
                Element( "PART_ChangeLabel" ).
                    Add( MenuItem.ClickEvent, new RoutedEventHandler( 
                                              ( o, e ) => IsLabelEditPopupOpen = true ) );
        }

        protected override void OnClick( object sender, RoutedEventArgs e )
        {
            Data.FolderWidgetData data = DataContext as Data.FolderWidgetData;

            if( data != null )
            {
                Process.Start( "explorer.exe", "\"" + data.Path + "\"" );
            }
        }

        private void OnFileDrop( object sender, FileDropEventArgs e )
        {
            ProcessFileDrop( e.Files, e.IsMove );
        }

        private void OnContextMenuOpening( object sender, ContextMenuEventArgs e )
        {
            IsPasteEnabled = Clipboard.ContainsFileDropList();
        }

        private void OnPaste( object sender, RoutedEventArgs e )
        {
            var files = Clipboard.GetData( "FileDrop" ) as string[];
            var dropEffectStream = Clipboard.GetData( "Preferred DropEffect" ) as System.IO.MemoryStream;

            if( files == null || dropEffectStream == null ||
                files.Length == 0 || dropEffectStream.Length != 4 ) return;

            var dropEffectReader = new System.IO.BinaryReader( dropEffectStream );
            var dropEffect =  (DragDropEffects)dropEffectReader.ReadInt32();

            ProcessFileDrop( files, dropEffect.HasFlag( DragDropEffects.Move ) );
        }

        private void ProcessFileDrop( string[] filePaths, bool isMove )
        {
            IFileOperation          fileOp = new FileOperationG1();
            Data.FolderWidgetData   data = DataContext as Data.FolderWidgetData;

            if( data == null ) return;

            fileOp.ParentWindow = Window.GetWindow( this );
            fileOp.Operation = isMove ? FILEOP_CODES.FO_MOVE :
                                        FILEOP_CODES.FO_COPY;
            fileOp.From = filePaths;
            fileOp.To = new string[ 1 ] { data.Path };
            fileOp.Flags = FILEOP_FLAGS.FOF_ALLOWUNDO;

            fileOp.Execute();
        }

        private void OnIsLabelEditPopupChanged()
        {
            RaiseEvent( new RoutedEventArgs( IsLabelEditPopupOpen ? WidgetSiteControl.UndraggableEvent :
                                                                    WidgetSiteControl.DraggableEvent     ) );
        }
    }
}
