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
    public enum Categories
    {
        [Describe("Solo Alojamiento")]
        SoloAlojamiento = 1,
        [Describe("Con Desayuno")]
        ConDesayuno,
        [Describe("Media Pensión")]
        MediaPensión,
        [Describe("Pensión Completa")]
        PensiónCompleta,
    }

    [Serializable]
    public class BookingForm2
    {
        [Describe("Régimen de uso")]
        [Prompt("Régimen de uso: {||}")]
        public Categories Regimen_uso { get; set; }

        [Prompt("¿Puedes decirme tu nombre? \U0001F524\U0001F524")]
        public string Nombre { get; set; }

        [Prompt("¿Puedo obtener tu número de teléfono, por favor? \U0001F4F1\U0001F4F1")]
        public string Num_Telefono { get; set; }

        [Optional]
        [Prompt("¿Quiere añadir información adicional? \U00002139\U00002139")]
        public string Peticiones_extra { get; set; }


        // Cache of culture specific forms. 
        private static ConcurrentDictionary<CultureInfo, IForm<BookingForm2>> _forms = new ConcurrentDictionary<CultureInfo, IForm<BookingForm2>>();

        public static IForm<BookingForm2> BuildForm2()
        {

            return new FormBuilder<BookingForm2>()

                .Field(nameof(Regimen_uso))
                .Field(nameof(Nombre), validate: ValidateName)
                .Field(nameof(Num_Telefono), validate: ValidatePhNum)
                .Field(nameof(Peticiones_extra))
                
                .Build();
        }

        /*private static bool BookingEnabled(BookingForm state) =>
            !string.IsNullOrWhiteSpace(state.Email) && !string.IsNullOrWhiteSpace(state.Name);
            */

        private static Task<ValidateResult> ValidateName(BookingForm2 state, object response)
        {
            var result = new ValidateResult();
            string name = (string)response;

            if (!Regex.Match(name, @"^[a-zA-Z ]*$").Success)
            {
                result.IsValid = false;
                result.Feedback = "Nombre no válido. Por favor, introduce únicamente letras.";
            }
            else
            {
                result.IsValid = true;
                result.Value = response;
            }
            return Task.FromResult(result);
        }
        
        private static Task<ValidateResult> ValidatePhNum(BookingForm2 state, object response)
        {
            var result = new ValidateResult();
            string phoneNumber = string.Empty;

            if (IsPhNum((string)response))
            {
                result.IsValid = true;
                result.Value = response;
            }
            else
            {
                result.IsValid = false;
                result.Feedback = "El número de teléfono introducido no es válido.";
            }
            return Task.FromResult(result);
        }

        private static bool IsPhNum(string response)
        {

            if (Regex.IsMatch(response, @"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$") & response.Length == 9)
            {
                return true;
            }

            return false;
        }

    }

}