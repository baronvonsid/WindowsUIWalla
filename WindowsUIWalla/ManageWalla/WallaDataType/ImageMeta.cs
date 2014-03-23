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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.example.org/ImageMeta")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://www.example.org/ImageMeta", IsNullable=false)]
    public partial class ImageMeta {
        
        private string nameField;
        
        private string descField;
        
        private string originalFileNameField;
        
        private string formatField;
        
        private string localPathField;
        
        private long userAppIdField;
        
        private int statusField;
        
        private int widthField;
        
        private int heightField;
        
        private long sizeField;
        
        private string cameraMakerField;
        
        private string cameraModelField;
        
        private string apertureField;
        
        private string shutterSpeedField;
        
        private int iSOField;
        
        private int orientationField;
        
        private System.DateTime takenDateField;
        
        private bool takenDateSpecified1Field;
        
        private System.DateTime takenDateFileField;
        
        private System.DateTime takenDateMetaField;
        
        private System.DateTime uploadDateField;
        
        private string udfChar1Field;
        
        private string udfChar2Field;
        
        private string udfChar3Field;
        
        private string udfText1Field;
        
        private decimal udfNum1Field;
        
        private decimal udfNum2Field;
        
        private decimal udfNum3Field;
        
        private System.DateTime udfDate1Field;
        
        private System.DateTime udfDate2Field;
        
        private System.DateTime udfDate3Field;
        
        private ImageMetaTagRef[] tagsField;
        
        private long idField;
        
        private int versionField;
        
        private long categoryIdField;
        
        public ImageMeta() {
            this.statusField = 0;
            this.widthField = 0;
            this.heightField = 0;
            this.sizeField = ((long)(0));
            this.idField = ((long)(0));
            this.versionField = 0;
            this.categoryIdField = ((long)(0));
        }
        
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
        public string OriginalFileName {
            get {
                return this.originalFileNameField;
            }
            set {
                this.originalFileNameField = value;
            }
        }
        
        /// <remarks/>
        public string Format {
            get {
                return this.formatField;
            }
            set {
                this.formatField = value;
            }
        }
        
        /// <remarks/>
        public string LocalPath {
            get {
                return this.localPathField;
            }
            set {
                this.localPathField = value;
            }
        }
        
        /// <remarks/>
        public long UserAppId {
            get {
                return this.userAppIdField;
            }
            set {
                this.userAppIdField = value;
            }
        }
        
        /// <remarks/>
        public int Status {
            get {
                return this.statusField;
            }
            set {
                this.statusField = value;
            }
        }
        
        /// <remarks/>
        public int Width {
            get {
                return this.widthField;
            }
            set {
                this.widthField = value;
            }
        }
        
        /// <remarks/>
        public int Height {
            get {
                return this.heightField;
            }
            set {
                this.heightField = value;
            }
        }
        
        /// <remarks/>
        public long Size {
            get {
                return this.sizeField;
            }
            set {
                this.sizeField = value;
            }
        }
        
        /// <remarks/>
        public string CameraMaker {
            get {
                return this.cameraMakerField;
            }
            set {
                this.cameraMakerField = value;
            }
        }
        
        /// <remarks/>
        public string CameraModel {
            get {
                return this.cameraModelField;
            }
            set {
                this.cameraModelField = value;
            }
        }
        
        /// <remarks/>
        public string Aperture {
            get {
                return this.apertureField;
            }
            set {
                this.apertureField = value;
            }
        }
        
        /// <remarks/>
        public string ShutterSpeed {
            get {
                return this.shutterSpeedField;
            }
            set {
                this.shutterSpeedField = value;
            }
        }
        
        /// <remarks/>
        public int ISO {
            get {
                return this.iSOField;
            }
            set {
                this.iSOField = value;
            }
        }
        
        /// <remarks/>
        public int Orientation {
            get {
                return this.orientationField;
            }
            set {
                this.orientationField = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime TakenDate {
            get {
                return this.takenDateField;
            }
            set {
                this.takenDateField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("TakenDateSpecified")]
        public bool TakenDateSpecified1 {
            get {
                return this.takenDateSpecified1Field;
            }
            set {
                this.takenDateSpecified1Field = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime TakenDateFile {
            get {
                return this.takenDateFileField;
            }
            set {
                this.takenDateFileField = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime TakenDateMeta {
            get {
                return this.takenDateMetaField;
            }
            set {
                this.takenDateMetaField = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime UploadDate {
            get {
                return this.uploadDateField;
            }
            set {
                this.uploadDateField = value;
            }
        }
        
        /// <remarks/>
        public string UdfChar1 {
            get {
                return this.udfChar1Field;
            }
            set {
                this.udfChar1Field = value;
            }
        }
        
        /// <remarks/>
        public string UdfChar2 {
            get {
                return this.udfChar2Field;
            }
            set {
                this.udfChar2Field = value;
            }
        }
        
        /// <remarks/>
        public string UdfChar3 {
            get {
                return this.udfChar3Field;
            }
            set {
                this.udfChar3Field = value;
            }
        }
        
        /// <remarks/>
        public string UdfText1 {
            get {
                return this.udfText1Field;
            }
            set {
                this.udfText1Field = value;
            }
        }
        
        /// <remarks/>
        public decimal UdfNum1 {
            get {
                return this.udfNum1Field;
            }
            set {
                this.udfNum1Field = value;
            }
        }
        
        /// <remarks/>
        public decimal UdfNum2 {
            get {
                return this.udfNum2Field;
            }
            set {
                this.udfNum2Field = value;
            }
        }
        
        /// <remarks/>
        public decimal UdfNum3 {
            get {
                return this.udfNum3Field;
            }
            set {
                this.udfNum3Field = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime UdfDate1 {
            get {
                return this.udfDate1Field;
            }
            set {
                this.udfDate1Field = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime UdfDate2 {
            get {
                return this.udfDate2Field;
            }
            set {
                this.udfDate2Field = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime UdfDate3 {
            get {
                return this.udfDate3Field;
            }
            set {
                this.udfDate3Field = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("TagRef", IsNullable=false)]
        public ImageMetaTagRef[] Tags {
            get {
                return this.tagsField;
            }
            set {
                this.tagsField = value;
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
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        [System.ComponentModel.DefaultValueAttribute(typeof(long), "0")]
        public long categoryId {
            get {
                return this.categoryIdField;
            }
            set {
                this.categoryIdField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.17929")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://www.example.org/ImageMeta")]
    public partial class ImageMetaTagRef {
        
        private long idField;
        
        private string opField;
        
        private string nameField;
        
        public ImageMetaTagRef() {
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
        public string op {
            get {
                return this.opField;
            }
            set {
                this.opField = value;
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
    }
}
