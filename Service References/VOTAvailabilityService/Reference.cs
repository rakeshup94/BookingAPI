﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TravillioXMLOutService.VOTAvailabilityService {
    
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    [System.ServiceModel.ServiceContractAttribute(ConfigurationName="VOTAvailabilityService.IAvailabilityServices")]
    public interface IAvailabilityServices {
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IAvailabilityServices/GetHotelRoomAvailability", ReplyAction="http://tempuri.org/IAvailabilityServices/GetHotelRoomAvailabilityResponse")]
        string GetHotelRoomAvailability(string AgentUsername, string AgentPassword, int CountryCode, int DestinationCode, string FromDate, string ToDate, string Occupancy, int HotelCode, int CategoryCode, string PassengerNationality, string SortBy);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IAvailabilityServices/GetHotelRoomAvailability", ReplyAction="http://tempuri.org/IAvailabilityServices/GetHotelRoomAvailabilityResponse")]
        System.Threading.Tasks.Task<string> GetHotelRoomAvailabilityAsync(string AgentUsername, string AgentPassword, int CountryCode, int DestinationCode, string FromDate, string ToDate, string Occupancy, int HotelCode, int CategoryCode, string PassengerNationality, string SortBy);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IAvailabilityServices/ConfirmHotelRoomAvailability", ReplyAction="http://tempuri.org/IAvailabilityServices/ConfirmHotelRoomAvailabilityResponse")]
        string ConfirmHotelRoomAvailability(string AgentUsername, string AgentPassword, string SessionNB, string ReferenceNB, string RoomPurchaseToken);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IAvailabilityServices/ConfirmHotelRoomAvailability", ReplyAction="http://tempuri.org/IAvailabilityServices/ConfirmHotelRoomAvailabilityResponse")]
        System.Threading.Tasks.Task<string> ConfirmHotelRoomAvailabilityAsync(string AgentUsername, string AgentPassword, string SessionNB, string ReferenceNB, string RoomPurchaseToken);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IAvailabilityServices/MakeBooking", ReplyAction="http://tempuri.org/IAvailabilityServices/MakeBookingResponse")]
        string MakeBooking(string AgentUsername, string AgentPassword, string SessionNB, string ReferenceNB, string RoomPurchaseToken, string Passengers, string ClientFirstName, string ClientLastName, string ClientEmail, string ClientComments, string ClientBooingReferenceNB);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IAvailabilityServices/MakeBooking", ReplyAction="http://tempuri.org/IAvailabilityServices/MakeBookingResponse")]
        System.Threading.Tasks.Task<string> MakeBookingAsync(string AgentUsername, string AgentPassword, string SessionNB, string ReferenceNB, string RoomPurchaseToken, string Passengers, string ClientFirstName, string ClientLastName, string ClientEmail, string ClientComments, string ClientBooingReferenceNB);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IAvailabilityServices/CancelBooking", ReplyAction="http://tempuri.org/IAvailabilityServices/CancelBookingResponse")]
        string CancelBooking(string AgentUsername, string AgentPassword, string BookingLocator, string Type);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IAvailabilityServices/CancelBooking", ReplyAction="http://tempuri.org/IAvailabilityServices/CancelBookingResponse")]
        System.Threading.Tasks.Task<string> CancelBookingAsync(string AgentUsername, string AgentPassword, string BookingLocator, string Type);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IAvailabilityServices/CheckBooking", ReplyAction="http://tempuri.org/IAvailabilityServices/CheckBookingResponse")]
        string CheckBooking(string AgentUsername, string AgentPassword, string BookingLocator, string FromDate, string ToDate, string DateType, string ClientBookingReferenceNB);
        
        [System.ServiceModel.OperationContractAttribute(Action="http://tempuri.org/IAvailabilityServices/CheckBooking", ReplyAction="http://tempuri.org/IAvailabilityServices/CheckBookingResponse")]
        System.Threading.Tasks.Task<string> CheckBookingAsync(string AgentUsername, string AgentPassword, string BookingLocator, string FromDate, string ToDate, string DateType, string ClientBookingReferenceNB);
    }
    
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public interface IAvailabilityServicesChannel : TravillioXMLOutService.VOTAvailabilityService.IAvailabilityServices, System.ServiceModel.IClientChannel {
    }
    
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.ServiceModel", "4.0.0.0")]
    public partial class AvailabilityServicesClient : System.ServiceModel.ClientBase<TravillioXMLOutService.VOTAvailabilityService.IAvailabilityServices>, TravillioXMLOutService.VOTAvailabilityService.IAvailabilityServices {
        
        public AvailabilityServicesClient() {
        }
        
        public AvailabilityServicesClient(string endpointConfigurationName) : 
                base(endpointConfigurationName) {
        }
        
        public AvailabilityServicesClient(string endpointConfigurationName, string remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public AvailabilityServicesClient(string endpointConfigurationName, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(endpointConfigurationName, remoteAddress) {
        }
        
        public AvailabilityServicesClient(System.ServiceModel.Channels.Binding binding, System.ServiceModel.EndpointAddress remoteAddress) : 
                base(binding, remoteAddress) {
        }
        
        public string GetHotelRoomAvailability(string AgentUsername, string AgentPassword, int CountryCode, int DestinationCode, string FromDate, string ToDate, string Occupancy, int HotelCode, int CategoryCode, string PassengerNationality, string SortBy) {
            return base.Channel.GetHotelRoomAvailability(AgentUsername, AgentPassword, CountryCode, DestinationCode, FromDate, ToDate, Occupancy, HotelCode, CategoryCode, PassengerNationality, SortBy);
        }
        
        public System.Threading.Tasks.Task<string> GetHotelRoomAvailabilityAsync(string AgentUsername, string AgentPassword, int CountryCode, int DestinationCode, string FromDate, string ToDate, string Occupancy, int HotelCode, int CategoryCode, string PassengerNationality, string SortBy) {
            return base.Channel.GetHotelRoomAvailabilityAsync(AgentUsername, AgentPassword, CountryCode, DestinationCode, FromDate, ToDate, Occupancy, HotelCode, CategoryCode, PassengerNationality, SortBy);
        }
        
        public string ConfirmHotelRoomAvailability(string AgentUsername, string AgentPassword, string SessionNB, string ReferenceNB, string RoomPurchaseToken) {
            return base.Channel.ConfirmHotelRoomAvailability(AgentUsername, AgentPassword, SessionNB, ReferenceNB, RoomPurchaseToken);
        }
        
        public System.Threading.Tasks.Task<string> ConfirmHotelRoomAvailabilityAsync(string AgentUsername, string AgentPassword, string SessionNB, string ReferenceNB, string RoomPurchaseToken) {
            return base.Channel.ConfirmHotelRoomAvailabilityAsync(AgentUsername, AgentPassword, SessionNB, ReferenceNB, RoomPurchaseToken);
        }
        
        public string MakeBooking(string AgentUsername, string AgentPassword, string SessionNB, string ReferenceNB, string RoomPurchaseToken, string Passengers, string ClientFirstName, string ClientLastName, string ClientEmail, string ClientComments, string ClientBooingReferenceNB) {
            return base.Channel.MakeBooking(AgentUsername, AgentPassword, SessionNB, ReferenceNB, RoomPurchaseToken, Passengers, ClientFirstName, ClientLastName, ClientEmail, ClientComments, ClientBooingReferenceNB);
        }
        
        public System.Threading.Tasks.Task<string> MakeBookingAsync(string AgentUsername, string AgentPassword, string SessionNB, string ReferenceNB, string RoomPurchaseToken, string Passengers, string ClientFirstName, string ClientLastName, string ClientEmail, string ClientComments, string ClientBooingReferenceNB) {
            return base.Channel.MakeBookingAsync(AgentUsername, AgentPassword, SessionNB, ReferenceNB, RoomPurchaseToken, Passengers, ClientFirstName, ClientLastName, ClientEmail, ClientComments, ClientBooingReferenceNB);
        }
        
        public string CancelBooking(string AgentUsername, string AgentPassword, string BookingLocator, string Type) {
            return base.Channel.CancelBooking(AgentUsername, AgentPassword, BookingLocator, Type);
        }
        
        public System.Threading.Tasks.Task<string> CancelBookingAsync(string AgentUsername, string AgentPassword, string BookingLocator, string Type) {
            return base.Channel.CancelBookingAsync(AgentUsername, AgentPassword, BookingLocator, Type);
        }
        
        public string CheckBooking(string AgentUsername, string AgentPassword, string BookingLocator, string FromDate, string ToDate, string DateType, string ClientBookingReferenceNB) {
            return base.Channel.CheckBooking(AgentUsername, AgentPassword, BookingLocator, FromDate, ToDate, DateType, ClientBookingReferenceNB);
        }
        
        public System.Threading.Tasks.Task<string> CheckBookingAsync(string AgentUsername, string AgentPassword, string BookingLocator, string FromDate, string ToDate, string DateType, string ClientBookingReferenceNB) {
            return base.Channel.CheckBookingAsync(AgentUsername, AgentPassword, BookingLocator, FromDate, ToDate, DateType, ClientBookingReferenceNB);
        }
    }
}
