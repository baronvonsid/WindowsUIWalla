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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://ws.fotowalla.com/Tag")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://ws.fotowalla.com/Tag", IsNullable=false)]
    public partial class Tag {
        
        private string nameField;
        
        private string descField;
        
        private int imageCountField;
        
        private System.DateTime lastChangedField;
        
        private bool systemOwnedField;
        
        private long idField;
        
        private bool idFieldSpecified;
        
        private int versionField;
        
        private bool versionFieldSpecified;
        
        /// <remarks/>
        public string Name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }
        
        /// <remarks/>
        public string Desc {
            get {
                return this.descField;
            }
            set {
                this.descField = value;
            }
        }
        
        /// <remarks/>
        public int ImageCount {
            get {
                return this.imageCountField;
            }
            set {
                this.imageCountField = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime LastChanged {
            get {
                return this.lastChangedField;
            }
            set {
                this.lastChangedField = value;
            }
        }
        
        /// <remarks/>
        public bool SystemOwned {
            get {
                return this.systemOwnedField;
            }
            set {
                this.systemOwnedField = value;
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
        public int version {
            get {
                return this.versionField;
            }
            set {
                this.versionField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlIgnoreAttribute()]
        public bool versionSpecified {
            get {
                return this.versionFieldSpecified;
            }
            set {
                this.versionFieldSpecified = value;
            }
        }
    }
}
