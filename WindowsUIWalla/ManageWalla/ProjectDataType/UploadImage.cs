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
        private string filePath;
        private Image image;
        private ImageMeta meta;

        public enum UploadState
        {
            None = 0,
            Success = 1,
            Error = 2
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public UploadImage()
        {

        }

        async public Task Setup(string value)
        {
            State = UploadImage.UploadState.None;
            UploadError = "";
            filePath = value;
            FolderPath = Path.GetDirectoryName(filePath);
            image = new Image();

            string format = GetFormat(filePath);
            if (format == null)
            {
                UploadError = "Format is not supported (" + Path.GetExtension(filePath).ToUpper().Substring(1) + "), image is excluded from Upload";
                State = UploadState.Error;
                image = UnavailableBitmapThumbnail(false);
            }
            else
            {
                image = await LoadBitmapAsync(filePath, format);
            }

            meta = new ImageMeta();

            FileInfo fileInfo = new FileInfo(filePath);
            meta.OriginalFileName = Path.GetFileName(filePath);
            meta.Name = Path.GetFileNameWithoutExtension(filePath);
            meta.Format = format;
            meta.LocalPath = filePath;
            meta.MachineId = 500001;

            //MapFileProperties();

            meta.UploadDate = DateTime.Now;
            meta.TakenDateFile = fileInfo.LastWriteTime;

            meta.TakenDateMeta = DateTime.Parse("01/01/1900");
            meta.UdfDate1 = DateTime.Parse("01/01/1900");
            meta.UdfDate2 = DateTime.Parse("01/01/1900");
            meta.UdfDate3 = DateTime.Parse("01/01/1900");

            //meta.Width = image.PixelWidth;
            //meta.Height = image.PixelHeight;
            meta.Size = fileInfo.Length;
            
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
                loadingImagePath = @"pack://application:,,,/Icons/UnavailableThumbnail.gif";
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
            //FileStream fileStream = null;
            //MemoryStream memoryStream = null;

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
                        myBitmapImage.DecodePixelHeight = 300;
                    else
                        myBitmapImage.DecodePixelWidth = 300;
                    
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
/*
                myBitmapImage = new BitmapImage();
                myBitmapImage.BeginInit();

                myBitmapImage.DecodePixelWidth = 300;
                //myBitmapImage.DecodePixelHeight = 300;
                //myBitmapImage.CacheOption = BitmapCacheOption.OnLoad; 
                myBitmapImage.SourceRect = croppedBitmap.SourceRect;
                myBitmapImage.EndInit();
                myBitmapImage.Freeze();
                
                /*
                myBitmapImage = await System.Threading.Tasks.Task.Run(() =>
                {


                    return myBitmapImage;
                });

                 
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                memoryStream = new MemoryStream();
                myBitmapImage = new BitmapImage();

                encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));
                encoder.Save(memoryStream);

                //myBitmapImage = null;
                myBitmapImage.BeginInit();
                myBitmapImage.DecodePixelWidth = 300;
                myBitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
                myBitmapImage.EndInit();
                myBitmapImage.Freeze();

                //memoryStream.Close();

                


                return myBitmapImage;
                */
            }
            catch (Exception ex)
            {
                UploadError = "Error converting image to Thumbnail." + ex.Message;
                State = UploadState.Error;
                return UnavailableBitmapThumbnail(false);
            }
            finally
            {
                //if (fileStream != null) { fileStream.Close(); }
                //if (memoryStream != null) { memoryStream.Close(); }
            }
        }




        //TODO delete
        private void MapFileProperties()
        {
            
            //FileInfo file = new FileInfo(filePath);
            //meta.Width = image.
            //meta.Height = image.PixelHeight;
            //meta.Size = file.Length;

            /*
            List<string> arrHeaders = new List<string>();

            Shell32.Shell shell = new Shell32.Shell();
            Shell32.Folder objFolder;

            objFolder = shell.NameSpace(Path.GetDirectoryName(filePath));

            for (int i = 0; i < short.MaxValue; i++)
            {
                string header = objFolder.GetDetailsOf(null, i);
                if (String.IsNullOrEmpty(header))
                    break;
                arrHeaders.Add(header);
            }

            foreach (Shell32.FolderItem2 item in objFolder.Items())
            {
                for (int i = 0; i < arrHeaders.Count; i++)
                {
                    Console.WriteLine("{0}\t{1}: {2}", i, arrHeaders[i], objFolder.GetDetailsOf(item, i));
                }
            }
            */
              
            /* TODO
                Camera model: Canon PowerShot S95
                Dimensions: ?3648 x 2736?
                Item type: JPEG Image
                Size: 1.89 MB
                Date taken: ?16/?12/?2012 ??13:40
                Comments:
                File description: 
                160	Bit depth: 24
                161	Horizontal resolution: ?180 dpi
                162	Width: ?3648 pixels
                163	Vertical resolution: ?180 dpi
                164	Height: ?2736 pixel
             * 
             */

        }

        async public Task ResetMeta()
        {
            await Setup(filePath);
        }

        public ImageMeta Meta
        {
            get {return meta;}
        }

        public String FilePath { get { return filePath; } }
        public Image Image { get { return image; } }
        public string FolderPath { get; set; } 
        public UploadState State { get; set; }
        public String UploadError { get; set; }

        public string HttpFormat
        {
            
            get
            {
                switch (meta.Format)
                {
                    case "JPEG":
                        return "image/jpeg";
                    default:
                        return "image/jpeg";
                }
            }
        }
    }
}



/*
Image myImage = new Image();
myImage.Source = myBitmapImage;
myImage.Style = (Style)FindResource("styleImageThumb");
return myImage;
 */

//image = BitmapFrame.Create(new Uri(value));