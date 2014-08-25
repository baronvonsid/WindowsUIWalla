using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Drawing;
using System.Drawing.Imaging;
using System.Xml;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace ManageWalla
{
    [System.SerializableAttribute()]
    //[System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.example.org/ThumbnailCache", IsNullable = false)]
    public class ThumbCache
    {
        public DateTime lastAccessed { get; set; }
        public long imageId { get; set; }
        public byte[] imageArray { get; set; }
        public long imageSize { get { return imageArray.LongLength; } }
    }
}
