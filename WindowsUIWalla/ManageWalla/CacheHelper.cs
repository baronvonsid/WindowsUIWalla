using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace ManageWalla
{
    static public class CacheHelper
    {
        //static List<ThumbCache> thumbCacheList = null;
    
        private static byte[] key = { 9, 2, 7, 1, 5, 4, 7, 8 };
        private static byte[] iv = { 1, 8, 3, 4, 1, 6, 5, 9 };
        //private static string cacheFileLocation = Application.UserAppDataPath;

        #region GlobalState
        public static GlobalState GetGlobalState(string profileName)
        {
            // Try to load from File
            GlobalState state = RetrieveFromFile(profileName);
            if (state == null)
            {
                state = new GlobalState();

                state.tagImageList = new List<ImageList>();
                state.categoryImageList = new List<ImageList>();
                state.galleryImageList = new List<ImageList>();
                state.imageMetaList = new List<ImageMeta>();
                //state.mainCopyCacheList = new List<MainCopyCache>();
                state.connectionState = GlobalState.ConnectionState.NoAccount;
                //state.mainCopyFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "FotoWalla Copies");
                //state.mainCopyCacheSizeMB = Properties.Settings.Default.MainCopyCacheSizeMB;
                state.account = new Account();
                state.account.ProfileName = "";
            }

            //state.imageFetchSize = Properties.Settings.Default.ImageFetchSize;
            //state.thumbCacheSizeMB = Properties.Settings.Default.ThumbCacheSizeMB;

            state.categoryLoadState = GlobalState.DataLoadState.No;
            state.tagLoadState = GlobalState.DataLoadState.No;
            state.galleryLoadState = GlobalState.DataLoadState.No;
            state.uploadStatusListState = GlobalState.DataLoadState.No;

            return state;
        }

        public static void SaveGlobalState(GlobalState state, string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + Properties.Settings.Default.GlobalStateCacheFileName);
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                var cryptoStream = new CryptoStream(fs, des.CreateEncryptor(key, iv), CryptoStreamMode.Write);
                BinaryFormatter formatter = new BinaryFormatter();

                // This is where you serialize the class
                formatter.Serialize(cryptoStream, state);
                cryptoStream.FlushFinalBlock();
            }
        }

        private static GlobalState RetrieveFromFile(string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + Properties.Settings.Default.GlobalStateCacheFileName);
            GlobalState stateTemp = null;

            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            if (File.Exists(fileName))
            {
                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    var cryptoStream = new CryptoStream(fs, des.CreateDecryptor(key, iv), CryptoStreamMode.Read);
                    BinaryFormatter formatter = new BinaryFormatter();
                    stateTemp = (GlobalState)formatter.Deserialize(cryptoStream);
                }

                return stateTemp;
            }

            return null;
        }
        #endregion

        #region ThumbCache
        public static List<ThumbCache> GetThumbCacheList(string profileName)
        {
            // Try to load from File
            List<ThumbCache> thumbCacheList = RetrieveThumbCacheFromFile(profileName);
            if (thumbCacheList == null)
            {
                thumbCacheList = new List<ThumbCache>();
            }

            return thumbCacheList;
        }

        public static void SaveThumbCacheList(List<ThumbCache> thumbCacheList, string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + Properties.Settings.Default.ThumbCacheFileName);

            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, thumbCacheList);
                fs.Close();
            }
        }

        public static Byte[] GetImageArray(long imageId, List<ThumbCache> thumbCacheList)
        {
            ThumbCache current = thumbCacheList.Find(m => (m.imageId == imageId));
            if (current != null)
            {
                current.lastAccessed = DateTime.Now;
                return current.imageArray;
            }
            return null;
        }

        public static void SaveImageArray(long imageId, Byte[] newImageArray, List<ThumbCache> thumbCacheList, int thumbCacheSizeMB)
        {
            //Check current size of cache, if exceeding limit then remove images.
            ReduceThumbCacheSize(thumbCacheList, thumbCacheSizeMB);
            ThumbCache newCacheItem = new ThumbCache();
            newCacheItem.imageId = imageId;
            newCacheItem.lastAccessed = DateTime.Now;
            newCacheItem.imageArray = newImageArray;

            thumbCacheList.Add(newCacheItem);
        }

        public static void ReduceThumbCacheSize(List<ThumbCache> thumbCacheList, int thumbCacheSizeMB)
        {
            //long targetSizeMB = Properties.Settings.Default.ThumbCacheSizeMB;
            long totalSize = (long)thumbCacheList.Sum(r => r.imageSize);

            long thumbSize = 14000; //30KB average
            long buffer = thumbSize * 10;
            long targetSizeBytes = thumbCacheSizeMB * 131072;
            while (totalSize > (targetSizeBytes - buffer))
            {
                //Find oldest entry and remove form list.
                ThumbCache oldest = thumbCacheList.First(m => m.lastAccessed == (thumbCacheList.Max(e => e.lastAccessed)));
                thumbCacheList.Remove(oldest);
                targetSizeBytes = targetSizeBytes - oldest.imageSize;
            }

        }

        private static List<ThumbCache> RetrieveThumbCacheFromFile(string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + Properties.Settings.Default.ThumbCacheFileName);
            List<ThumbCache> thumbStateTemp = null;

            if (File.Exists(fileName))
            {
                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    thumbStateTemp = formatter.Deserialize(fs) as List<ThumbCache>;
                    fs.Close();
                }
                return thumbStateTemp;
            }
            return null;
        }
        #endregion

        #region MainCopyCache
        public static List<MainCopyCache> GetMainCopyCacheList(string profileName)
        {
            // Try to load from File
            List<MainCopyCache> mainCopyCacheList = RetrieveMainCopyCacheFromFile(profileName);
            if (mainCopyCacheList == null)
            {
                mainCopyCacheList = new List<MainCopyCache>();
            }
            return mainCopyCacheList;
        }

        public static void SaveMainCopyCacheList(List<MainCopyCache> mainCopyCacheList, string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + Properties.Settings.Default.MainCopyCacheFileName);

            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, mainCopyCacheList);
                fs.Close();
            }
        }

        public static string GetMainCopyFileName(long imageId, List<MainCopyCache> mainCopyCacheList, string folder)
        {
            MainCopyCache current = mainCopyCacheList.Find(m => (m.imageId == imageId));
            if (current != null)
            {
                current.lastAccessed = DateTime.Now;
                string filePath = Path.Combine(folder, current.imageId.ToString()) + ".jpg";
                if (File.Exists(filePath))
                    return filePath;
                else
                    mainCopyCacheList.Remove(current);
            }
            return "";
        }

        public static void SaveMainCopyToCache(long imageId, Byte[] mainCopyByteArray, List<MainCopyCache> mainCopyCacheList, string folder, int mainCopyCacheSizeMB)
        {
            //Check current size of cache, if exceeding limit then remove images.
            ReduceMainCopyCacheSize(mainCopyCacheList, folder, mainCopyCacheSizeMB);

            string fileName = Path.Combine(folder, imageId.ToString()) + ".jpg";
            if (!File.Exists(fileName))
            {
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
                File.WriteAllBytes(fileName,mainCopyByteArray);
            }

            MainCopyCache newCacheItem = new MainCopyCache();
            newCacheItem.imageId = imageId;
            newCacheItem.lastAccessed = DateTime.Now;
            newCacheItem.imageSize = mainCopyByteArray.LongLength;
            mainCopyCacheList.Add(newCacheItem);
        }

        public static void ReduceMainCopyCacheSize(List<MainCopyCache> mainCopyCacheList, string folder, int mainCopyCacheSizeMB)
        {
            //long targetSizeMB = Properties.Settings.Default.MainCopyCacheSizeMB;
            if (mainCopyCacheSizeMB >= 500)
                return;

            long totalSize = (long)mainCopyCacheList.Sum(r => r.imageSize);

            long mainCopySize = 100000; //200KB average
            long buffer = mainCopySize * 10;
            long targetSizeBytes = mainCopyCacheSizeMB / 1024 / 1024;
            while (totalSize > (targetSizeBytes - buffer))
            {
                //Find oldest entry and remove from list.
                MainCopyCache oldest = mainCopyCacheList.First(m => m.lastAccessed == (mainCopyCacheList.Max(e => e.lastAccessed)));
                mainCopyCacheList.Remove(oldest);
                string path = Path.Combine(folder, oldest.imageId.ToString()) + ".jpg";
                File.Delete(path);
                targetSizeBytes = targetSizeBytes - oldest.imageSize;
            }

        }

        private static List<MainCopyCache> RetrieveMainCopyCacheFromFile(string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + Properties.Settings.Default.MainCopyCacheFileName);
            List<MainCopyCache> mainCopyTemp = null;

            if (File.Exists(fileName))
            {
                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    mainCopyTemp = formatter.Deserialize(fs) as List<MainCopyCache>;
                    fs.Close();
                }
                return mainCopyTemp;
            }
            return null;
        }
        #endregion

        #region UploadHistoryCache
        public static void GetUploadImageStateList(UploadImageStateList uploadImageStateList, string profileName)
        {
            RetrieveUploadImageStateFromFile(uploadImageStateList, profileName);
        }

        public static void SaveUploadImageStateList(UploadImageStateList uploadImageStateList, string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + Properties.Settings.Default.UploadImageStateFileName);

            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, uploadImageStateList);
                fs.Close();
            }
        }

        public static long[] GetUploadImageListQueryIds(UploadImageStateList uploadImageStateList)
        {
            var queryItems = uploadImageStateList.Where(
                                            r => (r.uploadState == UploadImage.UploadState.AwaitingProcessed
                                            || r.uploadState == UploadImage.UploadState.BeingProcessed
                                            || r.uploadState == UploadImage.UploadState.FileReceived) 
                                            && r.lastUpdated > DateTime.Now.AddMonths(-1));

            return queryItems.Select(r => r.imageId).ToArray();
        }

        public static void UpdateUploadImageStateListWithServerStatus(UploadImageStateList uploadImageStateList, UploadStatusList uploadStatusList)
        {
            //TODO

            //Loop through uploadStatusList and reflect new reality in uploadHistoryCacheList 
        }

        public static void DeleteUploadedFiles(UploadImageStateList uploadImageStateList, string autoUploadFolder)
        {
            var needDeletingItems = uploadImageStateList.Where(
                                            r => r.uploadState == UploadImage.UploadState.Complete
                                            && r.isDeleted == false);

            foreach (UploadImageState uploadedItem in needDeletingItems)
            {
                try
                {
                    string deleteFile = Path.Combine(autoUploadFolder, uploadedItem.fileName);
                    if (File.Exists(deleteFile))
                    {
                        File.Delete(deleteFile);
                    }
                    uploadedItem.isDeleted = true;
                    uploadedItem.lastUpdated = DateTime.Now;
                }
                catch (Exception ex)
                {
                    uploadedItem.errorMessage = "File cannot be deleted.  Error: " + ex.Message;
                    uploadedItem.lastUpdated = DateTime.Now;
                }
            }
        }

        public static void ClearUploadImageStateListOldEntries(UploadImageStateList uploadImageStateList)
        {
            var clearItems = uploadImageStateList.Where(
                                            r => (r.uploadState == UploadImage.UploadState.FileReceived
                                            || r.uploadState == UploadImage.UploadState.None)
                                            && r.lastUpdated < DateTime.Now.AddMonths(-1));

            foreach (UploadImageState remove in clearItems)
            {
                uploadImageStateList.Remove(remove);
            }
        }

        public static UploadImageState GetOrCreateCacheItem(UploadImageStateList uploadImageStateList, string fileName, string fullPath, string name, long size, bool isAuto, long userAppId, string machineName)
        {
            //Method checks for existing entries.  Adds in a new entry if none is found.

            var existingItem = uploadImageStateList.FirstOrDefault(r => r.fileName.ToUpper() == fileName.ToUpper() 
                && r.sizeBytes == size 
                && (r.uploadState == UploadImage.UploadState.None));

            if (existingItem != null)
            {
                return existingItem;
            }
            else
            {
                UploadImageState newUploadEntry = new UploadImageState();
                newUploadEntry.imageId = 0;
                newUploadEntry.errorMessage = "";
                newUploadEntry.hasError = false;
                newUploadEntry.fileName = fileName;
                newUploadEntry.fullPath = fullPath;
                newUploadEntry.isAutoUpload = isAuto;
                newUploadEntry.isDeleted = false;
                newUploadEntry.lastUpdated = DateTime.Now;
                newUploadEntry.name = name;
                newUploadEntry.sizeBytes = size;
                newUploadEntry.uploadDate = DateTime.Now;
                newUploadEntry.uploadState = UploadImage.UploadState.None;
                newUploadEntry.userAppId = userAppId;
                newUploadEntry.machineName = machineName;

                uploadImageStateList.Add(newUploadEntry);

                return newUploadEntry;
            }
        }

        private static void RetrieveUploadImageStateFromFile(UploadImageStateList uploadImageStateList,string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + Properties.Settings.Default.UploadImageStateFileName);
            UploadImageStateList uploadImageStateListTemp = null;

            if (File.Exists(fileName))
            {
                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    //UploadImageStateList temp = formatter.Deserialize(fs) as UploadImageStateList;
                    uploadImageStateListTemp = formatter.Deserialize(fs) as UploadImageStateList;
                    fs.Close();
                }
            }

            if (uploadImageStateListTemp != null)
            {
                foreach (UploadImageState current in uploadImageStateListTemp)
                {
                    uploadImageStateList.Add(current);
                }
            }

        }
        #endregion
    }











            //Infra Properties
        
    /*
        private const long cacheSize = 14000000; //3072000; 14,000 average thumb size array length.

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

        public static Byte[] GetImageArray(long imageId)
        {
            ThumbnailCache current = thumbState.thumbList.Find(m => (m.imageId == imageId));
            if (current != null)
            {
                current.lastAccessed = DateTime.Now;
                return current.imageArray;
            }
            return null;
        }

        public static void SaveImageArray(long imageId, Byte[] newImageArray)
        {
            //Check current size of cache, if exceeding limit then remove images.
            ReduceCacheSize(cacheSize);
            ThumbnailCache newCacheItem = new ThumbnailCache();
            newCacheItem.imageId = imageId;
            newCacheItem.lastAccessed = DateTime.Now;
            newCacheItem.imageArray = newImageArray;

            thumbState.thumbList.Add(newCacheItem);
        }

        public static void ReduceCacheSize(long targetSize)
        {
            long totalSize = (long)thumbState.thumbList.Sum(r => r.imageSize);

            long thumbSize = 14000; //30KB average
            long buffer = thumbSize * 10;

            while (totalSize > (targetSize - buffer))
            {
                //Find oldest entry and remove form list.
                ThumbnailCache oldest = thumbState.thumbList.First(m => m.lastAccessed == (thumbState.thumbList.Max(e => e.lastAccessed)));
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

    */

}
