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

    public enum CategoriesShort
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
    public class BookingFormShort2
    {

        [Describe("Régimen de uso")]
        [Prompt("Régimen de uso: {||}")]
        public CategoriesShort Regimen_uso { get; set; }

        [Optional]
        [Prompt("¿Quiere añadir información adicional? \U00002139\U00002139")]
        public string Peticiones_extra { get; set; }

        // Cache of culture specific forms. 
        private static ConcurrentDictionary<CultureInfo, IForm<BookingFormShort2>> _forms = new ConcurrentDictionary<CultureInfo, IForm<BookingFormShort2>>();

        public static IForm<BookingFormShort2> BuildFormShort2()
        {

            return new FormBuilder<BookingFormShort2>()

                .Field(nameof(Regimen_uso))
                .Field(nameof(Peticiones_extra))

                .Build();
        }

    }

}