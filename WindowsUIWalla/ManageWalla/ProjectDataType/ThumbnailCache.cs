using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Controls;
using System.Xml;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace ManageWalla
{
    [System.SerializableAttribute()]
    //[System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.example.org/ThumbnailCache", IsNullable = false)]
    public class ThumbnailCache : ISerializable
    {
        public DateTime lastAccessed { get; set; }
        public long imageId { get; set; }
        public long imageSize { get; set; }
        public byte[] serializedImage { get; set; }
        public Image image { get; set; }

        /*
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("SerializedImage", serializedImage);
        }
        */

        public ThumbnailCache() { }

        protected ThumbnailCache(SerializationInfo info, StreamingContext context)
        {
            serializedImage = (byte[])info.GetValue("serializedImage", typeof(byte[]));
            imageId = (long)info.GetValue("imageId", typeof(long));
            lastAccessed = (DateTime)info.GetValue("lastAccessed", typeof(DateTime));
            imageSize = (long)info.GetValue("imageSize", typeof(long));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serializedImage", serializedImage);
            info.AddValue("imageId", imageId);
            info.AddValue("lastAccessed", lastAccessed);
            info.AddValue("imageSize", imageSize);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext sc)
        {
            BitmapImage bitmapImage = image.Source as BitmapImage;

            if (bitmapImage == null)
                return;

            MemoryStream stream = new MemoryStream();
            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            encoder.Save(stream);
            serializedImage = stream.ToArray();
            stream.Close();

            imageSize = serializedImage.LongLength;
        }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext sc)
        {
            MemoryStream stream = new MemoryStream(serializedImage);
            image = new Image
            {
                Source = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad)
            };
            stream.Close();
        }

    }
}
