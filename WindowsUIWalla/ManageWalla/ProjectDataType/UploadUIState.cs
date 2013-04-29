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
        private bool mapToSubFolders = false;
        private bool gotSubFolders = false;
        private long categoryId = 0;
        private string categoryPath = null;
        private bool uploadToNewCategory = false;
        private string categoryName = null;
        private string categoryDesc = null;
        private bool tagsAll = false;

        public enum UploadMode
        {
            None = 0,
            Folder = 1,
            Images = 2
        }

        public event PropertyChangedEventHandler PropertyChanged;

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

        public bool TagsAll
        {
            get { return tagsAll; }
            set
            {
                tagsAll = value;
                OnPropertyChanged("TagsAll");
            }
        } 

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

    }
}
