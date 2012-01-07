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
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Droppy.Data
{
    public enum WidgetDocSaveFormat
    {
        Binary,
        Xml
    }

    public class WidgetDocument
    {
        public WidgetDocument()
        {
        }

        public WidgetContainerData Root
        {
            get { return _rootNode; }

            private set
            {
                if( _rootNode == value ) return;
                
                bool origIsDirty = this.IsDirty;

                if( _rootNode != null )
                {
                    _rootNode.IsDirtyChanged -= OnRootNodeIsDirtyChanged;
                }
                
                _rootNode = value;

                if( _rootNode != null )
                {
                    _rootNode.IsDirtyChanged += OnRootNodeIsDirtyChanged;
                }

                if( origIsDirty != this.IsDirty )
                {
                    OnIsDirtyChanged();
                }
            }
        }

        public bool IsDirty
        {
            get { return _rootNode != null ? _rootNode.IsDirty : false; }

            set
            {
                if( value == true )
                {
                    throw new InvalidOperationException( "Client can only clear the dirty flag" );
                }
                else if( _rootNode != null )
                {
                    _rootNode.ClearDirtyFlag( true );
                }
            }
        }


        public event EventHandler IsDirtyChanged;


        public void Load()
        {
            try
            {
                using( IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly() )
                using( Stream stream = new IsolatedStorageFileStream( "DroppyData", FileMode.Open, FileAccess.Read, FileShare.Read, isf ) )
                {
                    Load( stream, _defaultSaveFormat );
                }
            }
            catch( Exception )
            {
            }

            if( _rootNode == null )
            {
                this.Root = new WidgetContainerData( 6, 1 );
            }
        }

        public void Load( string filePath, WidgetDocSaveFormat format )
        {
            using( Stream stream = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
            {
                Load( stream, format );
            }
        }

        public void Load( Stream stream, WidgetDocSaveFormat format )
        {
            WidgetContainerData     container = null;

            if( format == WidgetDocSaveFormat.Xml )
            {
                using( XmlTextReader reader = new XmlTextReader( stream ) )
                {
                    reader.MoveToContent();

                    container = (WidgetContainerData)WidgetData.Create( reader );
                }
            }
            else if( format == WidgetDocSaveFormat.Binary )
            {
                IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                container = (WidgetContainerData)formatter.Deserialize( stream );
            }
            else
            {
                throw new InvalidOperationException( string.Format( "Invalid save mode specified ({0})", (int)format ) );
            }

            container.PostDeserialize();

            this.Root = container;
        }

        public void Save()
        {
            using( IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly() )
            using( Stream stream = new IsolatedStorageFileStream( "DroppyData", FileMode.Create, FileAccess.Write, FileShare.None, isf ) )
            {
                Save( stream, _defaultSaveFormat );
            }
        }

        public void Save( string filePath, WidgetDocSaveFormat format )
        {
            using( Stream stream = new FileStream( filePath, FileMode.Create, FileAccess.Write, FileShare.None ) )
            {
                Save( stream, format );
            }
        }

        public void Save( Stream stream, WidgetDocSaveFormat format )
        {
            if( format == WidgetDocSaveFormat.Xml )
            {
                using( XmlTextWriter writer = new XmlTextWriter( stream, Encoding.Unicode ) )
                {
                    ( (IXmlSerializable)_rootNode ).WriteXml( writer );
                }
            }
            else if( format == WidgetDocSaveFormat.Binary )
            {
                IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                
                formatter.Serialize( stream, _rootNode );
            }
            else
            {
                throw new InvalidOperationException( string.Format( "Invalid save mode specified ({0})", (int)format ) );
            }
        }


        private void OnRootNodeIsDirtyChanged( object sender, EventArgs e )
        {
            OnIsDirtyChanged();
        }

        private void OnIsDirtyChanged()
        {
            if( IsDirtyChanged != null )
            {
                IsDirtyChanged( this, new EventArgs() );
            }
        }

        private WidgetContainerData             _rootNode;
        private const WidgetDocSaveFormat       _defaultSaveFormat = WidgetDocSaveFormat.Binary;
    }
}
