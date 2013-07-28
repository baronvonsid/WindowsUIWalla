using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using Shell32;

namespace ManageWalla
{
    public class UploadImage : INotifyPropertyChanged
    {
        private string filePath;
        private BitmapImage image;
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

            image = await LoadBitmapAsync(filePath);

            string format = GetFormat(filePath);
            if (format == null)
            { return; }

            FileInfo fileInfo = new FileInfo(filePath);
            meta = new ImageMeta();
            meta.OriginalFileName = Path.GetFileName(filePath);
            meta.Name = Path.GetFileNameWithoutExtension(filePath);
            meta.Format = format;

            MapFileProperties();

            meta.UdfDate1 = DateTime.Now;
            meta.UdfDate1 = DateTime.Now;
            meta.UdfDate1 = DateTime.Now;
            meta.UploadDate = DateTime.Now;
            meta.TakenDate = DateTime.Now;

            //meta.TakenDate = ;
            //meta.Camera = ;

        }

        private string GetFormat(string fileName)
        {
            switch (Path.GetExtension(fileName).ToUpper())
            {
                case ".JPG":
                case ".JPEG":
                    return "JPEG";
                case ".BMP":
                    return "BMP";
                default:
                    return null;
            }
        }

        async private Task<BitmapImage> LoadBitmapAsync(string filePath)
        {
            //BitmapImage myBitmapImage; // = new BitmapImage();

            FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

            BitmapImage myBitmapImage = await System.Threading.Tasks.Task.Run(() => 
            {
                myBitmapImage = new BitmapImage();
                //BitmapImage myBitmapImage = new BitmapImage(); 
                myBitmapImage.BeginInit(); 
                myBitmapImage.DecodePixelWidth = 300;
                //myBitmapImage.StreamSource = fileStream;
               // myBitmapImage.CacheOption = BitmapCacheOption.OnLoad; 
                //myBitmapImage.StreamSource = fileStream;
                myBitmapImage.UriSource = new Uri(filePath);
                myBitmapImage.EndInit();
                myBitmapImage.Freeze();

                return myBitmapImage;
            });

            return myBitmapImage;
         //A custom class that reads the bytes of off the HD and shoves them into the MemoryStream. You could just replace the MemoryStream with something like this: FileStream fs = File.Open(@"C:\ImageFileName.jpg", FileMode.Open);
            /*
            myBitmapImage.BeginInit();
            myBitmapImage.DecodePixelWidth = 200;
            myBitmapImage.StreamSource = fileStream;
            //myBitmapImage.UriSource = new Uri(filePath);
            myBitmapImage.EndInit();
            myBitmapImage.Freeze();

            return new Task<myBitmapImage>;
             */ 
        }

        private void MapFileProperties()
        {
            
            FileInfo file = new FileInfo(filePath);
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
            meta.Width = image.PixelWidth;
            meta.Height = image.PixelHeight;
            meta.Size = (long)(file.Length / 1000);
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
        public BitmapImage Image { get { return image; } }
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
                        return null;
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