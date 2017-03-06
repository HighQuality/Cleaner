using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Cleaner
{
    [Serializable()]
    [XmlRoot("configuration")]
    public class Configuration
    {
        [XmlArray("deletePatterns")]
        [XmlArrayItem("pattern")]
        public List<string> DeletePatterns;

        [XmlArray("doNotDeletePatterns")]
        [XmlArrayItem("pattern")]
        public List<string> DoNotDeletePatterns;

        [XmlElement("copyTo")]
        public string CopyTo;
        
        [XmlElement("removeEmptyDirectories")]
        public bool RemoveEmptyDirectories;
    }
}
