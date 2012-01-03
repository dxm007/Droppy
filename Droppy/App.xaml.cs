using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;


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
            new MainWindow().Show();
        }

        private void SignalOtherInstanceToActivate()
        {
            Win32.SendNotifyMessage( HWND_BROADCAST, WM_SHOWME, IntPtr.Zero, IntPtr.Zero );
        }

        private bool IsFirstInstance()
        {
            return _singleAppInstanceMutex.WaitOne( TimeSpan.Zero, true );
        }

        private static readonly IntPtr      HWND_BROADCAST = (IntPtr)0xffff;
        public static readonly int          WM_SHOWME = 
                    Win32.RegisterWindowMessage( "{BD14E533-C8F9-4470-BAD8-E033423DF334}" );
        private static Mutex                _singleAppInstanceMutex = 
                    new Mutex( true, "Local\\{BD14E533-C8F9-4470-BAD8-E033423DF334}" );
    }
}
