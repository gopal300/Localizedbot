namespace LuisBot
{
    using System;
    using Microsoft.Bot.Builder.FormFlow;

    public enum TypeRoom1
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
    public class RoomsQuery
    {
        [Describe("Seleccione la opción deseada: ")]
        [Prompt("Que tipo de habitación quiere? {||}")]
        public TypeRoom TipoHabitacion { get; set; }
    }
}