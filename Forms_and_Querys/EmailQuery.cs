namespace LuisBot
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using LuisBot.Extensions;
    using LuisBot.Utilities;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Connector;

    [Serializable]
    public class EmailQuery
    {
        [Prompt("Introduce tu mail \U0001F4E7 para comprobar si tienes datos guardados " )]
        public string Email { get; set; }


        public static IForm<EmailQuery> BuildEmailForm()
        {

            OnCompletionAsyncDelegate<EmailQuery> processEmailSearch = async (context, state) =>
            {
               string langcode;
                context.UserData.TryGetValue("CurrentLang", out langcode);
                context.UserData.SetValue(StringConstants.UserLanguageKey, langcode);

                string response = $"Buscando datos...\U0001F50D\U0001F50D";
                await context.PostAsync(response.ToUserLocale(context));

            };

            return new FormBuilder<EmailQuery>()
                .Field(nameof(Email), validate: ValidateEmail)
                .OnCompletion(processEmailSearch)
                .Build();

        }

        private static Task<ValidateResult> ValidateEmail(EmailQuery state, object response)
        {
            var result = new ValidateResult();
            string email = (string)response;

            if (Regex.IsMatch(email, @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?"))
            {
                result.IsValid = true;
                result.Value = email;
            }
            else
            {
                result.IsValid = false;
                result.Feedback = "No ingresaste una dirección de correo electrónico válida";

            }
            return Task.FromResult(result);
        }
    }
}