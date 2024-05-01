using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.App_Code
{
    public class HotelsProHtlDetail
    {
        string hoteldescription = string.Empty;
        string Phone = string.Empty;
        string Fax = string.Empty;
        string checkintime = string.Empty;
        string checkouttime = string.Empty;
        IEnumerable<XElement> image = null;
        IEnumerable<XElement> facility = null;

        #region Hotel Detail Request
        public XElement HotelDetailHotelsPro(XElement req)
        {
            string username = req.Descendants("UserName").Single().Value;
            string password = req.Descendants("Password").Single().Value;
            string AgentID = req.Descendants("AgentID").Single().Value;
            string ServiceType = req.Descendants("ServiceType").Single().Value;
            string ServiceVersion = req.Descendants("ServiceVersion").Single().Value;

            try
            {
                XElement hoteldetail = null;
                string hotelid = req.Descendants("HotelID").SingleOrDefault().Value;

                HotelsPro_Detail htlprostat = new HotelsPro_Detail();
                HotelsPro_Hotelstatic htlprostaticdet = new HotelsPro_Hotelstatic();
                htlprostat.HotelCode = hotelid;
                DataTable dt = htlprostaticdet.GetHotelDetail_HotelsPro(htlprostat);

                if (dt != null)
                {
                    string jsonres = dt.Rows[0]["HotelJson"].ToString();
                    dynamic hotellist = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonres);
                    try
                    {
                        try
                        {
                            hoteldescription = hotellist.descriptions.attraction_information;
                        }
                        catch { }
                        try
                        {
                            Phone = hotellist.phone;
                        }
                        catch { }
                        try
                        {
                            checkintime = hotellist.checkin_from;
                        }
                        catch { }
                        try
                        {
                            checkouttime = hotellist.checkout_to;
                        }
                        catch { }
                        try
                        {
                            dynamic images = hotellist.images;
                            dynamic facilities = hotellist.facilities;
                            image = hotelimagesHotelsPro(images);
                            facility = hotelfacilitiesHotelsPro(facilities);
                        }
                        catch { }


                        #region Hotel Details XML OUT
                        IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                        XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                        XElement hoteldescdoc = new XElement(
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

                        hoteldetail = hoteldescdoc;

                        #endregion

                    }
                    catch { }

                }
                return hoteldetail;
            }
            catch (Exception ex)
            {
                #region Exception
                IEnumerable<XElement> request = req.Descendants("hoteldescRequest");
                XNamespace soapenv = "http://schemas.xmlsoap.org/soap/envelope/";
                XElement hoteldescdoc = new XElement(
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
                       new XElement("hoteldescResponse",
                           new XElement("ErrorTxt", ex.Message)
                                   )
                               )
              ));
                return hoteldescdoc;
                #endregion
            }

        }
        #endregion
        #region Hotel Images HotelsPro
        public IEnumerable<XElement> hotelimagesHotelsPro(dynamic images)
        {
            Int32 length = images.Count;
            List<XElement> image = new List<XElement>();

            if (length == 0)
            {
                image.Add(new XElement("Image", new XAttribute("Path", ""), new XAttribute("Caption", "")));
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    image.Add(new XElement("Image",
                        new XAttribute("Path", Convert.ToString(images[i].thumbnail_images.large)),
                      new XAttribute("Caption", Convert.ToString(images[i].tag))));
                }
            }
            return image;
        }
        #endregion
        #region Hotel Facilities HotelsPro
        public IEnumerable<XElement> hotelfacilitiesHotelsPro(dynamic facility)
        {
            Int32 length = 0;
            if (facility != null)
            {
                length = facility.Count;
            }

            //Int32 length = facility.Count;
            List<XElement> faci = new List<XElement>();
            string facilityname = string.Empty;
            if (length == 0)
            {
                faci.Add(new XElement("Facility", "No Facility Available"));
            }
            else
            {

                XElement fac = XElement.Load(HttpContext.Current.Server.MapPath(@"~\App_Data\HotelsPro\Facilities.xml"));

                List<XElement> totfac = fac.Descendants("item").Where(x => x.Attribute("type").Value == "object").ToList();

                for (int i = 0; i < length; i++)
                {
                    try
                    {
                        string code = facility[i].Value;
                        XElement fcname = totfac.Where(x => x.Descendants("code").FirstOrDefault().Value == code).FirstOrDefault();
                        facilityname = fcname.Descendants("name").FirstOrDefault().Value;

                        faci.Add(new XElement("Facility", Convert.ToString(facilityname)));
                    }
                    catch { }
                }
            }
            return faci;
        }
        #endregion
    }
}