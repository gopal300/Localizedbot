namespace LuisBot
{
    using System;
    using Microsoft.Bot.Builder.FormFlow;

    public enum Turnos
    {
        [Describe("12:30")]
        Primer = 1,
        [Describe("13:00")]
        Segundo,
        [Describe("13:30")]
        Tercer,
        [Describe("14:00")]
        Cuarto
    }

    [Serializable]
    public class RestaurantForm
    {
        [Describe("Seleccione la opción deseada: ")]
        [Prompt("A que hora quieres reservar mesa en el restaurante? {||}")]
        public Turnos TurnoRestaurante { get; set; }
    }
}