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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://ws.fotowalla.com/ClientApp")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://ws.fotowalla.com/ClientApp", IsNullable=false)]
    public partial class ClientApp {
        
        private string wSKeyField;
        
        private string osField;
        
        private string machineTypeField;
        
        private int majorField;
        
        private int minorField;
        
        /// <remarks/>
        public string WSKey {
            get {
                return this.wSKeyField;
            }
            set {
                this.wSKeyField = value;
            }
        }
        
        /// <remarks/>
        public string OS {
            get {
                return this.osField;
            }
            set {
                this.osField = value;
            }
        }
        
        /// <remarks/>
        public string MachineType {
            get {
                return this.machineTypeField;
            }
            set {
                this.machineTypeField = value;
            }
        }
        
        /// <remarks/>
        public int Major {
            get {
                return this.majorField;
            }
            set {
                this.majorField = value;
            }
        }
        
        /// <remarks/>
        public int Minor {
            get {
                return this.minorField;
            }
            set {
                this.minorField = value;
            }
        }
    }
}
