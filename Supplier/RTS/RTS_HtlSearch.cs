using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;

using System.Threading;
using TravillioXMLOutService.Models;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;


namespace TravillioXMLOutService.Supplier.RTS
{
    public class RTS_HtlSearch
    {
        string customerid = string.Empty;
        string dmc = string.Empty;
        public XDocument SearchHotel(XElement Req, string citycode, string GuestCotyCode, string custID, string custName)
        {
            customerid = custID;
            dmc = custName;
            //string sitecode = ConfigurationManager.AppSettings["RTSitecode"].ToString();
            //string Password = ConfigurationManager.AppSettings["RTSPassword"].ToString();
            //string Requetype = ConfigurationManager.AppSettings["RTSReqType"].ToString();
            //string RTSClientCurrencyCode = ConfigurationManager.AppSettings["RTSClientCurrencyCode"].ToString();
            //string RTSellerMarkup = ConfigurationManager.AppSettings["RTSellerMarkup"].ToString();
            #region Credential
            XElement suppliercred = supplier_Cred.getsupplier_credentials(custID, "9");
            string sitecode = suppliercred.Descendants("RTSitecode").FirstOrDefault().Value;
            string Password = suppliercred.Descendants("RTSPassword").FirstOrDefault().Value;
            string Requetype = suppliercred.Descendants("RTSReqType").FirstOrDefault().Value;
            string RTSClientCurrencyCode = suppliercred.Descendants("RTSClientCurrencyCode").FirstOrDefault().Value;
            string RTSellerMarkup = suppliercred.Descendants("RTSellerMarkup").FirstOrDefault().Value;
            #endregion




            #region New
            string ht_giataid = Req.Descendants("HotelID").FirstOrDefault().Value;
            string CityID = Req.Descendants("CityID").FirstOrDefault().Value;


            List<string> Hids = null;
            if (!string.IsNullOrEmpty(ht_giataid))
            {

                string name = null;
                if (Req.Descendants("HotelName").FirstOrDefault() != null)
                    name = Req.Descendants("HotelName").FirstOrDefault().Value;
                RTS_gethotelbyGiata rtshtids = new RTS_gethotelbyGiata();
                DataTable Hotelids = rtshtids.GeHotel_RTS(ht_giataid, name, CityID);
                
                if (Hotelids != null && Hotelids.Rows.Count != 0)
                {
                    Hids = new List<string>();
                    if (Hotelids.Rows.Count != 0)
                    {
                        Hids.Add(Hotelids.Rows[0]["hotelid"].ToString());
                    }
                }
                else
                {
                    //throw new Exception("There is no hotel available in database");
                    return null;
                }
            }
            #endregion










            XDocument ele = null;
            try
            {

                XNamespace soap = "http://schemas.xmlsoap.org/soap/envelope/";
                XNamespace rts = "http://www.rts.co.kr/";
                IEnumerable<XElement> lst = from htl in Req.Descendants("searchRequest")
                                            select new XElement(soap + "Envelope",
                                                                            new XAttribute(XNamespace.Xmlns + "soapenv", soap),
                                                                             new XAttribute(XNamespace.Xmlns + "rts", rts),
                                                                             new XElement(soap + "Header",
                                                                                 new XElement(rts + "BaseInfo",
                                                                                 new XElement(rts + "SiteCode", sitecode),
                                                                                  new XElement(rts + "Password", Password),
                                                                                   new XElement(rts + "RequestType", Requetype))),
                                                                                   new XElement(soap + "Body",
                                                                                       new XElement(rts + "GetHotelSearchListForCustomerCount",
                                                                                           new XElement(rts + "HotelSearchListNetGuestCount",
                                                                                           new XElement(rts + "LanguageCode", "EN"),
                                                                                           new XElement(rts + "TravelerNationality", GuestCotyCode),
                                                                                           new XElement(rts + "CityCode", citycode),
                                                                                           new XElement(rts + "CheckInDate", JacHelper.MyDate(htl.Element("FromDate").Value)),
                                                                                           new XElement(rts + "CheckOutDate", JacHelper.MyDate(htl.Element("ToDate").Value)),
                                                                                           new XElement(rts + "StarRating", 0),
                                                                                           new XElement(rts + "LocationCode", ""),
                                                                                           new XElement(rts + "SupplierCompCode", ""),
                                                                                            new XElement(rts + "AvailableHotelOnly", true),
                                                                                             new XElement(rts + "RecommendHotelOnly", false),
                                                                                             new XElement(rts + "ClientCurrencyCode", RTSClientCurrencyCode),
                                                                                             new XElement(rts + "ItemName", ""),
                                                                                              new XElement(rts + "SellerMarkup", RTSellerMarkup),
                                                                                               new XElement(rts + "CompareYn", false),
                                                                                                new XElement(rts + "SortType", ""),
                                                                                                new XElement(rts + "ItemCodeList", GetHotelList(Hids, rts)),
                                                //new XElement(rts + "ItemCodeList",
                                                //    new XElement(rts + "ItemCodeInfo",
                                                //        new XElement(rts + "ItemCode", htl.Element("HotelID").Value == "0" ? string.Empty : htl.Element("HotelID").Value),
                                                //        new XElement(rts + "ItemNo", 0))),
                                                                                                         new XElement(rts + "GuestList",
                                                                                                             GetRomTag(htl.Descendants("Rooms").FirstOrDefault(), rts))
                                                                                                             ))));





                //string RTSrhhtlURL = ConfigurationManager.AppSettings["RTSrhhtlURL"].ToString();
                string RTSrhhtlURL = suppliercred.Descendants("RTSrhhtlURL").FirstOrDefault().Value;
                RequestClass obj = new RequestClass();
                ele = obj.HttpPostRequestxmlout(RTSrhhtlURL, lst.FirstOrDefault().ToString(), Req, "Search", 1, custID);
            }
            catch (Exception ex)
            {
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "SearchHotel";
                ex1.PageName = "RTS_HtlSearch";
                ex1.CustomerID = customerid;
                ex1.TranID = Req.Descendants("TransID").Single().Value;
                SaveAPILog saveex = new SaveAPILog();
                saveex.SendCustomExcepToDB(ex1);
                return null;
            }
            return ele;
        }




        List<XElement> GetRomTag(XElement rooms, XNamespace rts)
        {
            List<XElement> finalele = new List<XElement>();
            Dictionary<int, int> dic = new Dictionary<int, int>();
            List<XElement> adultlst = rooms.Descendants("RoomPax").Where(x => x.Element("Child").Value == "0").ToList();
            List<XElement> chidlst = rooms.Descendants("RoomPax").Where(x => x.Element("Child").Value != "0").ToList();


            foreach (XElement item in adultlst)
            {
                int val = Convert.ToInt16(item.Element("Adult").Value);
                if (dic.ContainsKey(val))
                {
                    int dicval = dic[val] + 1;
                    dic[val] = dicval;
                }
                else
                {
                    dic.Add(val, 1);
                }
            }

            foreach (var item in dic)
            {
                XElement ele = new XElement(rts + "GuestsInfo",
                                 new XElement(rts + "AdultCount", item.Key),
                                  new XElement(rts + "ChildCount", 0),
                                  new XElement(rts + "RoomCount", item.Value),
                                  new XElement(rts + "ChildAge1", "0"),
                                  new XElement(rts + "ChildAge2", "0"));
                finalele.Add(ele);
            }
            foreach (XElement item in chidlst)
            {
                XElement ele = new XElement(rts + "GuestsInfo",
                    new XElement(rts + "AdultCount", item.Element("Adult").Value),
                          new XElement(rts + "ChildCount", item.Element("Child").Value),
                               new XElement(rts + "RoomCount", 1),
                               GetChildAge(item.Descendants("ChildAge"), rts));
                finalele.Add(ele);
            }
            return finalele;
        }


        List<XElement> GetChildAge(IEnumerable<XElement> chld, XNamespace rts)
        {
            List<XElement> lst = new List<XElement>();
            int count = chld.Count();
            int i = 1;
            foreach (XElement item in chld)
            {
                string str = "ChildAge" + i.ToString();
                lst.Add(new XElement(rts + str, item.Value));
                i++;
            }
            for (int j = lst.Count; j < 2; j++)
            {
                i = j + 1;
                string str = "ChildAge" + i.ToString();
                lst.Add(new XElement(rts + str, 0));
            }


            return lst;
        }

        public IEnumerable<XElement> HTLResponce(XDocument ele, XElement Req, XElement staticdata, string custID, string dmc)
        {

            IEnumerable<XElement> Htlst = null;
            XNamespace ns = "http://www.rts.co.kr/";
            customerid = custID;
            string xmlouttype = string.Empty;
            try
            {
                if (dmc == "RTS")
                {
                    xmlouttype = "false";
                }
                else
                { xmlouttype = "true"; }
            }
            catch { }
            foreach (XElement Srch in Req.Descendants("searchRequest"))
            {
                Htlst = from htl in ele.Descendants(ns + "HotelItemInfo")
                        join htldesc in staticdata.Descendants("HtlData")
                 on htl.Element(ns + "ItemCode").Value equals htldesc.Element("ItemCode").Value
                        select (Convert.ToInt16(Srch.Element("MinStarRating").Value) <= Convert.ToInt16(htl.Element(ns + "StarRating").Value == string.Empty ? "0" : htl.Element(ns + "StarRating").Value.Replace("+", string.Empty)) && Convert.ToInt16(Convert.ToInt16(htl.Element(ns + "StarRating").Value == string.Empty ? "0" : htl.Element(ns + "StarRating").Value.Replace("+", string.Empty))) <= Convert.ToInt16(Srch.Element("MaxStarRating").Value)) ?
                        new XElement("Hotel",
                                    new XElement("HotelID", htl.Element(ns + "ItemCode").Value),
                                    new XElement("HotelName", htl.Element(ns + "ItemName").Value),
                                    new XElement("PropertyTypeName", string.Empty),
                                   new XElement("CountryID", Srch.Element("CountryID").Value),
                                    new XElement("RequestID", string.Empty),
                                    new XElement("CountryCode", ele.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CountryCode").Value),
                                    new XElement("CountryName", ele.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CountryName").Value),
                                    new XElement("CityID", Srch.Element("CityID") == null ? string.Empty : Srch.Element("CityID").Value),
                                   new XElement("CityCode", ele.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CityCode") == null ? string.Empty : ele.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CityCode").Value),
                                    new XElement("CityName", ele.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CityName") == null ? string.Empty : ele.Descendants(ns + "GetHotelSearchListResult").FirstOrDefault().Element(ns + "CityName").Value),
                                    new XElement("AreaName", htldesc.Element("Location") != null ? htldesc.Element("Location").Value : string.Empty),
                                    new XElement("AreaId", ""),
                                    new XElement("Address", htldesc.Element("Address") != null ? htldesc.Element("Address").Value : string.Empty),
                                    new XElement("Location", ""),
                                    new XElement("Description", string.Empty),
                                    new XElement("StarRating", htl.Element(ns + "StarRating").Value.Replace("+", string.Empty)),
                                    new XElement("HotelImgSmall", htldesc.Element("Img") != null ? htldesc.Element("Img").Value : string.Empty),
                                    new XElement("HotelImgLarge", htldesc.Element("Img") != null ? htldesc.Element("Img").Value : string.Empty),
                                    new XElement("MinRate", GetMinRate(ns, htl)),
                                    new XElement("Longitude", htl.Element(ns + "GeoCode") != null ? htl.Element(ns + "GeoCode").Element(ns + "Longitude").Value : string.Empty),
                                    new XElement("Latitude", htl.Element(ns + "GeoCode") != null ? htl.Element(ns + "GeoCode").Element(ns + "Latitude").Value : string.Empty),
                                    new XElement("xmloutcustid", customerid),
                                    new XElement("xmlouttype", xmlouttype),
                                    new XElement("DMC", dmc),
                                    new XElement("SupplierID", "9"),
                                    new XElement("Currency", ele.Descendants(ns + "ClientCurrencyCode").FirstOrDefault().Value),
                                    new XElement("Offers", ""),
                                    new XElement("Facilities", null)
                            //htldesc.Descendants("Facility") != null ? htldesc.Descendants("Facility") : null)
                                    , new XElement("Rooms", null)) : null;
            }

            return Htlst;
        }


        public decimal GetMinRate(XNamespace ns, XElement rom)
        {
            IEnumerable<XElement> lstrom = rom.Descendants(ns + "PriceInfo");
            decimal MinRate = 0;
            foreach (XElement item in lstrom)
            {
                if (Convert.ToDecimal(item.Element(ns + "SellerNetPrice").Value) < MinRate || MinRate == 0)
                {
                    MinRate = Convert.ToDecimal(item.Element(ns + "SellerNetPrice").Value);

                }
            }
            return MinRate;
        }



        List<XElement> GetHotelList(List<string> hotls, XNamespace rts)
        {


            List<XElement> Htids = new List<XElement>();
            if (hotls != null)
                foreach (var item in hotls)
                {
                    Htids.Add(new XElement(rts + "ItemCodeInfo",
                      new XElement(rts + "ItemCode", item),
                        new XElement(rts + "ItemNo", 1)));


                }
            else
            {
                Htids.Add(new XElement(rts + "ItemCodeInfo", new XElement(rts + "ItemCode", string.Empty), new XElement(rts + "ItemNo", 0)));
            }

            return Htids;
        }






    }




    public class RTS_gethotelbyGiata
    {
        SqlConnection con;
        private void connecttion()
        {
            string constr = ConfigurationManager.ConnectionStrings["INGMContext"].ToString();
            con = new SqlConnection(constr);
            con.Open();
        }
        public DataTable GeHotel_RTS(string giataid, string hotelname, string city)
        {
            DataTable dt = new DataTable();
            try
            {



                connecttion();
                SqlCommand com = new SqlCommand("usp_gethtlidsbygiatadata_test", con);
                com.CommandType = CommandType.StoredProcedure;
                com.Parameters.AddWithValue("@hotelcode", giataid);
                com.Parameters.AddWithValue("@hotelname", hotelname);
                com.Parameters.AddWithValue("@city", city);

                connecttion();
                dt.Load(com.ExecuteReader());
                return dt;
            }
            finally
            {
                con.Dispose();
            }
        }
    }








}
