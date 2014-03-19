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
    public class UploadImageState
    {
        public DateTime uploadDate { get; set; }
        public DateTime lastUpdated { get; set; }
        public long imageId { get; set; }
        public UploadImage.UploadState uploadState { get; set; }
        public string errorMessage { get; set; }
        public bool hasError { get; set; }
        public string name { get; set; }
        public string fileName { get; set; }
        public string fullPath { get; set; }
        public bool isAutoUpload { get; set; }
        public long sizeBytes { get; set; }
        public bool isDeleted { get; set; }
        public long userAppId { get; set; }
        public string machineName { get; set; }
    }
}
