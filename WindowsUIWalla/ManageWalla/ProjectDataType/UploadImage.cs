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

namespace ManageWalla
{
    public class UploadImage : INotifyPropertyChanged
    {
        private Image image;
        private ImageMeta meta;

        //None 0, File received 1, Awaiting processing 2, Being processed 3, Complete 4, Inactive 5
        public enum UploadState
        {
            None = 0,
            FileReceived = 1,
            AwaitingProcessed = 2,
            BeingProcessed = 3,
            Complete = 4,
            Inactive = 5
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public String FilePath { get; set; }
        public Image Image { get { return image; } }
        public string FolderPath { get; set; }

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
                meta.Name = Path.GetFileNameWithoutExtension(FilePath);
                meta.Format = format;

                meta.UploadDate = DateTime.Now;
                meta.TakenDateFile = fileInfo.LastWriteTime;

                meta.TakenDateMeta = DateTime.Parse("01/01/1900");
                meta.UdfDate1 = DateTime.Parse("01/01/1900");
                meta.UdfDate2 = DateTime.Parse("01/01/1900");
                meta.UdfDate3 = DateTime.Parse("01/01/1900");

                meta.Size = fileInfo.Length;

                return "OK";
            }
            catch (Exception ex)
            {
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
            switch (format)
            {
                case "JPG":
                case "TIF":
                case "PNG":
                case "BMP":
                case "GIF":
                    break;
                default:
                    return UnavailableBitmapThumbnail(true);
            }

            FileInfo fileInfo = new FileInfo(filePath);

            //10 MB.
            if (fileInfo.Length > 10485760)
                return UnavailableBitmapThumbnail(true);

            bool isLandscape = await IsLandscape(filePath);

            BitmapImage myBitmapImage = await System.Threading.Tasks.Task.Run(() =>
            {
                myBitmapImage = new BitmapImage();
                myBitmapImage.BeginInit();

                if (isLandscape)
                    myBitmapImage.DecodePixelHeight = 140;
                else
                    myBitmapImage.DecodePixelWidth = 140;
                    
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

            return image;
        }

        async public Task ResetMeta()
        {
            await Setup(FilePath, false);
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
        #endregion
    }
}