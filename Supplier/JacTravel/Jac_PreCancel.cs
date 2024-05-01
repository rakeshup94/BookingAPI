using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class Jac_PreCancel
    {


        public XElement Precancel(XElement doc, string User, string Password,string url)
        {
            try
            {
                IEnumerable<XElement> PreCxl = from htl in doc.Descendants("HotelCancellationRequest")
                                               select new XElement("PreCancelRequest",
                                                           new XElement("LoginDetails",
                                                                   new XElement("Login", User),
                                                                   new XElement("Password", Password),
                                                                   new XElement("Locale", ""),
                                                                   new XElement("AgentReference", "")),
                                                                   new XElement("BookingReference", htl.Element("ConfirmationNumber").Value));

                string Request = PreCxl.FirstOrDefault().ToString();
                string Responce = string.Empty;
                if (!string.IsNullOrEmpty(Request))
                {
                    RequestClass requobj = new RequestClass();
                    string PrecancelReq=PreCxl.FirstOrDefault().ToString();
                    int supid = Convert.ToInt32(doc.Descendants("SupplierID").FirstOrDefault().Value);
                    Responce = requobj.HttpPostRequest(url, doc, PrecancelReq, "PreCancel",supid,6);                    
                    if (Responce != "An error occurred: The operation has timed out")
                    {
                        XElement Ele = XElement.Parse(Responce);
                        return CancelBooking(doc, User, Password, Ele);
                    }
                }
            }
            catch 
            {

                return null;
            }

          
            return null; 
        }


        public XElement CancelBooking(XElement doc, string User, string Password, XElement Responce)
        {

            string status = (String)Responce.Element("ReturnStatus").Element("Success");
             if (status == "true")
             {
                 IEnumerable<XElement> Cxl = from htl in doc.Descendants("HotelCancellationRequest")
                                                select new XElement("CancelRequest",
                                                            new XElement("LoginDetails",
                                                                    new XElement("Login", User),
                                                                    new XElement("Password", Password),
                                                                    new XElement("Locale", ""),
                                                                    new XElement("AgentReference", "")),
                                                                    new XElement("BookingReference", htl.Element("ConfirmationNumber").Value),
                                                                    new XElement("CancellationCost", Responce.Element("CancellationCost").Value),
                                                                    new XElement("CancellationToken", Responce.Element("CancellationToken").Value));
                 string CxlRe = Cxl.FirstOrDefault().ToString();
                 if (!string.IsNullOrEmpty(CxlRe))
                 {
                     RequestClass requobj = new RequestClass();
                     int supid = Convert.ToInt32(doc.Descendants("SupplierID").FirstOrDefault().Value);
                     XElement suppliercred = supplier_Cred.getsupplier_credentials(doc.Descendants("CustomerID").FirstOrDefault().Value, Convert.ToString(supid));
                     string url = suppliercred.Descendants("endpoint").FirstOrDefault().Value;
                     CxlRe = requobj.HttpPostRequest(url, doc, CxlRe, "Cancel",supid,6);
                     
                     if (CxlRe != "An error occurred: The operation has timed out")
                     {
                         XElement cxl = XElement.Parse(CxlRe);
                         CxlRe = (String)cxl.Element("ReturnStatus").Element("Success");
                         if (CxlRe == "true")
                         {
                             XElement CxlResponce = new XElement("HotelCancellationResponse",
                                 new XElement("Rooms",
                                     new XElement("Room",
                                         new XElement("Cancellation",
                                             new XElement("Amount", Responce.Element("CancellationCost").Value),
                                             new XElement("Status", "Success")))));

                             IEnumerable<XElement> Descoll = doc.Descendants("HotelCancellationRequest");
                             foreach (XElement item in Descoll)
                             {
                                 item.AddAfterSelf(CxlResponce);
                             }                            
                            
                         }
                         else
                         {
                             return doc;
                         }
                     }                    
                 }   
             }
             return doc;
        }
    }
}
