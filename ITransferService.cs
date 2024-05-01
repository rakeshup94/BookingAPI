using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Xml.Linq;

namespace TravillioXMLOutService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "ITransferService" in both code and config file together.
    //[ServiceContract]   
     [ServiceContract(SessionMode = SessionMode.NotAllowed)]
    public interface ITransferService
    {
        #region Transfer
        [OperationContract, XmlSerializerFormat]
        object TransferAvailability(XElement req);
        [OperationContract, XmlSerializerFormat]
        object CXLPolicyTransfer(XElement req);
        [OperationContract, XmlSerializerFormat]
        object PreBookTransfer(XElement req);
        [OperationContract, XmlSerializerFormat]
        object ConfirmBookingTransfer(XElement req);
        [OperationContract, XmlSerializerFormat]
        object CancelBookingTransfer(XElement req);
        #endregion
    }
}
