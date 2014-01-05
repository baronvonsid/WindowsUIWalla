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

        public long imageId;
        public long categoryId { get; set; }
        public Image thumbnailImage { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime UploadDate { get; set; }
        public int MetaVersion { get; set; }

        private static readonly ILog logger = LogManager.GetLogger(typeof(GeneralImage));

        //TODO need this extra event plumbing ??
        public event PropertyChangedEventHandler PropertyChanged;

        public GeneralImage(ServerHelper serverHelperParam) 
        {
            serverHelper = serverHelperParam;
            thumbnailImage = WorkingBitmapThumbnail("Working");
        }

        async public Task LoadImage(CancellationToken cancelToken, ThumbState thumbState)
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
            BitmapImage responseImage = null;

            //TODO
            //Check local cache for thumbnail.  If exists then use.
            try
            {
                if (1 > 2)
                {
                    //Local version exists, nice !

                    //FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
                    /*
                    BitmapImage myBitmapImage = await System.Threading.Tasks.Task.Run(() =>
                    {
                        myBitmapImage = new BitmapImage();
                        myBitmapImage.BeginInit();
                        myBitmapImage.DecodePixelWidth = 250;
                        myBitmapImage.UriSource = new Uri("");
                        myBitmapImage.EndInit();
                        myBitmapImage.Freeze();

                        return myBitmapImage;
                    });

                    return myBitmapImage;
                     * */
                    return null;
                }
                else
                {
                    responseImage = await serverHelper.GetImage(imageId, 300, 300, cancelToken);
                    if (responseImage == null)
                        throw new Exception("The thumbnail could not be retrieved from the server, an unexpected error occured");
                }

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
