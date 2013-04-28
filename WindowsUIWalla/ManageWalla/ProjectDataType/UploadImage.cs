using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;

namespace ManageWalla
{
    public class UploadImage
    {
        private string filePath;
        private BitmapImage image;
        private ImageMeta meta;

        public UploadImage(string value)
        {
            filePath = value;

            
            image = LoadBitmap(filePath);
            SetupMeta();
            FolderPath = Path.GetDirectoryName(filePath);

        }

        private void SetupMeta()
        {
            string format = GetFormat(filePath);
            if (format == null)
            { return; }

            FileInfo fileInfo = new FileInfo(filePath);
            meta = new ImageMeta();
            meta.OriginalFileName = Path.GetFileName(filePath);
            meta.Name = Path.GetFileNameWithoutExtension(filePath);
            meta.Format = format;
            meta.Width = image.PixelWidth;
            meta.Height = image.PixelHeight;
            meta.Size = (long)(fileInfo.Length / 1000);
            
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

        private BitmapImage LoadBitmap(string fileName)
        {
            BitmapImage myBitmapImage = new BitmapImage();
            myBitmapImage.BeginInit();
            myBitmapImage.DecodePixelWidth = 100;
            myBitmapImage.UriSource = new Uri(filePath);
            myBitmapImage.EndInit();
            myBitmapImage.Freeze();

            return myBitmapImage;
        }

        public void ResetMeta()
        {
            SetupMeta();
        }

        public ImageMeta Meta
        {
            get {return meta;}
        }

        public String FilePath { get { return filePath; } }
        public BitmapImage Image { get { return image; } }
        public string FolderPath { get; set; }

    }
}



/*
Image myImage = new Image();
myImage.Source = myBitmapImage;
myImage.Style = (Style)FindResource("styleImageThumb");
return myImage;
 */

//image = BitmapFrame.Create(new Uri(value));