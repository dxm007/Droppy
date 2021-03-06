﻿//=================================================================================================
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
    /// <summary>
    /// Indicates which format should be used when saving/loading the document
    /// </summary>
    enum WidgetDocSaveFormat
    {
        Binary,
        Xml
    }

    /// <summary>
    /// The root of widget data object hierarchy. This class is where everything starts. It is responsible
    /// for loading, saving and maintaining the contained widget data model.
    /// </summary>
    class WidgetDocument
    {
        #region ----------------------- Public Members ------------------------

        #region - - - - - - - Properties  - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// Gets the top-level widget container data object
        /// </summary>
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

        /// <summary>
        /// Gets/sets a flag which indicates whether or not widget document needs to be written out
        /// to disk because something was modified
        /// </summary>
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

        #endregion
        
        #region - - - - - - - Events  - - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// Gets fired whenever the value of IsDirty property changes
        /// </summary>
        public event EventHandler IsDirtyChanged;

        #endregion

        /// <summary>
        /// Loads widget data. This function loads data from default, application-specific isolated storage location
        /// </summary>
        /// <remarks>
        /// When this function is used, the application need not be concerned with or where the application data is 
        /// stored. Once this method is called, internall the WidgetDocument will take care of locating and opening 
        /// the persistent file.
        /// </remarks>
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

        /// <summary>
        /// Loads widget data from an external file whose format must match the specified format that is expected
        /// </summary>
        /// <param name="filePath">Path to the file to be opened</param>
        /// <param name="format">Identifies the expected format of the file being opened</param>
        public void Load( string filePath, WidgetDocSaveFormat format )
        {
            using( Stream stream = new FileStream( filePath, FileMode.Open, FileAccess.Read, FileShare.Read ) )
            {
                Load( stream, format );
            }
        }

        /// <summary>
        /// Loads widget data from a stream whose format must match the specified format that is expected.
        /// </summary>
        /// <param name="stream">Stream object which contains serialized data</param>
        /// <param name="format">Identifies the expected format of the stream being read</param>
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

        /// <summary>
        /// Saves widget data. This function saves the data in default, application-specific isolated storage location
        /// </summary>
        /// <remarks>
        /// When this function is used, the application need not be concerned with or where the application data is 
        /// stored. Once this method is called, internall the WidgetDocument will take care of opening and saving to 
        /// the persistent file.
        /// /// </remarks>
        public void Save()
        {
            using( IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly() )
            using( Stream stream = new IsolatedStorageFileStream( "DroppyData", FileMode.Create, FileAccess.Write, FileShare.None, isf ) )
            {
                Save( stream, _defaultSaveFormat );
            }
        }

        /// <summary>
        /// Saves widget data object hierarchy to an external file whose format is specified
        /// </summary>
        /// <param name="filePath">File path referencing a new file to be created</param>
        /// <param name="format">Specifies the format in which the file is to be written out</param>
        public void Save( string filePath, WidgetDocSaveFormat format )
        {
            using( Stream stream = new FileStream( filePath, FileMode.Create, FileAccess.Write, FileShare.None ) )
            {
                Save( stream, format );
            }
        }

        /// <summary>
        /// Saves widget data object hierarchy to a stream using the specified serialization format
        /// </summary>
        /// <param name="stream">Stream object where serialized data is to be written to</param>
        /// <param name="format">Specifies the format in which the file is to be written out</param>
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

        #endregion

        #region ----------------------- Private Members -----------------------

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

        #endregion
    }
}
