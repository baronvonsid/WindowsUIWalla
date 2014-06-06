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
using System.ComponentModel;
using System.Xml;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace ManageWalla
{
    [System.SerializableAttribute()]
    public class GallerySectionItem
    {
        public long sectionId { get; set; }
        public string name { get; set; }
        public string desc { get; set; }
        public string nameOveride { get; set; }
        public string descOveride { get; set; }
        public int sequence { get; set; }
    }
}

