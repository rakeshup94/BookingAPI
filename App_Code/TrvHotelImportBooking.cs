using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Net;
using System.Xml;
using TravillioXMLOutService.App_Code;
using TravillioXMLOutService.Models;
using System.Xml.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using TravillioXMLOutService.Supplier.TouricoHolidays;
using TravillioXMLOutService.Supplier.HotelsPro;

namespace TravillioXMLOutService.App_Code
{
    public class TrvHotelImportBooking:IDisposable
    {
        #region Logs
        public void WriteToFile(string text)
        {
            try
            {
                string path = Convert.ToString(HttpContext.Current.Server.MapPath(@"~\log.txt"));
                using (StreamWriter writer = new StreamWriter(path, true))
                {
                    writer.WriteLine(string.Format(text, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt")));
                    writer.WriteLine(DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt"));
                    writer.WriteLine("---------------------------Import Booking Response-----------------------------------------");
                    writer.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }
        #endregion
        #region Booking (XML OUT for Travayoo)
        public XElement HotelImportBooking(XElement req)
        {
            #region XML OUT
            #region Header Authentication and header's parameters
            HeaderAuth headercheck = new HeaderAuth();
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;
            string supplierid = req.Descendants("SuppliersID").Single().Value;
            #endregion
            if (headercheck.Headervalidate(username, password, AgentID, ServiceType, ServiceVersion) == true)
            {
                #region Booking
                #region Supplier Credentials
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(supplier_Cred).TypeHandle);
                #endregion
                try
                {                    
                    #region Tourico
                    if (supplierid == "2")
                    {
                        #region Tourico
                        Tr_importbkng touricoreq = new Tr_importbkng();
                        XElement hotelBooking = touricoreq.importbooking_tourico(req);
                        return hotelBooking;
                        #endregion
                    }
                    #endregion
                    #region HotelBeds
                    if (supplierid == "4")
                    {
                        #region HotelBeds
                        ImprtBookingHotelBeds hbreq = new ImprtBookingHotelBeds();
                        XElement hotelBooking = hbreq.ImportBookingHotelBeds(req);
                        return hotelBooking;
                        #endregion
                    }
                    #endregion
                    #region HotelsPro
                    if (supplierid == "6")
                    {
                        #region HotelsPro
                        HotelsProImport hpreq = new HotelsProImport();
                        XElement hotelBooking = hpreq.importbooking_hotelspro(req);
                        return hotelBooking;
                        #endregion
                    }
                    #endregion
                    #region Supplier doesn't Exists
                    else
                    {
                        #region No Supplier's Details Found
                        IEnumerable<XElement> request = req.Descendants("HotelImportBookingRequest");
                        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                        XElement bookingdoc = new XElement(
                          new XElement(soapenv + "Envelope",
                                    new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                    new XElement(soapenv + "Header",
                                     new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                     new XElement("Authentication",
                                         new XElement("AgentID", AgentID),
                                         new XElement("UserName", username),
                                         new XElement("Password", password),
                                         new XElement("ServiceType", ServiceType),
                                         new XElement("ServiceVersion", ServiceVersion))),
                                     new XElement(soapenv + "Body",
                                         new XElement(request.Single()),
                               new XElement("HotelImportBookingResponse",
                                   new XElement("ErrorTxt", "No Supplier's Details Found")
                                           )
                                       )
                      ));
                        return bookingdoc;
                        #endregion
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    #region Exception
                    IEnumerable<XElement> request = req.Descendants("HotelImportBookingRequest");
                    XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                    XElement bookingdoc = new XElement(
                      new XElement(soapenv + "Envelope",
                                new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                new XElement(soapenv + "Header",
                                 new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                                 new XElement("Authentication",
                                     new XElement("AgentID", AgentID),
                                     new XElement("UserName", username),
                                     new XElement("Password", password),
                                     new XElement("ServiceType", ServiceType),
                                     new XElement("ServiceVersion", ServiceVersion))),
                                 new XElement(soapenv + "Body",
                                     new XElement(request.Single()),
                           new XElement("HotelImportBookingResponse",
                               new XElement("ErrorTxt", ex.Message)
                                       )
                                   )
                  ));
                    return bookingdoc;
                    #endregion
                }
                #endregion
            }
            else
            {
                #region Invalid Credential
                IEnumerable<XElement> request = req.Descendants("HotelImportBookingRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement bookingdoc = new XElement(
                  new XElement(soapenv + "Envelope",
                            new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                            new XElement(soapenv + "Header",
                             new XAttribute(XNamespace.Xmlns + "soapenv", soapenv),
                             new XElement("Authentication",
                                 new XElement("AgentID", AgentID),
                                 new XElement("UserName", username),
                                 new XElement("Password", password),
                                 new XElement("ServiceType", ServiceType),
                                 new XElement("ServiceVersion", ServiceVersion))),
                             new XElement(soapenv + "Body",
                                 new XElement(request.Single()),
                       new XElement("HotelImportBookingResponse",
                           new XElement("ErrorTxt", "Invalid Credentials")
                                   )
                               )
              ));
                return bookingdoc;
                #endregion
            }
            #endregion
        }
        #endregion        
        #region Dispose
        /// <summary>
        /// Dispose all used resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}