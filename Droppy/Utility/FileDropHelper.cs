using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Droppy
{
    /// <summary>
    /// Provides data for FileDropHelper's FileDrop event
    /// </summary>
    class FileDropEventArgs : EventArgs
    {
        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="source">Drop target UI element</param>
        /// <param name="isMove">True of the user is moving the file(s). False if the files are 
        /// being copied</param>
        /// <param name="files">List of files being moved or copied</param>
        public FileDropEventArgs( FrameworkElement source, bool isMove, string[] files )
            : base()
        {
            _source = source;
            _fileList = files;
            _isMove = isMove;
        }

        /// <summary>
        /// Gets a list of files which are being moved or copied
        /// </summary>
        public string[] Files { get { return _fileList; } }

        /// <summary>
        /// Gets a flag which indicates if the files are being moved.  If this property is false,
        /// the files are being copied.
        /// </summary>
        public bool IsMove { get { return _isMove; } }

        /// <summary>
        /// Gets drop target UI element
        /// </summary>
        public FrameworkElement Source { get { return _source; } }

        private FrameworkElement    _source;
        private string[]            _fileList;
        private bool                _isMove;
    }


    /// <summary>
    /// Defines a UI behavior which can transform any framework element into a drop target
    /// for files from Windows Explorer
    /// </summary>
    class FileDropHelper : DropHelper
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Initializing constructor
        /// </summary>
        /// <param name="parent">UI element that will act as file drop target</param>
        /// <param name="allowMultiple">Indicates whether or not the drop target will accept
        /// multiple files.</param>
        public FileDropHelper( FrameworkElement parent, bool allowMultiple )
            : base( parent )
        {
            _allowMultipleFiles = allowMultiple;
        }

        /// <summary>
        /// Gets fired whenever one or more files is dropped into the drop target. It is up to the
        /// event listener to act on this event and perform useful work with the list of files which
        /// is reported by this event
        /// </summary>
        public event EventHandler< FileDropEventArgs >  FileDrop;

        #endregion

        #region ----------------------- Protected Members ---------------------

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
                eventData.Effects = IsMove( eventData ) ? DragDropEffects.Move : DragDropEffects.Copy;
            }

            eventData.Handled = true;
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        private bool IsMove( DragEventArgs e )
        {
            return e.KeyStates.HasFlag( DragDropKeyStates.ShiftKey );
        }

        private bool                _allowMultipleFiles;

        #endregion
    }
}

