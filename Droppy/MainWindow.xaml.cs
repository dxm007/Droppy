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
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using Droppy.Data;


namespace Droppy
{

    /// <summary>
    /// Main window of the application
    /// </summary>
    partial class MainWindow : Window,
                               IFreezableAutoHideWindow
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Default Constructor
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            _windowData = new MainWindowData();

            _windowData.Document = new Data.WidgetDocument();
            _windowData.Document.IsDirtyChanged += OnDocumentDirtyFlagChanged;
            _windowData.Document.Load();

            widgetContainer.Source = _windowData.Document.Root;

            this.Loaded += new RoutedEventHandler(OnLoaded);
            this.Closing += OnClosing;
        }

        /// <summary>
        /// Opens the windows and returns immediately. Because this window class has auto-hide
        /// functionality, this method overrides Window.Show() so that extra actions would be
        /// taken to ensure that the window becomes visible.
        /// </summary>
        public new void Show()
        {
            base.Show();
            _windowAutoHider.ShowWindow();
        }

        #region - - - - - - - IFreezableAutoHideWindow Interface  - - - - - - -

        public void FreezeAutoHide()
        {
            _windowAutoHider.Freeze();
        }

        public void UnfreezeAutoHide()
        {
            _windowAutoHider.Unfreeze();
        }

        #endregion
        #endregion

        #region ----------------------- Private Members -----------------------

        private void OnClosing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            if( _windowData.Document.IsDirty )
            {
                SaveDocument();
            }
        }

        private void OnLoaded( object sender, RoutedEventArgs e )
        {
            new TrashCanPopupManager( this );

            IWindowMover windowMover = new WindowMover( this );

            _windowResizer = new MainWindowResizer( this );

            IWindowDocker docker = new WindowDocker( this, windowMover );

            _windowAutoHider = new WindowAutoHider( this );

            new WindowDockMediator( this, _windowAutoHider, docker );
        }

        private void OnToolsBtnClick( object sender, RoutedEventArgs e )
        {
            ToolsMenu.PlacementTarget = ToolsHdrButton;
            ToolsMenu.IsOpen = true;
        }

        private void OnCloseBtnClick( object sender, RoutedEventArgs e )
        {
            Close();
        }

        private void OnExportMenuItemClick( object sender, RoutedEventArgs e )
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            dlg.Title = "Export Configuration";
            
            AssignCommonFileDialogProps( dlg );

            if( dlg.ShowDialog() != true ) return;

            try
            {
                _windowData.Document.Save( dlg.OpenFile(), WidgetDocSaveFormat.Xml );
            }
            catch( Exception ex )
            {
                ReportToUserApplicationError( ex, "There were issues exporting configuration." );
            }
        }

        private void OnImportMenuItemClick( object sender, RoutedEventArgs e )
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.Title = "Import Configuration";

            AssignCommonFileDialogProps( dlg );

            if( dlg.ShowDialog() != true ) return;

            try
            {
                _windowData.Document.Load( dlg.OpenFile(), WidgetDocSaveFormat.Xml );

                widgetContainer.Source = _windowData.Document.Root;

                _windowResizer.SizeParentToContent();
            }
            catch( Exception ex )
            {
                ReportToUserApplicationError( ex, "There were issues importing configuration." );
            }
        }

        private static void AssignCommonFileDialogProps( Microsoft.Win32.FileDialog dlg )
        {
            dlg.DefaultExt = "*.droppy";
            dlg.Filter = "Droppy Configuration Files (*.droppy)|*.droppy";
        }

        private void OnDocumentDirtyFlagChanged( object sender, EventArgs e )
        {
            if( _windowData.Document.IsDirty )
            {
                DispatcherTimer timer = new DispatcherTimer( DispatcherPriority.Background );

                timer.Interval = new TimeSpan( 0, 0, 5 );
                timer.Tick += (o,e2)=> {
                       ( (DispatcherTimer)o ).Stop();
                       SaveDocument();
                    };

                timer.Start();
            }
        }

        private void SaveDocument()
        {
            try
            {
                _windowData.Document.Save();
                _windowData.Document.IsDirty = false;
            }
            catch( Exception ex )
            {
                ReportToUserApplicationError( ex, "There were issues saving changes" );
            }
        }

        private void ReportToUserApplicationError( string format, params object[] args )
        {
            MessageBox.Show( string.Format( format, args ),
                             "Application Error", MessageBoxButton.OK, MessageBoxImage.Warning );
        }

        private void ReportToUserApplicationError( Exception ex, string format, params object[] args )
        {
            object[] additionalArgs = new object[] { ex.GetType().Name, ex.Message };

            ReportToUserApplicationError(
                string.Format( "{0}\n\nException Type: {{{1}}}\nException Text: {{{2}}}",
                               format, args.Length, args.Length + 1 ),
                args.Concat( additionalArgs ).ToArray()                                   );
        }


        private MainWindowData      _windowData;
        private MainWindowResizer   _windowResizer;
        private IWindowAutoHider    _windowAutoHider;

        #endregion
    }


    /// <summary>
    /// Contains non-UI application data which is maintained by the Droppy's main window
    /// </summary>
    class MainWindowData
    {
        /// <summary>
        /// Gets/Sets document object which represents the root of Droppy's data model
        /// </summary>
        public Data.WidgetDocument Document { get; set; }
    }

}
