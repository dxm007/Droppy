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
    /// <summary>
    /// To be implemented by any window that has auto-hiding behavior and wishes to support the ability
    /// to temporarily freeze/unfreeze that behavior.
    /// </summary>
    interface IFreezableAutoHideWindow
    {
        /// <summary>
        /// Invoked to temporarily freeze the auto-hide behavior
        /// </summary>
        void FreezeAutoHide();

        /// <summary>
        /// Undoes the effects of FreezeAutoHide() call. This method must be called same number of times
        /// as the FreezeAutoHide() method
        /// </summary>
        void UnfreezeAutoHide();
    }


    /// <summary>
    /// This control is loaded into a widget site whenever a corresponding site cell does not have any 
    /// widget data behind it.
    /// </summary>
    /// <remarks>
    /// This control presents the UI which allows the user to select the type of widget that should be 
    /// added to its widget site. Upon successful selection, this control modifies underlying widget 
    /// container and that in turn causes this control to be unloaded and a different one to be loaded 
    /// in its place.
    /// </remarks>
    class EmptyWidgetControl : WidgetControl
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Default constructor
        /// </summary>
        public EmptyWidgetControl()
        {
            new FileDropHelper( this, false ).FileDrop += OnDrop;
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
        }

        #endregion

        #region ----------------------- Protected Members ---------------------

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

        #endregion

        #region ----------------------- Private Members -----------------------

        static EmptyWidgetControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata( typeof( EmptyWidgetControl ), new FrameworkPropertyMetadata( typeof( EmptyWidgetControl ) ) );
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

        #endregion
    }

}
