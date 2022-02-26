using EchoBot1.Helpers;
using EchoBot1.Bots;
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
    public class CambioCorreoDialog: ComponentDialog
    {

        #region Variables
        private readonly StateService _botStateService;
        private readonly BotServices _botService;

        #endregion
        public CambioCorreoDialog(string dialogId, StateService stateService) : base(dialogId)
        {
            _botStateService = stateService ?? throw new System.ArgumentNullException(nameof(stateService));

            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            //Creamos pasos de la cascada
            var waterfallSteps = new WaterfallStep[]
            {
                ConfirmarReposicionStepAsync,
                GeneracionTicketStepAsync,
                ConfirmarRegresoMenuStepAsync,
                FinalStepAsync,
            };

            //Agregamos subdialogos
            AddDialog(new WaterfallDialog($"{nameof(CambioCorreoDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(CambioCorreoDialog)}.confirmarReposicion"));
            AddDialog(new TextPrompt($"{nameof(UniformesDialog)}.confirmarRegreso"));

            //Indicamos con cual subdialogo comenzar
            InitialDialogId = $"{nameof(CambioCorreoDialog)}.mainFlow";
        }


        private async Task<DialogTurnResult> ConfirmarReposicionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var options = await stepContext.PromptAsync($"{nameof(CambioCorreoDialog)}.confirmarReposicion",
                new PromptOptions
                {
                    Prompt = CreateSuggestedActions(stepContext)
                }, cancellationToken);
            return options;
        }
        private async Task<DialogTurnResult> GeneracionTicketStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccesor.GetAsync(stepContext.Context, () => new UserProfile());
            stepContext.Values["confirmarReposicion"] = (string)stepContext.Result;
            if ((string)stepContext.Values["confirmarReposicion"] == "SI")
            {
                var IDTicket = userProfile.IDEmpleado + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString();
                var detallesReposicion = "Cambio de correo";
                try
                {
                    var bd = await ConexionDB.RegistroTicketDB(IDTicket, detallesReposicion);
                    await stepContext.Context.SendActivityAsync($"¡Listo! Tu solicitud ha sido registrada", cancellationToken: cancellationToken);
                    await stepContext.Context.SendActivityAsync($"Tu número de ticket para el seguimiento es: {IDTicket}", cancellationToken: cancellationToken);
                }
                catch (Exception)
                {
                    await stepContext.Context.SendActivityAsync($"Hubo un error inesperado, por favor intentalo nuevamente", cancellationToken: cancellationToken);
                }
                try
                {
                    await SendEmails.SendAsync(detallesReposicion , detallesReposicion+ " Número de ticket: "+ IDTicket);
                }
                catch (Exception)
                {
                    await stepContext.Context.SendActivityAsync($"No se pudo enviar email", cancellationToken: cancellationToken);
                }
                return await stepContext.NextAsync(null, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync($"Tu solicitud no ha sido registrada", cancellationToken: cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }
        }

        private Activity CreateSuggestedActions(WaterfallStepContext stepContext)
        {
            var reply = MessageFactory.Text($"¿Confirmas tu solicitud para cambiar tu correo electónico?");

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
    }
}
