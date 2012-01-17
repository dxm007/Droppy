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
using System.Xml;
using System.Xml.Serialization;

namespace Droppy.Data
{

    /// <summary>
    /// Data object for a folder widget
    /// </summary>
    [Serializable]
    class FolderWidgetData : WidgetData
    {
        #region ----------------------- Public Members ------------------------

        /// <summary>
        /// Gets/sets a path to a folder
        /// </summary>
        public string Path
        {
            get { return _path; }
            set { _path = value; IsDirty = true; }
        }

        /// <summary>
        /// Gets/sets a custom label for a folder. By default, if a client doesn't provide a label, this
        /// property will return Path property
        /// </summary>
        public string Label
        {
            get { return string.IsNullOrEmpty( _label ) ? _path : _label; }
            set { _label = value; IsDirty = true; }
        }

        #endregion

        #region ----------------------- Protected Members ---------------------

        /// <inheritdoc/>
        protected override void SerializeToXml( XmlWriter writer )
        {
            writer.WriteAttributeString( "path", _path );
            
            if( _label != null )
            {
                writer.WriteAttributeString( "label", _label );
            }
        }

        /// <inheritdoc/>
        protected override void DeserializeFromXml( XmlReader reader )
        {
            _path = reader.GetAttribute( "path" );
            _label = reader.GetAttribute( "label" );
        }

        #endregion

        #region ----------------------- Private Members -----------------------

        private string      _path;
        private string      _label;

        #endregion
    }
}
