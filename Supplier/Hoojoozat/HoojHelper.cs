using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;
using System.Xml.Linq;
using System.Xml;

namespace TravillioXMLOutService.Supplier.Hoojoozat
{
    public static class HoojHelper
    {
        public static string HoojDateString(this string strDate)
        {
            if (!string.IsNullOrEmpty(strDate))
            {
                DateTime dt = DateTime.ParseExact(strDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                return dt.ToString("MM/dd/yyyy");
            }
            else
            {
                return DateTime.MinValue.ToString("MM/dd/yyyy");
            }
        }
        public static decimal ModifyToDecimal(this string str)
        {
            decimal num = 0;
            if (!string.IsNullOrEmpty(str))
            {
                num = Convert.ToDecimal(str);
            }
            return num;

        }
        public static int ModifyToInt(this string str)
        {
            int num = 0;
            if (!string.IsNullOrEmpty(str))
            {
                num = int.Parse(str);
            }
            return num;

        }
        public static DateTime ConvertToDate(this string strDate)
        {
            if (!string.IsNullOrEmpty(strDate))
            {
                DateTime dt = DateTime.ParseExact(strDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                return dt;
            }
            else
            {
                return DateTime.MinValue;
            }
        }
        public static long ConvertToLong(this string str)
        {
            long num = 0;
            if (!string.IsNullOrEmpty(str))
            {
                num = long.Parse(str);
            }
            return num;

        }

        public static string HoojOccupancy(this XElement roompax)
        {
            string occupancyStr = string.Empty;
            List<string> arrOcp = new List<string>();
            foreach (var rpax in roompax.Descendants("RoomPax"))
            {
                string occupancy = rpax.Descendants("Adult").FirstOrDefault().Value + "_" + rpax.Descendants("Child").FirstOrDefault().Value;
                if (!arrOcp.Contains(occupancy))
                {
                    arrOcp.Add(occupancy);
                }
            }
            foreach (var ocp in arrOcp)
            {
                string[] pxarr = ocp.Split('_');
                int cnt = 0;
                string childage = string.Empty;
                foreach (var pax in roompax.Descendants("RoomPax").Where(x => x.Element("Adult").Value == pxarr[0] && x.Element("Child").Value == pxarr[1]))
                {
                    foreach (var ch_age in pax.Descendants("ChildAge"))
                    {
                        if (string.IsNullOrEmpty(childage))
                        {
                            childage = ch_age.Value;
                        }
                        else
                        {
                            childage += "_" + ch_age.Value;
                        }

                    }
                    cnt++;

                }
                if (string.IsNullOrEmpty(childage))
                {
                    childage = "-1";
                }
                if (string.IsNullOrEmpty(occupancyStr))
                {
                    occupancyStr = cnt.ToString() + "_" + ocp + "_" + childage;
                }
                else
                {
                    occupancyStr += ";" + cnt.ToString() + "_" + ocp + "_" + childage;
                }
            }
            return occupancyStr;
        }
        public static XElement RemoveXmlns(this XElement doc)
        {
            doc.Descendants().Attributes().Where(x => x.IsNamespaceDeclaration).Remove();
            foreach (var elem in doc.Descendants())
                elem.Name = elem.Name.LocalName;

            return doc;
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
        public static string getWordInBetween(string sentence, string wordOne, string wordTwo)
        {

            int start = sentence.IndexOf(wordOne) + wordOne.Length + 1;

            int end = sentence.IndexOf(wordTwo) - start - 1;

            return sentence.Substring(start, end);


        }
        public static XElement getHotelImages(this string imagesStr)
        {
            if (string.IsNullOrEmpty(imagesStr))
            {
                return new XElement("Images");
            }
            else
            {
                XElement images = XElement.Parse(imagesStr);
                return images;

            }
        }
        public static string[] Split(this string toSplit, string splitOn)
        {
            return toSplit.Split(new string[] { splitOn }, StringSplitOptions.None);
        }
        public static Dictionary<DateTime, decimal> AddCxlPolicy(this Dictionary<DateTime, decimal> cxlPolicies, DateTime Cxldate, decimal cxlCharges)
        {
            if (cxlPolicies.Count == 0)
            {
                cxlPolicies.Add(Cxldate, cxlCharges);
            }
            else
            {
                int count = cxlPolicies.Count;
                for (int i = 0; i < count; i++)
                {
                    var item = cxlPolicies.ElementAt(i);
                    if (item.Key == Cxldate)
                    {
                        cxlPolicies[item.Key] = item.Value + cxlCharges;
                    }
                    else if (item.Key < Cxldate)
                    {
                        cxlPolicies.Add(Cxldate, item.Value + cxlCharges);
                    }
                    else
                    {
                        cxlPolicies.Add(Cxldate, cxlCharges);
                        cxlPolicies[item.Key] = item.Value + cxlCharges;
                    }
                }

            }
            return cxlPolicies;
        }

        public static string VotToTravyooRating(this string votRating)
        {
            string travayooRating = "0";
            if (votRating == "6" || votRating == "13")
            {
                travayooRating = "0";
            }
            else if (votRating == "16")
            {
                travayooRating = "5";
            }
            else
            {
                travayooRating = votRating;
            }
            return travayooRating;
        }

    }
}