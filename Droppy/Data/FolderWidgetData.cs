using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Droppy.Data
{
    [Serializable]
    public class FolderWidgetData : WidgetData
    {
        public string Path
        {
            get { return _path; }
            set { _path = value; IsDirty = true; }
        }

        public string Label
        {
            get { return _label ?? _path; }
            set { _label = value; IsDirty = true; }
        }

        protected override void SerializeToXml( XmlWriter writer )
        {
            writer.WriteAttributeString( "path", _path );
            
            if( _label != null )
            {
                writer.WriteAttributeString( "label", _label );
            }
        }

        protected override void DeserializeFromXml( XmlReader reader )
        {
            _path = reader.GetAttribute( "path" );
            _label = reader.GetAttribute( "label" );
        }


        private string      _path;
        private string      _label;
    }
}
