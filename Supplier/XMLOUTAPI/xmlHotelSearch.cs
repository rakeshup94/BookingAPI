using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Models;

namespace TravillioXMLOutService.Supplier.XMLOUTAPI
{
    public class xmlHotelSearch : IDisposable
    {
        XElement reqTravayoo;
        string dmc = string.Empty;
        string customerid = string.Empty;
        #region Hotel Search for XML OUT API
        public List<XElement> hotelSearchXMLOUTAPI(XElement req, string custID, string custName)
        {
            dmc = custName;
            customerid = custID;
            reqTravayoo = req;
            List<XElement> hotelsearchresponse = null;
            try
            {
                #region Request
                XElement request = new XElement("searchRQ",
                    new XAttribute("customerID", custID),
                    new XAttribute("agencyID", req.Descendants("SubAgentID").FirstOrDefault().Value),
                    new XAttribute("sessionID", req.Descendants("TransID").FirstOrDefault().Value),
                    new XAttribute("sourceMarket", req.Descendants("PaxNationality_CountryCode").FirstOrDefault().Value),
                    new XAttribute("cityID", req.Descendants("CityID").FirstOrDefault().Value),
                    new XAttribute("cityCode", req.Descendants("CityCode").FirstOrDefault().Value),
                    new XAttribute("cityName", req.Descendants("CityName").FirstOrDefault().Value),
                     new XAttribute("countryID", req.Descendants("CityID").FirstOrDefault().Value),
                    new XAttribute("countryCode", req.Descendants("CityCode").FirstOrDefault().Value),
                    new XAttribute("currency", req.Descendants("DesiredCurrencyCode").FirstOrDefault().Value),
                    new XElement("stay", new XAttribute("checkIn", req.Descendants("FromDate").FirstOrDefault().Value), new XAttribute("checkOut", req.Descendants("ToDate").FirstOrDefault().Value)),
                    new XElement("occupancies", bindoccupancy(req.Descendants("RoomPax").ToList())),
                    new XElement("hotels",new XElement("hotel")),
                    new XElement("filter", new XAttribute("minRating", req.Descendants("MinStarRating").FirstOrDefault().Value), new XAttribute("maxRating", req.Descendants("MaxStarRating").FirstOrDefault().Value))
                    );
                #endregion
                #region Response
                xmlResponseRequest apireq = new xmlResponseRequest();
                string apiresponse = apireq.xmloutHTTPResponse(request, "Search", 1, req.Descendants("TransID").FirstOrDefault().Value, req.Descendants("CustomerID").FirstOrDefault().Value, "501");
                XElement apiresp = null;
                try
                {
                    apiresp = XElement.Parse(apiresponse);
                    XElement hoteldetailsresponse = XElement.Parse(apiresponse.ToString());
                    apiresp = RemoveAllNamespaces(hoteldetailsresponse);
                }
                catch { }
                hotelsearchresponse = bindhotelList(apiresp.Descendants("hotel").ToList());
                #endregion
            }
            catch(Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "hotelSearchXMLOUTAPI";
                ex1.PageName = "xmlHotelSearch";
                ex1.CustomerID = custID;
                ex1.TranID = reqTravayoo.Descendants("TransID").FirstOrDefault().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
                #endregion
            }
            return hotelsearchresponse;
        }
        #endregion
        #region Bind Hotel List
        private List<XElement> bindhotelList(List<XElement> htlst)
        {
            List<XElement> htlrslt = new List<XElement>();
            string xmlouttype = "false"; 
            try
            {
                Int32 length = Convert.ToInt32(htlst.Count());
                try
                {
                    for (int i = 0; i < length; i++)
                    {
                        try
                        {
                            #region Fetch hotel
                            htlrslt.Add(new XElement("Hotel",
                                                   new XElement("HotelID", Convert.ToString(htlst[i].Attribute("code").Value)),
                                                   new XElement("HotelName", Convert.ToString(htlst[i].Attribute("name").Value)),
                                                   new XElement("PropertyTypeName", Convert.ToString("")),
                                                   new XElement("CountryID", Convert.ToString(reqTravayoo.Descendants("CountryID").FirstOrDefault().Value)),
                                                   new XElement("CountryName", Convert.ToString(reqTravayoo.Descendants("CountryName").FirstOrDefault().Value)),
                                                   new XElement("CountryCode", Convert.ToString(reqTravayoo.Descendants("CountryCode").FirstOrDefault().Value)),
                                                   new XElement("CityId", Convert.ToString(reqTravayoo.Descendants("CityID").FirstOrDefault().Value)),
                                                   new XElement("CityCode", Convert.ToString(reqTravayoo.Descendants("CityCode").FirstOrDefault().Value)),
                                                   new XElement("CityName", Convert.ToString(reqTravayoo.Descendants("CityName").FirstOrDefault().Value)),
                                                   new XElement("AreaId", Convert.ToString(htlst[i].Attribute("zoneCode").Value)),
                                                   new XElement("AreaName", Convert.ToString(htlst[i].Attribute("zoneName").Value)),
                                                   new XElement("RequestID", Convert.ToString(htlst[i].Attribute("zoneCode").Value)),
                                                   new XElement("Address", Convert.ToString(htlst[i].Attribute("address").Value)),
                                                   new XElement("Location", Convert.ToString(htlst[i].Attribute("address").Value)),
                                                   new XElement("StarRating", Convert.ToString(htlst[i].Attribute("categoryName").Value)),
                                                   new XElement("MinRate", Convert.ToString(htlst[i].Attribute("minRate").Value)),
                                                   new XElement("HotelImgSmall", Convert.ToString(htlst[i].Attribute("imgSmall").Value)),
                                                   new XElement("HotelImgLarge", Convert.ToString(htlst[i].Attribute("imgLarge").Value)),
                                                   new XElement("MapLink", ""),
                                                   new XElement("Longitude", Convert.ToString(htlst[i].Attribute("longitude").Value)),
                                                   new XElement("Latitude", Convert.ToString(htlst[i].Attribute("latitude").Value)),
                                                   new XElement("xmloutcustid", customerid),
                                                   new XElement("xmlouttype", xmlouttype),
                                                   new XElement("sessionKey", Convert.ToString(htlst[i].Attribute("sessionKey").Value)),
                                                   new XElement("sourcekey", Convert.ToString(htlst[i].Attribute("sourcekey").Value)),
                                                   new XElement("publishedkey", Convert.ToString(htlst[i].Attribute("publishedkey").Value)),
                                                   new XElement("DMC", dmc),
                                                   new XElement("SupplierID", "501"),
                                                   new XElement("Currency", Convert.ToString(htlst[i].Attribute("currency").Value)),
                                                   new XElement("Offers", "")
                                                   , new XElement("Facilities", null)
                                                   , new XElement("Rooms", ""
                                                       )
                            ));
                            #endregion
                        }
                        catch { }
                    };
                }
                catch (Exception ex)
                {
                    return htlrslt;
                }
            }
            catch { }
            return htlrslt;
        }
        #endregion
        #region Bind Occupancy Request
        private List<XElement> bindoccupancy(List<XElement> roompax)
        {
            List<XElement> occuplst = new List<XElement>();
            try
            {
                for (int i = 0; i < roompax.Count(); i++)
                {
                    string childage = string.Empty;
                    if(Convert.ToInt16(roompax[i].Descendants("Child").FirstOrDefault().Value)>0)
                    {
                        List<XElement> chldlst = roompax[i].Descendants("ChildAge").ToList();
                        for (int j = 0; j < chldlst.Count(); j++)
                        {
                            if(j == roompax[i].Descendants("ChildAge").Count()-1)
                            {
                                childage += childage + chldlst[j].Value;
                            }
                            else
                            {
                                childage = childage + chldlst[j].Value + ",";
                            }
                        }
                    }
                    occuplst.Add(new XElement("occupancy",
                           new XAttribute("room", Convert.ToString("1")),
                           new XAttribute("adult", Convert.ToString(roompax[i].Descendants("Adult").FirstOrDefault().Value)),
                           new XAttribute("children", Convert.ToString(roompax[i].Descendants("Child").FirstOrDefault().Value)),
                           new XAttribute("ages", childage))
                    );
                }
            }
            catch { }
            return occuplst;
        }
        #endregion
        #region Remove Namespaces
        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            XElement xmlDocumentWithoutNs = removeAllNamespaces(xmlDocument);
            return xmlDocumentWithoutNs;
        }

        private static XElement removeAllNamespaces(XElement xmlDocument)
        {
            var stripped = new XElement(xmlDocument.Name.LocalName);
            foreach (var attribute in
                    xmlDocument.Attributes().Where(
                    attribute =>
                        !attribute.IsNamespaceDeclaration &&
                        String.IsNullOrEmpty(attribute.Name.NamespaceName)))
            {
                stripped.Add(new XAttribute(attribute.Name.LocalName, attribute.Value));
            }
            if (!xmlDocument.HasElements)
            {
                stripped.Value = xmlDocument.Value;
                return stripped;
            }
            stripped.Add(xmlDocument.Elements().Select(
                el =>
                    RemoveAllNamespaces(el)));
            return stripped;
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