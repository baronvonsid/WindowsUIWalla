﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.0.30319.18020.
// 
namespace ManageWalla {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.18020")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://ws.fotowalla.com/UserApp")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://ws.fotowalla.com/UserApp", IsNullable=false)]
    public partial class UserApp {
        
        private int appIdField;
        
        private int platformIdField;
        
        private string machineNameField;
        
        private System.DateTime lastUsedField;
        
        private long tagIdField;
        
        private long userAppCategoryIdField;
        
        private long userDefaultCategoryIdField;
        
        private long galleryIdField;
        
        private int thumbCacheSizeMBField;
        
        private int mainCopyCacheSizeMBField;
        
        private int fetchSizeField;
        
        private string mainCopyFolderField;
        
        private string autoUploadFolderField;
        
        private bool autoUploadField;
        
        private bool blockedField;
        
        private long idField;
        
        private int versionField;
        
        public UserApp() {
            this.tagIdField = ((long)(0));
            this.userAppCategoryIdField = ((long)(0));
            this.userDefaultCategoryIdField = ((long)(0));
            this.galleryIdField = ((long)(0));
            this.idField = ((long)(0));
            this.versionField = 0;
        }
        
        /// <remarks/>
        public int AppId {
            get {
                return this.appIdField;
            }
            set {
                this.appIdField = value;
            }
        }
        
        /// <remarks/>
        public int PlatformId {
            get {
                return this.platformIdField;
            }
            set {
                this.platformIdField = value;
            }
        }
        
        /// <remarks/>
        public string MachineName {
            get {
                return this.machineNameField;
            }
            set {
                this.machineNameField = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime LastUsed {
            get {
                return this.lastUsedField;
            }
            set {
                this.lastUsedField = value;
            }
        }
        
        /// <remarks/>
        public long TagId {
            get {
                return this.tagIdField;
            }
            set {
                this.tagIdField = value;
            }
        }
        
        /// <remarks/>
        public long UserAppCategoryId {
            get {
                return this.userAppCategoryIdField;
            }
            set {
                this.userAppCategoryIdField = value;
            }
        }
        
        /// <remarks/>
        public long UserDefaultCategoryId {
            get {
                return this.userDefaultCategoryIdField;
            }
            set {
                this.userDefaultCategoryIdField = value;
            }
        }
        
        /// <remarks/>
        public long GalleryId {
            get {
                return this.galleryIdField;
            }
            set {
                this.galleryIdField = value;
            }
        }
        
        /// <remarks/>
        public int ThumbCacheSizeMB {
            get {
                return this.thumbCacheSizeMBField;
            }
            set {
                this.thumbCacheSizeMBField = value;
            }
        }
        
        /// <remarks/>
        public int MainCopyCacheSizeMB {
            get {
                return this.mainCopyCacheSizeMBField;
            }
            set {
                this.mainCopyCacheSizeMBField = value;
            }
        }
        
        /// <remarks/>
        public int FetchSize {
            get {
                return this.fetchSizeField;
            }
            set {
                this.fetchSizeField = value;
            }
        }
        
        /// <remarks/>
        public string MainCopyFolder {
            get {
                return this.mainCopyFolderField;
            }
            set {
                this.mainCopyFolderField = value;
            }
        }
        
        /// <remarks/>
        public string AutoUploadFolder {
            get {
                return this.autoUploadFolderField;
            }
            set {
                this.autoUploadFolderField = value;
            }
        }
        
        /// <remarks/>
        public bool AutoUpload {
            get {
                return this.autoUploadField;
            }
            set {
                this.autoUploadField = value;
            }
        }
        
        /// <remarks/>
        public bool Blocked {
            get {
                return this.blockedField;
            }
            set {
                this.blockedField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(typeof(long), "0")]
        public long id {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(0)]
        public int version {
            get {
                return this.versionField;
            }
            set {
                this.versionField = value;
            }
        }
    }
}
