﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Este código fue generado por una herramienta.
//     Versión de runtime:4.0.30319.42000
//
//     Los cambios en este archivo podrían causar un comportamiento incorrecto y se perderán si
//     se vuelve a generar el código.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ASAI.TicketRedemptionFactory
{
    using System.Runtime.Serialization;
    
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="RequestMsgFactory", Namespace="http://schemas.datacontract.org/2004/07/ASAI.TicketRedemptionFactory")]
    public partial class RequestMsgFactory : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        private decimal AmountField;
        
        private string TicketNumberField;
        
        private System.Collections.Generic.Dictionary<string, string> TicketOutParametersField;
        
        private string TransactionIDField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get
            {
                return this.extensionDataField;
            }
            set
            {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal Amount
        {
            get
            {
                return this.AmountField;
            }
            set
            {
                this.AmountField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string TicketNumber
        {
            get
            {
                return this.TicketNumberField;
            }
            set
            {
                this.TicketNumberField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Collections.Generic.Dictionary<string, string> TicketOutParameters
        {
            get
            {
                return this.TicketOutParametersField;
            }
            set
            {
                this.TicketOutParametersField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string TransactionID
        {
            get
            {
                return this.TransactionIDField;
            }
            set
            {
                this.TransactionIDField = value;
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="ResponseMsgFactory", Namespace="http://schemas.datacontract.org/2004/07/ASAI.TicketRedemptionFactory")]
    public partial class ResponseMsgFactory : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        private decimal AmountField;
        
        private string RequestLogField;
        
        private System.DateTime RequestLogDateTimeField;
        
        private string ResponseCodeField;
        
        private string ResponseDescriptionField;
        
        private string ResponseLogField;
        
        private System.DateTime ResponseLogDateTimeField;
        
        private string TicketNumberField;
        
        private System.Collections.Generic.Dictionary<string, string> TicketOutParametersField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get
            {
                return this.extensionDataField;
            }
            set
            {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public decimal Amount
        {
            get
            {
                return this.AmountField;
            }
            set
            {
                this.AmountField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string RequestLog
        {
            get
            {
                return this.RequestLogField;
            }
            set
            {
                this.RequestLogField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.DateTime RequestLogDateTime
        {
            get
            {
                return this.RequestLogDateTimeField;
            }
            set
            {
                this.RequestLogDateTimeField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string ResponseCode
        {
            get
            {
                return this.ResponseCodeField;
            }
            set
            {
                this.ResponseCodeField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string ResponseDescription
        {
            get
            {
                return this.ResponseDescriptionField;
            }
            set
            {
                this.ResponseDescriptionField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string ResponseLog
        {
            get
            {
                return this.ResponseLogField;
            }
            set
            {
                this.ResponseLogField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.DateTime ResponseLogDateTime
        {
            get
            {
                return this.ResponseLogDateTimeField;
            }
            set
            {
                this.ResponseLogDateTimeField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string TicketNumber
        {
            get
            {
                return this.TicketNumberField;
            }
            set
            {
                this.TicketNumberField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public System.Collections.Generic.Dictionary<string, string> TicketOutParameters
        {
            get
            {
                return this.TicketOutParametersField;
            }
            set
            {
                this.TicketOutParametersField = value;
            }
        }
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Runtime.Serialization", "4.0.0.0")]
    [System.Runtime.Serialization.DataContractAttribute(Name="TicketFaultExceptionFactory", Namespace="http://schemas.datacontract.org/2004/07/ASAI.TicketRedemptionFactory")]
    public partial class TicketFaultExceptionFactory : object, System.Runtime.Serialization.IExtensibleDataObject
    {
        
        private System.Runtime.Serialization.ExtensionDataObject extensionDataField;
        
        private string ReasonField;
        
        public System.Runtime.Serialization.ExtensionDataObject ExtensionData
        {
            get
            {
                return this.extensionDataField;
            }
            set
            {
                this.extensionDataField = value;
            }
        }
        
        [System.Runtime.Serialization.DataMemberAttribute()]
        public string Reason
        {
            get
            {
                return this.ReasonField;
            }
            set
            {
                this.ReasonField = value;
            }
        }
    }
}


[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
[System.ServiceModel.ServiceContractAttribute(ConfigurationName="ITicketRedemptionFactory", SessionMode=System.ServiceModel.SessionMode.Required)]
public interface ITicketRedemptionFactory
{
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ITicketRedemptionFactory/Initialize", ReplyAction="http://tempuri.org/ITicketRedemptionFactory/InitializeResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(ASAI.TicketRedemptionFactory.TicketFaultExceptionFactory), Action="http://tempuri.org/ITicketRedemptionFactory/InitializeTicketFaultExceptionFactory" +
        "Fault", Name="TicketFaultExceptionFactory", Namespace="http://schemas.datacontract.org/2004/07/ASAI.TicketRedemptionFactory")]
    ASAI.TicketRedemptionFactory.ResponseMsgFactory Initialize(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ITicketRedemptionFactory/StartUp", ReplyAction="http://tempuri.org/ITicketRedemptionFactory/StartUpResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(ASAI.TicketRedemptionFactory.TicketFaultExceptionFactory), Action="http://tempuri.org/ITicketRedemptionFactory/StartUpTicketFaultExceptionFactoryFau" +
        "lt", Name="TicketFaultExceptionFactory", Namespace="http://schemas.datacontract.org/2004/07/ASAI.TicketRedemptionFactory")]
    ASAI.TicketRedemptionFactory.ResponseMsgFactory StartUp(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ITicketRedemptionFactory/ValidateTicket", ReplyAction="http://tempuri.org/ITicketRedemptionFactory/ValidateTicketResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(ASAI.TicketRedemptionFactory.TicketFaultExceptionFactory), Action="http://tempuri.org/ITicketRedemptionFactory/ValidateTicketTicketFaultExceptionFac" +
        "toryFault", Name="TicketFaultExceptionFactory", Namespace="http://schemas.datacontract.org/2004/07/ASAI.TicketRedemptionFactory")]
    ASAI.TicketRedemptionFactory.ResponseMsgFactory ValidateTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ITicketRedemptionFactory/RedemptionTicket", ReplyAction="http://tempuri.org/ITicketRedemptionFactory/RedemptionTicketResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(ASAI.TicketRedemptionFactory.TicketFaultExceptionFactory), Action="http://tempuri.org/ITicketRedemptionFactory/RedemptionTicketTicketFaultExceptionF" +
        "actoryFault", Name="TicketFaultExceptionFactory", Namespace="http://schemas.datacontract.org/2004/07/ASAI.TicketRedemptionFactory")]
    ASAI.TicketRedemptionFactory.ResponseMsgFactory RedemptionTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ITicketRedemptionFactory/CancelTicket", ReplyAction="http://tempuri.org/ITicketRedemptionFactory/CancelTicketResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(ASAI.TicketRedemptionFactory.TicketFaultExceptionFactory), Action="http://tempuri.org/ITicketRedemptionFactory/CancelTicketTicketFaultExceptionFacto" +
        "ryFault", Name="TicketFaultExceptionFactory", Namespace="http://schemas.datacontract.org/2004/07/ASAI.TicketRedemptionFactory")]
    ASAI.TicketRedemptionFactory.ResponseMsgFactory CancelTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ITicketRedemptionFactory/CreateTicket", ReplyAction="http://tempuri.org/ITicketRedemptionFactory/CreateTicketResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(ASAI.TicketRedemptionFactory.TicketFaultExceptionFactory), Action="http://tempuri.org/ITicketRedemptionFactory/CreateTicketTicketFaultExceptionFacto" +
        "ryFault", Name="TicketFaultExceptionFactory", Namespace="http://schemas.datacontract.org/2004/07/ASAI.TicketRedemptionFactory")]
    ASAI.TicketRedemptionFactory.ResponseMsgFactory CreateTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ITicketRedemptionFactory/AcknowledgmentTicket", ReplyAction="http://tempuri.org/ITicketRedemptionFactory/AcknowledgmentTicketResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(ASAI.TicketRedemptionFactory.TicketFaultExceptionFactory), Action="http://tempuri.org/ITicketRedemptionFactory/AcknowledgmentTicketTicketFaultExcept" +
        "ionFactoryFault", Name="TicketFaultExceptionFactory", Namespace="http://schemas.datacontract.org/2004/07/ASAI.TicketRedemptionFactory")]
    ASAI.TicketRedemptionFactory.ResponseMsgFactory AcknowledgmentTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg);
    
    [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/ITicketRedemptionFactory/IntegrationTicket", ReplyAction="http://tempuri.org/ITicketRedemptionFactory/IntegrationTicketResponse")]
    [System.ServiceModel.FaultContractAttribute(typeof(ASAI.TicketRedemptionFactory.TicketFaultExceptionFactory), Action="http://tempuri.org/ITicketRedemptionFactory/IntegrationTicketTicketFaultException" +
        "FactoryFault", Name="TicketFaultExceptionFactory", Namespace="http://schemas.datacontract.org/2004/07/ASAI.TicketRedemptionFactory")]
    ASAI.TicketRedemptionFactory.ResponseMsgFactory IntegrationTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg);
}

[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
public interface ITicketRedemptionFactoryChannel : ITicketRedemptionFactory, System.ServiceModel.IClientChannel
{
}

[System.Diagnostics.DebuggerStepThroughAttribute()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
public partial class TicketRedemptionFactoryClient : System.ServiceModel.ClientBase<ITicketRedemptionFactory>, ITicketRedemptionFactory
{
    
    public TicketRedemptionFactoryClient()
    {
    }
    
    public TicketRedemptionFactoryClient(string endpointConfigurationName) : 
            base(endpointConfigurationName)
    {
    }
    
    public TicketRedemptionFactoryClient(string endpointConfigurationName, string remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
    {
    }
    
    public TicketRedemptionFactoryClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(endpointConfigurationName, remoteAddress)
    {
    }
    
    public TicketRedemptionFactoryClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
            base(binding, remoteAddress)
    {
    }
    
    public ASAI.TicketRedemptionFactory.ResponseMsgFactory Initialize(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg)
    {
        return base.Channel.Initialize(requestMsg);
    }
    
    public ASAI.TicketRedemptionFactory.ResponseMsgFactory StartUp(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg)
    {
        return base.Channel.StartUp(requestMsg);
    }
    
    public ASAI.TicketRedemptionFactory.ResponseMsgFactory ValidateTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg)
    {
        return base.Channel.ValidateTicket(requestMsg);
    }
    
    public ASAI.TicketRedemptionFactory.ResponseMsgFactory RedemptionTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg)
    {
        return base.Channel.RedemptionTicket(requestMsg);
    }
    
    public ASAI.TicketRedemptionFactory.ResponseMsgFactory CancelTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg)
    {
        return base.Channel.CancelTicket(requestMsg);
    }
    
    public ASAI.TicketRedemptionFactory.ResponseMsgFactory CreateTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg)
    {
        return base.Channel.CreateTicket(requestMsg);
    }
    
    public ASAI.TicketRedemptionFactory.ResponseMsgFactory AcknowledgmentTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg)
    {
        return base.Channel.AcknowledgmentTicket(requestMsg);
    }
    
    public ASAI.TicketRedemptionFactory.ResponseMsgFactory IntegrationTicket(ASAI.TicketRedemptionFactory.RequestMsgFactory requestMsg)
    {
        return base.Channel.IntegrationTicket(requestMsg);
    }
}