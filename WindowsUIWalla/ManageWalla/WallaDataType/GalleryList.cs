﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.18444
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

// 
// This source code was auto-generated by xsd, Version=4.0.30319.17929.
// 
namespace ManageWalla {
    using System.Xml.Serialization;
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.example.org/GalleryList")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://www.example.org/GalleryList", IsNullable=false)]
    public partial class GalleryList {
        
        private GalleryListGalleryRef[] galleryRefField;
        
        private System.DateTime lastChangedField;
        
        private bool lastChangedFieldSpecified;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("GalleryRef")]
        public GalleryListGalleryRef[] GalleryRef {
            get {
                return this.galleryRefField;
            }
            set {
                this.galleryRefField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public System.DateTime lastChanged {
            get {
                return this.lastChangedField;
            }
            set {
                this.lastChangedField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool lastChangedSpecified {
            get {
                return this.lastChangedFieldSpecified;
            }
            set {
                this.lastChangedFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.example.org/GalleryList")]
    public partial class GalleryListGalleryRef {
        
        private GalleryListGalleryRefSectionRef[] sectionRefField;
        
        private long idField;
        
        private bool idFieldSpecified;
        
        private int countField;
        
        private bool countFieldSpecified;
        
        private string nameField;
        
        private string descField;
        
        private string urlComplexField;
        
        private bool systemOwnedField;
        
        private bool systemOwnedFieldSpecified;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("SectionRef")]
        public GalleryListGalleryRefSectionRef[] SectionRef {
            get {
                return this.sectionRefField;
            }
            set {
                this.sectionRefField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public long id {
            get {
                return this.idField;
            }
            set {
                this.idField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool idSpecified {
            get {
                return this.idFieldSpecified;
            }
            set {
                this.idFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int count {
            get {
                return this.countField;
            }
            set {
                this.countField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool countSpecified {
            get {
                return this.countFieldSpecified;
            }
            set {
                this.countFieldSpecified = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string desc {
            get {
                return this.descField;
            }
            set {
                this.descField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string urlComplex {
            get {
                return this.urlComplexField;
            }
            set {
                this.urlComplexField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool systemOwned {
            get {
                return this.systemOwnedField;
            }
            set {
                this.systemOwnedField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool systemOwnedSpecified {
            get {
                return this.systemOwnedFieldSpecified;
            }
            set {
                this.systemOwnedFieldSpecified = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.example.org/GalleryList")]
    public partial class GalleryListGalleryRefSectionRef {
        
        private long idField;
        
        private string nameField;
        
        private int imageCountField;
        
        private bool imageCountFieldSpecified;
        
        public GalleryListGalleryRefSectionRef() {
            this.idField = ((long)(0));
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
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public int imageCount {
            get {
                return this.imageCountField;
            }
            set {
                this.imageCountField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool imageCountSpecified {
            get {
                return this.imageCountFieldSpecified;
            }
            set {
                this.imageCountFieldSpecified = value;
            }
        }
    }
}
