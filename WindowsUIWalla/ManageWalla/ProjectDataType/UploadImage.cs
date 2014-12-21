using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using Shell32;
using System.Windows;
using System.Windows.Controls;
using log4net;
using log4net.Config;

namespace ManageWalla
{
    public class UploadImage // : INotifyPropertyChanged
    {
        private Image image;
        private ImageMeta meta;
        private static readonly ILog logger = LogManager.GetLogger(typeof(UploadImage));

        //None 0, File received 1, Awaiting processing 2, Being processed 3, Complete 4, Inactive 5
        public enum ImageStatus
        {
            None = 0,
            FileReceived = 1,
            AwaitingProcessed = 2,
            BeingProcessed = 3,
            Complete = 4,
            Inactive = 5
        }

        public enum ImageViewState
        {
            Loaded = 0,
            NoPreview = 1,
            Error = 2
        }

        //public event PropertyChangedEventHandler PropertyChanged;

        public String FilePath { get; set; }
        public Image Image { get { return image; } }
        public string FolderPath { get; set; }
        public ImageViewState thumbPreviewState { get; set; }

        //public UploadState State { get; set; }
        //public String UploadError { get; set; }

        public ImageMeta Meta
        {
            get { return meta; }
        }

        #region Methods
        async public Task<string> Setup(string path, bool loadImage)
        {
            try
            {
                FolderPath = Path.GetDirectoryName(path);
                FilePath = path;
                string format = GetFormat(FilePath);

                if (loadImage)
                {
                    image = new Image();    
                    if (format == null)
                    {
                        throw new Exception("Format is not supported (" + Path.GetExtension(FilePath).ToUpper().Substring(1) + "), image is excluded from Upload");
                    }
                    else
                    {
                        image = await LoadBitmapAsync(FilePath, format);
                    }
                }
                meta = new ImageMeta();

                FileInfo fileInfo = new FileInfo(FilePath);
                meta.OriginalFileName = Path.GetFileName(FilePath);

                meta.Name = Path.GetFileNameWithoutExtension(FilePath).Trim();
                if (meta.Name.Length > 30)
                    meta.Name = meta.Name.Substring(0, 30);
                
                meta.Format = format;

                meta.UploadDate = DateTime.Now;

                if (fileInfo.LastWriteTime > DateTime.Now.AddYears(200))
                    meta.TakenDateFile = fileInfo.LastWriteTime;
                else
                    meta.TakenDateFile = DateTime.Now;

                meta.TakenDate = DateTime.Now;
                meta.TakenDateSet = false;

                meta.UdfDate1 = DateTime.Now;
                meta.UdfDate2 = DateTime.Now;
                meta.UdfDate3 = DateTime.Now;

                meta.Size = fileInfo.Length;

                return "OK";
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return ex.Message;
            }
        }

        async private Task<bool> IsLandscape(string filePath)
        {
            BitmapImage myBitmapImage = await System.Threading.Tasks.Task.Run(() =>
            {
                myBitmapImage = new BitmapImage();
                myBitmapImage.BeginInit();

                myBitmapImage.DecodePixelWidth = 10;
                myBitmapImage.UriSource = new Uri(filePath);
                myBitmapImage.EndInit();
                myBitmapImage.Freeze();

                return myBitmapImage;
            });

           

            if (myBitmapImage.PixelHeight > myBitmapImage.PixelWidth)
                return false;
            else
                return true;
        }

        async private Task<Image> LoadBitmapAsync(string filePath, string format)
        {
            DateTime startTime = DateTime.Now;
            try
            {
                switch (format)
                {
                    case "JPG":
                    case "TIF":
                    case "PNG":
                    case "BMP":
                    case "GIF":
                        break;
                    default:
                        thumbPreviewState = ImageViewState.NoPreview;
                        return null;
                }

                FileInfo fileInfo = new FileInfo(filePath);

                //10 MB.
                if (fileInfo.Length > 10485760)
                {
                    thumbPreviewState = ImageViewState.NoPreview;
                    return null;
                }

                bool isLandscape = await IsLandscape(filePath);

                BitmapImage myBitmapImage = await System.Threading.Tasks.Task.Run(() =>
                {
                    myBitmapImage = new BitmapImage();
                    myBitmapImage.BeginInit();

                    if (isLandscape)
                        myBitmapImage.DecodePixelHeight = 130;
                    else
                        myBitmapImage.DecodePixelWidth = 130;

                    myBitmapImage.UriSource = new Uri(filePath);
                    myBitmapImage.EndInit();
                    myBitmapImage.Freeze();

                    return myBitmapImage;
                });

                int startX = 0;
                int startY = 0;
                int width = 0;
                int height = 0;

                if (isLandscape)
                {
                    double remainder = myBitmapImage.PixelWidth - myBitmapImage.PixelHeight;
                    startX = Convert.ToInt32(remainder / 2.0);
                    startY = 0;
                    width = Convert.ToInt32(myBitmapImage.PixelHeight);
                    height = Convert.ToInt32(myBitmapImage.PixelHeight);
                }
                else
                {
                    //Portrait, so crop the tops and bottoms.
                    double remainder = myBitmapImage.PixelHeight - myBitmapImage.PixelWidth;
                    startX = 0;
                    startY = Convert.ToInt32(remainder / 2.0);
                    width = Convert.ToInt32(myBitmapImage.PixelWidth);
                    height = Convert.ToInt32(myBitmapImage.PixelWidth);
                }

                CroppedBitmap croppedBitmap = new CroppedBitmap(myBitmapImage, new Int32Rect(startX, startY, width, height));
                Image image = new Image();
                image.Source = croppedBitmap;

                thumbPreviewState = ImageViewState.Loaded;

                return image;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                thumbPreviewState = ImageViewState.Error;
                return null;
            }
            finally
            {
                TimeSpan duration = DateTime.Now - startTime;
                if (logger.IsDebugEnabled) { logger.DebugFormat("Method: {0} Duration {1}ms Param: {2}", "UploadImage.LoadBitmapAsync()", (int)duration.TotalMilliseconds, ""); }
            }
        }

        async public Task ResetMeta()
        {
            string response = await Setup(FilePath, false);
            if (response != "OK")
                throw new Exception(response);
        }

        private string GetFormat(string fileName)
        {
            //JPG,JPEG,TIF,TIFF,PSD,PNG,BMP,GIF,CR2,ARW,NEF
            //Need to investigate CRW/NEF/ORF/RW2 and other RAW types.
            string extension = Path.GetExtension(fileName).ToUpper().Substring(1);

            switch (extension)
            {
                case "JPG":
                case "JPEG":
                    return "JPG";
                case "TIF":
                case "TIFF":
                    return "TIF";
                case "PSD":
                case "PNG":
                case "BMP":
                case "GIF":
                case "CR2":
                case "ARW":
                case "NEF":
                    return extension;
                default:
                    return null;
            }
        }

        /*
        private Image UnavailableBitmapThumbnail(bool unavailable)
        {
            string loadingImagePath = "";
            if (unavailable)
            {
                loadingImagePath = @"pack://application:,,,/resources/icons/Error.gif";
            }
            else
            {
                loadingImagePath = @"pack://application:,,,/resources/icons/warning.gif";
            }

            BitmapImage loadingImage = new BitmapImage();
            loadingImage.BeginInit();
            loadingImage.DecodePixelWidth = 32;
            loadingImage.UriSource = new Uri(loadingImagePath);
            loadingImage.EndInit();
            loadingImage.Freeze();

            Image newImage = new Image();
            newImage.Source = loadingImage;
            newImage.MaxHeight = 32.0;

            return newImage;
        }
        */
        #endregion
    }
}