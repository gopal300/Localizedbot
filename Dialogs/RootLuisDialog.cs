namespace LuisBot.Dialogs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.FormFlow;
    using Microsoft.Bot.Builder.Luis;
    using Microsoft.Bot.Builder.Luis.Models;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Builder.ConnectorEx;
    using System.Text.RegularExpressions;
    using System.Timers;
    using System.Threading;
    using AdaptiveCards;
    using System.Web.Hosting;
    using System.IO;
    using LuisBot.Extensions;
    using LuisBot.Utilities;
    using Autofac;
    using Microsoft.Bot.Builder.Resource;
    using System.Globalization;
    using Microsoft.Bot.Builder.Dialogs.Internals;
   // using Microsoft.Bot.Builder.Internals.Fibers;
  

   

    [LuisModel("7bb1fa4a-8d34-4d03-893d-2b48fa806f25", "0ece212a492d4f38bee3314298858f05")]
    [Serializable]
    public class RootLuisDialog : LuisDialog<object>
    {
        /*   private const string EntityGeographyCity = "builtin.geography.city";

           private const string EntityHotelName = "Hotel";

           private const string EntityAirportCode = "AirportCode";*/
        //private IList<CardImage> https;
          


        /* private IList<string> titleOptions = new List<string> { "“Very stylish, great stay, great staff”", "“good hotel awful meals”", "“Need more attention to little things”", "“Lovely small hotel ideally situated to explore the area.”", "“Positive surprise”", "“Beautiful suite and resort”" };
         private ResumeAfter<BookingForm> BookingFormComplete;*/

        public object ConversationStarter { get; private set; }

        protected virtual async Task MessageReceived(IDialogContext context, IAwaitable<IMessageActivity> item)
        {

            var message = await item;
            var messageText = await GetLuisQueryTextAsync(context, message);

            if (messageText != null)
            {
                // Modify request by the service to add attributes and then by the dialog to reflect the particular query
                var tasks = this.services.Select(s => s.QueryAsync(ModifyLuisRequest(s.ModifyRequest(new LuisRequest(messageText))), context.CancellationToken)).ToArray();
                var results = await Task.WhenAll(tasks);

                var winners = from result in results.Select((value, index) => new { value, index })
                              let resultWinner = BestIntentFrom(result.value)
                              where resultWinner != null
                              select new LuisServiceResult(result.value, resultWinner, this.services[result.index]);

                var winner = this.BestResultFrom(winners);
                if (winner?.BestIntent?.Score < 0.5)
                {
                    winner.BestIntent.Intent = "None";
                }

                if (winner == null)
                {
                    throw new InvalidOperationException("No winning intent selected from Luis results.");
                }

                if (winner.Result.Dialog?.Status == DialogResponse.DialogStatus.Question)
                {
#pragma warning disable CS0618
                    var childDialog = await MakeLuisActionDialog(winner.LuisService,
                                                                 winner.Result.Dialog.ContextId,
                                                                 winner.Result.Dialog.Prompt);
#pragma warning restore CS0618
                    context.Call(childDialog, LuisActionDialogFinished);
                }
                else
                {
                    await DispatchToIntentHandler(context, item, winner.BestIntent, winner.Result);
                }
            }
            else
            {
                var intent = new IntentRecommendation() { Intent = string.Empty, Score = 1.0 };
                var result = new LuisResult() { TopScoringIntent = intent };
                await DispatchToIntentHandler(context, item, intent, result);

            }

        }

        private void timerEvent(object state)
        {
            throw new NotImplementedException();
        }

        [LuisIntent("")]
        [LuisIntent("None")]
        public async Task None(IDialogContext context, LuisResult result)
        {

            string message = $"Lo siento, no puedo procesar tu peticion: '{result.Query}'.";
            await context.PostAsync(message.ToUserLocale(context));


            string idea = $"¿Quieres que te pongamos en contacto con recepción para transimitirles tu petición?" + "Si/No";
            string idea1 = $"Poner en contacto con recepción?" + "Si/No";


            PromptDialog.Confirm(
                   context,
                   this.AfterNone,
                 idea.ToUserLocale(context),
                  idea1.ToUserLocale(context),
                  promptStyle: PromptStyle.Auto);
        }

        public async Task AfterNone(IDialogContext context, IAwaitable<bool> argument)
        {
            try
            {
                var confirm = await argument;
                if (confirm)
                {
                    //  await context.PostAsync("De acuerdo, acabamos de enviar tu petición a recepción.");

                    string message = $"De acuerdo, acabamos de enviar tu petición a recepción.";
                    await context.PostAsync(message.ToUserLocale(context));
                }
                else
                {
                    //await context.PostAsync("Si necesitas ayuda para saber que decirme escribe 'Ayuda'.");

                    string message = $"Si necesitas ayuda para saber que decirme escribe 'Ayuda'.";
                    await context.PostAsync(message.ToUserLocale(context));
                }
            }
            catch (Exception)
            {
                // await context.PostAsync("Algo salió mal. Eek");

                string message = $"Algo salió mal. Eek";
                await context.PostAsync(message.ToUserLocale(context));
            }
        }

        [LuisIntent("Greetings")]
        public async Task Greetings(IDialogContext context, LuisResult result)
        {

            // var message = ChatResponse.Greetings;
            //await context.PostAsync("Hola. ¿Como puedo ayudarte?");
            string message = $"Hola. \U0001F44B ¿Como puedo ayudarte?";
            await context.PostAsync(message.ToUserLocale(context));
            context.Wait(MessageReceived);
        }

        //-------------------------------------------------
        //
        // Booking Form process
        //
        //------------------------------------------------

        [LuisIntent("SearchRoom")]
        public async Task Search(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            // await context.PostAsync($"Bienvenido al buscador de habitaciones de Suitech! Estamos analizando tu petición: '{message.Text}'...");
            string response = $"Bienvenido  \U0001F64F \U0001F64F al buscador de habitaciones de Suitech! Estamos analizando tu petición: '{message.Text}'... \U0000231B \U0000231B ";
            await context.PostAsync(response.ToUserLocale(context));

          
            var emailQuery = new EmailQuery();

            var emailFormDialog = new FormDialog<EmailQuery>(emailQuery, this.BuildEmailForm, FormOptions.PromptInStart);

            context.Call(emailFormDialog, ResumeAfterMailsFormDialog);
        }

        
    private async Task ResumeAfterMailsFormDialog(IDialogContext context, IAwaitable<EmailQuery> result)
        {
            var searchQuery = await result;
            Booking saveData = new Booking();
            String serviceBook;
            if (context.UserData.TryGetValue("saving", out saveData))
            {
                context.UserData.TryGetValue("serviceBook", out serviceBook);

                if (searchQuery.Email.ToString() == saveData.Email)
                {

                    //await context.PostAsync("Datos encontrados");
                    string response = "Datos encontrados \U00002714 \U00002714";
                    await context.PostAsync(response.ToUserLocale(context));


                    var savedata = (" \U00002705 Tienes una reserva a nombre de  " + saveData.Name +
                        " con los siguientes datos:" +
                        "\n\n\t Tipo de Habitación \U00001F6CF: " + saveData.typeRoom +
                        "\n\n\t Número de huéspedes \U0001F469: " + saveData.NumPeople +
                        "\n\n\t Niños \U0001F6BC:" + saveData.Kids +
                        "\n\n\t Email de contacto \U0001F4E7: " + saveData.Email +
                        "\n\n\t Teléfono de contacto \U0001F4F1: " + saveData.PhNum +
                        "\n\n\t Información adicional \U00002139: " + saveData.Requests);
                    await context.PostAsync(savedata.ToUserLocale(context));

                    /* await context.PostAsync("Tienes una reserva a nombre de " + saveData.Name +
                         " con los siguientes datos: \n\nTipo de Habitación: " + saveData.typeRoom +
                         "\n\nNúmero de huéspedes: " + saveData.NumPeople +
                         "\t\tNiños: " + saveData.Kids +
                         "\n\nEmail de contacto : " + saveData.Email +
                         "\t\tTeléfono de contacto: " + saveData.PhNum +
                         "\n\nInformación adicional: " + saveData.Requests);*/

                 /*   string cho = "Usar los mismos datos \U0001F5C2";
                    string cho1 = "Hacer una nueva reserva \U0001F195";

                    string cho3 = "¿Quieres usar los mismos datos o quieres hacer una nueva reserva?";
                    string cho4 = "Usar los mismos datos para la reserva?";*/
                    PromptDialog.Choice<string>(
                       context,
                       this.AfterOptionSelectedAsync,
                       new List<string>() { "Usar los mismos datos \U0001F5C2", "Hacer una nueva reserva \U0001F195" },
                       "¿Quieres usar los mismos datos o quieres hacer una nueva reserva?",
                      "Usar los mismos datos para la reserva?",
                       promptStyle: PromptStyle.Auto);

                }
                   else
                  {
                      // await context.PostAsync("No se han encontrado datos para este Email");
                      string response = $"No se han encontrado datos para este Email \U0000274C";
                      await context.PostAsync(response.ToUserLocale(context));

                      context.UserData.SetValue("emailAux", searchQuery.Email.ToString());

                   // string trying = $"¿Quieres hacer una nueva reserva?";
                    //string trying1 = $"Empezar nueva reserva? (Si/No)";

                    PromptDialog.Confirm(
                               context,
                               this.AfterNewBooking,
                                    $"¿Quieres hacer una nueva reserva?",
                                    $"Empezar nueva reserva? (Si/No)",
                                 // prompt: trying.ToUserLocale(context),
                                //  retry: trying1.ToUserLocale(context),
                               promptStyle: PromptStyle.Auto);
                  }
                       }
                else
                {

                    //await context.PostAsync("No se han encontrado datos para este Email");
                    string response = $"No se han encontrado datos para este Email \U0000274C";
                    await context.PostAsync(response.ToUserLocale(context));

  
    
                context.UserData.SetValue("emailAux", searchQuery.Email.ToString());
              //   string trying = $"¿Quieres hacer una nueva reserva?";
               //string trying1 = $"Empezar nueva reserva? (Si/No)";

               
             
                
                PromptDialog.Confirm(
                                  context,
                                  this.AfterNewBooking,
                                 $"¿Quieres hacer una nueva reserva?",
                                 $"Empezar nueva reserva? (Si/No)",
                   //               prompt: trying.ToUserLocale(context),
                 //                 retry: trying1.ToUserLocale(context),
                                  promptStyle: PromptStyle.Auto);

            }
            }

        private Task AfterNewBooking(IDialogContext context)
        {
            throw new NotImplementedException();
        }

        private async Task AfterOptionSelectedAsync(IDialogContext context, IAwaitable<string> result)
        {
            try
            {

                String optionSelected = await result;
                switch (optionSelected)
                {
                    case "Usar los mismos datos":

                        var datequery = new FormDialog<DateQuery>(new DateQuery(), DateQuery.BuildDateForm, FormOptions.PromptInStart);
                        context.Call(datequery, this.DateQueryComplete);

                        break;

                    case "Hacer una nueva reserva":

                        Booking saveData = new Booking();
                        context.UserData.TryGetValue("saving", out saveData);

                           string confirm = "¿Quieres hacer la reserva con los mismos datos de usuario?\n\n Nombre: " + saveData.Name +
                        "/n/nTeléfono de contacto: " + saveData.PhNum;

                        string confirm1 = "Usar mismo usuario? (Si/No)";

                        PromptDialog.Confirm(
                        context,
                        this.AfterSameUserBooking,
                        confirm.ToUserLocale(context),
                        confirm1.ToUserLocale(context),
                        promptStyle: PromptStyle.Auto);

                        break;

                    default:
                        break;
                }
            }
            catch (Exception)
            {
                await context.PostAsync("Algo salió mal. Eek");
            }
        }

        public async Task AfterSameUserBooking(IDialogContext context, IAwaitable<bool> argument)
        {
            try
            {
                var confirm = await argument;
                if (confirm)
                {
                    var bookingShortForm = new FormDialog<BookingFormShort1>(new BookingFormShort1(), BookingFormShort1.BuildFormShort1, FormOptions.PromptInStart);
                    context.Call(bookingShortForm, this.BookingFormShort1Complete);
                }
                else
                {
                    var bookingForm = new FormDialog<BookingForm1>(new BookingForm1(), BookingForm1.BuildForm1, FormOptions.PromptInStart);
                    context.Call(bookingForm, this.BookingForm1Complete);
                }
            }
            catch (Exception)
            {
                await context.PostAsync("Algo salió mal. Eek");
            }
        }

        private async Task DateQueryComplete(IDialogContext context, IAwaitable<DateQuery> result)
        {
            try
            {
                var datequery = await result;

                Booking booking = new Booking();

                context.UserData.TryGetValue("saving", out booking);

                booking.BookingDate = datequery.Fecha;
                booking.Nights = datequery.Noches;

                context.UserData.SetValue("booking", booking);

                context.UserData.RemoveValue("restbookD");
                context.UserData.RemoveValue("restbookT");

                await ServicesBox(context);

            }
            catch (FormCanceledException)
            {
                string noresv = "No hice tu reserva. \U00002716";
                await context.PostAsync(noresv.ToUserLocale(context));
              //  await context.PostAsync("No hice tu reserva.");
            }
            catch (Exception)
            {
                string error = "Algo realmente malo sucedió. Puedes volver a intentarlo mientras tanto, verificaremos qué salió mal. \U000026D4 \U000026D4";
                await context.PostAsync(error.ToUserLocale(context));

                //await context.PostAsync("Algo realmente malo sucedió. Puedes volver a intentarlo mientras tanto, verificaremos qué salió mal. ");
            }

        }


        public async Task AfterNewBooking(IDialogContext context, IAwaitable<bool> argument)
        {
            try
            {

                var confirm = await argument;
                if (confirm)
                {

                    var bookingForm = new FormDialog<BookingForm1>(new BookingForm1(), BookingForm1.BuildForm1, FormOptions.PromptInStart);
                    context.Call(bookingForm, this.BookingForm1Complete);
                    /*
                    var roomsQuery = new RoomsQuery();
                    var roomsFormDialog = new FormDialog<RoomsQuery>(roomsQuery, BuildRoomsForm, FormOptions.PromptInStart);
                    context.Call(roomsFormDialog, ResumeAfterRoomsFormDialog);*/
                }
                else
                {
                    //await context.PostAsync("Se ha cancelado el proceso para reservar u(na habitación.");
                    string response = $"Se ha cancelado el proceso para reservar una habitación. \U00002716";
                    await context.PostAsync(response.ToUserLocale(context));
                }
            }
            catch (Exception)
            {
               // await context.PostAsync("Algo salió mal. Eek");
                string response = $"Algo salió mal. Eek \U000026D4";
                await context.PostAsync(response.ToUserLocale(context));
            }
        }

        private async Task BookingForm1Complete(IDialogContext context, IAwaitable<BookingForm1> result)
        {
            try
            {
                var bookingform = await result;

                Booking booking1 = new Booking();
                string email;

                context.UserData.TryGetValue("emailAux", out email);
                context.UserData.RemoveValue("emailAux");

                booking1.Email = email;
                booking1.BookingDate = bookingform.Fecha.Date;
                booking1.NumPeople = bookingform.Num_Huespedes;
                booking1.Nights = bookingform.Noches;
                booking1.Kids = bookingform.Niños;
                booking1.typeRoom = bookingform.TipoHabitacion.ToString();

                context.UserData.SetValue("booking1", booking1);


                var searchQuery = await result;
                string typeroom = searchQuery.TipoHabitacion.ToString();

                var rooms = this.GetRooms(typeroom);

             //   await context.PostAsync($"He encontrado {rooms.Count()} habitaciones:");

                string response = $"He encontrado {rooms.Count()} \U00002705 habitaciones:";
                await context.PostAsync(response.ToUserLocale(context));

                var resultMessage = context.MakeMessage();
                resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                resultMessage.Attachments = new List<Attachment>();

                for (var i = 0; i < rooms.Count; i++)
                {
                    HeroCard heroCard = new HeroCard()
                    {
                        Title = rooms[i].Name,
                        Subtitle = $"{rooms[i].Price} por noche.",
                        Images = new List<CardImage>()
                        {
                            new CardImage() { Url = rooms[i].Image }
                        },
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "Reserva",
                                Type = ActionTypes.ImBack,
                                Value = "Reservar " + rooms[i].Name,

                             }

                        },
                    };

                    resultMessage.Attachments.Add(heroCard.ToAttachment());
                }
                await context.PostAsync(resultMessage);

            }
            catch (FormCanceledException)
            {
               // await context.PostAsync("No hice tu reserva.");

                string response = $"No hice tu reserva.  \U00002716";
                await context.PostAsync(response.ToUserLocale(context));
            }
            catch (Exception)
            {
               // await context.PostAsync("Algo realmente malo sucedió. Puedes volver a intentarlo mientras tanto, verificaremos qué salió mal. ");
                string response = $"Algo realmente malo sucedió. Puedes volver a intentarlo mientras tanto, verificaremos qué salió mal. \U000026D4 \U000026D4";
                await context.PostAsync(response.ToUserLocale(context));
            }

        }

        private async Task BookingFormShort1Complete(IDialogContext context, IAwaitable<BookingFormShort1> result)
        {
            try
            {
                var bookingform = await result;

                Booking savedData = new Booking();
                Booking booking1 = new Booking();

                context.UserData.TryGetValue("saving", out savedData);

                booking1.Email = savedData.Email;
                booking1.BookingDate = bookingform.Fecha.Date;
                booking1.NumPeople = bookingform.No_Huespedes;
                booking1.Nights = bookingform.Noches;
                booking1.Kids = bookingform.Niños;
                booking1.typeRoom = bookingform.TipoHabitacion.ToString();
                booking1.saveData = true;

                context.UserData.RemoveValue("booking1");
                context.UserData.SetValue("booking1", booking1);

                var searchQuery = await result;
                string typeroom = searchQuery.TipoHabitacion.ToString();

                var rooms = this.GetRooms(typeroom);

                string found = $"He encontrado {rooms.Count()} \U00002705 habitaciones:";
              //  await context.PostAsync($"He encontrado {rooms.Count()} habitaciones:");
                await context.PostAsync(found.ToUserLocale(context));

                var resultMessage = context.MakeMessage();
                resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                resultMessage.Attachments = new List<Attachment>();

                for (var i = 0; i < rooms.Count; i++)
                {
                    HeroCard heroCard = new HeroCard()
                    {
                        Title = rooms[i].Name,
                        Subtitle = $"{rooms[i].Price} por noche.",
                        Images = new List<CardImage>()
                        {
                            new CardImage() { Url = rooms[i].Image }
                        },
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "Reserva",
                                Type = ActionTypes.ImBack,
                                Value = "Reservar " + rooms[i].Name,

                             }

                        },
                    };

                    resultMessage.Attachments.Add(heroCard.ToAttachment());
                }
                await context.PostAsync(resultMessage);

            }
            catch (FormCanceledException)
            {
                // await context.PostAsync("No hice tu reserva.");

                string response = $"No hice tu reserva. \U00002716";
                await context.PostAsync(response.ToUserLocale(context));
            }
            catch (Exception)
            {
                // await context.PostAsync("Algo realmente malo sucedió. Puedes volver a intentarlo mientras tanto, verificaremos qué salió mal. ");
                string response = $"Algo realmente malo sucedió. Puedes volver a intentarlo mientras tanto, verificaremos qué salió mal. \U000026D4 \U000026D4";
                await context.PostAsync(response.ToUserLocale(context));
            }

        
    }
        [LuisIntent("HacerReserva")]
        public async Task HacerReserva(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {

            try
            {
                //await context.PostAsync("Claro, necesitaré algunos detalles tuyos.");
                string response = $"Claro, necesitaré algunos detalles tuyos. \U0001F4DD";
                await context.PostAsync(response.ToUserLocale(context));
                var bookingForm = new FormDialog<BookingForm2>(new BookingForm2(), BookingForm2.BuildForm2, FormOptions.PromptInStart);
                context.Call(bookingForm, this.BookingForm2Complete);

            }
            catch (Exception)
            {
               // await context.PostAsync("Algo realmente malo sucedió. Puedes intentarlo más tarde, mientras tanto, verificaremos qué salió mal.");

                string response = $"Algo realmente malo sucedió. Puedes intentarlo más tarde, mientras tanto, verificaremos qué salió mal. \U000026D4 \U000026D4";
                await context.PostAsync(response.ToUserLocale(context));
                context.Wait(MessageReceived);
            }
        }

        private async Task BookingForm2Complete(IDialogContext context, IAwaitable<BookingForm2> result)
        {
            try
            {
                var bookingform = await result;

                Booking booking = new Booking();
                Booking booking1 = new Booking();

                context.UserData.TryGetValue("booking1", out booking1);

                booking.Email = booking1.Email;
                booking.BookingDate = booking1.BookingDate.Date;
                booking.Nights = booking1.Nights;
                booking.NumPeople = booking1.NumPeople;
                booking.Kids = booking1.Kids;
                booking.typeRoom = booking1.typeRoom;
                booking.Regimen = bookingform.Regimen_uso.ToString();
                booking.Name = bookingform.Nombre;
                booking.PhNum = bookingform.Num_Telefono;
                booking.Requests = bookingform.Peticiones_extra;

                context.UserData.SetValue("booking", booking);
                
                await ServicesBox(context);

            }
            catch (FormCanceledException)
            {
              //  await context.PostAsync("No hice tu reserva.");

                string response = $"No hice tu reserva. \U00002716";
                await context.PostAsync(response.ToUserLocale(context));
            }
            catch (Exception)
            {
              //  await context.PostAsync("Algo realmente malo sucedió. Puedes volver a intentarlo mientras tanto, verificaremos qué salió mal. ");
                string response = $"Algo realmente malo sucedió. Puedes intentarlo más tarde, mientras tanto, verificaremos qué salió mal. \U000026D4 \U000026D4";
                await context.PostAsync(response.ToUserLocale(context));
            }

        }


////////////////////////////////////////////////////////////////////
        /// <summary>
        /// // Testing Form for boking
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>

    /*    [LuisIntent("RoomReservationForm")]
        public async Task RoomReservationForm(IDialogContext context, LuisResult result)
        {

            var replyMessage = context.MakeMessage();
           var json = await GetCardText("reservatest");

            // AdaptiveCardParseResult jsonResult = AdaptiveCard.FromJson(json);
            var results = AdaptiveCard.FromJson(json);
            var card = results.Card;
            replyMessage.Attachments.Add(new Attachment()
            {
                Content = card,
               // Content = await GetCardText("reservatest"),
                ContentType = AdaptiveCard.ContentType,
                Name = "Card"
            });

            await context.PostAsync(replyMessage);
        }

        public async Task<string> GetCardText(string cardName)
        {
            var path = HostingEnvironment.MapPath($"/{cardName}.json");
            if (!File.Exists(path))
                return string.Empty;

            using (var f = File.OpenText(path))
            {
                return await f.ReadToEndAsync();
            }
        }
        */
//////////////////////////////////////////////////////////////////



        [LuisIntent("Servicios")]
        public async Task Servicios(IDialogContext context, LuisResult result)
        {
            await ServicesBox(context);

        }

        //Intents for Services

        [LuisIntent("DetailsWifi")]
        public async Task DetailsWifi(IDialogContext context, LuisResult result)
        {
            string id = "SuitechDemo_FreeWiFi ";
            string password = "pswIsNotThepsw";

            string details = "El nombre de usuario es \U0001F194\U0001F194	: " + id + "\n\nLa contraseña es \U0001F6E1\U0001F6E1: " + password;
            await context.PostAsync(details.ToUserLocale(context));
           // await context.PostAsync(details);
            context.Wait(MessageReceived);
        }

        [LuisIntent("DetailsLimpieza")]
        public async Task DetailsLimpieza(IDialogContext context, LuisResult result)
        {
            var details = context.MakeMessage();
            //await context.PostAsync("Horario de Limpieza:");

            string response = $"Horario de Limpieza:";
            await context.PostAsync(response.ToUserLocale(context));

            details.Attachments = new List<Attachment>()
            {

                 new Attachment()
                {
                    Name="LimpiezaHorario",
                     //     ContentUrl = @"C:\Users\08024\Downloads\BotBuilder-Samples\BotBuilder-Samples-master\CSharp\intelligence-LUIS\images\Services Images\Cleaning.png",
                     ContentUrl =@"https://i.pinimg.com/originals/67/6b/28/676b281c1cf1d8e2dd7d1811ba9454f8.jpg",
                     ContentType = "image/png",

                }
            };

            await context.PostAsync(details);
            context.Wait(MessageReceived);
        }


        [LuisIntent("DetailsMinibar")]
        public async Task DetailsMinibar(IDialogContext context, LuisResult result)
        {

            var details = context.MakeMessage();
        //    await context.PostAsync("Precio de Productos de Minibar :");

            string response = $"Precio de Productos de Minibar :";
            await context.PostAsync(response.ToUserLocale(context));

            details.Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    Name="Minibarprecio",
                     ContentUrl = $"https://media-cdn.tripadvisor.com/media/photo-s/05/d5/0a/eb/ac-hotel-alicante-by.jpg",
                     ContentType = "image/png",

                }
            };

            await context.PostAsync(details);
            context.Wait(MessageReceived);
        }

        [LuisIntent("DetailsServicioHabitaciones")]
        public async Task DetailsServicioHabitaciones(IDialogContext context, LuisResult result)
        {
            var details = context.MakeMessage();
        //    await context.PostAsync("Menú del restaurante para el servicio de habitaciones:");

            string response = $"Menú del restaurante para el servicio de habitaciones:";
            await context.PostAsync(response.ToUserLocale(context));

            details.Attachments = new List<Attachment>()
            {
                 new Attachment()
                {
                    Name="ServicioHabitacionesMenu",
                     ContentUrl = "https://media-cdn.tripadvisor.com/media/photo-s/08/c7/f8/1b/eurohotel-diagonal-port.jpg",
                    ContentType = "image/jpg",

                }
            };

            await context.PostAsync(details);
            context.Wait(MessageReceived);
        }


        [LuisIntent("DetailsConsigna")]
        public async Task DetailsConsigna(IDialogContext context, LuisResult result)
        {
            var details = context.MakeMessage();
           // await context.PostAsync("Horario de Consigna:");

            string response = $"Horario de Consigna:";
            await context.PostAsync(response.ToUserLocale(context));

            details.Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    Name="ConsignaHorario",
                     ContentUrl = "https://parisbytrain.com/wp-content/uploads/2013/02/paris-train-station-storage-lockers-sign.jpg",
                    ContentType = "image/jpg",

                }
            };

            await context.PostAsync(details);
            context.Wait(MessageReceived);
        }

        [LuisIntent("DetailsTaxi")]
        public async Task DetailsTaxi(IDialogContext context, LuisResult result)
        {
            var details = context.MakeMessage();
            await context.PostAsync("Precios de Taxi:");

            details.Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    Name="TaxiPrecio",
                     ContentUrl = "https://kingcountywatertaxi.files.wordpress.com/2017/06/revisedwsschedule.jpg?w=700",
                    ContentType = "image/jpg",

                }
            };

            await context.PostAsync(details);
            context.Wait(MessageReceived);
        }


        [LuisIntent("DetailsPiscina")]
        public async Task DetailsPiscina(IDialogContext context, LuisResult result)
        {
            var details = context.MakeMessage();
           // await context.PostAsync("Horario de Piscina:");


            string response = $"Horario de Piscina:";
            await context.PostAsync(response.ToUserLocale(context));

            details.Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    Name="PiscinaHorario",
                     ContentUrl = "http://www.hamptonpool.co.uk/_img/timetables/timetable_36m-win.jpg",
                    ContentType = "image/jpg",

                }
            };

            await context.PostAsync(details);
            context.Wait(MessageReceived);
        }


        [LuisIntent("DetailsGimnasio")]
        public async Task DetailsGimnasio(IDialogContext context, LuisResult result)
        {
            var details = context.MakeMessage();
           // await context.PostAsync("Horario de Gimnasio:");


            string response = $"Horario de Gimnasio:";
            await context.PostAsync(response.ToUserLocale(context));

            details.Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    Name="GimnasioHorario",
                     ContentUrl = "https://pbs.twimg.com/media/BkXheoYCUAAxPmJ.jpg",
                    ContentType = "image/jpg",

                }
            };

            await context.PostAsync(details);
            context.Wait(MessageReceived);
        }

        [LuisIntent("DetailsRestaurante")]
        public async Task DetailsRestaurante(IDialogContext context, LuisResult result)
        {
            var details = context.MakeMessage();
          //  await context.PostAsync("Horario y Menú de Resturante:");

            string response = $"Horario y Menú de Resturante:";
            await context.PostAsync(response.ToUserLocale(context));

            details.Attachments = new List<Attachment>()
            {
                new Attachment()
                {
                    Name="ResturanteHorario",
                     ContentUrl = "http://www.woodcotegreen.com/images/image/QuickLinks/Opening%20Times/Waterfall-Cafe-Opening-Times.jpg",
                    ContentType = "image/jpg",

                },
                new Attachment()
                {
                    Name="ResturanteMenu",
                     ContentUrl = "https://media-cdn.tripadvisor.com/media/photo-s/08/c7/f8/1b/eurohotel-diagonal-port.jpg",
                    ContentType = "image/jpg",

                }
            };
            await context.PostAsync(details);
            context.Wait(MessageReceived);
        }

        [LuisIntent("ReservaRestaurante")]
        public async Task ReservaRestaurante(IDialogContext context, LuisResult result)
        {
            var restaurantForm = new RestaurantForm();
            var restaurantFormDialog = new FormDialog<RestaurantForm>(restaurantForm, BuildRestaurantForm, FormOptions.PromptInStart);
            context.Call(restaurantFormDialog, ResumeAfterRestaurantForm);

        }

        private IForm<RestaurantForm> BuildRestaurantForm()
        {
            return new FormBuilder<RestaurantForm>()
                .Field(nameof(RestaurantForm.DiaRestaurante), validate: ValidateRestDay)
                .Field(nameof(RestaurantForm.TurnoRestaurante))
                .Build();
        }

        private async Task ResumeAfterRestaurantForm(IDialogContext context, IAwaitable<RestaurantForm> result)
        {
            try
            {
                var message = await result;
                DateTime restbookD = message.DiaRestaurante;
                DateTime restbookT = message.TurnoRestaurante;

                context.UserData.SetValue("restbookD", restbookD);
                context.UserData.SetValue("restbookT", restbookT);

                //await context.PostAsync($"La reserva de la mesa ha sido confirmada");

                string response = $"La reserva de la mesa ha sido confirmada \U00002705\U00002705";
                await context.PostAsync(response.ToUserLocale(context));

                var resultMessage2 = context.MakeMessage();
                resultMessage2.Attachments = new List<Attachment>();

                List<CardAction> botonPago = new List<CardAction>();
                CardAction button = new CardAction()
                {
                    Title = "Pagar",
                    Type = ActionTypes.ImBack,
                    Value = "Pagar"
                };
                botonPago.Add(button);

                HeroCard buttonCard = new HeroCard()
                {
                    Buttons = botonPago
                };
                resultMessage2.Attachments.Add(buttonCard.ToAttachment());
                
                await context.PostAsync(resultMessage2);

            }
            catch (FormCanceledException ex)
            {
                string reply;

                if (ex.InnerException == null)
                {
                    reply = "You have canceled the operation. \U00002716";

                    await context.PostAsync(reply.ToUserLocale(context));

                }
                else
                {
                    reply = $"Oops \U0000203C\U0000203C Something went wrong \U0001F625 Technical Details: {ex.InnerException.Message}";
                    await context.PostAsync(reply.ToUserLocale(context));
                }

                await context.PostAsync(reply);
            }
            finally
            {
                context.Done<object>(null);
            }
        }

        [LuisIntent("Pagar")]
        public async Task RealizarPago(IDialogContext context, LuisResult result)
        {

            System.Diagnostics.Process.Start("file:///C:/backup%20old%20system/Gopal/Desktop%20Backup/Payment/Payment.html");

            await SaveBooking(context);

            SaveData(context);
        }
        
        private async Task SaveBooking(IDialogContext context)
        {
            //------------------------------
            //
            // Save data to the BBDD
            // Now data is being stored on BookingFormComplete
            //
            //------------------------------

            /*var booking = new Booking();
            //var listBooking = new List<Booking>();
            //context.UserData.TryGetValue("booking", out listBooking);
            
            string tipoHabitacion;
            context.UserData.TryGetValue("typeRoomAux", out tipoHabitacion);
            context.UserData.RemoveValue("typeRoomAux");
            
            booking.BookingDateTime = bookingform.Date.Date;
            booking.Name = bookingform.Name;
            booking.NumPeople = bookingform.NumPeople;
            booking.Nights = bookingform.Nights;
            booking.Kids = bookingform.Niños;
            booking.PhNum = bookingform.PhNum;
            booking.Email = bookingform.Email;
            booking.Requests = bookingform.Requests;
            booking.typeRoom = tipoHabitacion;

            //listBooking.Add(booking);

            //context.UserData.SetValue("booking", listBooking);
            context.UserData.SetValue("booking", booking);
            */

         //   await context.PostAsync("Tu reserva está confirmada.");

            string response = $"Tu reserva está confirmada.";
            await context.PostAsync(response.ToUserLocale(context));

        }

        [LuisIntent("CancelarReservar")]
        public async Task CancelBooking(IDialogContext context, LuisResult result)
        {
            Booking booking;
            if (context.UserData.TryGetValue<Booking>("booking", out booking))
            {

                string cancel = $"¿Seguro que quieres cancelar su reserva actual para \U00002753 " + booking.BookingDate + "? (Sí/No)";
                string cancel1 = $"Cancelar la reserva actual? (Si/No)";

               PromptDialog.Confirm(
                       context,
                       this.AfterCancelBooking,
                       cancel.ToUserLocale(context),
                      cancel1.ToUserLocale(context),
                       promptStyle: PromptStyle.Auto);


               // string response = $"Tu reserva está confirmada.";
            //    await context.PostAsync(PromptDialog.ToUserLocale(context));
            }
            else
            {
             //   await context.PostAsync("Actualmente no tienes ninguna reserva.");

                string response = $"Actualmente no tienes ninguna reserva. \U00002755\U00002755 ";
                await context.PostAsync(response.ToUserLocale(context));

                context.Wait(MessageReceived);
            }
        }
       
        public async Task AfterCancelBooking(IDialogContext context, IAwaitable<bool> argument)
        {
            try
            {
                var confirm = await argument;
                if (confirm)
                {
                    context.UserData.RemoveValue("booking");
                    //await context.PostAsync("Tu reserva ha sido cancelada");

                    string response = $"Tu reserva ha sido cancelada \U0000274C";
                    await context.PostAsync(response.ToUserLocale(context));
                }
                else
                {
                   // await context.PostAsync("No cancelé tu reserva.");

                    string response = $"No cancelé tu reserva. \U0000274E";
                    await context.PostAsync(response.ToUserLocale(context));
                }
            }
            catch (Exception)
            {
                //await context.PostAsync("Algo salió mal. Eek");

                string response = $"Algo salió mal. Eek \U00002049	";
                await context.PostAsync(response.ToUserLocale(context));

            }
            finally
            {
                context.Wait(MessageReceived);
            }
        }
        [LuisIntent("VerReserva")]
        public async Task ViewBooking(IDialogContext context, LuisResult result)
        {
            try
            {
                Booking booking;
                DateTime restbookD;
                DateTime restbookT;
                string serviceBook;

                if (context.UserData.TryGetValue("restbookD", out restbookD) & context.UserData.TryGetValue("restbookT", out restbookT))
                {
                    serviceBook = "Restaurante el día " + restbookD.Date + " a las " + restbookT.TimeOfDay;

                    await context.PostAsync(serviceBook.ToUserLocale(context));

                }
                else
                {

                    serviceBook = "No se ha reservado ningún servicio \U00002755";

                    await context.PostAsync(serviceBook.ToUserLocale(context));

                }
                
                if (context.UserData.TryGetValue("booking", out booking))
                {
                    
                   /* await context.PostAsync("Tienes una reserva a nombre de " + booking.Name +
                        " con los siguientes datos: \n\nTipo de Habitación:" + booking.typeRoom +
                        "\n\nEntrada: " + booking.BookingDate.Date +
                        "\t\tSalida: " + booking.BookingDate.AddDays(booking.Nights).Date +
                        "\n\nNúmero de huéspedes: " + booking.NumPeople +
                        "\t\tNiños: " + booking.Kids +
                        "\n\nEmail de contacto : " + booking.Email +
                        "\n\nTeléfono de contacto: " + booking.PhNum +
                        "\n\nServicios reservados: " +  serviceBook +
                        "\n\nInformación adicional: " + booking.Requests);*/

                   var  bookinconfirm = " \U0001F524 Tienes una reserva a nombre de " + booking.Name +
                        " con los siguientes datos: \n\n \U0001F6CF Tipo de Habitación:" + booking.typeRoom +
                        "\n\n \U0000231A Entrada: " + booking.BookingDate.Date +
                        "\t\t \U000023F0 Salida: " + booking.BookingDate.AddDays(booking.Nights).Date +
                        "\n\n \U00000032 Número de huéspedes: " + booking.NumPeople +
                        "\t\t \U0001F476 Niños: " + booking.Kids +
                        "\n\n \U0001F4E7 Email de contacto : " + booking.Email +
                        "\n\n \U0001F4F1 Teléfono de contacto: " + booking.PhNum +
                        "\n\n \U0001F523 Servicios reservados: " + serviceBook +
                        "\n\n \U00002139 Información adicional: " + booking.Requests ;

                    await context.PostAsync(bookinconfirm.ToUserLocale(context));


                }
                else
                {
                   // await context.PostAsync("Actualmente no tienes ninguna reserva.");

                    var viewbook = "Actualmente no tienes ninguna reserva. \U00002755\U00002755";

                    await context.PostAsync(viewbook.ToUserLocale(context));
                }
            }
            catch (Exception)
            {
               // await context.PostAsync("Algo salió mal, lo siento :(");

                string response = $"Algo salió mal. Eek \U00002049";
                await context.PostAsync(response.ToUserLocale(context));
            }
            finally
            {
                context.Wait(MessageReceived);
            }
        }
        private void SaveData(IDialogContext context)
        {

            string save= $"Quieres que guardemos tus datos para futuras reservas? \U0001F4BE (Sí/No)";
            string save1 = $"Guardamos sus datos? \U0001F4E5 (Si/No)";


            PromptDialog.Confirm(context, this.AfterSaveData,
              save.ToUserLocale(context),
                save1.ToUserLocale(context),
                promptStyle: PromptStyle.Auto);
        }

        public async Task AfterSaveData(IDialogContext context, IAwaitable<bool> argument)
        {
            try
            {
                var confirm = await argument;

                if (confirm)
                { 
                    //List<Booking> listBooking = new List<Booking>();
                    //List<Booking> listSaving = new List<Booking>();
                    var booking = new Booking();

                    //if (context.UserData.TryGetValue("booking", out listBooking))
                    if (context.UserData.TryGetValue("booking", out booking))
                    {

                        //booking = listBooking[listBooking.Count - 1];
                        

                    }
                    else
                    {
                       // await context.PostAsync("No he encontrado datos entre tus reservas");

                        string nobook = $"No he encontrado datos entre tus reservas \U00002755";
                        await context.PostAsync(nobook.ToUserLocale(context));
                    }


                    //context.UserData.TryGetValue("saving", out listSaving);
                    //listSaving.Add(booking);
                    //context.UserData.SetValue("saving", listSaving);
                    context.UserData.SetValue("saving", booking);

                   // await context.PostAsync("Tu datos han sido guardados");

                    string response = $"Tu datos han sido guardados \U0001F4E5";
                    await context.PostAsync(response.ToUserLocale(context));
                }
                else
                {
                    //await context.PostAsync("No se han guardado tus datos.");

                    string response = $"No se han guardado tus datos. \U0001F4E5\U0000274C";
                    await context.PostAsync(response.ToUserLocale(context));
                }

            }
            catch (Exception)
            {
              //  await context.PostAsync("Algo salió mal. Eek");

                string response = $"Algo salió mal, lo siento \U00002049";
                await context.PostAsync(response.ToUserLocale(context));
            }
        }
        [LuisIntent("HoraDeEntrada")]
        public async Task OpeningHours(IDialogContext context, LuisResult result)
        {
            try
            {
               // await context.PostAsync("Aquí están nuestras horas de check in: ");
                //await context.PostAsync("de lunes a viernes: 8.00am to 11.00am \n\n" +
        //"sábado & domingo: 8.00am to 14.00pm \n\n");


                string response = $"Aquí están nuestras horas de check in \U0001F55B:";
                string response1 = $"de lunes a viernes: 8.00am to 11.00am \n\n" +
        "sábado & domingo: 8.00am to 14.00pm \n\n";
                await context.PostAsync(response.ToUserLocale(context));
                await context.PostAsync(response1.ToUserLocale(context));

            }
            catch (Exception)
            {
               // await context.PostAsync("Algo salió mal, lo siento :(");

                string response = $"Algo salió mal, lo siento \U00002049";
                await context.PostAsync(response.ToUserLocale(context));

            }
            finally
            {
                context.Wait(MessageReceived);
            }
        }
        [LuisIntent("ObtenerUbicación")]
        public async Task GetLocation(IDialogContext context, LuisResult result)
        {
            try
            {
               // await context.PostAsync("Aquí está nuestra dirección y un mapa para tu referencia: ");
                //await context.PostAsync("Rambla de Catalunya, 26, 08007 Barcelona");

                string response = $"Aquí está nuestra dirección y un mapa para tu referencia:";
                string response1 = $"Rambla de Catalunya, 26, 08007 Barcelona";
                await context.PostAsync(response.ToUserLocale(context));
                await context.PostAsync(response1.ToUserLocale(context));

                var reply = context.MakeMessage();

                reply.Attachments = new List<Attachment>()
                    {
                        new Attachment()
                        {
                           ContentUrl = "https://maps.google.com/maps/api/staticmap?&channel=ta.desktop&zoom=14&size=300x190&client=gme-tripadvisorinc&sensor=false&language=es_ES&center=41.384670,2.172806&maptype=roadmap&&markers=icon:http%3A%2F%2Fc1.tacdn.com%2Fimg2%2Fmaps%2Ficons%2Fpin_v2_CurrentCenter.png|41.38467,2.172806&signature=AeZfpCHm7wWKJOo0R4NqcnQ_d_I=",
                            ContentType = "image/jpg",
                            Name = "Map.jpg"
                        }
                    };
                await context.PostAsync(reply);
            }
            catch (Exception)
            {
              //  await context.PostAsync("Algo salió mal, lo siento:(");


                string response = $"Algo salió mal, lo siento \U00002049";
                await context.PostAsync(response.ToUserLocale(context));
            }
            finally
            {
                context.Wait(MessageReceived);
            }
        }
        [LuisIntent("Gracias")]

        public async Task Gracias(IDialogContext context, LuisResult result)
        {
          //  await context.PostAsync("¡De nada! ¿Necesitas algo más?");

            string response = $"¡De nada! \U0001F642 ¿Necesitas algo más?";
            await context.PostAsync(response.ToUserLocale(context));

            context.Wait(MessageReceived);
        }

        [LuisIntent("Si")]

        public async Task Si(IDialogContext context, LuisResult result)
        {
           // await context.PostAsync("Ok, ¿cómo puedo ayudarte?");

            string response = $"Ok \U0001F9D0, ¿cómo puedo ayudarte?";
            await context.PostAsync(response.ToUserLocale(context));

            context.Wait(MessageReceived);
        }

        /* [LuisIntent("QueryAmenities")]

            public async Task QueryAmenities(IDialogContext context, LuisResult result)
            {
                foreach (var entity in result.Entities.Where(Entity => Entity.Type == "Amenity"))
                {
                    var value = entity.Entity.ToLower();
                    if (value == "pool" || value == "gym" || value == "wifi" || value == "towels")
                    {
                        await context.PostAsync("Si nosotros lo tenemos!");
                        context.Wait(MessageReceived);
                        return;
                    }
                    else
                    {
                        await context.PostAsync("Lo siento, no tenemos este amenidad");
                        context.Wait(MessageReceived);
                        return;
                    }
                }
              await context.PostAsync("Lo siento, no tenemos este amenidad");
                context.Wait(MessageReceived);
                return;
            }
            */

        [LuisIntent("No")]

        public async Task No(IDialogContext context, LuisResult result)
        {
          //  await context.PostAsync("¡¡Estupendo!! Que tengas un buen día !!");

            string response = $"¡¡Estupendo!! \U0001F929 Que tengas un buen día !!";
            await context.PostAsync(response.ToUserLocale(context));

            context.Wait(MessageReceived);
        }


        [LuisIntent("Help")]
        public async Task Help(IDialogContext context, LuisResult result)
        {
            //await context.PostAsync("¡Hola! Prueba diciendome algo como 'Encuéntrame una habitación'.");

            string response = $"¡Hola! Prueba diciendome algo como 'Encuéntrame una habitación'.";
            await context.PostAsync(response.ToUserLocale(context));

            context.Wait(this.MessageReceived);
        }

        private IForm<RoomsQuery> BuildRoomsForm()
        {

            OnCompletionAsyncDelegate<RoomsQuery> processRoomsSearch = async (context, state) =>
            {
                string tipoHabitacion = state.TipoHabitacion.ToString();
                var message = "Buscando una habitación";
                await context.PostAsync(message.ToUserLocale(context));

                if (!string.IsNullOrEmpty(state.TipoHabitacion.ToString()))
                {
                    message += $" {state.TipoHabitacion}...";
                }

                await context.PostAsync(message);
            };

            return new FormBuilder<RoomsQuery>()
                .Field(nameof(RoomsQuery.TipoHabitacion))//, (state) => string.IsNullOrEmpty(state.TipoHabitacion.ToString()))
                .OnCompletion(processRoomsSearch)
                .Build();
        }

        private IForm<EmailQuery> BuildEmailForm()
        {
            OnCompletionAsyncDelegate<EmailQuery> processEmailSearch = async (context, state) =>
            {
                string email = state.Email.ToString();
                
              //  await context.PostAsync("Buscando datos...");

                string response = $"Buscando datos...";
                await context.PostAsync(response.ToUserLocale(context));

            };

            return new FormBuilder<EmailQuery>()
                .Field(nameof(EmailQuery.Email), validate: ValidateEmail)
                .OnCompletion(processEmailSearch)
                .Build();
        }

        /*
        private async Task ResumeAfterRoomsFormDialog(IDialogContext context, IAwaitable<RoomsQuery> result)
        {
            try
            {
                var searchQuery = await result;

                var rooms = this.GetRooms(searchQuery);

                await context.PostAsync($"He encontrado {rooms.Count()} habitaciones:");

                var resultMessage = context.MakeMessage();
                resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                resultMessage.Attachments = new List<Attachment>();
                
                for (var i = 0; i < rooms.Count; i++)
                {
                    HeroCard heroCard = new HeroCard()
                    {
                        Title = rooms[i].Name,
                        Subtitle = $"{rooms[i].Price} por noche.",
                        Images = new List<CardImage>()
                        {
                            new CardImage() { Url = rooms[i].Image }
                        },
                        Buttons = new List<CardAction>()
                        {
                            new CardAction()
                            {
                                Title = "Más detalles",
                                Type = ActionTypes.ImBack,
                                Value = "check in horas?"
                                //Value = "Más información de la " + rooms[i].Name    
                            },
                            new CardAction()
                            {
                                Title = "Reserva",
                                Type = ActionTypes.ImBack,
                                Value = "Reservar " + rooms[i].Name,

                             }

                        },
                    };

                    resultMessage.Attachments.Add(heroCard.ToAttachment());
                }
                await context.PostAsync(resultMessage);

            }
            catch (FormCanceledException ex)
            {
                string reply;

                if (ex.InnerException == null)
                {
                    reply = "You have canceled the operation.";
                }
                else
                {
                    reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
                }

                await context.PostAsync(reply);
            }
            finally
            {
                context.Done<object>(null);
            }
        }
        */

        /// <summary>
        /// ///
        /// 
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// 


private async Task ServicesBox(IDialogContext context)
        {
            try
            {
                var services = this.GetServices();

                //await context.PostAsync($"Puedes echarle un vistazo a los diferentes servicios que ofrecemos:");

                string response = $"Puedes echarle un vistazo a los diferentes servicios que ofrecemos:";
                await context.PostAsync(response.ToUserLocale(context));

                var resultMessage = context.MakeMessage();
                resultMessage.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                resultMessage.Attachments = new List<Attachment>();

                foreach (var service in services)
                {

                    List<CardAction> cardButtons = new List<CardAction>();

                    // wifi
                    string valuetitle = "Más detalles";
                    string value = "Más detalles wi-fi";


                    // Limpieza
                    string valuetitle1 = "Más detalles";
                    string value1 = "Más detalles limpieza";


                    // minibar
                    string valuetitle2 = "Más detalles";
                    string value2 = "Más detalles minibar";


                    // servicio habitaciones
                    string valuetitle3 = "Más detalles";
                    string value3 = "Más detalles servicio habitaciones";


                    // consigna
                    string valuetitle4 = "Más detalles";
                    string value4 = "Más detalles consigna";

                    // taxi
                    string valuetitle5 = "Más detalles";
                    string value5 = "Más detalles taxi";

                    // piscina
                    string valuetitle6 = "Más detalles";
                    string value6 = "Más detalles piscina";

                    // gimnasio
                    string valuetitle7 = "Más detalles";
                    string value7 = "Más detalles gimnasio";


                    // restaurante
                    string valuetitle8 = "Más detalles";
                    string value8 = "Más detalles restaurante";


                    // Reservar restaurante
                    string valuetitle9 = "Más detalles";
                    string value9 = "Reservar restaurante";


                    switch (service.Name)
                    {
                        case "Wi-fi":
                            string details = service.id + "\n\n" + service.password;
                            CardAction Button1 = new CardAction()
                            {
                                Title = valuetitle.ToUserLocale(context),
                                Type = ActionTypes.ImBack,
                                Value = value.ToUserLocale(context)
                            };
                            cardButtons.Add(Button1);
                            break;
                        case "Limpieza":
                            CardAction Button2 = new CardAction()
                            {
                                Title = valuetitle1.ToUserLocale(context),
                                Type = ActionTypes.ImBack,
                                Value = value1.ToUserLocale(context)
                            };
                            cardButtons.Add(Button2);
                            break;
                        case "Minibar":
                            CardAction Button3 = new CardAction()
                            {
                                Title = valuetitle2.ToUserLocale(context),
                                Type = ActionTypes.ImBack,
                                Value = value2.ToUserLocale(context)
                            };
                            cardButtons.Add(Button3);
                            break;
                        case "Servicio de habitaciones":
                            CardAction Button4 = new CardAction()
                            {
                                Title = valuetitle3.ToUserLocale(context),
                                Type = ActionTypes.ImBack,
                                Value = value3.ToUserLocale(context)
                            };
                            cardButtons.Add(Button4);
                            break;
                        case "Consigna":
                            CardAction Button5 = new CardAction()
                            {
                                Title = valuetitle4.ToUserLocale(context),
                                Type = ActionTypes.ImBack,
                                Value = value4.ToUserLocale(context)
                            };
                            cardButtons.Add(Button5);
                            break;
                        case "Pedir un taxi":
                            CardAction Button6 = new CardAction()
                            {
                                Title = valuetitle5.ToUserLocale(context),
                                Type = ActionTypes.ImBack,
                                Value = value5.ToUserLocale(context)
                            };
                            cardButtons.Add(Button6);
                            break;
                        case "Piscina":
                            CardAction Button7 = new CardAction()
                            {
                                Title = valuetitle6.ToUserLocale(context),
                                Type = ActionTypes.ImBack,
                                Value = value6.ToUserLocale(context)
                            };
                            cardButtons.Add(Button7);
                            break;
                        case "Gimnasio":
                            CardAction Button8 = new CardAction()
                            {
                                Title = valuetitle7.ToUserLocale(context),
                                Type = ActionTypes.ImBack,
                                Value = value7.ToUserLocale(context)
                            };
                            cardButtons.Add(Button8);
                            break;
                        case "Restaurante":
                            CardAction Button9 = new CardAction()
                            {
                                Title = valuetitle8.ToUserLocale(context),
                                Type = ActionTypes.ImBack,
                                Value = value8.ToUserLocale(context)
                            };
                            cardButtons.Add(Button9);
                            CardAction Button10 = new CardAction()
                            {
                                Title = valuetitle9.ToUserLocale(context),
                                Type = ActionTypes.ImBack,
                                Value = value9.ToUserLocale(context)
                            };
                            cardButtons.Add(Button10);

                            break;
                        default:
                            break;
                    }

                    HeroCard heroCard = new HeroCard()
                    {
                        Title = service.Name,
                        Images = new List<CardImage>()
                                {
                                    new CardImage(){ Url = service.Image }
                                },
                        Buttons = cardButtons
                    };
                    resultMessage.Attachments.Add(heroCard.ToAttachment());

                }

                var resultMessage2 = context.MakeMessage();
                resultMessage2.Attachments = new List<Attachment>();



                string valuetitle10 = "Pagar";
                string value10 = "Pagar";
                List<CardAction> botonPago = new List<CardAction>();
                CardAction button = new CardAction()
                {
                    Title = valuetitle10.ToUserLocale(context),
                    Type = ActionTypes.ImBack,
                    Value = value10.ToUserLocale(context)
                };
                botonPago.Add(button);
                HeroCard buttonCard = new HeroCard()
                {
                    Buttons = botonPago
                };
                resultMessage2.Attachments.Add(buttonCard.ToAttachment());



                await context.PostAsync(resultMessage);
                await context.PostAsync(resultMessage2);
            }
            catch (FormCanceledException ex)
            {
                string reply;

                if (ex.InnerException == null)
                {
                    reply = "You have canceled the operation.";
                    await context.PostAsync(reply.ToUserLocale(context));
                }
                else
                {
                    reply = $"Oops! Something went wrong :( Technical Details: {ex.InnerException.Message}";
                    await context.PostAsync(reply.ToUserLocale(context));
                }

                await context.PostAsync(reply);
            }
            finally
            {
                context.Done<object>(null);
            }
        }
        
        private List<Room> GetRooms(string searchQuery)
        {
            var rooms = new List<Room>();
            List<string> auxPicture = new List<string>(new string[] { "Imagen no encontrada", "Imagen no encontrada", "Imagen no encontrada" });
            double auxPrice = 0;

            switch (searchQuery)
            {

                case "Individual":
                 /*   auxPicture[0] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Individual1.jpg";
                    auxPicture[1] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Individual2.jpg";
                    auxPicture[2] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Individual3.jpg";
                    */
                    auxPicture[0] = @"https://image.ibb.co/cFR5q7/Individual1.jpg";
                    auxPicture[1] = @"https://image.ibb.co/iGr5q7/Individual2.jpg";
                    auxPicture[2] = @"https://image.ibb.co/jxW5q7/Individual3.jpg";


                    auxPrice = 1;
                    break;
                case "Doble":
                   /* auxPicture[0] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Doble1.png";
                    auxPicture[1] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Doble2.jpg";
                    auxPicture[2] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Doble3.png";
                    */

                    auxPicture[0] = @"https://image.ibb.co/jsUEiS/Doble1.jpg";
                    auxPicture[1] = @"https://image.ibb.co/cWZfOS/Doble2.jpg";
                    auxPicture[2] = @"https://image.ibb.co/i4HdA7/Doble3.png";

                    auxPrice = 2;
                    break;
                case "Matrimonial":
  /*                  auxPicture[0] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Matrimonial1.jpg";
                    auxPicture[1] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Matrimonial2.jpg";
                    auxPicture[2] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Matrimonial3.jpg";

*/

                    auxPicture[0] = @"https://image.ibb.co/jz8JA7/Matrimonial1.jpg";
                    auxPicture[1] = @"https://image.ibb.co/is0WV7/Matrimonial2.jpg";
                    auxPicture[2] = @"https://image.ibb.co/czO73S/Matrimonial3.jpg";



                    auxPrice = 2.3;
                    break;
                case "Triple":
                  /*  auxPicture[0] = @"https://image.ibb.co/ircBV7/Triple1.png";
                    //auxPicture[0] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Triple1.png";
                    auxPicture[1] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Triple2.png";
                    auxPicture[2] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Triple3.jpg";
                    */


                    auxPicture[0] = @"https://image.ibb.co/ircBV7/Triple1.png";
                    auxPicture[1] = @"https://image.ibb.co/hTFbxn/Triple2.png";
                    auxPicture[2] = @"https://image.ibb.co/cVdwxn/Triple3.jpg";


                    auxPrice = 3;
                    break;
                case "Suite":
                    /*    auxPicture[0] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Suite1.png";
                         auxPicture[1] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Suite2.png";
                         auxPicture[2] = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Suite3.png";

         */
                    auxPicture[0] = @"https://image.ibb.co/dbVyA7/Suite1.png";
                    auxPicture[1] = @"https://image.ibb.co/e4Tkq7/Suite2.jpg";
                    auxPicture[2] = @"https://image.ibb.co/hJDkq7/Suite3.jpg";



                    auxPrice = 4;
                    break;
            }

            // Filling the rooms results manually just for demo purposes
            for (int i = 0; i <= 2; i++)
            {

                
                var random = new Random(i);
                Room room = new Room()
                {
                    Name = $"Habitación {searchQuery} {i + 1}",
                    Image = auxPicture[i],
                    Price = (int)(random.Next(50, 100) * auxPrice)

                };

                rooms.Add(room);
            }

            return rooms;
        }

        private IEnumerable<Service> GetServices()
        {
            var services = new List<Service>();

            for (int i = 0; i < 10; i++)
            {

                switch (i)
                {
                    case 0:
                        Service service0 = new Service()
                        {
                            Name = $"Wi-fi",
                          //  Image = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Wifi.jpg",
                            Image = @"https://image.ibb.co/jv1kOS/Wifi.jpg",
                            id = $"SuitechDemo_FreeWiFi",
                            password = $"pswIsNotThepsw"
                        };
                        services.Add(service0);
                        break;
                    case 1:
                        Service service1 = new Service()
                        {
                            Name = $"Limpieza",

                          //  Image = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Limpieza.jpg",
                            Image = @"https://image.ibb.co/j4Cgxn/Limpieza.jpg",
                            //    schedule = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Cleaning.jpg"
                            schedule = $"https://i.pinimg.com/originals/67/6b/28/676b281c1cf1d8e2dd7d1811ba9454f8.jpg"

                        };
                        services.Add(service1);
                        break;
                    case 2:
                        Service service2 = new Service()
                        {
                            Name = $"Minibar",
                            /*Image = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Minibar.jpg",
                            itemPrice = $"https://media-cdn.tripadvisor.com/media/photo-s/05/d5/0a/eb/ac-hotel-alicante-by.jpg"
                            */
                            Image = @"https://image.ibb.co/ce3Mxn/Minibar.jpg",
                            itemPrice = $"https://media-cdn.tripadvisor.com/media/photo-s/05/d5/0a/eb/ac-hotel-alicante-by.jpg"

                        };
                        services.Add(service2);
                        break;
                    case 3:
                        Service service3 = new Service()
                        {
                            Name = $"Servicio de habitaciones",
                            //    Image = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Serviciodehabitaciones.jpg",
                            Image = @"https://image.ibb.co/dyXs3S/Serviciodehabitaciones.jpg",
                            itemPrice = $"https://media-cdn.tripadvisor.com/media/photo-s/08/c7/f8/1b/eurohotel-diagonal-port.jpg"
                        };
                        services.Add(service3);
                        break;
                    case 4:
                        Service service4 = new Service()
                        {
                            Name = $"Consigna",
                           // Image = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Consigna.jpg",
                            Image = @"https://image.ibb.co/j3QOA7/Consigna.jpg",
                            schedule = $"https://parisbytrain.com/wp-content/uploads/2013/02/paris-train-station-storage-lockers-sign.jpg"
                        };
                        services.Add(service4);
                        break;
                    case 5:
                        Service service5 = new Service()
                        {
                            Name = $"Pedir un taxi",
                         //   Image = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\PedirunTaxi.jpg",
                            Image = @"https://image.ibb.co/k0qeiS/Pedirun_Taxi.jpg",
                            itemPrice = $"https://kingcountywatertaxi.files.wordpress.com/2017/06/revisedwsschedule.jpg?w=700 "
                        };
                        services.Add(service5);
                        break;
                    case 6:
                        Service service6 = new Service()
                        {
                            Name = $"Piscina",
                            //Image = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Piscina.jpg",
                            Image = @"https://image.ibb.co/bAj5OS/Piscina.jpg",
                            schedule = $"http://www.hamptonpool.co.uk/_img/timetables/timetable_36m-win.jpg "
                        };
                        services.Add(service6);
                        break;
                    case 7:
                        Service service7 = new Service()
                        {
                            Name = $"Gimnasio",
                           // Image = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Gimnasio.jpg",
                            Image = @"https://image.ibb.co/g0ns3S/Gimnasio.jpg",
                            schedule = $"https://pbs.twimg.com/media/BkXheoYCUAAxPmJ.jpg"
                        };
                        services.Add(service7);
                        break;
                    case 8:
                        Service service8 = new Service()
                        {
                            Name = $"Restaurante",
                           // Image = @"C:\Users\08024\Documents\Cortana Intelligence Suite\Suitech Bot\Images\Restaurante.jpg",
                            Image = @"https://image.ibb.co/cBgVq7/Restaurante.jpg",
                            schedule = $"http://www.woodcotegreen.com/images/image/QuickLinks/Opening%20Times/Waterfall-Cafe-Opening-Times.jpg",
                            itemPrice = $"https://media-cdn.tripadvisor.com/media/photo-s/08/c7/f8/1b/eurohotel-diagonal-port.jpg"
                        };
                        services.Add(service8);
                        break;
                    default:
                        break;
                }

            }

            return services;
        }


        //TODO
        private async Task ShowDetails(IDialogContext context, IAwaitable<object> result)
        {
            var room = await result;

            if (room.ToString().Equals("Más información de la Habitación Individual 1"))
            {
               await context.PostAsync($"http://www.hotelautrocadero.com/inc/uploads/sites/4/2015/10/BWTROCADERO27.jpg");
            }
            context.Done(new object());
        }

        private static Task<ValidateResult> ValidateEmail(EmailQuery state, object response)
        {
            var result = new ValidateResult();
            string email = (string) response;
            
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

        private static Task<ValidateResult> ValidateRestDay(RestaurantForm state, object response)
        {
            var result = new ValidateResult();
            var dt = (DateTime)response;
            DateTime hardcode = new DateTime(2018, 3, 4, 0, 0, 0);

            // Do the checks here whether the time is available. 
            if (dt.Date == hardcode.Date)
            {
                result.IsValid = true;
                result.Value = response;
            }
            else
            {
                result.IsValid = false;
                result.Feedback = "Fecha no válida. Introduce una fecha que coincida con los días de la reserva.";
            }
            return Task.FromResult(result);
        }
    }
}