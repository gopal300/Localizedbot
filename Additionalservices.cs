using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;


namespace LuisBot
{ 
public enum AdditionalServices
{
        Serviciosdeestacionamiento, Cuidadoinfantil, Piscina, Gimnasio, Campodegolf, Restaurantedealtonivel
    };
    [Serializable]
    public class SandwichOrder
    {
        public AdditionalServices? Serviciosdeestacionamiento;
        public AdditionalServices? Cuidadoinfantil;
        public AdditionalServices? BrPiscinaead;
        public AdditionalServices? Gimnasio;
        public AdditionalServices? Campodegolf;
        public AdditionalServices? Restaurantedealtonivel;
        

        public static IForm<ServicesOrder> BuildForm()
        {
            return new FormBuilder<ServicesOrder>()
                       .Build();
        }
    };
}