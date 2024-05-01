// (c) Ingenium Technologies 2016
// Created in 2016 as an unpublished copyrighted work.  This program
// and the information contained in it is confidential and proprietary to
// Ingenium Technologies and may not be used, copied, or reproduced without the prior written
// permission of Ingenium.

//*****************************************************************************************************************
// Revision History
//*****************************************************************************************************************
//    Date        Author                     Version     Defect ID       Change Description
//*****************************************************************************************************************
// 8 June 17      Rakesh Gangwar              1.0                          Initial Version    

//*****************************************************************************************************************
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace TravillioXMLOutService.Common.DotW
{

    public static class CommonHelper
    {
        static readonly string travyoPath = ConfigurationManager.AppSettings["TravyoPath"];
        static readonly string dotWPath = ConfigurationManager.AppSettings["DotWPath"];
        //static readonly XDocument dotWCrcyData;
        //static readonly XDocument dotWRatingData;
        //static readonly XDocument dotWMealData;
        //static readonly XDocument dotWTitleData;
        static readonly int SuplId;
        static int requestNo = 0;
        static CommonHelper()
        {
            try
            {
                SuplId = 5;
                //dotWCrcyData = XDocument.Load(HttpContext.Current.Server.MapPath(dotWPath + "CurrencyList.xml"));
                //dotWRatingData = XDocument.Load(HttpContext.Current.Server.MapPath(dotWPath + "HotelClassification.xml"));
                //dotWMealData = XDocument.Load(HttpContext.Current.Server.MapPath(dotWPath + "Ratebasisi.xml"));
                //dotWTitleData = XDocument.Load(HttpContext.Current.Server.MapPath(dotWPath + "Salutations.xml"));
            }
            catch { }
        }
        public static string DotWDate(this XElement item, string defaultValue = null)
        {
            string date = string.Empty;
            if (item == null)
                return defaultValue;
            else
            {
                DateTime _Startdate;
                //_Startdate = DateTime.Parse(item.Value, new CultureInfo("en-CA"));

                _Startdate = DateTime.ParseExact(item.Value, "dd/MM/yyyy", CultureInfo.InvariantCulture);
                date = _Startdate.ToString("yyyy-MM-dd");
            }
            return date;
        }
        public static string travyoDate(this string strDate)
        {
            string date = string.Empty;
            if (strDate != null)
            {
                DateTime _Startdate;
                DateTime.TryParse(strDate, out _Startdate);
                date = _Startdate.ToString("dd/MM/yyyy");
            }
            return date;
        }

        public static string DotWCurrencyCode(this XElement item, string defaultValue = null)
        {
            var xmlMealData = LoadCurrency();
            string code = xmlMealData.Descendants("option").Where(x => x.Attribute("Value").Value == item.Value).FirstOrDefault().Attribute("shortcut").Value;
            return code;
        }

        public static string DotWCity(this string item)
        {
            string code = string.Empty;
            if (!string.IsNullOrEmpty(item))
            {
                SqlParameter[] pList = new SqlParameter[3];
                var flag = new SqlParameter();
                flag.ParameterName = "@flag";
                flag.Direction = ParameterDirection.Input;
                flag.SqlDbType = SqlDbType.Int;
                flag.Value = 1;
                pList[0] = flag;

                var supplier = new SqlParameter();
                supplier.ParameterName = "@SuplId";
                supplier.Direction = ParameterDirection.Input;
                supplier.SqlDbType = SqlDbType.BigInt;
                supplier.Value = 5;
                pList[1] = supplier;


                var city = new SqlParameter();
                city.ParameterName = "@CityId";
                city.Direction = ParameterDirection.Input;
                city.SqlDbType = SqlDbType.BigInt;
                city.Value = Convert.ToInt64(item);

                pList[2] = city;
                DataTable result = DotwDataAcess.Get("dotwProc", pList);
                code = result.Rows[0]["SupCityId"].ToString().Trim();

            }
            return code;
        }
        public static string DotWCountry(this string item)
        {
            string code = string.Empty;

            if (!string.IsNullOrEmpty(item))
            {
                SqlParameter[] pList = new SqlParameter[3];
                var flag = new SqlParameter();
                flag.ParameterName = "@flag";
                flag.Direction = ParameterDirection.Input;
                flag.SqlDbType = SqlDbType.Int;
                flag.Value = 2;
                pList[0] = flag;

                var supplier = new SqlParameter();
                supplier.ParameterName = "@SuplId";
                supplier.Direction = ParameterDirection.Input;
                supplier.SqlDbType = SqlDbType.BigInt;
                supplier.Value = 5;
                pList[1] = supplier;


                var city = new SqlParameter();
                city.ParameterName = "@CountryId";
                city.Direction = ParameterDirection.Input;
                city.SqlDbType = SqlDbType.BigInt;
                city.Value = Convert.ToInt64(item);

                pList[2] = city;
                DataTable result = DotwDataAcess.Get("dotwProc", pList);
                code = result.Rows[0]["SupCntId"].ToString().Trim();

            }
            return code;

        }

        public static string travyoCity(this string item)
        {
            string code = string.Empty;

            if (!string.IsNullOrEmpty(item))
            {
                SqlParameter[] pList = new SqlParameter[3];
                var flag = new SqlParameter();
                flag.ParameterName = "@flag";
                flag.Direction = ParameterDirection.Input;
                flag.SqlDbType = SqlDbType.Int;
                flag.Value = 3;
                pList[0] = flag;

                var supplier = new SqlParameter();
                supplier.ParameterName = "@SuplId";
                supplier.Direction = ParameterDirection.Input;
                supplier.SqlDbType = SqlDbType.BigInt;
                supplier.Value = 5;
                pList[1] = supplier;


                var city = new SqlParameter();
                city.ParameterName = "@CityId";
                city.Direction = ParameterDirection.Input;
                city.SqlDbType = SqlDbType.BigInt;
                city.Value = Convert.ToInt64(item);

                pList[2] = city;
                DataTable result = DotwDataAcess.Get("dotwProc", pList);
                code = result.Rows[0]["CityId"].ToString().Trim();

            }
            return code;
        }
        public static string travyoCountry(this string item)
        {
            string code = string.Empty;
            if (!string.IsNullOrEmpty(item))
            {
                SqlParameter[] pList = new SqlParameter[3];
                var flag = new SqlParameter();
                flag.ParameterName = "@flag";
                flag.Direction = ParameterDirection.Input;
                flag.SqlDbType = SqlDbType.Int;
                flag.Value = 4;
                pList[0] = flag;

                var supplier = new SqlParameter();
                supplier.ParameterName = "@SuplId";
                supplier.Direction = ParameterDirection.Input;
                supplier.SqlDbType = SqlDbType.BigInt;
                supplier.Value = 5;
                pList[1] = supplier;


                var city = new SqlParameter();
                city.ParameterName = "@CountryId";
                city.Direction = ParameterDirection.Input;
                city.SqlDbType = SqlDbType.BigInt;
                city.Value = Convert.ToInt64(item);

                pList[2] = city;
                DataTable result = DotwDataAcess.Get("dotwProc", pList);
                code = result.Rows[0]["cntId"].ToString().Trim();

            }
            return code;

        }

        public static int DotWCurrency(this XElement item, string defaultValue = null)
        {
            return 769;
        }

        public static string DotWMealType(this string item)
        {
            var xmlMealData = LoadMeal();
            string meal;
            if (!string.IsNullOrEmpty(item))
            {
                meal = xmlMealData.Descendants("option").Where(x => x.Attribute("travyo").Value == item).FirstOrDefault().Attribute("value").Value;
            }
            else
            {
                meal = "-1";
            }
            return meal;
        }

        public static string travyoMealType(this string item)
        {
            var xmlMealData = LoadMeal();
            string meal = string.Empty;
            if (!string.IsNullOrEmpty(item))
            {
                var mealCount = xmlMealData.Descendants("option").Where(x => x.Attribute("value").Value == item).Count();
                meal = mealCount > 0 ? xmlMealData.Descendants("option").Where(x => x.Attribute("value").Value == item).FirstOrDefault().Attribute("travyo").Value : "";
            }
            return meal;
        }

        //public static int DotWRating(this string item)
        //{
        //    int rating = 0;
        //    try
        //    {
        //        if (!string.IsNullOrEmpty(item))
        //        {
        //            var listData = dotWRatingData.Descendants("option").Where(x => x.Attribute("value").Value == item).ToList();

        //            if (listData.Count > 0)
        //            {
        //                string ratstr = listData.FirstOrDefault().Attribute("runno").Value;
        //                rating = Convert.ToInt16(ratstr);
        //                if (rating <= 4)
        //                {
        //                    rating = rating + 1;
        //                }
        //                else
        //                {
        //                    rating = 0;
        //                }
        //            }
        //        }
        //    }
        //    catch
        //    {
        //        rating = 0;
        //    }
        //    return rating;
        //}



        //public static string ToDotWRating(this string item)
        //{
        //    string Rating = string.Empty;
        //    try
        //    {

        //        int travyoRating = Convert.ToInt32(item);
        //        if (travyoRating != 0)
        //        {

        //            var listData = dotWRatingData.Descendants("option").Where(x => x.Attribute("runno").Value == (travyoRating - 1).ToString()).ToList();
        //            if (listData.Count > 0)
        //            {
        //                Rating = listData.FirstOrDefault().Attribute("value").Value;
        //            }
        //            else
        //            {
        //                Rating = "55835";
        //            }
        //        }
        //        else
        //        {
        //            Rating = "55835";
        //        }
        //    }
        //    catch
        //    {
        //        Rating = "55835";
        //    }
        //    return Rating;
        //}

        public static int DotWRating(this string item)
        {
            int rating = 0;
            switch (item)
            {
                case "559": rating = 1; break;
                case "560": rating = 2; break;
                case "561": rating = 3; break;
                case "562": rating = 4; break;
                case "563": rating = 5; break;
                case "55835": rating = 0; break;
            }
            return rating;
        }

        public static string ToDotWRating(this string item)
        {
            string Rating = string.Empty;
            switch (item)
            {
                case "1": Rating = "559"; break;
                case "2": Rating = "560"; break;
                case "3": Rating = "561"; break;
                case "4": Rating = "562"; break;
                case "5": Rating = "563"; break;
                case "0": Rating = "55835"; break;
            }
            return Rating;
        }







        public static string DotWTitle(this string item)
        {
            string code = string.Empty;
            if (!string.IsNullOrEmpty(item))
            {
                var xmlMealData = LoadTitle();
                var result = xmlMealData.Descendants("option").Where(x => x.Attribute("travyo").Value == item).FirstOrDefault();
                code = result != null ? result.Attribute("value").Value : "3801";
            }
            return code;

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

        public static string Escape(this string input)
        {
            char[] toEscape = "\0\x1\x2\x3\x4\x5\x6\a\b\t\n\v\f\r\xe\xf\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1a\x1b\x1c\x1d\x1e\x1f\x2C\"\\".ToCharArray();
            string[] literals = @"\0,\x0001,\x0002,\x0003,\x0004,\x0005,\x0006,\a,\b,\t,\n,\v,\f,\r,\x000e,\x000f,\x0010,\x0011,\x0012,\x0013,\x0014,\x0015,\x0016,\x0017,\x0018,\x0019,\x001a,\x001b,\x001c,\x001d,\x001e,\x001f\x002C".Split(new char[] { ',' });

            int i = input.IndexOfAny(toEscape);
            if (i < 0) return input;

            var sb = new System.Text.StringBuilder(input.Length + 5);
            int j = 0;
            do
            {
                sb.Append(input, j, i - j);
                var c = input[i];
                if (c < 0x20) sb.Append(literals[c]); else sb.Append(@"\").Append(c);
            } while ((i = input.IndexOfAny(toEscape, j = ++i)) > 0);

            return sb.Append(input, j, input.Length - j).ToString();
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



        public static XElement RemoveXmlns(this XElement doc)
        {
            doc.Descendants().Attributes().Where(x => x.IsNamespaceDeclaration).Remove();
            foreach (var elem in doc.Descendants())
                elem.Name = elem.Name.LocalName;

            return doc;
        }


        public static List<XElement> rmIndex(this IEnumerable<XElement> rmlst, string atrName)
        {
            List<XElement> lst = rmlst.ToList();
            int index = 0;
            lst.ForEach(r => r.Attribute(atrName).Value = (++index).ToString());
            return lst;
        }


        public static string DistinctiveId()
        {
            var ticks = DateTime.Now.Ticks;
            var guid = Guid.NewGuid().ToString();
            var uniqueSessionId = ticks.ToString() + '-' + guid;
            return uniqueSessionId;
        }

        public static string RequestNo()
        {
            if (requestNo < 2000)
            {
                ++requestNo;
            }
            else
            {
                requestNo = 0;
            }
            return requestNo.ToString();
        }

        public static DateTime chnagetoTime(this string strDate)
        {
            DateTime oDate = DateTime.ParseExact(strDate, "yyyy-MM-dd HH:mm:ss", null);
            return oDate;
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

        public static string CxlDate(this XElement item)
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (item.Element("fromDate") != null)
            {
                date = item.Element("fromDate").Value;
            }
            return date;
        }

        public static string CxlToDate(this XElement item, string checkinDate)
        {
            string date = checkinDate;
            if (item.Element("toDate") != null)
            {
                date = item.Element("toDate").Value;
            }
            return date;
        }

        public static string GuestName(this XElement gst)
        {
            string Name = string.Empty;
            if (gst.Element("MiddleName").Value != null)
            {
                Name = gst.Element("FirstName").Value.Trim() + gst.Element("MiddleName").Value.Trim();
            }
            else
            {
                Name = gst.Element("FirstName").Value.Trim();
            }
            return Name;
        }

        public static string Address(this string str)
        {
            string Name = string.Empty;
            if (!string.IsNullOrEmpty(str))
            {
                Name = str + ",";
            }
            return Name;
        }

        public static List<List<T>> BreakIntoChunks<T>(List<T> list, int chunkSize)
        {
            if (chunkSize <= 0)
            {
                throw new ArgumentException("chunkSize must be greater than 0.");
            }
            List<List<T>> retVal = new List<List<T>>();
            while (list.Count > 0)
            {
                int count = list.Count > chunkSize ? chunkSize : list.Count;
                retVal.Add(list.GetRange(0, count));
                list.RemoveRange(0, count);
            }

            return retVal;
        }

        public static bool GetMinstay(this XElement item, int nights)
        {
            int minstay = nights;
            if (item != null)
            {
                if (!string.IsNullOrEmpty(item.Value))
                {
                    minstay = Convert.ToInt16(item.Value);
                }
            }
            return minstay <= nights;
        }

        public static DateTime GetDateTime(this string strDateTime, string dateTimeFormate)
        {
            DateTime dte = DateTime.ParseExact(strDateTime.Trim(), dateTimeFormate, CultureInfo.InvariantCulture);
            return dte;
        }

        public static string CxlDate(this XElement item, string bookingDate)
        {
            string date = bookingDate;
            if (item.Element("fromDate") != null)
            {
                date = item.Element("fromDate").Value;
            }
            return date;

        }



        public static decimal AttributetoDecimal(this XAttribute item)
        {
            decimal result = 0.0m;
            if (item != null)
            {
                result = Convert.ToDecimal(item.Value);
            }
            return result;
        }
        public static decimal ElementtoDecimal(this XElement item)
        {
            decimal result = 0.0m;
            if (item != null)
            {
                result = Convert.ToDecimal(item.Value);
            }
            return result;
        }
        public static decimal StringDecimal(this string item)
        {
            decimal result = 0.0m;
            if (item != null)
            {
                result = Convert.ToDecimal(item);
            }
            return result;
        }

      
 public static string GroupKey(this IEnumerable<XAttribute> lst,string mealType)
        {
            string result = "";
            List<long> intLst = new List<long>();
            foreach (var item in lst)
            {
                intLst.Add(Convert.ToInt64(item.Value));
            }
            long[] intArray = intLst.ToArray();
            Array.Sort(intArray);
            foreach (var item in intArray.ToList())
            {
                result += item;
                
            }
            result += mealType;  
            return result;
        }
 public static DateTime GetCheckinDate(this XAttribute item, string dateTimeFormate, string defaultValue = null)
 {
     string date = string.Empty;
     if (item != null)
     {
         date = item.Value;
     }
     else
     {
         date = defaultValue;
     }
     DateTime dte = DateTime.ParseExact(date.Trim(), dateTimeFormate, CultureInfo.InvariantCulture);
     return dte;
 }
 public static string GroupKey(this IEnumerable<long> lst, int mealType)
 {
     string result = "";
     long[] intArray = lst.ToArray();
     Array.Sort(intArray);
     foreach (var item in intArray.ToList())
     {
         result += item;
     }
     result += mealType;
     return result;
 }
 public static string RemoveInvalidXmlChars(string text)
 {
     if (string.IsNullOrEmpty(text))
         return text;

     int length = text.Length;
     StringBuilder stringBuilder = new StringBuilder(length);

     for (int i = 0; i < length; ++i)
     {
         if (XmlConvert.IsXmlChar(text[i]))
         {
             stringBuilder.Append(text[i]);
         }
         else if (i + 1 < length && XmlConvert.IsXmlSurrogatePair(text[i + 1], text[i]))
         {
             stringBuilder.Append(text[i]);
             stringBuilder.Append(text[i + 1]);
             ++i;
         }
     }

     return stringBuilder.ToString();
 }


 public static string CleanInvalidXmlChars(string text)
 {
     // From xml spec valid chars: 
     // #x9 | #xA | #xD | [#x20-#xD7FF] | [#xE000-#xFFFD] | [#x10000-#x10FFFF]     
     // any Unicode character, excluding the surrogate blocks, FFFE, and FFFF. 
     string re = @"[^\0x02\x09\x0A\x0D\x20-\xD7FF\xE000-\xFFFD\x10000-x10FFFF]";
     return Regex.Replace(text, re, "");
 }
 static XDocument LoadMeal()
 {
     string xml = @"<?xml version='1.0' encoding='utf-8' ?>
<result command='getratebasisids' tID='1493807136000001' ip='127.0.0.1' sip='127.0.0.1' xmlns:a='http://xmldev.dotwconnect.com/xsd/atomicCondition' xmlns:c='http://xmldev.dotwconnect.com/xsd/complexCondition' date='2017-05-03 10:25:36' version='2.0' elapsedTime='0.0045340061187744'>
  <ratebasis count='8'>
    <option runno='0' travyo=''   value='-1'>Best Available</option>
    <option runno='1' travyo='RO' value='0'>Room Only</option>
    <option runno='2' travyo=''   value='1'>All Rates</option>
    <option runno='3' travyo='BB' value='1331'>Breakfast</option>
    <option runno='4' travyo=''   value='15064'>Do Not Use - Self Catering</option>
    <option runno='5' travyo='HB' value='1334'>Half Board</option>
    <option runno='6' travyo='FB' value='1335'>Full Board</option>
    <option runno='7' travyo='AI' value='1336'>All Inclusive</option>
  </ratebasis>
  <successful>TRUE</successful>
</result>";
     return XDocument.Parse(xml);

 }
 static XDocument LoadTitle()
 {
     string xml = @"<salutations count='10'>
    <option runno='0' travyo='Master' value='14632'>Child</option>
    <option runno='1' travyo='Dr' value='558'>Dr.</option>
    <option runno='2' travyo='Madam' value='1671'>Madam</option>
    <option runno='3' travyo='Messrs' value='9234'>Messrs.</option>
    <option runno='4' travyo='Miss' value='15134'>Miss</option>
    <option runno='5' travyo='Mr' value='147'>Mr.</option>
    <option runno='6' travyo='Mrs' value='149'>Mrs.</option>
    <option runno='7' travyo='Ms' value='148'>Ms.</option>
    <option runno='8' travyo='Sir' value='1328'>Sir</option>
    <option runno='9' travyo='Sir/Madam' value='3801'>Sir/Madam</option>
  </salutations>";
     return XDocument.Parse(xml);

 }
 static XDocument LoadCurrency()
 {
     string xml = @"<currency count='1'>
<option runno='0' shortcut='KWD' value='769'>Kuwaiti Dinars</option>
</currency>";
     return XDocument.Parse(xml);

 }
    }
}