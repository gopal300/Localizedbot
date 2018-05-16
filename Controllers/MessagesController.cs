namespace LuisBot
{

    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Autofac;
    using LuisBot.Translator;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Internals;
    using Microsoft.Bot.Connector;
    using LuisBot.Extensions;
    using LuisBot.Models;
    using LuisBot.Utilities;
    using LuisBot.Dialogs;
    using System.Text.RegularExpressions;

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        //  private static readonly bool IsSpellCorrectionEnabled = bool.Parse(WebConfigurationManager.AppSettings["IsSpellCorrectionEnabled"]);

        //    private readonly BingSpellCheckService spellService = new BingSpellCheckService();





        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            Trace.TraceInformation($"Incoming Activity is {activity.ToJson()}");
            if (activity.Type == ActivityTypes.Message)
            {
                if (!String.IsNullOrEmpty(activity.Text))
                {
                    //detect language of input text

                    var userLanguage = TranslationHandler.DetectLanguage(activity);


                    // save user´s languagecode 
                    var message = activity as IMessageActivity;

                    try
                    {
                        using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
                        {
                            var botDataStore = scope.Resolve<IBotDataStore<BotData>>();
                            var key = new AddressKey()
                            {
                                BotId = message.Recipient.Id,
                                ChannelId = message.ChannelId,
                                UserId = message.From.Id,
                                ConversationId = message.Conversation.Id,
                                ServiceUrl = message.ServiceUrl
                            };

                            var userData = await botDataStore.LoadAsync(key, BotStoreType.BotUserData, CancellationToken.None);

                            var storedLanguageCode = userData.GetProperty<string>(StringConstants.UserLanguageKey);

                            //update user's language in Azure Table Storage

                            if (storedLanguageCode != userLanguage && !Regex.IsMatch(activity.Text, @"[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?"))
                            {
                                userData.SetProperty(StringConstants.UserLanguageKey, userLanguage);
                                await botDataStore.SaveAsync(key, BotStoreType.BotUserData, userData, CancellationToken.None);
                                await botDataStore.FlushAsync(key, CancellationToken.None);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    //translate activity.Text to Spanish before sending to LUIS for intent
                    activity.Text = TranslationHandler.TranslateTextToDefaultLanguage(activity, userLanguage);
                    //   activity.Text = TranslationHandler.TranslateText(String ,"es",userLanguage);
                  
                   // await Conversation.SendAsync(activity, MakeRoot);
                    await Conversation.SendAsync(activity, () => new RootLuisDialog());
                }

            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

      /*  internal static IDialog<object> MakeRoot()
        {
            try
            {
                return Chain.From(() => new RootLuisDialog());
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        */
        //  var userLanguage = TranslationHandler.DetectLanguage(activity);
        // activity.Text = TranslationHandler.TranslateTextToDefaultLanguage(activity,userLanguage);

        /*   if (IsSpellCorrectionEnabled)
                  {
                      try
                      {

                          activity.Text = await this.spellService.GetCorrectedTextAsync(activity.Text);
                      }
                      catch (Exception ex)
                      {
                          Trace.TraceError(ex.ToString());
                      }
                  }

                  await Conversation.SendAsync(activity, () => new RootLuisDialog());
              }
              else
              {
                  this.HandleSystemMessage(activity);
              }

              var response = Request.CreateResponse(HttpStatusCode.OK);
              return response;
          }
        */

        private Activity HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {

                /*if (message.MembersAdded.Any(o => o.Id == message.Recipient.Id))
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                    Activity reply = message.CreateReply("Son las 12:30. Recuerda que dentro de 1 hora, a las 13:30 tienes una reserva en el Restaurante del Hotel.");
                    connector.Conversations.ReplyToActivityAsync(reply);
                }*/
                if (message.MembersAdded.Any(o => o.Id == message.Recipient.Id))
                {
                    ConnectorClient connector = new ConnectorClient(new Uri(message.ServiceUrl));
                    Activity reply = message.CreateReply("Bienvenido al ChatBot de Suitech!!  \U0001F60A \U0001F60A  \U0001F60A  ");
                    connector.Conversations.ReplyToActivityAsync(reply);
                }


            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}