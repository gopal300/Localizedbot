namespace LuisBot
{
    using Microsoft.Bot.Builder.FormFlow;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Web;


    [Serializable]
    public class DateQuery
    {
        [Prompt("¿Para qué fecha quieres reservar la habitación? \U0001F5D3\U0001F5D3")]
        public DateTime Fecha { get; set; }

        [Prompt("¿Por cuántas noches quieres reservar la habitación? \U0001F4C5\U0001F4C5")]
        public int Noches { get; set; }




        public static IForm<DateQuery> BuildDateForm()
        {
            return new FormBuilder<DateQuery>()
                .Field(nameof(Fecha), validate: ValidateDate)
                .Field(nameof(Noches))
                .Build();
        }

        private static Task<ValidateResult> ValidateDate(DateQuery state, object response)
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
    }
}