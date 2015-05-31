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
using log4net;

namespace ManageWalla
{
    static public class CacheHelper
    {
        private static byte[] key = { 9, 2, 7, 1, 5, 4, 7, 8, 5, 7, 9, 2, 7, 1, 5, 4, 7, 8, 5, 7, 9, 2, 7, 1, 5, 4, 7, 8, 5, 7, 3, 4 };
        private static byte[] iv = { 9, 2, 7, 1, 5, 4, 7, 8, 5, 7, 9, 2, 7, 1, 5, 4 };
        private static string version = Application.ProductVersion.Replace(".", "-");

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
                state.connectionState = GlobalState.ConnectionState.NoAccount;
                state.account = new Account();
                state.account.ProfileName = "";
                state.account.Password = "";
            }

            state.categoryLoadState = GlobalState.DataLoadState.No;
            state.tagLoadState = GlobalState.DataLoadState.No;
            state.galleryLoadState = GlobalState.DataLoadState.No;
            state.uploadStatusListState = GlobalState.DataLoadState.No;

            return state;
        }

        public static void ResetGlobalState(GlobalState state)
        {
            state.tagImageList = new List<ImageList>();
            state.categoryImageList = new List<ImageList>();
            state.galleryImageList = new List<ImageList>();
            state.imageMetaList = new List<ImageMeta>();
            state.connectionState = GlobalState.ConnectionState.NoAccount;
            state.account = new Account();
            state.account.ProfileName = "";
            state.account.Password = "";

            state.categoryLoadState = GlobalState.DataLoadState.No;
            state.tagLoadState = GlobalState.DataLoadState.No;
            state.galleryLoadState = GlobalState.DataLoadState.No;
            state.uploadStatusListState = GlobalState.DataLoadState.No;

        }

        public static void SaveGlobalState(GlobalState state, string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + version + "-" + Properties.Settings.Default.GlobalStateCacheFileName);
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            aes.KeySize = 256;

            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                var cryptoStream = new CryptoStream(fs, aes.CreateEncryptor(key, iv), CryptoStreamMode.Write);
                BinaryFormatter formatter = new BinaryFormatter();

                // This is where you serialize the class
                formatter.Serialize(cryptoStream, state);
                cryptoStream.FlushFinalBlock();
            }
        }

        private static GlobalState RetrieveFromFile(string profileName)
        {
            string version = Application.ProductVersion.Replace(".","-");

            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + version + "-" + Properties.Settings.Default.GlobalStateCacheFileName);
            GlobalState stateTemp = null;

            AesCryptoServiceProvider aes = new AesCryptoServiceProvider();
            aes.KeySize = 256;

            if (File.Exists(fileName))
            {
                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    var cryptoStream = new CryptoStream(fs, aes.CreateDecryptor(key, iv), CryptoStreamMode.Read);
                    BinaryFormatter formatter = new BinaryFormatter();
                    stateTemp = (GlobalState)formatter.Deserialize(cryptoStream);
                }

                return stateTemp;
            }

            return null;
        }

        public static bool CacheFilesPresent(string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + version + "-" + Properties.Settings.Default.GlobalStateCacheFileName);
            return File.Exists(fileName);
        }

        public static void GalleryPresentationPopulateFromState(GlobalState state, GalleryPresentationList list)
        {
            if (state.galleryPresentationList != null)
            {
                list.Clear();
                foreach (GalleryPresentationItem item in state.galleryPresentationList)
                {
                    list.Add(item);
                }
            }
        }

        public static void GalleryStylePopulateFromState(GlobalState state, GalleryStyleList list)
        {
            if (state.galleryStyleList != null)
            {
                list.Clear();
                foreach (GalleryStyleItem item in state.galleryStyleList)
                {
                    list.Add(item);
                }
            }
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
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + version + "-" + Properties.Settings.Default.ThumbCacheFileName);

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

            long thumbSize = 40000; 
            long buffer = thumbSize * 10;
            long targetSizeBytes = thumbCacheSizeMB * 1024 * 1024;
            while (totalSize > (targetSizeBytes - buffer))
            {
                //Find oldest entry and remove form list.
                var oldest = thumbCacheList.OrderByDescending(t => t.lastAccessed).FirstOrDefault();

                //DateTime maxDate = thumbCacheList.Max(e => e.lastAccessed);
                //ThumbCache oldest = thumbCacheList.FirstOrDefault<ThumbCache>(m => m.lastAccessed == maxDate);
                if (oldest == null)
                    return;

                //ThumbCache oldest2 = thumbCacheList.First<ThumbCache>(m => m.lastAccessed == (thumbCacheList.Max(e => e.lastAccessed)));
                thumbCacheList.Remove(oldest);
                //targetSizeBytes = targetSizeBytes - oldest.imageSize;
                totalSize = (long)thumbCacheList.Sum(r => r.imageSize);
            }

        }

        private static List<ThumbCache> RetrieveThumbCacheFromFile(string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + version + "-" + Properties.Settings.Default.ThumbCacheFileName);
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
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + version + "-" + Properties.Settings.Default.MainCopyCacheFileName);

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

            if (mainCopyCacheSizeMB > 0)
            {
                string fileName = Path.Combine(folder, imageId.ToString()) + ".jpg";
                if (!File.Exists(fileName))
                {
                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }
                    File.WriteAllBytes(fileName, mainCopyByteArray);
                }

                MainCopyCache newCacheItem = new MainCopyCache();
                newCacheItem.imageId = imageId;
                newCacheItem.lastAccessed = DateTime.Now;
                newCacheItem.imageSize = mainCopyByteArray.LongLength;
                mainCopyCacheList.Add(newCacheItem);
            }
        }

        public static void ReduceMainCopyCacheSize(List<MainCopyCache> mainCopyCacheList, string folder, int mainCopyCacheSizeMB)
        {
            //long targetSizeMB = Properties.Settings.Default.MainCopyCacheSizeMB;
            if (mainCopyCacheSizeMB >= 500)
                return;

            long totalSize = (long)mainCopyCacheList.Sum(r => r.imageSize);

            long mainCopySize = 100000; //200KB average
            long buffer = mainCopySize * 10;
            long targetSizeBytes = mainCopyCacheSizeMB * 1024 * 1024;
            while (totalSize > (targetSizeBytes - buffer) && mainCopyCacheList.Count > 0)
            {
                //Find oldest entry and remove from list.
                MainCopyCache oldest = mainCopyCacheList.FirstOrDefault<MainCopyCache>(m => m.lastAccessed == (mainCopyCacheList.Max(e => e.lastAccessed)));
                if (oldest != null)
                {
                    mainCopyCacheList.Remove(oldest);
                    string path = Path.Combine(folder, oldest.imageId.ToString()) + ".jpg";
                    if (File.Exists(path))
                    {
                        try
                        {
                            File.Delete(path);
                        }
                        catch (Exception ex) { }
                    }

                    targetSizeBytes = targetSizeBytes - oldest.imageSize;
                }
            }

        }

        private static List<MainCopyCache> RetrieveMainCopyCacheFromFile(string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + version + "-" + Properties.Settings.Default.MainCopyCacheFileName);
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
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + version + "-" + Properties.Settings.Default.UploadImageStateFileName);

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
                                            r => (r.status == UploadImage.ImageStatus.AwaitingProcessed
                                            || r.status == UploadImage.ImageStatus.BeingProcessed
                                            || r.status == UploadImage.ImageStatus.FileReceived) 
                                            && r.lastUpdated > DateTime.Now.AddMonths(-1));

            long[] arrayTemp = queryItems.Select(r => r.imageId).ToArray();
            if (arrayTemp.Length > 100)
            {
                long[] arrayReduced = new long[100];
                for (int i = 0; i < 100; i++)
                    arrayReduced[i] = arrayTemp[i];

                return arrayReduced;
            }
            else
                return arrayTemp;


        }

        public static void UpdateUploadImageStateListWithServerStatus(UploadImageStateList uploadImageStateList, UploadStatusList uploadStatusList)
        {
            //TODO

            //Loop through uploadStatusList and reflect new reality in uploadHistoryCacheList 
        }

        public static void DeleteUploadedFiles(UploadImageStateList uploadImageStateList, string autoUploadFolder, string machineName, ILog logger)
        {
            var needDeletingItems = uploadImageStateList.Where(
                                            r => r.status == UploadImage.ImageStatus.Complete
                                            && r.isDeleted == false && r.isAutoUpload == true && r.machineName == machineName);

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
                    logger.Error(ex);
                    uploadedItem.errorMessage = "File cannot be deleted.  Error: " + ex.Message;
                    uploadedItem.lastUpdated = DateTime.Now;
                }
            }
        }

        public static void ClearUploadImageStateListOldEntries(UploadImageStateList uploadImageStateList)
        {
            var clearItems = uploadImageStateList.Where(
                                            r => (r.status == UploadImage.ImageStatus.FileReceived
                                            || r.status == UploadImage.ImageStatus.None)
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
                && (r.status == UploadImage.ImageStatus.None || (r.status == UploadImage.ImageStatus.FileReceived && r.error == true)));

            if (existingItem != null)
            {
                existingItem.error = false;
                existingItem.errorMessage = "";
                existingItem.imageId = 0;
                existingItem.status = UploadImage.ImageStatus.None;

                return existingItem;
            }
            else
            {
                UploadImageState newUploadEntry = new UploadImageState();
                newUploadEntry.imageId = 0;
                newUploadEntry.errorMessage = "";
                newUploadEntry.error = false;
                newUploadEntry.fileName = fileName;
                newUploadEntry.fullPath = fullPath;
                newUploadEntry.isAutoUpload = isAuto;
                newUploadEntry.isDeleted = false;
                newUploadEntry.lastUpdated = DateTime.Now;
                newUploadEntry.name = name;
                newUploadEntry.sizeBytes = size;
                newUploadEntry.uploadDate = DateTime.Now;
                newUploadEntry.status = UploadImage.ImageStatus.None;
                newUploadEntry.userAppId = userAppId;
                newUploadEntry.machineName = machineName;

                uploadImageStateList.Add(newUploadEntry);

                return newUploadEntry;
            }
        }

        private static void RetrieveUploadImageStateFromFile(UploadImageStateList uploadImageStateList,string profileName)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, profileName + "-" + version + "-" + Properties.Settings.Default.UploadImageStateFileName);
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
}
