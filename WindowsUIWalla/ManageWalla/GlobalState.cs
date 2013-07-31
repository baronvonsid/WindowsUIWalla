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
        public string userName {get; set;}
        private static byte[] key = { 1, 2, 3, 4, 5, 6, 7, 8 };
        private static byte[] iv = { 1, 2, 3, 4, 5, 6, 7, 8 };
        //public UploadImageFileList uploadImageFileList {get; set;}

        //Business Objects
        public TagList tagList { get; set; }
        public String categoryXml { get; set; }
        public String uploadStatusListXml { get; set; }

        public DataLoadState categoryLoadState { get; set; }
        public DataLoadState tagLoadState { get; set; }
        public DataLoadState viewLoadState { get; set; }
        public DataLoadState uploadStatusListState { get; set; }

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

        public static GlobalState GetState(string userNameParam)
        {
            // Try to load from File
            state = RetreiveFromFile(userNameParam);
            if (state == null)
            {
                state = new GlobalState();
                state.userName = userNameParam;
            }

            return state;
        }

        private static GlobalState RetreiveFromFile(string userNameParam)
        {
            string fileName = Path.Combine(Application.UserAppDataPath, "Walla-" + userNameParam + ".config");
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

                if (stateTemp.userName == userNameParam)
                {
                    return stateTemp;
                }
            }

            return null;
        }

        public void SaveState()
        {

            string fileName = Path.Combine(Application.UserAppDataPath, "Walla-" + userName + ".config");
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
