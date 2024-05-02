using System;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace TravillioXMLOutService.Transfer.Helpers
{
    public static class CommonHelper
    {
        static readonly XElement _credentialList;
        static CommonHelper()
        {
            _credentialList = XElement.Load(HttpContext.Current.Server.MapPath(@"~/App_Data/SupplierCredentialTransfer/transfercredentials.xml"));
        }
        public static XElement ReadCredential(this string custId, string supplId)
        {
            XElement _credential = null;
            try
            {
                if (!string.IsNullOrEmpty(custId))
                {
                    _credential = _credentialList.Descendants("credential").Where(x => x.Attribute("customerid").Value == custId && x.Attribute("supplierid").Value == supplId).FirstOrDefault();
                }
                return _credential;
            }
            catch
            {
                return null;
            }
        }



        public static List<XElement> rmIndex(this IEnumerable<XElement> rmlst, string atrName)
        {
            List<XElement> lst = rmlst.ToList();
            int index = 0;
            lst.ForEach(r => r.Attribute(atrName).Value = (++index).ToString());
            return lst;
        }

        public static IEnumerable<List<T>> CrossJoinLists<T>(List<List<T>> listofObjects)
        {
            var result = from obj in listofObjects.First()
                         select new List<T> { obj };
            for (var i = 1; i < listofObjects.Count(); i++)
            {
                var iLocal = i;
                result = from obj in result
                         from obj2 in listofObjects.ElementAt(iLocal)
                         select new List<T>(obj) { obj2 };
            }
            return result;
        }


        public static string IsXmlDateString(this string strDate)
        {
            string date = string.Empty;
            if (strDate != null)
            {
                DateTime _Startdate;
                DateTime.TryParse(strDate, out _Startdate);
                date = _Startdate.ToString("dd-MM-yyyy");
            }
            return date;
        }
        public static XmlDocument ToXmlDocument(this XDocument xDocument)
        {
            var xmlDocument = new XmlDocument();
            using (var xmlReader = xDocument.CreateReader())
            {
                xmlDocument.Load(xmlReader);
            }
            return xmlDocument;
        }

        public static int ToINT(this XAttribute item, int defaultValue = 0)
        {
            if (item == null)
                return defaultValue;
            else
                return Convert.ToInt32(item.Value);
        }

        public static string GetValueOrDefault(this XElement item, string defaultValue = null)
        {
            if (item == null)
                return defaultValue;
            else
                return item.Value;
        }
        public static string GetValueOrDefault(this XAttribute attribute, string defaultValue = null)
        {
            if (attribute == null)
                return defaultValue;
            else
                return attribute.Value;
        }



        public static string GetValuewithSuffix(this XElement item)
        {
            string data = string.Empty;
            if (item != null)
                data = item.Value + ",";
            return data;
        }



        public static IEnumerable<XElement> DescendantsOrEmpty(this XElement item, XName name)
        {
            IEnumerable<XElement> result;
            if (item != null)
                result = item.Descendants(name).Count() != 0 ? item.Descendants(name) : Enumerable.Empty<XElement>();
            else
                result = Enumerable.Empty<XElement>();
            return result;
        }


        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source != null)
            {
                foreach (T obj in source)
                {
                    return false;
                }
            }
            return true;
        }

        public static DateTime chnagetoTime(this string strDate)
        {
            DateTime oDate = DateTime.ParseExact(strDate, "yyyy-MM-dd HH:mm:ss", null);
            return oDate;
        }

        public static string CxlDate(this XElement item)
        {
            string date = DateTime.Now.AddDays(-10).ToString("yyyy-MM-dd");
            if (item.Element("fromDate") != null)
            {
                date = item.Element("fromDate").Value;
            }
            else if (item.Element("toDate") != null)
            {
                date = item.Element("toDate").Value;
            }
            return date;

        }
        public static int ChangeToInt(this XElement item)
        {
            if (item == null)
                return 0;
            else
                return Convert.ToInt32(item.Value);
        }



        public static XElement RemoveAllNamespaces(this XElement xmlDocument)
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


        public static string AlterFormat(this string strDate, string oldFormat, string newFormat)
        {
            DateTime dte = DateTime.ParseExact(strDate.Trim(), oldFormat, CultureInfo.InvariantCulture);
            return dte.ToString(newFormat);
        }


        public static DateTime GetDateTime(this string strDate, string strTime, string dateTimeFormate)
        {
            string dateTimeStr = strDate + " " + strTime;
            DateTime dte = DateTime.ParseExact(dateTimeStr.Trim(), dateTimeFormate, CultureInfo.InvariantCulture);
            return dte;
        }

        public static DateTime GetDateTime(this string strDateTime, string dateTimeFormate)
        {
            DateTime dte = DateTime.ParseExact(strDateTime.Trim(), dateTimeFormate, CultureInfo.InvariantCulture);
            return dte;
        }

        public static ApiAction GetAction(this string ElementName)
        {
            if (ElementName == "SearchRequest")
            {
                return ApiAction.Search;
            }
            else if (ElementName == "PrebookRequest")
            {
                return ApiAction.PreBook;

            }
            else if (ElementName == "bookRequest")
            {
                return ApiAction.Book;
            }
            else if (ElementName == "TransferCXLPolicyRequest")
            {
                return ApiAction.CXLPolicy;
            }
            else
            {
                return ApiAction.Cancel;
            }

        }

        public static string IsNullString(this string str)
        {
            string result = string.Empty;
            if (str != null)
            {
                result = str;
            }
            return result;
        }
        public static decimal IsNullNumber(this decimal? item)
        {
            return item.HasValue ? item.Value : 0.00m; ;
        }





        public static XElement mergCXLPolicy(this IEnumerable<XElement> PolicyList)
        {

            List<XElement> cxlList = new List<XElement>();
            IEnumerable<XElement> dateLst = PolicyList.GroupBy(r => new
            {
                r.Attribute("lastDate").Value.GetDateTime("yyyy-MM-ddTHH:mm:ss").Date,
                noshow = r.Attribute("noShow").Value
            }).Select(y => y.First()).
               OrderBy(p => p.Attribute("lastDate").Value.GetDateTime("yyyy-MM-ddTHH:mm:ss"));

            if (dateLst.Count() > 0)
            {
                foreach (var item in dateLst)
                {
                    string date = item.Attribute("lastDate").Value;
                    string noShow = item.Attribute("noShow").Value;
                    decimal datePrice = 0.0m;
                    foreach (var rm in PolicyList)
                    {
                        var prItem = rm.Descendants("cancellationPolicy").
                       Where(pq => (pq.Attribute("noShow").Value == noShow && pq.Attribute("lastDate").Value == date)).
                       FirstOrDefault();

                        if (prItem != null)
                        {
                            var price = prItem.Attribute("amount").Value;
                            datePrice += Convert.ToDecimal(price);
                        }
                        else
                        {
                            if (noShow == "1")
                            {
                                datePrice += Convert.ToDecimal(rm.Element("TotalAmount").Value);
                            }
                            else
                            {
                                var lastItem = rm.Descendants("cancellationPolicy").
                                    Where(pq => (pq.Attribute("noShow").Value == noShow && pq.Attribute("lastDate").Value.GetDateTime("yyyy-MM-ddTHH:mm:ss") < date.GetDateTime("yyyy-MM-ddTHH:mm:ss")));
                                if (lastItem.Count() > 0)
                                {
                                    var lastDate = lastItem.Max(y => y.Attribute("lastDate").Value);
                                    var lastprice = rm.Descendants("cancellationPolicy").
                                        Where(pq => (pq.Attribute("noShow").Value == noShow && pq.Attribute("lastDate").Value == lastDate)).
                                        FirstOrDefault().Attribute("amount").Value;
                                    datePrice += Convert.ToDecimal(lastprice);
                                }
                            }
                        }
                    }




                    XElement pItem = new XElement("cancellationPolicy",
                        new XAttribute("lastDate", date),
                        new XAttribute("amount", datePrice),
                        new XAttribute("noShow", noShow));
                    cxlList.Add(pItem);
                }

                cxlList = cxlList.GroupBy(x => new { x.Attribute("lastDate").Value.GetDateTime("yyyyMMdd HHmm").Date, x.Attribute("noShow").Value }).
                    Select(y => new XElement("cancellationPolicy",
                        new XAttribute("lastDate", y.Key.Date.ToString("d MMM, yy")),
                        new XAttribute("amount", y.Max(p => Convert.ToDecimal(p.Attribute("amount").Value))),
                        new XAttribute("noShow", y.Key.Value))).OrderBy(p => p.Attribute("lastDate").Value.GetDateTime("d MMM, yy").Date).ToList();

                var fItem = cxlList.FirstOrDefault();

                if (Convert.ToDecimal(fItem.Attribute("amount").Value) != 0.0m)
                {
                    cxlList.Insert(0, new XElement("cancellationPolicy", new XAttribute("lastDate", fItem.Attribute("lastDate").Value.GetDateTime("d MMM, yy").AddDays(-1).Date.ToString("d MMM, yy")), new XAttribute("amount", "0.00"), new XAttribute("noShow", "0")));
                }
            }
            XElement cxlItem = new XElement("cancellationPolicies", cxlList);
            return cxlItem;
        }











    }



    /// </summary>  
    #region Enums
    public enum ApiAction
    {
        //[Description("Searching")]
        Search = 1,
        //[Description("Cancellation Policy")]
        CXLPolicy = 3,
        //[Description("PreBooking")]
        PreBook = 4,
        //[Description("Booking")]
        Book = 5,
        //[Description("Cancellation")]
        Cancel = 6,

    }

    public enum PickUpType
    {
        //[Description("Airport Terminal")]
        Terminal = 1,
        //[Description("Accomodation")]
        Hotel = 2,


    }

    #endregion








}