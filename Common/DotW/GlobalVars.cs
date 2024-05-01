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
// 13 Oct 16      Rakesh Gangwar              1.0                          Initial Version    

//*****************************************************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace TravillioXMLOutService.Common.DotW
{


    /// <summary>
    /// This region  is  created by user Rakesh
    /// </summary>  
    #region Rakesh

    public enum Status
    {
        [Description("In-Active")]
        InActive = 0,
        [Description("Active")]
        Active = 1,
        [Description("Suspended")]
        Suspended = 2,
        [Description("Deleted")]
        Deleted = 3,
    }

    public enum DotWProduct
    {
        hotel,
        Apartment,
        Transfer,
        Flight
    }

    #endregion




    

 












}