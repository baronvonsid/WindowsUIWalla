using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Windows.Controls;

namespace ManageWalla
{
    [System.SerializableAttribute()]
    public class ThumbState : ISerializable
    {
        //Infra Properties
        static ThumbState thumbState = null;

        private const long cacheSize = 1000000; //3072000;

        //Business Objects
        public List<ThumbnailCache> thumbList { get; set; }


        #region InfraCodes
        public ThumbState() { }


        protected ThumbState(SerializationInfo info, StreamingContext context)
        {
            thumbList = (List<ThumbnailCache>)info.GetValue("thumbList", typeof(List<ThumbnailCache>));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("thumbList", thumbList);
        }

        public static ThumbState GetThumbs()
        {
            // Try to load from File
            thumbState = RetreiveFromFile();
            if (thumbState == null)
            {
                thumbState = new ThumbState();
                thumbState.thumbList = new List<ThumbnailCache>();
            }

            return thumbState;
        }

        public static Image GetImage(long imageId)
        {
            ThumbnailCache current = thumbState.thumbList.Find(m => (m.imageId == imageId));
            if (current != null)
            {
                current.lastAccessed = DateTime.Now;
                return current.image;
            }
            return null;
        }

        public static void SaveImage(long imageId, Image newImage)
        {
            //Check current size of cache, if exceeding limit then remove images.
            ReduceCacheSize(cacheSize);
            ThumbnailCache newCacheItem = new ThumbnailCache();
            newCacheItem.imageId = imageId;
            newCacheItem.lastAccessed = DateTime.Now;
            newCacheItem.image = newImage;

            thumbState.thumbList.Add(newCacheItem);
        }

        public static void ReduceCacheSize(long targetSize)
        {
            long totalSize = (long)thumbState.thumbList.Sum(r => r.imageSize);

            long thumbSize = 30720; //30KB average
            long buffer = thumbSize * 10;

            while (totalSize > (targetSize - buffer))
            {
                //Find oldest entry and remove form list.
                ThumbnailCache oldest = thumbState.thumbList.First(m=> m.imageSize == (thumbState.thumbList.Max(e => e.imageSize)));
                thumbState.thumbList.Remove(oldest);
                totalSize = totalSize - oldest.imageSize;
            }

        }

        private static ThumbState RetreiveFromFile()
        {
            string fileName = Path.Combine(Application.UserAppDataPath, "Walla-LocalThumbCache.db");
            ThumbState thumbStateTemp = null;

            if (File.Exists(fileName))
            {
                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    thumbStateTemp = formatter.Deserialize(fs) as ThumbState;
                    fs.Close();
                }

                return thumbStateTemp;
            }

            return null;
        }

        public void SaveState()
        {
            string fileName = Path.Combine(Application.UserAppDataPath, "Walla-LocalThumbCache.db");

            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, this);
                fs.Close();
            }
        }
        #endregion

        /*
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
         */ 
    }
}
