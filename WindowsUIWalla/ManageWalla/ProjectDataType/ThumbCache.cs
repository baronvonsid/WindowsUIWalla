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

/*        
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("SerializedImage", serializedImage);
        }
        

        public ThumbCache() { }

        protected ThumbCache(SerializationInfo info, StreamingContext context)
        {
            imageArray = (byte[])info.GetValue("serializedImage", typeof(byte[]));
            imageId = (long)info.GetValue("imageId", typeof(long));
            lastAccessed = (DateTime)info.GetValue("lastAccessed", typeof(DateTime));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serializedImage", imageArray);
            info.AddValue("imageId", imageId);
            info.AddValue("lastAccessed", lastAccessed);
            info.AddValue("imageSize", imageSize);
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext sc)
        {
            
            //BitmapImage bitmapImage = image.Source as BitmapImage;

            //if (bitmapImage == null)
            //    return;
               /*
            MemoryStream memory = new MemoryStream();
            image.Save(memory,ImageFormat.Jpeg);



            var encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(
            encoder.Frames.Add(BitmapFrame.Create(image.Source));
            encoder.Save(ms);


            using (MemoryStream ms = new MemoryStream())
            {
                var encoder = new JpegBitmapEncoder();
                //encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(ms);

                return ms.GetBuffer();
            }


            MemoryStream stream = new MemoryStream();
         
            
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(
            encoder.Save(stream);
            

            BmpBitmapEncoder encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            encoder.Save(stream);
            serializedImage = stream.ToArray();
            stream.Close();

            //imageSize = serializedImage.LongLength;
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


        

        
        byte[] SaveImage(BitmapSource bitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(ms);

                return ms.GetBuffer();
            }
        }
        */
    }
}
