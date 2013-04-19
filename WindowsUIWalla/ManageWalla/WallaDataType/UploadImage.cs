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
            FileInfo fileInfo = new FileInfo(filePath);
            
            string format = GetFormat(filePath);
            if (format == null)
            { return; }

            image = LoadBitmap(filePath);

            meta = new ImageMeta();
            meta.OriginalFileName = Path.GetFileName(value);
            meta.Name = Path.GetFileNameWithoutExtension(value);
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



        public ImageMeta Meta { get; set; }
        public String FilePath { get { return filePath; } }
        public BitmapImage Image { get { return image; } }


    }
}



/*
Image myImage = new Image();
myImage.Source = myBitmapImage;
myImage.Style = (Style)FindResource("styleImageThumb");
return myImage;
 */

//image = BitmapFrame.Create(new Uri(value));