using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;


namespace Droppy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );

            if( IsFirstInstance() )
            {
                ActivateMainWindow();
            }
            else
            {
                SignalOtherInstanceToActivate();
                this.Shutdown();
            }
        }

        private void ActivateMainWindow()
        {
            var mainWindow = new MainWindow();

            mainWindow.SourceInitialized += ( sender, e ) =>
            {
                HwndSource source = HwndSource.FromHwnd( new WindowInteropHelper( mainWindow ).Handle );
                source.AddHook( new HwndSourceHook( ShowFirstInstanceWinHook ) );
            };

            mainWindow.Show();
        }

        private IntPtr ShowFirstInstanceWinHook( IntPtr hwnd, int msg, IntPtr wParam,
                                                 IntPtr lParam, ref bool handled      )
        {
            if( msg == WM_SHOWFIRSTINSTANCE )
            {
                MainWindow.Activate();
                ( (MainWindow)MainWindow ).Show();
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void SignalOtherInstanceToActivate()
        {
            Win32.SendNotifyMessage( HWND_BROADCAST, WM_SHOWFIRSTINSTANCE, IntPtr.Zero, IntPtr.Zero );
        }

        private bool IsFirstInstance()
        {
            return _singleAppInstanceMutex.WaitOne( TimeSpan.Zero, true );
        }

        private static readonly IntPtr      HWND_BROADCAST = (IntPtr)0xffff;
        private static readonly int         WM_SHOWFIRSTINSTANCE = 
                    Win32.RegisterWindowMessage( "{BD14E533-C8F9-4470-BAD8-E033423DF334}" );
        private static Mutex                _singleAppInstanceMutex = 
                    new Mutex( true, "Local\\{BD14E533-C8F9-4470-BAD8-E033423DF334}" );
    }
}
