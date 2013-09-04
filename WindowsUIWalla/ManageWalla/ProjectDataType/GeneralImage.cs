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
    public class GeneralImage : INotifyPropertyChanged
    {
        private ServerHelper serverHelper;
        private BitmapImage image;

        public long imageId;
        public String FilePath { get; set; }
        public BitmapImage Image { get { return image; } }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime UploadDate { get; set; }

        //TODO need this extra event plumbing ??
        public event PropertyChangedEventHandler PropertyChanged;

        public GeneralImage(ServerHelper serverHelperParam) 
        {
            serverHelper = serverHelperParam;
            image = LoadingBitmap();
        }

        async public Task LoadImage()
        {
            if (FilePath == null && imageId == 0)
                return;

            image = await LoadBitmapAsync();
            OnPropertyChanged("Image");
        }

        private BitmapImage LoadingBitmap()
        {
            //const string loadingImagePath = @"/Icons/loading.gif";
            const string loadingImagePath = @"pack://application:,,,/Icons/loading.gif";
         
            BitmapImage loadingImage = new BitmapImage();
            loadingImage.BeginInit();
            loadingImage.DecodePixelWidth = 250;
            loadingImage.UriSource = new Uri(loadingImagePath);
            loadingImage.EndInit();
            loadingImage.Freeze();

            return loadingImage;
        }

        async private Task<BitmapImage> LoadBitmapAsync()
        {
            if (File.Exists(FilePath ?? ""))
            {
                //Local version exists, nice !

                //FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

                BitmapImage myBitmapImage = await System.Threading.Tasks.Task.Run(() =>
                {
                    myBitmapImage = new BitmapImage();
                    myBitmapImage.BeginInit();
                    myBitmapImage.DecodePixelWidth = 250;
                    myBitmapImage.UriSource = new Uri(FilePath);
                    myBitmapImage.EndInit();
                    myBitmapImage.Freeze();

                    return myBitmapImage;
                });

                return myBitmapImage;
            }
            else
            {
                return await serverHelper.GetImage(imageId, 250);
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
