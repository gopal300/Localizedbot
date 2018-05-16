using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace LuisBot
{
    public enum AmenitiesOptions
    {
        Kitchen,
        ExtraTowels,
        GymAccess,
        Wifi,
        Pool
    }

    public class Booking
    {
        public DateTime BookingDate { get; set; }
        public int Nights { get; set; }
        public string Name { get; set; }
        public int NumPeople { get; set; }
        public int Kids { get; set; }
        public string Email { get; set; }
        public string PhNum { get; set; }
        public string Requests { get; set; }
        public string typeRoom { get; set; }
        public string Regimen { get; set; }
        public Boolean saveData { get; set; }
    }
}