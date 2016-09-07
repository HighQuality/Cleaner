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
        [XmlArray("extensionsToDelete")]
        [XmlArrayItem("extension")]
        public HashSet<string> ExtensionsToDelete;

        [XmlElement("copyTo")]
        public string CopyTo;

        [XmlElement("copyDirectory")]
        public bool CopyDirectory;

        [XmlElement("removeEmptyDirectories")]
        public bool RemoveEmptyDirectories;
    }
}
