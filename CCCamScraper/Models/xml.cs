﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System.Xml.Serialization;

// 
// This source code was auto-generated by xsd, Version=4.8.3928.0.
// 


/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
public partial class oscam {
    
    private oscamReader[] readerField;
    
    private string versionField;
    
    private string revisionField;
    
    private string starttimeField;
    
    private string uptimeField;
    
    private string readonlyField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("reader", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public oscamReader[] reader {
        get {
            return this.readerField;
        }
        set {
            this.readerField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string version {
        get {
            return this.versionField;
        }
        set {
            this.versionField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string revision {
        get {
            return this.revisionField;
        }
        set {
            this.revisionField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string starttime {
        get {
            return this.starttimeField;
        }
        set {
            this.starttimeField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string uptime {
        get {
            return this.uptimeField;
        }
        set {
            this.uptimeField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string @readonly {
        get {
            return this.readonlyField;
        }
        set {
            this.readonlyField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class oscamReader {
    
    private oscamReaderCardlist[] cardlistField;
    
    private string labelField;
    
    private string hostaddressField;
    
    private string hostportField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("cardlist", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public oscamReaderCardlist[] cardlist {
        get {
            return this.cardlistField;
        }
        set {
            this.cardlistField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string label {
        get {
            return this.labelField;
        }
        set {
            this.labelField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string hostaddress {
        get {
            return this.hostaddressField;
        }
        set {
            this.hostaddressField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string hostport {
        get {
            return this.hostportField;
        }
        set {
            this.hostportField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class oscamReaderCardlist {
    
    private oscamReaderCardlistCard[] cardField;
    
    private string totalcardsField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("card", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public oscamReaderCardlistCard[] card {
        get {
            return this.cardField;
        }
        set {
            this.cardField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string totalcards {
        get {
            return this.totalcardsField;
        }
        set {
            this.totalcardsField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class oscamReaderCardlistCard {
    
    private string shareidField;
    
    private string remoteidField;
    
    private oscamReaderCardlistCardProviders[] providersField;
    
    private oscamReaderCardlistCardNodes[] nodesField;
    
    private string numberField;
    
    private string caidField;
    
    private string systemField;
    
    private string reshareField;
    
    private string hopField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public string shareid {
        get {
            return this.shareidField;
        }
        set {
            this.shareidField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute(Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public string remoteid {
        get {
            return this.remoteidField;
        }
        set {
            this.remoteidField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("providers", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public oscamReaderCardlistCardProviders[] providers {
        get {
            return this.providersField;
        }
        set {
            this.providersField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("nodes", Form=System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public oscamReaderCardlistCardNodes[] nodes {
        get {
            return this.nodesField;
        }
        set {
            this.nodesField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string number {
        get {
            return this.numberField;
        }
        set {
            this.numberField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string caid {
        get {
            return this.caidField;
        }
        set {
            this.caidField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string system {
        get {
            return this.systemField;
        }
        set {
            this.systemField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string reshare {
        get {
            return this.reshareField;
        }
        set {
            this.reshareField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string hop {
        get {
            return this.hopField;
        }
        set {
            this.hopField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class oscamReaderCardlistCardProviders {
    
    private oscamReaderCardlistCardProvidersProvider[] providerField;
    
    private string totalprovidersField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("provider", Form=System.Xml.Schema.XmlSchemaForm.Unqualified, IsNullable=true)]
    public oscamReaderCardlistCardProvidersProvider[] provider {
        get {
            return this.providerField;
        }
        set {
            this.providerField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string totalproviders {
        get {
            return this.totalprovidersField;
        }
        set {
            this.totalprovidersField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class oscamReaderCardlistCardProvidersProvider {
    
    private string numberField;
    
    private string saField;
    
    private string caidField;
    
    private string providField;
    
    private string valueField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string number {
        get {
            return this.numberField;
        }
        set {
            this.numberField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string sa {
        get {
            return this.saField;
        }
        set {
            this.saField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string caid {
        get {
            return this.caidField;
        }
        set {
            this.caidField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string provid {
        get {
            return this.providField;
        }
        set {
            this.providField = value;
        }
    }
    
    /// <remarks/>
    [System.Xml.Serialization.XmlTextAttribute()]
    public string Value {
        get {
            return this.valueField;
        }
        set {
            this.valueField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
public partial class oscamReaderCardlistCardNodes {
    
    private string totalnodesField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string totalnodes {
        get {
            return this.totalnodesField;
        }
        set {
            this.totalnodesField = value;
        }
    }
}

/// <remarks/>
[System.CodeDom.Compiler.GeneratedCodeAttribute("xsd", "4.8.3928.0")]
[System.SerializableAttribute()]
[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType=true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace="", IsNullable=false)]
public partial class NewDataSet {
    
    private oscam[] itemsField;
    
    /// <remarks/>
    [System.Xml.Serialization.XmlElementAttribute("oscam")]
    public oscam[] Items {
        get {
            return this.itemsField;
        }
        set {
            this.itemsField = value;
        }
    }
}
