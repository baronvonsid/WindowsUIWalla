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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://ws.fotowalla.com/GalleryLogon")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://ws.fotowalla.com/GalleryLogon", IsNullable=false)]
    public partial class GalleryLogon {
        
        private long userIdField;
        
        private string profileNameField;
        
        private string galleryNameField;
        
        private string passwordField;
        
        private string passwordHashField;
        
        private string gallerySaltField;
        
        private string tempSaltField;
        
        private string complexUrlField;
        
        private int accessTypeField;
        
        private string keyField;
        
        /// <remarks/>
        public long UserId {
            get {
                return this.userIdField;
            }
            set {
                this.userIdField = value;
            }
        }
        
        /// <remarks/>
        public string ProfileName {
            get {
                return this.profileNameField;
            }
            set {
                this.profileNameField = value;
            }
        }
        
        /// <remarks/>
        public string GalleryName {
            get {
                return this.galleryNameField;
            }
            set {
                this.galleryNameField = value;
            }
        }
        
        /// <remarks/>
        public string Password {
            get {
                return this.passwordField;
            }
            set {
                this.passwordField = value;
            }
        }
        
        /// <remarks/>
        public string PasswordHash {
            get {
                return this.passwordHashField;
            }
            set {
                this.passwordHashField = value;
            }
        }
        
        /// <remarks/>
        public string GallerySalt {
            get {
                return this.gallerySaltField;
            }
            set {
                this.gallerySaltField = value;
            }
        }
        
        /// <remarks/>
        public string TempSalt {
            get {
                return this.tempSaltField;
            }
            set {
                this.tempSaltField = value;
            }
        }
        
        /// <remarks/>
        public string ComplexUrl {
            get {
                return this.complexUrlField;
            }
            set {
                this.complexUrlField = value;
            }
        }
        
        /// <remarks/>
        public int AccessType {
            get {
                return this.accessTypeField;
            }
            set {
                this.accessTypeField = value;
            }
        }
        
        /// <remarks/>
        public string Key {
            get {
                return this.keyField;
            }
            set {
                this.keyField = value;
            }
        }
    }
}
