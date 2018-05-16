using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace LuisBot
{
    public enum TypeRooms
    {
        [Describe("Individual")]
        Individual = 1,
        [Describe("Doble")]
        Doble,
        [Describe("Matrimonial")]
        Matrimonial,
        [Describe("Triple")]
        Triple,
        [Describe("Suite")]
        Suite
    }

    [Serializable]
    public class BookingFormShort1
    {

        [Prompt("¿Para qué fecha quieres reservar la habitación? \U0001F4C6\U0001F4C6")]
        public DateTime Fecha { get; set; }

        [Prompt("¿Por cuántas noches quieres reservar la habitación? \U0001F4C5\U0001F4C5")]
        public int Noches { get; set; }

        [Prompt("¿Para cuántas personas quieres la habitación? \U0001F465\U0001F465")]
        public int No_Huespedes { get; set; }

        [Prompt("¿Cuántos niños? \U0001F476\U0001F476")]
        public int Niños { get; set; }

        [Describe("Seleccione la opción deseada: ")]
        [Prompt("Que tipo de habitación quiere? {||}")]
        public TypeRoom TipoHabitacion { get; set; }


        // Cache of culture specific forms. 
        private static ConcurrentDictionary<CultureInfo, IForm<BookingFormShort1>> _forms = new ConcurrentDictionary<CultureInfo, IForm<BookingFormShort1>>();


        public static IForm<BookingFormShort1> BuildFormShort1()
        {

            return new FormBuilder<BookingFormShort1>()

                .Field(nameof(Fecha), validate: ValidateDate)
                .Field(nameof(Noches), validate: ValidateNights)
                .Field(nameof(No_Huespedes), validate: ValidateNumPeople)
                .Field(nameof(Niños), validate: ValidateKids)
                .Field(nameof(TipoHabitacion))

                .Build();
        }

        /*private static bool BookingEnabled(BookingForm state) =>
            !string.IsNullOrWhiteSpace(state.Email) && !string.IsNullOrWhiteSpace(state.Name);
            */

        private static Task<ValidateResult> ValidateDate(BookingFormShort1 state, object response)
        {
            var result = new ValidateResult();
            var dt = (DateTime)response;
            DateTime today = DateTime.Today;

            // Do the checks here whether the time is available. 
            if (dt <= today)
            {
                // If time not available
                result.IsValid = false;
                result.Feedback = "Fecha no válida. Introduce una fecha en el futuro contando des de hoy.";
            }
            else
            {
                result.IsValid = true;
                result.Value = response;
            }
            return Task.FromResult(result);
        }

        private static Task<ValidateResult> ValidateTime(BookingFormShort1 state, object response)
        {
            var result = new ValidateResult();
            var dt = (DateTime)response;

            // Do the checks here whether the time is available. 
            // Hard coded for demo purposes
            if (dt.ToString("HH:mm") == "20:30")
            {
                // If time not available
                result.IsValid = false;
                result.Feedback = "Lo sentimos, ¡ese momento no está disponible! Los horarios que están disponibles son: 18:30, 19:00, 20:00, 21:00";
            }
            else
            {
                result.IsValid = true;
                result.Value = response;
            }
            return Task.FromResult(result);
        }

        private static Task<ValidateResult> ValidateNights(BookingFormShort1 state, object response)
        {
            var result = new ValidateResult();
            string nights = response.ToString();
            int people = state.No_Huespedes;

            if (!Regex.IsMatch(nights, @"^[0-9]+$"))
            {
                result.IsValid = false;
                result.Feedback = "Número de noches no válido. Por favor, introduce únicamente valores numéricos.";
            }
            else
            {
                result.IsValid = true;
                result.Value = response;
            }
            return Task.FromResult(result);
        }

        private static Task<ValidateResult> ValidateNumPeople(BookingFormShort1 state, object response)
        {
            var result = new ValidateResult();
            string numPeople = response.ToString();
            int people = state.No_Huespedes;

            if (!Regex.IsMatch(numPeople, @"^[0-9]+$"))
            {
                result.IsValid = false;
                result.Feedback = "Número de personas no válido. Por favor, introduce únicamente valores numéricos.";
            }
            else
            {
                result.IsValid = true;
                result.Value = response;
            }
            return Task.FromResult(result);
        }

        private static Task<ValidateResult> ValidateKids(BookingFormShort1 state, object response)
        {
            var result = new ValidateResult();
            int kids = Convert.ToInt32(response);
            int people = state.No_Huespedes;

            if (people < kids)
            {
                // If time not available
                result.IsValid = false;
                result.Feedback = "Número de niños no válido. Introduce un número igual o menor al número de huéspedes.";
            }
            else
            {
                result.IsValid = true;
                result.Value = response;
            }
            return Task.FromResult(result);
        }

    }

}