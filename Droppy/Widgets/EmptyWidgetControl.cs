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
using System.IO;
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
    interface IFreezableAutoHideWindow
    {
        void FreezeAutoHide();

        void UnfreezeAutoHide();
    }


    /// <summary>
    /// </summary>
    class EmptyWidgetControl : WidgetControl
    {
        static EmptyWidgetControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata( typeof( EmptyWidgetControl ), new FrameworkPropertyMetadata( typeof( EmptyWidgetControl ) ) );
        }

        public EmptyWidgetControl()
        {
            new FileDropHelper( this, false ).FileDrop += OnDrop;
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        /// <inheritdoc/>
        protected override void OnClick( object sender, RoutedEventArgs e )
        {
            var     parentWindow = Window.GetWindow( this );
            var     autoHideControl = parentWindow as IFreezableAutoHideWindow;

            var     dlg = new System.Windows.Forms.FolderBrowserDialog();

            dlg.Description = "Select a folder";
            dlg.ShowNewFolderButton = true;

            if( autoHideControl != null ) autoHideControl.FreezeAutoHide();

            try
            {
                if( dlg.ShowDialog( new Win32Window( parentWindow ) ) !=
                                            System.Windows.Forms.DialogResult.OK ) return;

                Site.SetWidget( new Data.FolderWidgetData() { Path = dlg.SelectedPath } );
            }
            finally
            {
                if( autoHideControl != null ) autoHideControl.UnfreezeAutoHide();
            }
        }

        private void OnDrop( object sender, FileDropEventArgs e )
        {
            var fileInfo = new FileInfo( e.Files[0] );

            if( fileInfo == null ) return;

            if( fileInfo.Attributes.HasFlag( FileAttributes.Directory ) )
            {
                Site.SetWidget( new Data.FolderWidgetData() { Path = e.Files[0] } );
            }
        }
    }








    class FileDropEventArgs : EventArgs
    {
        public FileDropEventArgs( FrameworkElement source, bool isMove, string[] files ) : base()
        {
            _source = source;
            _fileList = files;
            _isMove = isMove;
        }

        public string[] Files { get { return _fileList; } }

        public bool IsMove { get { return _isMove; } }

        public FrameworkElement Source { get { return _source; } }

        private FrameworkElement    _source;
        private string[]            _fileList;
        private bool                _isMove;
    }

    class FileDropHelper : DropHelper
    {
        public FileDropHelper( FrameworkElement parent, bool allowMultiple )
                    : base( parent )
        {
            _allowMultipleFiles = allowMultiple;
        }

        public event EventHandler< FileDropEventArgs >  FileDrop;

        /// <inheritdoc/>
        protected override void OnTargetDrop( object sender, DragEventArgs e )
        {
            string[] files = e.Data.GetData( "FileDrop" ) as string[];

            if( files == null ) return;

            if( FileDrop != null & files.Length > 0 )
            {
                FileDrop( this, new FileDropEventArgs( (FrameworkElement)Target, IsMove( e ), files ) );
            }

            base.OnTargetDrop( sender, e );
        }

        /// <inheritdoc/>
        protected override void OnQueryDragDataValid( object sender, DragEventArgs eventData )
        {
            string[] files = eventData.Data.GetData( "FileDrop" ) as string[];

            eventData.Effects = DragDropEffects.None;

            if( files == null ) return;

            if( _allowMultipleFiles || files.Length == 1 )
            {
                eventData.Effects = IsMove( eventData ) ? DragDropEffects.Move : DragDropEffects.Copy ;
            }

            eventData.Handled = true;
        }

        private bool IsMove( DragEventArgs e )
        {
            return e.KeyStates.HasFlag( DragDropKeyStates.ShiftKey );
        }


        private bool                _allowMultipleFiles;
    }
}
