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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;

namespace Droppy
{
    enum FILEOP_CODES : uint
    {
        FO_MOVE     = 0x0001,
        FO_COPY     = 0x0002,
        FO_DELETE   = 0x0003,
        FO_RENAME   = 0x0004,
    }

    [Flags]
    enum FILEOP_FLAGS
    {
        FOF_MULTIDESTFILES          = 0x00000001,
        FOF_CONFIRMMOUSE            = 0x00000002,
        FOF_SILENT                  = 0x00000004,
        FOF_RENAMEONCOLLISION       = 0x00000008,
        FOF_NOCONFIRMATION          = 0x00000010,
        FOF_WANTMAPPINGHANDLE       = 0x00000020,
        FOF_ALLOWUNDO               = 0x00000040,
        FOF_FILESONLY               = 0x00000080,
        FOF_SIMPLEPROGRESS          = 0x00000100,
        FOF_NOCONFIRMMKDIR          = 0x00000200,
        FOF_NOERRORUI               = 0x00000400,
        FOF_NOCOPYSECURITYATTRIBS   = 0x00000800,
        FOF_NORECURSION             = 0x00001000, /* don't do recursion into directories */
        FOF_NO_CONNECTED_ELEMENTS   = 0x00002000, /* don't do connected files */
        FOF_WANTNUKEWARNING         = 0x00004000, /* during delete operation, warn if delete instead
                                               of recycling (even if FOF_NOCONFIRMATION) */
        FOF_NORECURSEREPARSE        = 0x00008000,

        // Flags below are available when using 2nd gne of 
        // file operation (IFileOperation COM-based)
        FOFX_NOSKIPJUNCTIONS        = 0x00010000,
        FOFX_PREFERHARDLINK         = 0x00020000,
        FOFX_SHOWELEVATIONPROMPT    = 0x00040000,
        FOFX_EARLYFAILURE           = 0x00100000,
        FOFX_PRESERVEFILEEXTENSIONS = 0x00200000,
        FOFX_KEEPNEWERFILE          = 0x00400000,
        FOFX_NOCOPYHOOKS            = 0x00800000,
        FOFX_NOMINIMIZEBOX          = 0x01000000,
        FOFX_MOVEACLSACROSSVOLUMES  = 0x02000000,
        FOFX_DONTDISPLAYSOURCEPATH  = 0x04000000,
        FOFX_DONTDISPLAYDESTPATH    = 0x08000000,

    }

    interface IFileOperation
    {
        Window ParentWindow { get; set; }
        FILEOP_CODES Operation { get; set; }
        FILEOP_FLAGS Flags { get; set; }
        string[] From { get; set; }
        string[] To { get; set; }
        bool AnyOpAborted { get; }
        Dictionary< string, string >[] NameMappings { get; }
        string ProgressTitle { get; set; }

        void Execute();
    }


    class FileOperationG1 : IFileOperation
    {
        public FileOperationG1()
        {
            _isAnyOpAborted = false;
        }

        public Window ParentWindow { get; set; }
        public FILEOP_CODES Operation { get; set; }
        public FILEOP_FLAGS Flags { get; set; }
        public string[] From { get; set; }
        public string[] To { get; set; }
        public bool AnyOpAborted { get { return _isAnyOpAborted; } }
        public Dictionary< string, string >[] NameMappings { get { return null; } }
        public string ProgressTitle { get; set; }

        public void Execute()
        {
            WindowInteropHelper wih = new WindowInteropHelper( ParentWindow );
            SHFILEOPSTRUCT      fileOp = new SHFILEOPSTRUCT();
            
            if( this.Flags.HasFlag( FILEOP_FLAGS.FOF_WANTMAPPINGHANDLE ) )
            {
                throw new NotImplementedException();
            }

            fileOp.hwnd = wih.Handle;
            fileOp.wFunc = Convert.ToInt32( this.Operation );
            fileOp.pFrom = ArrayToMultiString( this.From );
            fileOp.pTo = ArrayToMultiString( this.To );
            fileOp.fAnyOperationAborted = 0;
            fileOp.fFlags = Convert.ToInt16( this.Flags );
            fileOp.hNameMapping = IntPtr.Zero;
            fileOp.lpszProgressTitle = this.ProgressTitle;

            int res = SHFileOperation( ref fileOp );
        }


        private string ArrayToMultiString( string[] strings )
        {
            StringBuilder sb = new StringBuilder();

            foreach( var s in strings )
            {
                sb.Append( s ).Append( '\0' );
            }

            return sb.ToString();
        }

        #region --------------------------------- Shell API Declarations ----------------------

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr           hwnd;
            public Int32            wFunc;

            [MarshalAs(UnmanagedType.LPWStr)]
            public String           pFrom;

            [MarshalAs(UnmanagedType.LPWStr)]
            public String           pTo;

            public Int16            fFlags;             // zero or more FILEOP_FLAGS values (lower 16-bit only)
            public Int32            fAnyOperationAborted;
            public IntPtr           hNameMapping;

            [MarshalAs(UnmanagedType.LPWStr)]
            public String           lpszProgressTitle;
        }


        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern Int32 SHFileOperation( ref SHFILEOPSTRUCT lpFileOp );

        #endregion

        private bool    _isAnyOpAborted;
    }
}
