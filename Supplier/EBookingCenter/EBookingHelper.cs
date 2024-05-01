using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;
using System.Xml.Linq;
using System.Xml;

namespace TravillioXMLOutService.Supplier.EBookingCenter
{
    public static class EBookingHelper
    {
        public static string EBookDateString(this string strDate)
        {
            if (!string.IsNullOrEmpty(strDate))
            {
                DateTime dt = DateTime.ParseExact(strDate.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture);
                return dt.ToString("yyyy-MM-dd");
            }
            else
            {
                return DateTime.MinValue.ToString("yyyy-MM-dd");
            }
        }
        public static DateTime EBookCxlDate(this string strDate)
        {
            if (!string.IsNullOrEmpty(strDate))
            {

                string offset = strDate.Split('+')[1];
                DateTime dt = DateTime.ParseExact(strDate.Trim(), "yyyy-MM-ddTHH:mm:ss+" + offset, CultureInfo.InvariantCulture);
                DateTimeOffset travcoffset = new DateTimeOffset(dt.Ticks, new TimeSpan(-1, 0, 0));
                DateTime localdate = travcoffset.LocalDateTime;
                return localdate;
            }
            else
            {
                return DateTime.MinValue;
            }
        }

        public static XElement RemoveXmlns(this XElement doc)
        {
            doc.Descendants().Attributes().Where(x => x.IsNamespaceDeclaration).Remove();
            foreach (var elem in doc.Descendants())
                elem.Name = elem.Name.LocalName;

            return doc;
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
        public static long ConvertToLong(this string str)
        {
            long num = 0;
            if (!string.IsNullOrEmpty(str))
            {
                num = long.Parse(str);
            }
            return num;

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
        public static string DOB(this string agestr, string CheckInDate)
        {
            if (!string.IsNullOrEmpty(agestr))
            {
                int age = agestr.ModifyToInt();
                return CheckInDate.ConvertToDate().AddYears(-age).AddDays(-1).ToString("yyyy-MM-dd");

            }
            else
            {
                return string.Empty;
            }
        }
        public static string[] Split(this string toSplit, string splitOn)
        {
            return toSplit.Split(new string[] { splitOn }, StringSplitOptions.None);
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
    }
}