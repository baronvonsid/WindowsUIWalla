using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using Shell32;
using log4net;
using System.Threading;
using System.Windows.Controls;

namespace ManageWalla
{
    public class GeneralImage : INotifyPropertyChanged
    {
        #region Variables and Init
        private ServerHelper serverHelper;

        public long imageId { get; set; }
        public long categoryId { get; set; }
        public Image thumbnailImage { get; set; }
        public Image mainCopyImage {get; set; }
        public ImageMeta Meta { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string shotSummary { get; set; }
        public string fileSummary { get; set; }
        public DateTime uploadDate { get; set; }
        public int metaVersion { get; set; }
        
        public LoadState metaLoadState { get; set; }
        public LoadState mainImageLoadState { get; set; }
        public LoadState thumbImageLoadState { get; set; }

        public enum LoadState
        {
            NotLoaded = 0,
            Requested = 1,
            Loaded = 2,
            Error = 3
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(GeneralImage));

        public event PropertyChangedEventHandler PropertyChanged;

        public GeneralImage(ServerHelper serverHelperParam) 
        {
            serverHelper = serverHelperParam;
            mainImageLoadState = LoadState.NotLoaded;
            thumbImageLoadState = LoadState.NotLoaded;

            metaLoadState = LoadState.NotLoaded;
        }
        #endregion

        #region Load Images
        async public Task LoadThumb(CancellationToken cancelToken, List<ThumbCache> thumbCacheList, int thumbCacheSizeMB, GlobalState.ConnectionState connectionState)
        {
            if (imageId == 0)
                return;

            DateTime startTime = DateTime.Now;
            try
            {
                thumbImageLoadState = LoadState.Requested;
                OnPropertyChanged("thumbImageLoadState");


                byte[] thumbArray = CacheHelper.GetImageArray(imageId, thumbCacheList);
                if (thumbArray == null)
                {
                    if (connectionState == GlobalState.ConnectionState.OfflineMode)
                    {
                        thumbImageLoadState = LoadState.Error;
                        return;
                    }

                    /* GET /{userName}/image/{imageId}/{size}/ */
                    string requestUrl = "image/" + imageId.ToString() + "/300/300/";
                    thumbArray = await LoadImageArrayAsync(requestUrl, cancelToken);
                    CacheHelper.SaveImageArray(imageId, thumbArray, thumbCacheList, thumbCacheSizeMB);
                }

                thumbnailImage = ConvertByteArrayToImage(thumbArray);
                thumbImageLoadState = LoadState.Loaded;
            }
            catch (OperationCanceledException)
            {
                logger.Debug("LoadThumb has been cancelled");
                thumbnailImage = null;
                thumbImageLoadState = LoadState.NotLoaded;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                thumbImageLoadState = LoadState.Error;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "GeneralImage.LoadThumb()", (int)duration.TotalMilliseconds, ""); }

                OnPropertyChanged("thumbImageLoadState");

                if (thumbnailImage != null)
                    OnPropertyChanged("thumbnailImage");
            }
        }

        async public Task LoadMainCopyImage(CancellationToken cancelToken, List<MainCopyCache> mainCopyCacheList, string folder, int mainCopyCacheSizeMB, GlobalState.ConnectionState connectionState)
        {
            if (imageId == 0)
                return;

            DateTime startTime = DateTime.Now;
            try
            {
                mainImageLoadState = LoadState.Requested;
                OnPropertyChanged("mainImageLoadState");

                string fileName = CacheHelper.GetMainCopyFileName(imageId, mainCopyCacheList, folder);

                if (fileName.Length > 0)
                {
                    mainCopyImage = CreateImageFromFileName(fileName);
                }
                else
                {
                    if (connectionState == GlobalState.ConnectionState.OfflineMode)
                    {
                        mainImageLoadState = LoadState.Error;
                        return;
                    }

                    string requestUrl = "image/" + imageId.ToString() + "/maincopy";
                    byte[] mainImageArray = await LoadImageArrayAsync(requestUrl, cancelToken);
                    CacheHelper.SaveMainCopyToCache(imageId, mainImageArray, mainCopyCacheList, folder, mainCopyCacheSizeMB);
                    mainCopyImage = ConvertByteArrayToImage(mainImageArray);
                }

                mainImageLoadState = LoadState.Loaded;
            }
            catch (OperationCanceledException)
            {
                logger.Debug("LoadMainCopyImage has been cancelled");
                mainCopyImage = null;
                mainImageLoadState = LoadState.NotLoaded;
            }
            catch (Exception ex)
            {
                mainCopyImage = null;
                mainImageLoadState = LoadState.Error;
                logger.Error(ex);
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "GeneralImage.LoadMainCopyImage()", (int)duration.TotalMilliseconds, ""); }
                
                OnPropertyChanged("mainImageLoadState");

                if (mainCopyImage != null)
                    OnPropertyChanged("mainCopyImage");
            }
        }
        #endregion

        #region Meta
        async public Task LoadMeta(bool forceReload, CancellationToken cancelToken, GlobalState.ConnectionState connectionState)
        {
            if (imageId == 0)
                return;

            if (connectionState == GlobalState.ConnectionState.OfflineMode)
            {
                metaLoadState = LoadState.Error;
                return;
            }

            if (metaLoadState == LoadState.Error || metaLoadState == LoadState.NotLoaded || forceReload)
                Meta = await LoadImageMetaAsync(cancelToken);

            if (Meta != null)
                OnPropertyChanged("Meta");
        }

        async public Task SaveMeta(CancellationToken cancelToken)
        {
            if (imageId == 0)
                return;

            await SaveImageMetaAsync(Meta, cancelToken);

            //Dummy logged in value, assumed that the save can only be made when its logged in.
            await LoadMeta(true, cancelToken, GlobalState.ConnectionState.LoggedOn);
        }

        async private Task<ImageMeta> LoadImageMetaAsync(CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                metaLoadState = LoadState.Requested;

                ImageMeta responseMeta = await serverHelper.ImageGetMeta(imageId, cancelToken);
                if (responseMeta == null)
                    throw new Exception("The image meta data could not be retrieved from the server, an unexpected error occured");

                return responseMeta;
            }
            catch (OperationCanceledException)
            {
                metaLoadState = LoadState.Error;
                logger.Debug("LoadImageMetaAsync has been cancelled");
                return null;
            }
            catch (Exception ex)
            {
                metaLoadState = LoadState.Error;
                logger.Error(ex);
                return null;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "GeneralImage.LoadImageMetaAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async private Task SaveImageMetaAsync(ImageMeta imageMeta, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                await serverHelper.ImageUpdateMetaAsync(imageMeta, cancelToken);
            }
            catch (OperationCanceledException)
            {
                metaLoadState = LoadState.Error;
                logger.Debug("SaveImageMetaAsync has been cancelled");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                metaLoadState = LoadState.Error;
                throw ex;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "GeneralImage.SaveImageMetaAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }
        #endregion

        #region Utility Methods
        private Image todelete_GetTempBitmap(string type, bool isThumb)
        {
            //TODO isThumb when new images are available.

            string loadingImagePath = "";
            if (type == "Working")
            {
                loadingImagePath = @"pack://application:,,,/resources/anim/refresh_selected.gif";
                //mainImageLoadState = LoadState.NotLoaded;
            }
            else
            {
                loadingImagePath = @"pack://application:,,,/resources/icons/warning.gif";
                //mainImageLoadState = LoadState.Error;
            }

            BitmapImage loadingImage = new BitmapImage();
            loadingImage.BeginInit();
            loadingImage.DecodePixelWidth = 32;
            loadingImage.UriSource = new Uri(loadingImagePath);
            loadingImage.EndInit();
            loadingImage.Freeze();

            Image newImage = new Image();
            newImage.Source = loadingImage;
            return newImage;
        }

        private Image CreateImageFromFileName(string fileName)
        {
            BitmapImage loadingImage = new BitmapImage();
            loadingImage.BeginInit();
            loadingImage.UriSource = new Uri(fileName);
            loadingImage.EndInit();
            loadingImage.Freeze();

            Image newImage = new Image();
            newImage.Source = loadingImage;
            return newImage;
        }

        async private Task<byte[]> LoadImageArrayAsync(string requestUrl, CancellationToken cancelToken)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                return await serverHelper.GetByteArray(requestUrl, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("LoadImageArrayAsync has been cancelled");
                throw cancelEx;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "GeneralImage.LoadImageArrayAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        private Image ConvertByteArrayToImage(byte[] imageArray)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                Image newImage = new Image();
                using (MemoryStream ms = new MemoryStream(imageArray))
                {
                    var decoder = JpegBitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    newImage.Source = decoder.Frames[0];
                }
                return newImage;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "GeneralImage.ConvertByteArrayToImage()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion
    }
}