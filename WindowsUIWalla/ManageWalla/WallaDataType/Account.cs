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
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://ws.fotowalla.com/Account")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace="http://ws.fotowalla.com/Account", IsNullable=false)]
    public partial class Account {
        
        private string profileNameField;
        
        private string descField;
        
        private string countryField;
        
        private string timezoneField;
        
        private bool newsletterField;
        
        private string passwordField;
        
        private int statusField;
        
        private string accountMessageField;
        
        private int accountTypeField;
        
        private string accountTypeNameField;
        
        private System.DateTime passwordChangeDateField;
        
        private System.DateTime openDateField;
        
        private System.DateTime closeDateField;
        
        private string keyField;
        
        private string securityMessageField;
        
        private AccountEmailRef[] emailsField;
        
        private long idField;
        
        private int versionField;
        
        public Account() {
            this.idField = ((long)(0));
            this.versionField = 0;
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
        public string Desc {
            get {
                return this.descField;
            }
            set {
                this.descField = value;
            }
        }
        
        /// <remarks/>
        public string Country {
            get {
                return this.countryField;
            }
            set {
                this.countryField = value;
            }
        }
        
        /// <remarks/>
        public string Timezone {
            get {
                return this.timezoneField;
            }
            set {
                this.timezoneField = value;
            }
        }
        
        /// <remarks/>
        public bool Newsletter {
            get {
                return this.newsletterField;
            }
            set {
                this.newsletterField = value;
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
        public int Status {
            get {
                return this.statusField;
            }
            set {
                this.statusField = value;
            }
        }
        
        /// <remarks/>
        public string AccountMessage {
            get {
                return this.accountMessageField;
            }
            set {
                this.accountMessageField = value;
            }
        }
        
        /// <remarks/>
        public int AccountType {
            get {
                return this.accountTypeField;
            }
            set {
                this.accountTypeField = value;
            }
        }
        
        /// <remarks/>
        public string AccountTypeName {
            get {
                return this.accountTypeNameField;
            }
            set {
                this.accountTypeNameField = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime PasswordChangeDate {
            get {
                return this.passwordChangeDateField;
            }
            set {
                this.passwordChangeDateField = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime OpenDate {
            get {
                return this.openDateField;
            }
            set {
                this.openDateField = value;
            }
        }
        
        /// <remarks/>
        public System.DateTime CloseDate {
            get {
                return this.closeDateField;
            }
            set {
                this.closeDateField = value;
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
        
        /// <remarks/>
        public string SecurityMessage {
            get {
                return this.securityMessageField;
            }
            set {
                this.securityMessageField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlArrayItemAttribute("EmailRef", IsNullable=false)]
        public AccountEmailRef[] Emails {
            get {
                return this.emailsField;
            }
            set {
                this.emailsField = value;
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
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.0.30319.18020")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true, Namespace="http://ws.fotowalla.com/Account")]
    public partial class AccountEmailRef {
        
        private string addressField;
        
        private bool principleField;
        
        private bool secondaryField;
        
        private bool verifiedField;
        
        /// <remarks/>
        public string Address {
            get {
                return this.addressField;
            }
            set {
                this.addressField = value;
            }
        }
        
        /// <remarks/>
        public bool Principle {
            get {
                return this.principleField;
            }
            set {
                this.principleField = value;
            }
        }
        
        /// <remarks/>
        public bool Secondary {
            get {
                return this.secondaryField;
            }
            set {
                this.secondaryField = value;
            }
        }
        
        /// <remarks/>
        public bool Verified {
            get {
                return this.verifiedField;
            }
            set {
                this.verifiedField = value;
            }
        }
    }
}
