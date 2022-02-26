using EchoBot1.Bots;
using EchoBot1.DataBase;
using EchoBot1.Helpers;
using EchoBot1.Models;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EchoBot1.Dialogs
{
    public class ReposicionDialog : ComponentDialog 
    {
        #region Variables
        private readonly StateService _botStateService;
        private readonly BotServices _botService;

        #endregion
        public ReposicionDialog(string dialogId, StateService stateService) : base(dialogId)
        {
            _botStateService = stateService ?? throw new System.ArgumentNullException(nameof(stateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            //Creamos pasos de la cascada
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                MotivoReposicionStepAsync,
                ConfirmarReposicionStepAsync,
                RegistroReposcionStepAsync,
                ConfirmarFormato,
                ConfirmarRegresoMenuStepAsync,
                FinalStepAsync
            };

            //Agregamos subdialogos
            AddDialog(new WaterfallDialog($"{nameof(ReposicionDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(ReposicionDialog)}.tipoReposicion"));
            AddDialog(new ChoicePrompt($"{nameof(ReposicionDialog)}.motivoReposicion"));
            AddDialog(new TextPrompt($"{nameof(ReposicionDialog)}.confirmarReposicion"));
            AddDialog(new TextPrompt($"{nameof(ReposicionDialog)}.confirmarFormato"));

            AddDialog(new TextPrompt($"{nameof(UniformesDialog)}.confirmarRegreso"));

            //Indicamos con cual subdialogo comenzar
            InitialDialogId = $"{nameof(ReposicionDialog)}.mainFlow";
        }

       

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync("¿De qué tarjeta necesitas reposición?", cancellationToken: cancellationToken);
            return await stepContext.PromptAsync($"{nameof(ReposicionDialog)}.tipoReposicion",
                new PromptOptions { Prompt = Opciones()});
        }

        private async Task<DialogTurnResult> MotivoReposicionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["tipoReposicion"] = (string)stepContext.Result;
            return await stepContext.PromptAsync($"{nameof(ReposicionDialog)}.motivoReposicion",
               new PromptOptions
               {
                   Prompt = MessageFactory.Text("¿Cuál es el motivo por el que requieres la reposición?"),
                   Choices = ChoiceFactory.ToChoices(new List<string>
                   { 
                    "Extravié mi tarjeta",
                    "Mi tarjeta está desgastada",
                    "Mi tarjeta ya no funciona"
                   }),
                   Style = ListStyle.HeroCard
               }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmarReposicionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            stepContext.Values["motivoReposicion"] = (string)stepContext.Context.Activity.Text.ToLower();

            var options = await stepContext.PromptAsync($"{nameof(ReposicionDialog)}.confirmarReposicion",
                new PromptOptions
                {
                    Prompt = CreateSuggestedActions(stepContext)
                },cancellationToken);
            return options;
        }


        private async Task<DialogTurnResult> RegistroReposcionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccesor.GetAsync(stepContext.Context, () => new UserProfile());

            stepContext.Values["confirmarReposicion"] = (string)stepContext.Result;
            if ((string)stepContext.Values["confirmarReposicion"] == "SI")
            {
                var IDTicket = userProfile.IDEmpleado + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString();
                var detallesReposicion = (string)stepContext.Values["tipoReposicion"] + " " + (string)stepContext.Values["motivoReposicion"];
                //await stepContext.Context.SendActivityAsync($"¡Listo!, La reposición ha sido registrada", cancellationToken: cancellationToken);
               
                try
                {
                    var bd = await ConexionDB.RegistroTicketDB(IDTicket,detallesReposicion);
                    await stepContext.Context.SendActivityAsync($"¡Listo!, La reposición ha sido registrada", cancellationToken: cancellationToken);
                    await stepContext.Context.SendActivityAsync($"Tu número de ticket para el seguimiento es: {IDTicket}", cancellationToken: cancellationToken);
                    try
                    {
                        await SendEmails.SendAsync(detallesReposicion, detallesReposicion + " Número de ticket: " + IDTicket);
                    }
                    catch (Exception)
                    {
                        await stepContext.Context.SendActivityAsync($"No se pudo enviar email", cancellationToken: cancellationToken);
                    }
                    if ((string)stepContext.Values["tipoReposicion"]== "Credencial de ingreso") 
                    {
                        await stepContext.Context.SendActivityAsync($"El tiempo aproximado de reposición es de n días", cancellationToken: cancellationToken);
                        var options = await stepContext.PromptAsync($"{nameof(ReposicionDialog)}.confirmarFormato",
                        new PromptOptions
                        {
                            Prompt = ConfirmarFormato(stepContext)
                        }, cancellationToken);
                        return options;
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync($"El tiempo aproximado de reposición es de n días", cancellationToken: cancellationToken);
                        return await stepContext.NextAsync(null, cancellationToken);
                    }
                }
                catch (Exception)
                {
                    await stepContext.Context.SendActivityAsync($"Hubo un error inesperadooooo, por favor intentalo nuevamente", cancellationToken: cancellationToken);
                }
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync($"Tu solicitud no ha sido registrada", cancellationToken: cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }

            
        }

        private async Task<DialogTurnResult> ConfirmarFormato(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["confirmarFormato"] = (string)stepContext.Result;
            if ((string)stepContext.Values["confirmarFormato"] == "SI")
            {
                //await stepContext.Context.SendActivityAsync($"Aquí tienes, descarga tu formato", cancellationToken: cancellationToken);
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Aquí tienes, descarga tu formato"), cancellationToken);
                await stepContext.Context.SendActivityAsync(AttatchmentCard.GetDocumento("permiso"), cancellationToken);
            }
            //return await stepContext.NextAsync(null, cancellationToken);
            //return await stepContext.PromptAsync($"{nameof(ReposicionDialog)}.confirmarRegreso";
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmarRegresoMenuStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var options = await stepContext.PromptAsync($"{nameof(UniformesDialog)}.confirmarRegreso",
                new PromptOptions
                {
                    Prompt = CreateSuggestedActions2(stepContext)
                }, cancellationToken);
            return options;
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //stepContext.Values["confirmarFormato"] = (string)stepContext.Result;
            //if ((string)stepContext.Values["confirmarFormato"] == "SI")
            //{
            //    await stepContext.Context.SendActivityAsync($"Aquí tienes, descarga tu formato", cancellationToken: cancellationToken);
            //    await stepContext.Context.SendActivityAsync(AttatchmentCard.GetDocumento("permiso"), cancellationToken);
            //}

            stepContext.Values["confirmarRegreso"] = (string)stepContext.Result;
            if ((string)stepContext.Values["confirmarRegreso"] == "SI")
            {
                return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.mainFlow", null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync($"¡Hasta pronto!", cancellationToken: cancellationToken);
                // Clear state.
                await _botStateService.ConversationState.ClearStateAsync(null, cancellationToken);
                await _botStateService.UserState.ClearStateAsync(null, cancellationToken);
                return await stepContext.CancelAllDialogsAsync();
            }

            //return await stepContext.EndDialogAsync(null, cancellationToken);
          
        }

        private Activity CreateSuggestedActions2(WaterfallStepContext stepContext)
        {
            var reply = MessageFactory.Text($"¿Hay algo más en lo que pueda ayudarte?");

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(){Title="Sí", Value="SI", Type= ActionTypes.ImBack},
                    new CardAction(){Title="No", Value="NO", Type= ActionTypes.ImBack},
                }
            };
            return reply;
        }

        private Activity Opciones()
        {
            var listaReposiciones = ReposicionesDB.GetReposicions();

            var listaAttachments = new List<Attachment>();
            foreach (var item in listaReposiciones)
            {
                var card = new HeroCard()
                {
                    Title = item.titulo,
                    Subtitle = "",
                    Images = new List<CardImage>() { new CardImage(item.imagen) },
                    Buttons = new List<CardAction>()
                    {
                        new CardAction(){Title="Solicitar", Value=item.titulo, Type= ActionTypes.ImBack}
                    }
                };
                listaAttachments.Add(card.ToAttachment());
            }
            var reply = MessageFactory.Attachment(listaAttachments);
            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            return reply as Activity;
        }


        private Activity CreateSuggestedActions(WaterfallStepContext stepContext)
        {
            var motivo="";
            switch ((string)stepContext.Values["motivoReposicion"])
            {

                case "extravié mi tarjeta":
                    motivo = "la has extraviado";
                    break;
                case "mi tarjeta está desgastada":
                    motivo = "está desgastada";
                    break;
                case "mi tarjeta ya no funciona":
                    motivo = "ya no funciona";
                    break;
                default:
                    motivo = "otro motivo";
                    break;
            }

            var reply = MessageFactory.Text($"¿Confirmas tu solicitud de reposición de {stepContext.Values["tipoReposicion"]} porque {motivo}?");

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(){Title="Sí", Value="SI", Type= ActionTypes.ImBack},
                    new CardAction(){Title="No", Value="NO", Type= ActionTypes.ImBack},
                }
            };
            return reply;
        }
        private Activity ConfirmarFormato(WaterfallStepContext stepContext)
        {
            var reply = MessageFactory.Text($"¿Requieres un formato de acceso?");

            reply.SuggestedActions = new SuggestedActions()
            {
                Actions = new List<CardAction>()
                {
                    new CardAction(){Title="Sí", Value="SI", Type= ActionTypes.ImBack},
                    new CardAction(){Title="No", Value="NO", Type= ActionTypes.ImBack},
                }
            };
            return reply;
        }

    }
}
