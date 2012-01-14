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
    /// Base type for all widget data objects
    /// </summary>
    [Serializable]
    abstract class WidgetData : IXmlSerializable
    {
        #region ----------------------- Public Members ------------------------

        #region - - - - - - - Properties  - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// Gets the location of the widget within the parent container
        /// </summary>
        public MatrixLoc Location { get { return _location; } }

        /// <summary>
        /// Gets a reference to widget's parent container. Returns null if there is no parent
        /// </summary>
        public WidgetContainerData Parent { get { return _parent; } }
        
        /// <summary>
        /// Gets a flag which indicates whether or not the widget has a parent 
        /// </summary>
        public bool HasOwner { get { return Parent != null; } }

        /// <summary>
        /// Gets a flag which indicates whether or not the widget was modified since last save
        /// </summary>
        public bool IsDirty
        {
            get { return _isDirty; }
            
            protected set
            {
                bool originalValue = _isDirty;

                _isDirty = value;

                if( _isDirty != originalValue ) OnIsDirtyChanged();
            }
        }

        #endregion

        #region - - - - - - - Events  - - - - - - - - - - - - - - - - - - - - -

        /// <summary>
        /// Gets fired whenever the value of IsDirty property changes
        /// </summary>
        public event EventHandler IsDirtyChanged
        {
            add { _isDirtyChangedEvent += value; }
            remove { _isDirtyChangedEvent -= value; }
        }

        #endregion

        /// <summary>
        /// Sets parent container information on the current widget
        /// </summary>
        /// <param name="parent">Reference to the parent widget container</param>
        /// <param name="location">Location of the widget within the parent container</param>
        public void SetOwner( WidgetContainerData parent, MatrixLoc location )
        {
            _location = location;
            _parent = parent;
        }

        /// <summary>
        /// Removes any association between the widget and its parent
        /// </summary>
        public void ClearOwner()
        {
            _parent = null;
            _location = new MatrixLoc();
        }

        /// <summary>
        /// Resets IsDirty flag. This method is typically called after the widget data has been
        /// successfully saved to disk
        /// </summary>
        /// <param name="includeChildren"></param>
        public virtual void ClearDirtyFlag( bool includeChildren )
        {
            IsDirty = false;
        }

        /// <summary>
        /// Gets invoked after the widget object along with its parent is successfully loaded or
        /// deserialized. This function gives each widget a chance to perform final validation checks
        /// and to hook up any necessary internal linkages. Deriving class should make sure to always
        /// call base implementation of this method
        /// </summary>
        public virtual void PostDeserialize()
        {
        }

        /// <summary>
        /// Factory method for instanting widget objects based on the data passed in through xml
        /// </summary>
        /// <param name="reader">XML reader which is used to read serialized data object hierarchy</param>
        /// <returns>Newly created widget data instance</returns>
        public static WidgetData Create( XmlReader reader )
        {
            WidgetData      widget = null;

            // LATER: This method breaks OCP since addition/removal of widget types will require modification of the 
            // base class which is not desirable. For now we'll keep it this way, but if this method is revisited, we
            // should consider creating a stand-alone factory which could potentially use reflection to dynamically
            // build a list of creatable widgets.

            switch( reader.Name )
            {
                case "WidgetContainerData":
                    widget = new WidgetContainerData();
                    break;
                case "FolderWidgetData":
                    widget = new FolderWidgetData();
                    break;
                default:
                    // Unknown element, skip to next one
                    reader.Skip();
                    break;
            }

            if( widget != null )
            {
                ( ( IXmlSerializable)widget ).ReadXml( reader );
            }

            return widget;
        }

        #region - - - - - - - - IXmlSerializable Interface - - - - - - - - - -

        /// <inheritdoc/>
        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            // this is a reserved method and should always be returning NULL
            return null;
        }

        /// <inheritdoc/>
        void IXmlSerializable.ReadXml( XmlReader reader )
        {
            _location.Row = XmlConvert.ToInt32( reader.GetAttribute( "row" ) );
            _location.Column = XmlConvert.ToInt32( reader.GetAttribute( "column" ) );

            DeserializeFromXml( reader );

            if( reader.NodeType != XmlNodeType.EndElement )
            {
                // If deriving class didn't move the position from the starting element,
                // let's skip the rest of the current element.
                reader.Skip();
            }
            else
            {
                // If deriving class moved us to the end element, let's just consume
                // that advance to the next element
                reader.ReadEndElement();
            }
        }

        /// <inheritdoc/>
        void IXmlSerializable.WriteXml( XmlWriter writer )
        {
            writer.WriteStartElement( GetType().Name );
            writer.WriteAttributeString( "row", _location.Row.ToString() );
            writer.WriteAttributeString( "column", _location.Column.ToString() );

            SerializeToXml( writer );

            writer.WriteEndElement();
        }

        #endregion
        #endregion

        #region ----------------------- Protected Members ---------------------

        /// <summary>
        /// Gets invoked whenever the value of IsDirty property changes
        /// </summary>
        protected virtual void OnIsDirtyChanged()
        {
            if( _isDirtyChangedEvent != null )
            {
                _isDirtyChangedEvent( this, new EventArgs() );
            }
        }

        /// <summary>
        /// Must be implemented by the deriving class. This method gets invoked in order to serialize
        /// widget data into an XML format
        /// </summary>
        /// <param name="writer">XML writer object to serialize the data into</param>
        protected abstract void SerializeToXml( XmlWriter writer );

        /// <summary>
        /// Must be implemented by the deriving class. This method gets invoked in order to deserialize
        /// widget data that was previously written out to an XLM file via SerializeToXml()
        /// </summary>
        /// <param name="reader">XML reader object from which widget data is deserialized</param>
        protected abstract void DeserializeFromXml( XmlReader reader );

        #endregion

        #region ----------------------- Private Members -----------------------

        private MatrixLoc               _location;
        private WidgetContainerData     _parent;

        [NonSerialized]
        private bool                    _isDirty;

        [NonSerialized]
        private EventHandler            _isDirtyChangedEvent;

        #endregion
    }
}
