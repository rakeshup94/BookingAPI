using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TravillioXMLOutService.Air.Models.TBO
{
    public class tbobookrequest
    {
        public tbobookrequest()
        {

        }
        public string ResultId { get; set; }
        public string EndUserBrowserAgent { get; set; }
        public string PointOfSale { get; set; }
        public string RequestOrigin { get; set; }
        public string UserData { get; set; }
        public string TokenId { get; set; }
        public string TrackingId { get; set; }
        public string IPAddress { get; set; }
        public Itinerary Itinerary { get; set; }
    }
    public class Origin
    {
        public string AirportCode { get; set; }
        public string AirportName { get; set; }
        public string CityCode { get; set; }
        public string CityName { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string Terminal { get; set; }
    }
    public class Destination
    {
        public string AirportCode { get; set; }
        public string AirportName { get; set; }
        public string CityCode { get; set; }
        public string CityName { get; set; }
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string Terminal { get; set; }
    }
    public class AirlineDetails
    {
        public string AirlineCode { get; set; }
        public string FlightNumber { get; set; }
        public string Craft { get; set; }
        public string AirlineName { get; set; }
        public string OperatingCarrier { get; set; }
    }
    public class Segment
    {
        public int NoOfSeatAvailable { get; set; }
        public string OperatingCarrier { get; set; }
        public int SegmentIndicator { get; set; }
        public string Airline { get; set; }
        public Origin Origin { get; set; }
        public Destination Destination { get; set; }
        public string FlightNumber { get; set; }
        public DateTime DepartureTime { get; set; }
        public DateTime ArrivalTime { get; set; }
        public string BookingClass { get; set; }
        public object MealType { get; set; }
        public bool ETicketEligible { get; set; }
        public string Craft { get; set; }
        public bool StopOver { get; set; }
        public int Stops { get; set; }
        public int Mile { get; set; }
        public string Duration { get; set; }
        public string GroundTime { get; set; }
        public string AccumulatedDuration { get; set; }
        public object StopPoint { get; set; }
        public DateTime StopPointArrivalTime { get; set; }
        public DateTime StopPointDepartureTime { get; set; }
        public string IncludedBaggage { get; set; }
        public string CabinBaggage { get; set; }
        public object AdditionalBaggage { get; set; }
        public AirlineDetails AirlineDetails { get; set; }
        public string AirlineName { get; set; }
    }
    public class Nationality
    {
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
    }
    public class Country
    {
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
    }
    public class City
    {
        public object CountryCode { get; set; }
        public string CityCode { get; set; }
        public string CityName { get; set; }
    }
    public class Fare
    {
        public double TotalFare { get; set; }
        public string FareType { get; set; }
        public int AgentMarkup { get; set; }
        public int OtherCharges { get; set; }
        public string AgentPreferredCurrency { get; set; }
        public int ServiceFee { get; set; }
        public int Vat { get; set; }
        public double BaseFare { get; set; }
        public double Tax { get; set; }
    }
    public class Passenger
    {
        public object PassportIssueCountryCode { get; set; }
        public DateTime PassportIssueDate { get; set; }
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Mobile1 { get; set; }
        public string Mobile1CountryCode { get; set; }
        public object Mobile2 { get; set; }
        public bool IsLeadPax { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Type { get; set; }
        public string PassportNo { get; set; }
        public DateTime PassportExpiry { get; set; }
        public Nationality Nationality { get; set; }
        public Country Country { get; set; }
        public City City { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public int Gender { get; set; }
        public string Email { get; set; }
        public object Meal { get; set; }
        public object Seat { get; set; }
        public Fare Fare { get; set; }
        public object FFAirline { get; set; }
        public object FFNumber { get; set; }
        public int TboAirPaxId { get; set; }
        public List<object> PaxBaggage { get; set; }
        public List<object> PaxMeal { get; set; }
        public object IDCardNo { get; set; }
        public object ZipCode { get; set; }
        public object PaxSeat { get; set; }
        public object Ticket { get; set; }
    }
    public class FareRule
    {
        public string Origin { get; set; }
        public string Destination { get; set; }
        public string Airline { get; set; }
        public string FareRestriction { get; set; }
        public string FareBasisCode { get; set; }
        public string FareRuleDetail { get; set; }
        public DateTime DepartureDate { get; set; }
        public string FlightNumber { get; set; }
    }
    public class Itinerary
    {
        public List<Segment> Segments { get; set; }
        public List<Passenger> Passenger { get; set; }
        public List<FareRule> FareRules { get; set; }
        public string Destination { get; set; }
        public string FareType { get; set; }
        public object LastTicketDate { get; set; }
        public string Origin { get; set; }
        public DateTime CreatedOn { get; set; }
        public int FailedBookingId { get; set; }
        public string ValidatingAirlineCode { get; set; }
        public bool IsDomestic { get; set; }
        public object AirlineCode { get; set; }
        public DateTime TravelDate { get; set; }
        public bool NonRefundable { get; set; }
        public object AgentRefNo { get; set; }
        public bool IsLcc { get; set; }
        public string AirlineRemark { get; set; }
        public int SearchType { get; set; }
        public int OnBehalfOf { get; set; }
        public int EarnedLoyaltyPoints { get; set; }
        public string TripName { get; set; }
        public object StaffRemarks { get; set; }
        public object PricingKeyDetail { get; set; }
        public object SSRData { get; set; }
    }
}