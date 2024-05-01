using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;

namespace TravillioXMLOutService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IAirService" in both code and config file together.
    //[ServiceContract]
    [ServiceContract(SessionMode = SessionMode.NotAllowed)]
    public interface IAirService
    {
        #region Air
        [OperationContract, XmlSerializerFormat]
        object AirAvailability(XElement req);
        [OperationContract, XmlSerializerFormat]
        object AirPriceCheck(XElement req);
        [OperationContract, XmlSerializerFormat]
        object AirFareRules(XElement req);
        [OperationContract, XmlSerializerFormat]
        object AirBook(XElement req);
        [OperationContract, XmlSerializerFormat]
        object AirTicket(XElement req);
        [OperationContract, XmlSerializerFormat]
        object AirCancelTicket(XElement req);
        #endregion
    }
}
