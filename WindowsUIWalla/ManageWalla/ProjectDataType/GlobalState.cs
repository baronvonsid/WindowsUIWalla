using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;
using System.Xml;

namespace ManageWalla
{
    [System.SerializableAttribute()]
    public class GlobalState
    {
        public DateTime lastLoggedIn { get; set; }
        public Account account { get; set; }
        public UserApp userApp { get; set; }
        public GalleryOptions galleryOptions { get; set; }

        //Business Objects
        public TagList tagList { get; set; }
        public CategoryList categoryList { get; set; }
        public GalleryList galleryList { get; set; }

        public UploadStatusList uploadStatusList { get; set; }
        public List<ImageList> tagImageList { get; set; }
        public List<ImageList> categoryImageList { get; set; }
        public List<ImageList> galleryImageList { get; set; }
        public List<ImageMeta> imageMetaList { get; set; }

        public GalleryStyleList galleryStyleList { get; set; }
        public GalleryPresentationList galleryPresentationList { get; set; }
        public DataLoadState categoryLoadState { get; set; }
        public DataLoadState tagLoadState { get; set; }
        public DataLoadState galleryLoadState { get; set; }
        public DataLoadState uploadStatusListState { get; set; }

        public ConnectionState connectionState { get; set; }
        public enum ConnectionState
        {
            NoAccount = 0,
            Offline = 1,
            FailedLogin = 2,
            LoggedOn = 3,
            OfflineMode = 4
        }

        public enum DataLoadState
        {
            No = 0,
            Pending = 1,
            LocalCache = 2,
            Loaded = 3,
            Unavailable = 4
        }

        public GlobalState() { }
    }
}
