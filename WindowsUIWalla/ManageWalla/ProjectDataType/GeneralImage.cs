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
        private Image mainImageStore;
        public ImageMeta Meta { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string shotSummary { get; set; }
        public string fileSummary { get; set; }
        public DateTime uploadDate { get; set; }
        public int metaVersion { get; set; }
        private LoadState mainImageLoadState;
        private LoadState metaLoadState;


        private enum LoadState
        {
            NotLoaded = 0,
            Requested = 1,
            Loaded = 2,
            Error = 3
        }

        private static readonly ILog logger = LogManager.GetLogger(typeof(GeneralImage));

        //TODO need this extra event plumbing ??
        public event PropertyChangedEventHandler PropertyChanged;

        public GeneralImage(ServerHelper serverHelperParam) 
        {
            serverHelper = serverHelperParam;
            thumbnailImage = WorkingBitmapThumbnail("Working");
            mainImageStore = WorkingBitmapMain("Working");
            metaLoadState = LoadState.NotLoaded;
        }

        async public Task LoadThumb(CancellationToken cancelToken, ThumbState thumbState)
        {
            if (imageId == 0)
                return;

            Image newThumb = ThumbState.GetImage(imageId);
            if (newThumb == null)
            {
                newThumb = await LoadThumbnailAsync(cancelToken);
                if (newThumb != null)
                    ThumbState.SaveImage(imageId, newThumb);
            }

            thumbnailImage = newThumb;

            if (thumbnailImage != null)
                OnPropertyChanged("thumbnailImage");
        }

        async public Task LoadMainImage(CancellationToken cancelToken)
        {
            if (imageId == 0)
                return;

            if (mainImageLoadState != LoadState.Loaded && mainImageLoadState != LoadState.Requested)
                mainImageStore = await LoadImageMainAsync(cancelToken);

            if (mainImageStore != null)
                OnPropertyChanged("mainImage");
        }

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

        private Image WorkingBitmapThumbnail(string type)
        {
            string loadingImagePath = "";
            if (type == "Working")
            {
                loadingImagePath = @"pack://application:,,,/Icons/LoadingThumbnail.gif";
            }
            else
            {
                loadingImagePath = @"pack://application:,,,/Icons/ErrorThumbnail.gif";
            }

            BitmapImage loadingImage = new BitmapImage();
            loadingImage.BeginInit();
            loadingImage.DecodePixelWidth = 300;
            loadingImage.UriSource = new Uri(loadingImagePath);
            loadingImage.EndInit();
            loadingImage.Freeze();

            Image newImage = new Image();
            newImage.Source = loadingImage;
            return newImage;
        }

        async private Task<Image> LoadThumbnailAsync(CancellationToken cancelToken)
        {
            try
            {
                BitmapImage responseImage = await serverHelper.GetImage(imageId, 300, 300, cancelToken);
                if (responseImage == null)
                    throw new Exception("The thumbnail could not be retrieved from the server, an unexpected error occured");

                Image newImage = new Image();
                newImage.Source = responseImage;
                return newImage;
            }
            catch (OperationCanceledException)
            {
                logger.Debug("LoadThumbnailAsync has been cancelled");
                return null;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return WorkingBitmapThumbnail("Error");
            }
        }

        private Image WorkingBitmapMain(string type)
        {
            string loadingImagePath = "";
            if (type == "Working")
            {
                loadingImagePath = @"pack://application:,,,/Icons/LoadingMain.gif";
                mainImageLoadState = LoadState.NotLoaded;
            }
            else
            {
                loadingImagePath = @"pack://application:,,,/Icons/ErrorMain.gif";
                mainImageLoadState = LoadState.Error;
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

        public Image mainImage
        {
            get
            {
                if (mainImageLoadState == LoadState.NotLoaded)
                {
                    //CancellationToken cancelToken = new CancellationToken();
                    //mainImageStore = LoadImageMainAsync(cancelToken).Result;
                }
                return mainImageStore;
            }
        }


        async private Task<Image> LoadImageMainAsync(CancellationToken cancelToken)
        {
            try
            {
                mainImageLoadState = LoadState.Requested;

                BitmapImage responseImage = await serverHelper.GetMainImage(imageId, cancelToken);
                if (responseImage == null)
                    throw new Exception("The image could not be retrieved from the server, an unexpected error occured");

                Image newImage = new Image();
                newImage.Source = responseImage;

                mainImageLoadState = LoadState.Loaded;
                return newImage;
            }
            catch (OperationCanceledException)
            {
                mainImageLoadState = LoadState.Error;
                logger.Debug("LoadImageMainAsync has been cancelled");
                return null;
            }
            catch (Exception ex)
            {
                mainImageLoadState = LoadState.Error;
                logger.Error(ex);
                return WorkingBitmapMain("Error");
            }
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
                string response = await serverHelper.ImageUpdateMetaAsync(imageMeta, cancelToken);
                if (response == null)
                    throw new Exception("The image meta data could not be saved.  An unexpected error occured: " + response);
            }
            catch (OperationCanceledException)
            {
                logger.Debug("SaveImageMetaAsync has been cancelled");
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        #region Propery Events
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
