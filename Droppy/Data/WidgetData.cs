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
    


    [Serializable]
    public abstract class WidgetData : IXmlSerializable
    {
        public MatrixLoc Location { get { return _location; } }

        public WidgetContainerData Parent { get { return _parent; } }
        
        public bool HasOwner { get { return Parent != null; } }

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

        public event EventHandler IsDirtyChanged
        {
            add { _isDirtyChangedEvent += value; }
            remove { _isDirtyChangedEvent -= value; }
        }


        public void SetOwner( WidgetContainerData parent, MatrixLoc location )
        {
            _location = location;
            _parent = parent;
        }

        public void ClearOwner()
        {
            _parent = null;
            _location = new MatrixLoc();
        }

        public virtual void ClearDirtyFlag( bool includeChildren )
        {
            IsDirty = false;
        }

        public virtual void PostDeserialize()
        {
        }

        public static WidgetData Create( XmlReader reader )
        {
            WidgetData      widget = null;

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

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            // this is a reserved method and should always be returning NULL
            return null;
        }

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

        void IXmlSerializable.WriteXml( XmlWriter writer )
        {
            writer.WriteStartElement( GetType().Name );
            writer.WriteAttributeString( "row", _location.Row.ToString() );
            writer.WriteAttributeString( "column", _location.Column.ToString() );

            SerializeToXml( writer );

            writer.WriteEndElement();
        }

        #endregion


        protected virtual void OnIsDirtyChanged()
        {
            if( _isDirtyChangedEvent != null )
            {
                _isDirtyChangedEvent( this, new EventArgs() );
            }
        }

        protected abstract void SerializeToXml( XmlWriter writer );

        protected abstract void DeserializeFromXml( XmlReader reader );



        private MatrixLoc               _location;
        private WidgetContainerData     _parent;

        [NonSerialized]
        private bool                    _isDirty;

        [NonSerialized]
        private EventHandler            _isDirtyChangedEvent;
    }
}
