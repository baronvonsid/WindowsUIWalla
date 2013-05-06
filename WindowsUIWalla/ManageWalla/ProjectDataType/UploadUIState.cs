using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;


namespace ManageWalla
{
    public class UploadUIState : INotifyPropertyChanged
    {
        #region Variables
        private bool mapToSubFolders = false;
        private bool gotSubFolders = false;
        private bool uploading = false;
        private long categoryId = 1;
        private string categoryPath = null;
        private bool uploadToNewCategory = false;
        private string categoryName = null;
        private string categoryDesc = null;

        private string metaCamera = null;
		private string metaUdfChar1 = null;
		private string metaUdfChar2 = null;
		private string metaUdfChar3 = null;
		private string metaUdfText1 = null;
		private decimal metaUdfNum1 = 0;
		private decimal metaUdfNum2 = 0;
		private decimal metaUdfNum3 = 0;
		private DateTime metaUdfDate1;
        private DateTime metaUdfDate2;
        private DateTime metaUdfDate3;
        private DateTime metaTakenDate;
        private ImageMetaTagRef[] metaTagRef = null;

        private bool metaCameraAll = false;
        private bool metaUdfChar1All = false;
        private bool metaUdfChar2All = false;
        private bool metaUdfChar3All = false;
        private bool metaUdfText1All = false;
        private bool metaUdfNum1All = false;
        private bool metaUdfNum2All = false;
        private bool metaUdfNum3All = false;
        private bool metaUdfDate1All = false;
        private bool metaUdfDate2All = false;
        private bool metaUdfDate3All = false;
        private bool metaTakenDateAll = false;
        private bool metaTagRefAll = false;

        public enum UploadMode
        {
            None = 0,
            Folder = 1,
            Images = 2
        }

        public event PropertyChangedEventHandler PropertyChanged;
        #endregion

        #region General Upload attributes
        public bool MapToSubFolders {
            get { return mapToSubFolders; }
            set
            {
                mapToSubFolders = value;
                OnPropertyChanged("MapToSubFolders");
            }
        }

        public bool GotSubFolders
        {
            get { return gotSubFolders; }
            set
            {
                gotSubFolders = value;
                OnPropertyChanged("GotSubFolders");
            }
        }

        public long CategoryId
        {
            get { return categoryId; }
            set
            {
                categoryId = value;
                OnPropertyChanged("CategoryId");
            }
        }

        public string CategoryPath
        {
            get { return categoryPath; }
            set
            {
                categoryPath = value;
                OnPropertyChanged("CategoryPath");
            }
        }

        public bool UploadToNewCategory
        {
            get { return uploadToNewCategory; }
            set
            {
                uploadToNewCategory = value;
                OnPropertyChanged("UploadToNewCategory");
            }
        }

        public string CategoryName
        {
            get { return categoryName; }
            set
            {
                categoryName = value;
                OnPropertyChanged("CategoryName");
            }
        }

        public string CategoryDesc
        {
            get { return categoryDesc; }
            set
            {
                categoryDesc = value;
                OnPropertyChanged("CategoryDesc");
            }
        }

        public UploadMode Mode { get; set; }

        public string RootFolder { get; set; }

        public bool Uploading {
            get { return uploading; }
            set { uploading = value; }
        }

        #endregion

        #region Meta attributes
        public string MetaCamera
        {
            get { return metaCamera; }
            set
            {
                metaCamera = value;
                OnPropertyChanged("MetaCamera");
            }
        }

        public string MetaUdfChar1
        {
            get { return metaUdfChar1; }
            set
            {
                metaUdfChar1 = value;
                OnPropertyChanged("MetaUdfChar1");
            }
        }

        public string MetaUdfChar2
        {
            get { return metaUdfChar2; }
            set
            {
                metaUdfChar2 = value;
                OnPropertyChanged("MetaUdfChar2");
            }
        }

        public string MetaUdfChar3
        {
            get { return metaUdfChar3; }
            set
            {
                metaUdfChar3 = value;
                OnPropertyChanged("MetaUdfChar3");
            }
        }

        public string MetaUdfText1
        {
            get { return metaUdfText1; }
            set
            {
                metaUdfText1 = value;
                OnPropertyChanged("MetaUdfText1");
            }
        }

        public decimal MetaUdfNum1
        {
            get { return metaUdfNum1; }
            set
            {
                metaUdfNum1 = value;
                OnPropertyChanged("MetaUdfNum1");
            }
        }

        public decimal MetaUdfNum2
        {
            get { return metaUdfNum2; }
            set
            {
                metaUdfNum2 = value;
                OnPropertyChanged("MetaUdfNum2");
            }
        }

        public decimal MetaUdfNum3
        {
            get { return metaUdfNum3; }
            set
            {
                metaUdfNum3 = value;
                OnPropertyChanged("MetaUdfNum3");
            }
        }

        public DateTime MetaUdfDate1
        {
            get { return metaUdfDate1; }
            set
            {
                metaUdfDate1 = value;
                OnPropertyChanged("MetaUdfDate1");
            }
        }

        public DateTime MetaUdfDate2
        {
            get { return metaUdfDate2; }
            set
            {
                metaUdfDate2 = value;
                OnPropertyChanged("MetaUdfDate2");
            }
        }

        public DateTime MetaUdfDate3
        {
            get { return metaUdfDate3; }
            set
            {
                metaUdfDate3 = value;
                OnPropertyChanged("MetaUdfDate3");
            }
        }

        public DateTime MetaTakenDate
        {
            get { return metaTakenDate; }
            set
            {
                metaTakenDate = value;
                OnPropertyChanged("MetaTakenDate");
            }
        }

        public ImageMetaTagRef[] MetaTagRef
        {
            get { return metaTagRef; }
            set
            {
                metaTagRef = value;
                OnPropertyChanged("MetaTagRef");
            }
        }
        #endregion

        #region Meta All attributes
        public bool MetaCameraAll
        {
            get { return metaCameraAll; }
            set
            {
                metaCameraAll = value;
                OnPropertyChanged("metaCameraAll");
            }
        }

        public bool MetaUdfChar1All
        {
            get { return metaUdfChar1All; }
            set
            {
                metaUdfChar1All = value;
                OnPropertyChanged("MetaUdfChar1All");
            }
        }

        public bool MetaUdfChar2All
        {
            get { return metaUdfChar2All; }
            set
            {
                metaUdfChar2All = value;
                OnPropertyChanged("MetaUdfChar2All");
            }
        }

        public bool MetaUdfChar3All
        {
            get { return metaUdfChar3All; }
            set
            {
                metaUdfChar3All = value;
                OnPropertyChanged("MetaUdfChar3All");
            }
        }

        public bool MetaUdfText1All
        {
            get { return metaUdfText1All; }
            set
            {
                metaUdfText1All = value;
                OnPropertyChanged("MetaUdfText1All");
            }
        }

        public bool MetaUdfNum1All
        {
            get { return metaUdfNum1All; }
            set
            {
                metaUdfNum1All = value;
                OnPropertyChanged("MetaUdfNum1All");
            }
        }

        public bool MetaUdfNum2All
        {
            get { return metaUdfNum2All; }
            set
            {
                metaUdfNum2All = value;
                OnPropertyChanged("MetaUdfNum2All");
            }
        }

        public bool MetaUdfNum3All
        {
            get { return metaUdfNum3All; }
            set
            {
                metaUdfNum3All = value;
                OnPropertyChanged("MetaUdfNum3All");
            }
        }

        public bool MetaUdfDate1All
        {
            get { return metaUdfDate1All; }
            set
            {
                metaUdfDate1All = value;
                OnPropertyChanged("MetaUdfDate1All");
            }
        }

        public bool MetaUdfDate2All
        {
            get { return metaUdfDate2All; }
            set
            {
                metaUdfDate2All = value;
                OnPropertyChanged("MetaUdfDate2All");
            }
        }

        public bool MetaUdfDate3All
        {
            get { return metaUdfDate3All; }
            set
            {
                metaUdfDate3All = value;
                OnPropertyChanged("MetaUdfDate3All");
            }
        }

        public bool MetaTakenDateAll
        {
            get { return metaTakenDateAll; }
            set
            {
                metaTakenDateAll = value;
                OnPropertyChanged("MetaTakenDateAll");
            }
        }

        public bool MetaTagRefAll
        {
            get { return metaTagRefAll; }
            set
            {
                metaTagRefAll = value;
                OnPropertyChanged("MetaTagRefAll");
            }
        }
        #endregion

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
