using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Media;

namespace ManageWalla
{
    public class MainController
    {

        private MainWindow currentMain;

        public MainController(MainWindow currentMainParam)
        {
            currentMain = currentMainParam;
        }


        /// <summary>
        /// For each entity - Category, Tag, View List, Account Settings
        /// Check local cache for entries and check Walla Hub for updates
        /// Then refresh local data caches.
        /// </summary>
        public void RetrieveGeneralUserConfig()
        {


            GetCategoryTree();
        }

        public void PopulateImagePane()
        {
            //Assume a list of images were passed in.
            //Retreive from local cache or the web server if not present.

            DirectoryInfo imageDirectory = new DirectoryInfo(@"C:\Users\scansick\Pictures");

            foreach (FileInfo file in imageDirectory.GetFiles())
            {

                System.Windows.Media.Imaging.JpegBitmapDecoder newJpeg = new System.Windows.Media.Imaging.JpegBitmapDecoder(file.OpenRead(), System.Windows.Media.Imaging.BitmapCreateOptions.None, System.Windows.Media.Imaging.BitmapCacheOption.None);

                System.Windows.Media.Imaging.BitmapImage image = new System.Windows.Media.Imaging.BitmapImage();
                image.StreamSource = file.OpenRead();

                

                System.Windows.UIElement element = new System.Windows.UIElement();
                
                //System.Windows.Media.Imaging.BitmapImage image = new System.Windows.Media.Imaging.BitmapImage();
                //image.
                //currentMain.wrapImages.Children.Add(image);


            }

        }

        private void GetCategoryTree()
        {
            currentMain.RefreshCategoryTreeView();
        }


    }
}
