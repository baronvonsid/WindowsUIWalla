using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Runtime.Serialization;
using System.Windows.Media.Imaging;
using System.Xml;

namespace ManageWalla
{
    using System.Xml.Serialization;

    [System.SerializableAttribute()]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.example.org/GlobalState", IsNullable = false)]
    public class GlobalState
    {
        //Infra Properties
        static GlobalState state = null;
        public string userName { get; set; }
        public string password { get; set; }
        //public bool online { get; set; }
        public int platformId { get; set; }
        public string machineName { get; set; }
        public long machineId { get; set; }
        public int imageFetchSize { get; set; }
        public DateTime lastLoggedIn { get; set; }

        //Complex-ify !!!
        private static byte[] key = { 1, 2, 3, 4, 5, 6, 7, 8 };
        private static byte[] iv = { 1, 2, 3, 4, 5, 6, 7, 8 };

        //Business Objects
        public TagList tagList { get; set; }
        public CategoryList categoryList { get; set; }
        public GalleryList galleryList { get; set; }

        public String categoryXml { get; set; }
        public UploadStatusList uploadStatusList { get; set; }
        public List<ImageList> tagImageList { get; set; }
        public List<ImageList> categoryImageList { get; set; }
        public List<ImageList> galleryImageList { get; set; }

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
            LoggedOn = 3
        }

        public enum DataLoadState
        {
            No = 0,
            Pending = 1,
            LocalCache = 2,
            Loaded = 3,
            Unavailable = 4
        }

        #region InfraCodes
        public GlobalState() { }

        public static GlobalState GetState()
        {
            // Try to load from File
            state = RetreiveFromFile();
            if (state == null)
            {
                state = new GlobalState();

                state.tagImageList = new List<ImageList>();
                state.categoryImageList = new List<ImageList>();
                state.galleryImageList = new List<ImageList>();
            }

            //TODO - delete
            state.userName = "Simon";
            state.password = "simon";
            state.imageFetchSize = 50;

            state.categoryLoadState = GlobalState.DataLoadState.No;
            state.tagLoadState = GlobalState.DataLoadState.No;
            state.galleryLoadState = GlobalState.DataLoadState.No;
            state.uploadStatusListState = GlobalState.DataLoadState.No;

            return state;
        }

        private static GlobalState RetreiveFromFile()
        {
            string fileName = Path.Combine(Application.UserAppDataPath, "Walla-LocalCache.config");
            GlobalState stateTemp = null;

            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            if (File.Exists(fileName))
            {
                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    var cryptoStream = new CryptoStream(fs, des.CreateDecryptor(key, iv), CryptoStreamMode.Read);
                    BinaryFormatter formatter = new BinaryFormatter();
                    stateTemp = (GlobalState)formatter.Deserialize(cryptoStream);
                }

                return stateTemp;
            }

            return null;
        }

        public void SaveState()
        {
            string fileName = Path.Combine(Application.UserAppDataPath, "Walla-LocalCache.config");
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                var cryptoStream = new CryptoStream(fs, des.CreateEncryptor(key, iv), CryptoStreamMode.Write);
                BinaryFormatter formatter = new BinaryFormatter();

                // This is where you serialize the class
                formatter.Serialize(cryptoStream, this);
                cryptoStream.FlushFinalBlock();
            }
        }
        #endregion

        /*
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new NotImplementedException();
        }
         */ 
    }
}
