namespace LuisBot
{
    using System;
    using Microsoft.Bot.Builder.FormFlow;
    /*
    public enum Turnos
    {
        [Describe("a. 12:30")]
        a = 1,
        [Describe("b. 13:00")]
        b,
        [Describe("c. 13:30")]
        c,
        [Describe("d. 14:00")]
        d
    }

    [Serializable]
    public class RestaurantForm
    {
        [Describe("Seleccione la opción deseada: ")]
        [Prompt("A que hora quieres reservar mesa en el restaurante? {||}")]
        public Turnos TurnoRestaurante { get; set; }
    }*/

    [Serializable]
    public class RestaurantForm
    {
        [Prompt("¿Qué día quieres reservar mesa en el restaurante? \U0001F5D3\U0001F5D3")]
        public DateTime DiaRestaurante { get; set; }

        [Prompt("A que hora quieres reservar mesa en el restaurante? Horas de trabajo del restaurante: \n\n Desayuno \U0001F558: 8:00-10:30 \n\n Comida \U0001F551: 12:30-15:30 \n\n Cena \U0001F559: 20:00-23:00")]
        public DateTime TurnoRestaurante { get; set; }
    }

}