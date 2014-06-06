using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using Shell32;
using System.Windows.Controls;
using System.Windows.Forms;
using log4net;
using System.Threading;

namespace ManageWalla
{
    [System.SerializableAttribute()]
    public class GalleryPresentationItem
    {
        private byte[] imageArray { get; set; }
        public int PresentationId { get; set; }
        public String Name { get; set; }
        public String Desc { get; set; }
        public int MaxSections { get; set; }

        public Image PresentationImage
        {
            get
            {
                if (imageArray != null && imageArray.Length > 0)
                {
                    return ConvertByteArrayToImage(imageArray);
                }
                else
                {
                    return null;
                }
            }
        }

        async public Task LoadImageAsync(CancellationToken cancelToken, ServerHelper serverHelper)
        {
            if (imageArray == null)
            {
                string requestUrl = "appimage/Presentation-" + PresentationId.ToString() + "/200/100";
                imageArray = await serverHelper.GetByteArray(requestUrl, cancelToken);
            }
        }

        private Image ConvertByteArrayToImage(byte[] imageArray)
        {
            Image newImage = new Image();
            using (MemoryStream ms = new MemoryStream(imageArray))
            {
                var decoder = JpegBitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                newImage.Source = decoder.Frames[0];
            }
            return newImage;
        }
    }
}
