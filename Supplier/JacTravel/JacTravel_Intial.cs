using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using TravillioXMLOutService.Common.JacTravel;
using TravillioXMLOutService.Models;
using TravillioXMLOutService.Models.JacTravel;

namespace TravillioXMLOutService.Supplier.JacTravel
{
    public class JacTravel_Intial : IDisposable
    {

        public delegate void HtlstDelegate(List<XElement> lst);
        public event HtlstDelegate MyEvent;
        public void CallHtlSearch(XElement reqTravillio, string custID, string dmc, int supid)
        {
            try
            {
                Getresort jacobj = new Getresort();

                string HtId = reqTravillio.Descendants("HotelID").FirstOrDefault().Value;
                if (!string.IsNullOrEmpty(HtId))
                {
                    jacobj.MyEvent += jacobj_MyEvent;
                    jacobj.GetResortData(reqTravillio, custID, supid.ToString(), dmc);
                }
                else
                {


                    DataTable cityMapping = jac_staticdata.CityMapping(reqTravillio.Descendants("CityID").FirstOrDefault().Value, supid.ToString());
                    int RegionID = Convert.ToInt32(cityMapping.Rows[0]["SupCityId"].ToString());
                    string jacpath = ConfigurationManager.AppSettings["JacPath"];
                    string strPath = jacpath + @"Property\" + RegionID + ".xml";
                    string citypath = Path.Combine(HttpRuntime.AppDomainAppPath, strPath);
                    if (File.Exists(citypath))
                    {
                        int duration = Convert.ToInt32(reqTravillio.Descendants("Nights").FirstOrDefault().Value);
                        string mealpath = Path.Combine(HttpRuntime.AppDomainAppPath, jacpath + @"MealBasis.xml");
                        string Facilitypath = Path.Combine(HttpRuntime.AppDomainAppPath, jacpath + @"Facility.xml");
                        XElement suppliercred = supplier_Cred.getsupplier_credentials(custID, Convert.ToString(supid));
                        string Login = suppliercred.Descendants("Login").FirstOrDefault().Value;
                        string Password = suppliercred.Descendants("Password").FirstOrDefault().Value;
                        XElement JacHtl = XElement.Load(citypath);
                        XElement Jac_Meal = XElement.Load(mealpath);
                        XElement Jac_fac = XElement.Load(Facilitypath);

                        jacobj.MyEvent += jacobj_MyEvent;
                        jacobj.GetResortData(JacHtl, reqTravillio, Jac_Meal, duration, Jac_fac, Login, Password, RegionID, dmc, supid, custID);

                    }



                }
            }
            catch (Exception ex)
            {
                #region Exception
                CustomException ex1 = new CustomException(ex);
                ex1.MethodName = "CallHtlSearch";
                ex1.PageName = "JacTravel_Intial";
                ex1.CustomerID = custID;
                ex1.TranID = reqTravillio.Descendants("TransID").FirstOrDefault().Value;
                APILog.SendCustomExcepToDB(ex1);
                #endregion
            }
        }

        void jacobj_MyEvent(List<XElement> lst)
        {
            MyEvent(lst);
        }
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