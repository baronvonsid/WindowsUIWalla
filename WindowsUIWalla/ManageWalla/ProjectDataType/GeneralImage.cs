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
        private ServerHelper serverHelper;
        //private Image thumbnailImage;

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
        //private LoadState mainImageLoadState;
        private LoadState metaLoadState;


        private enum LoadState
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
            thumbnailImage = GetTempBitmap("Working", true);
            mainCopyImage = GetTempBitmap("Working", false);
            metaLoadState = LoadState.NotLoaded;
        }

        #region Thumb
        async public Task LoadThumb(CancellationToken cancelToken, List<ThumbCache> thumbCacheList)
        {
            if (imageId == 0)
                return;

            try
            {
                byte[] thumbArray = CacheHelper.GetImageArray(imageId, thumbCacheList);
                if (thumbArray == null)
                {
                    /* GET /{userName}/image/{imageId}/{size}/ */
                    string requestUrl = "image/" + imageId.ToString() + "/300/300/";
                    thumbArray = await LoadImageArrayAsync(requestUrl, cancelToken);
                    CacheHelper.SaveImageArray(imageId, thumbArray, thumbCacheList);
                }

                thumbnailImage = ConvertByteArrayToImage(thumbArray);
            }
            catch (OperationCanceledException)
            {
                logger.Debug("LoadThumb has been cancelled");
                thumbnailImage = null;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                thumbnailImage = GetTempBitmap("Error", true);
            }
            finally
            {
                if (thumbnailImage != null)
                    OnPropertyChanged("thumbnailImage");
            }
        }


        #endregion

        #region MainCopy
        async public Task LoadMainCopyImage(CancellationToken cancelToken, List<MainCopyCache> mainCopyCacheList, string folder)
        {
            if (imageId == 0)
                return;

            try
            {
                string fileName = CacheHelper.GetMainCopyFileName(imageId, mainCopyCacheList, folder);

                if (fileName.Length > 0)
                {
                    mainCopyImage = CreateImageFromFileName(fileName);
                }
                else
                {
                    string requestUrl = "image/" + imageId.ToString() + "/maincopy";
                    byte[] mainImageArray = await LoadImageArrayAsync(requestUrl, cancelToken);
                    CacheHelper.SaveMainCopyToCache(imageId, mainImageArray, mainCopyCacheList, folder);
                    mainCopyImage = ConvertByteArrayToImage(mainImageArray);
                }
            }
            catch (OperationCanceledException)
            {
                logger.Debug("LoadMainCopyImage has been cancelled");
                mainCopyImage = null;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                mainCopyImage = GetTempBitmap("Error", false);
            }
            finally
            {
                if (mainCopyImage != null)
                    OnPropertyChanged("mainCopyImage");
            }
        }


        #endregion

        #region Meta
        async public Task LoadMeta(bool forceReload, CancellationToken cancelToken)
        {
            if (imageId == 0)
                return;

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

            await LoadMeta(true, cancelToken);
        }

        async private Task<ImageMeta> LoadImageMetaAsync(CancellationToken cancelToken)
        {
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
        }

        async private Task SaveImageMetaAsync(ImageMeta imageMeta, CancellationToken cancelToken)
        {
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
        }
        #endregion

        #region Utility Methods
        private Image GetTempBitmap(string type, bool isThumb)
        {
            //TODO isThumb when new images are available.

            string loadingImagePath = "";
            if (type == "Working")
            {
                loadingImagePath = @"pack://application:,,,/Icons/LoadingMain.gif";
                //mainImageLoadState = LoadState.NotLoaded;
            }
            else
            {
                loadingImagePath = @"pack://application:,,,/Icons/ErrorMain.gif";
                //mainImageLoadState = LoadState.Error;
            }

            BitmapImage loadingImage = new BitmapImage();
            loadingImage.BeginInit();
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
            try
            {
                return await serverHelper.GetByteArray(requestUrl, cancelToken);
            }
            catch (OperationCanceledException cancelEx)
            {
                logger.Debug("LoadImageArrayAsync has been cancelled");
                throw cancelEx;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw ex;
            }
        }

        private Image ConvertByteArrayToImage(byte[] imageArray)
        {
            Image newImage = new Image();
            using (MemoryStream ms = new MemoryStream(imageArray))
            {
                var decoder = JpegBitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                newImage.Source = decoder.Frames[0];
            }
            return newImage;
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





/*
async private Task<Image> LoadImageMainAsync(CancellationToken cancelToken)
{
    try
    {
        //mainImageLoadState = LoadState.Requested;

        BitmapImage responseImage = await serverHelper.GetMainImage(imageId, cancelToken);
        if (responseImage == null)
            throw new Exception("The image could not be retrieved from the server, an unexpected error occured");

        Image newImage = new Image();
        newImage.Source = responseImage;

        //mainImageLoadState = LoadState.Loaded;
        return newImage;
    }
    catch (OperationCanceledException)
    {
        //mainImageLoadState = LoadState.Error;
        logger.Debug("LoadImageMainAsync has been cancelled");
        return null;
    }
    catch (Exception ex)
    {
        //mainImageLoadState = LoadState.Error;
        logger.Error(ex);
        return GetTempBitmap("Error");
    }
}
*/