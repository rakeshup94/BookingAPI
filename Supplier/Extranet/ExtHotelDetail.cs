using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.Extranet
{
    public class ExtHotelDetail
    {
        string Phone = string.Empty;
        string Fax = string.Empty;
        string checkintime = string.Empty;
        string checkouttime = string.Empty;
        IEnumerable<XElement> image = null;
        IEnumerable<XElement> facility = null;
        public XElement HotelDetailExtranet(XElement req)
        {
            XElement hoteldescdoc = null;
            HotelExtranet.ExtXmlOutServiceClient extclient = new HotelExtranet.ExtXmlOutServiceClient();
            #region Extranet Request/Response
            string requestxml = string.Empty;
            string hoteldescription = string.Empty;
            #region Credentials
            string exAgentID = string.Empty;
            string exUserName = string.Empty;
            string exPassword = string.Empty;
            string exServiceType = string.Empty;
            string exServiceVersion = string.Empty;
            XElement suppliercred = supplier_Cred.getsupplier_credentials(req.Descendants("CustomerID").FirstOrDefault().Value, "3");
            exAgentID = suppliercred.Descendants("AgentID").FirstOrDefault().Value;
            exUserName = suppliercred.Descendants("UserName").FirstOrDefault().Value;
            exPassword = suppliercred.Descendants("Password").FirstOrDefault().Value;
            exServiceType = suppliercred.Descendants("ServiceType").FirstOrDefault().Value;
            exServiceVersion = suppliercred.Descendants("ServiceVersion").FirstOrDefault().Value;
            #endregion
            requestxml = "<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                          "<soapenv:Header xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/'>" +
                            "<Authentication>" +
                                 "<AgentID>" + exAgentID + "</AgentID>" +
                                  "<UserName>" + exUserName + "</UserName>" +
                                  "<Password>" + exPassword + "</Password>" +
                                  "<ServiceType>" + exServiceType + "</ServiceType>" +
                                  "<ServiceVersion>" + exServiceVersion + "</ServiceVersion>" +
                                "</Authentication>" +
                          "</soapenv:Header>" +
                          "<soapenv:Body>" +
                            "<HotelDetailRequest>" +
                              "<Response_Type>XML</Response_Type>" +
                              "<HotelNo>" + req.Descendants("HotelID").SingleOrDefault().Value + "</HotelNo>" +
                            "</HotelDetailRequest>" +
                          "</soapenv:Body>" +
                        "</soapenv:Envelope>";
            try
            {
                object result = extclient.HotelDetailResponse(requestxml);
                if (result != null)
                {
                    XElement doc = XElement.Parse(result.ToString());
                    try
                    {
                        hoteldescription = doc.Descendants("Description").FirstOrDefault().Value;
                    }
                    catch { }
                    try
                    {
                        checkintime = doc.Descendants("CheckInTime").FirstOrDefault().Value;
                        checkouttime = doc.Descendants("CheckOutTime").FirstOrDefault().Value;
                    }
                    catch { }
                    try
                    { Phone = doc.Descendants("phoneno").FirstOrDefault().Value; }
                    catch { }
                    try {
                        facility = hotelfacilitiesExtranet(doc.Descendants("Facility").ToList());
                    }
                    catch { }
                    try { image = hotelimagesExtranet(doc.Descendants("Image").ToList()); }
                    catch { }
                    #region Hotel Details XML OUT
                    IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    hoteldescdoc = new XElement(
                      new XElement(soapenv + "Envelope",
                                new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                new XElement(soapenv + "Header",
                                 new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                 new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travillio"), new XElement("Password", "ing@tech"), new XElement("ServiceType", "HT_001"), new XElement("ServiceVersion", "v1.0"))),
                                 new XElement(soapenv + "Body",
                                     new XElement(request.Single()),
                           new XElement("hoteldescResponse",
                               new XElement("Hotels",
                                   new XElement("Hotel",
                                       new XElement("HotelID", Convert.ToString(req.Descendants("HotelID").SingleOrDefault().Value)),
                                       new XElement("Description", Convert.ToString(hoteldescription)),
                                       new XElement("Images", image),
                                       new XElement("Facilities", facility),
                                        new XElement("ContactDetails",
                                            new XElement("Phone", Convert.ToString(Phone)),
                                            new XElement("Fax", Convert.ToString(Fax))),
                                        new XElement("CheckinTime", Convert.ToString(checkintime)),
                                       new XElement("CheckoutTime", Convert.ToString(checkouttime))
                                       ))))));

                    return hoteldescdoc;
                    #endregion
                }
                else
                {

                    #region Hotel Details XML OUT
                    IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    hoteldescdoc = new XElement(
                      new XElement(soapenv + "Envelope",
                                new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                new XElement(soapenv + "Header",
                                 new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                 new XElement("Authentication", new XElement("AgentID", "TRV"), new XElement("UserName", "Travillio"), new XElement("Password", "ing@tech"), new XElement("ServiceType", "HT_001"), new XElement("ServiceVersion", "v1.0"))),
                                 new XElement(soapenv + "Body",
                                     new XElement(request.Single()),
                           new XElement("hoteldescResponse",
                               new XElement("Hotels",
                                   new XElement("Hotel",
                                       new XElement("HotelID", Convert.ToString(req.Descendants("HotelID").SingleOrDefault().Value)),
                                       new XElement("Description", Convert.ToString(hoteldescription)),
                                       new XElement("Images", null),
                                       new XElement("Facilities", null),
                                        new XElement("ContactDetails",
                                            new XElement("Phone", Convert.ToString(Phone)),
                                            new XElement("Fax", Convert.ToString(Fax))),
                                        new XElement("CheckinTime", Convert.ToString(checkintime)),
                                       new XElement("CheckoutTime", Convert.ToString(checkouttime))
                                       ))))));

                    return hoteldescdoc;
                    #endregion

                }
            }
            catch (Exception ex)
            {

                hoteldescription = "";
            }
            #endregion

            return hoteldescdoc;


        }
        #region Hotel Images Extranet
        public IEnumerable<XElement> hotelimagesExtranet(List<XElement> images)
        {
            Int32 length = images.Count();
            List<XElement> image = new List<XElement>();

            if (length == 0)
            {
                image.Add(new XElement("Image", new XAttribute("Path", ""), new XAttribute("Caption", "")));
            }
            else
            {
                Parallel.For(0, length, i =>
                {
                    image.Add(new XElement("Image",
                        new XAttribute("Path", Convert.ToString(images[i].Value)),
                      new XAttribute("Caption", Convert.ToString(""))));

                });
            }
            return image;
        }
        #endregion
        #region Hotel Facilities Extranet
        public IEnumerable<XElement> hotelfacilitiesExtranet(List<XElement> facility)
        {
            Int32 length = facility.Count();
            List<XElement> image = new List<XElement>();

            if (length == 0)
            {
                image.Add(new XElement("Facility", "No Facility Available"));
            }
            else
            {
                Parallel.For(0, length, i =>
                {
                    image.Add(new XElement("Facility", Convert.ToString(facility[i].Value)));

                });
            }
            return image;
        }
        #endregion


    }
}