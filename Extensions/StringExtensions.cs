using LuisBot.Translator;
using LuisBot.Utilities;
using Microsoft.Bot.Builder.Dialogs;

namespace LuisBot.Extensions
{
    public static class StringExtensions
    {
        public static object StateHelper { get; private set; }

        public static string ToUserLocale(this string text, IDialogContext context)
        {
                 context.UserData.TryGetValue(StringConstants.UserLanguageKey, out string userLanguageCode);

                 text = TranslationHandler.TranslateText(text, StringConstants.DefaultLanguage, userLanguageCode);

                     

            return text;
        }
    }
}