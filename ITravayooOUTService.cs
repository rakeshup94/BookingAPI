using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Linq;
using TravillioXMLOutService.App_Code;

namespace TravillioXMLOutService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ITravayooOUTService" in both code and config file together.
    //[ServiceContract]
    [ServiceContract(SessionMode = SessionMode.NotAllowed)]
    public interface ITravayooOUTService
    {
        //[OperationContract(Name = "Main Function for checking hotels + apartments availabilities & Rates, You need to specify Country, City , Check in / out Date, Adult Pax + Child Pax , it is highly recommended to provide Hotel Type filters , Detailed Description can be found in Travillio XML API Documentation. Response provides available Rates per Stay"), XmlSerializerFormat]
        #region Hotel
        [OperationContract, XmlSerializerFormat]
        object HotelAvailability(XElement req);
        [OperationContract, XmlSerializerFormat]
        object HotelDetails(XElement req);
        [OperationContract, XmlSerializerFormat]
        object HotelDetailWithCancellations(XElement req);
        [OperationContract, XmlSerializerFormat]
        object HotelPreBooking(XElement req);
        [OperationContract, XmlSerializerFormat]
        object HotelBookingConfirmation(XElement req);
        [OperationContract, XmlSerializerFormat]
        object HotelImportBooking(XElement req);
        [OperationContract, XmlSerializerFormat]
        object HotelCancellation(XElement req);
        [OperationContract, XmlSerializerFormat]
        object HotelCancellationFee(XElement req);
        [OperationContract, XmlSerializerFormat]
        object HotelRoomsAvail(XElement req);
        [OperationContract, XmlSerializerFormat]
        object HotelRoomsDesc(XElement req);
        #endregion
    }
}
