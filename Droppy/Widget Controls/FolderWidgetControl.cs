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
            IFileOperation          fileOp = new FileOperationG1();
            Data.FolderWidgetData   data = DataContext as Data.FolderWidgetData;

            if( data == null ) return;

            fileOp.ParentWindow = Window.GetWindow( this );
            fileOp.Operation = e.IsMove ? FILEOP_CODES.FO_MOVE :
                                          FILEOP_CODES.FO_COPY ;
            fileOp.From = e.Files;
            fileOp.To = new string[1] { data.Path };
            fileOp.Flags = FILEOP_FLAGS.FOF_ALLOWUNDO;
            
            fileOp.Execute();
        }
    }
}
