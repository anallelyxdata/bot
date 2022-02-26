using EchoBot1.Bots;
using EchoBot1.Helpers;
using EchoBot1.Models;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EchoBot1.Dialogs
{
    public class SolicitarFormatoDialog: ComponentDialog
    {
        #region Variables
        private readonly StateService _botStateService;
        private readonly BotServices _botService;

        #endregion
        public SolicitarFormatoDialog(string dialogId, StateService stateService) : base(dialogId)
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
                DescargarFormatoStepAsync,
                ConfirmarRegresoMenuStepAsync,
                FinalStepAsync,
            };

            //Agregamos subdialogos
            AddDialog(new WaterfallDialog($"{nameof(SolicitarFormatoDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(SolicitarFormatoDialog)}.tipoFormato"));
            AddDialog(new TextPrompt($"{nameof(UniformesDialog)}.confirmarRegreso"));
            //Indicamos con cual subdialogo comenzar
            InitialDialogId = $"{nameof(SolicitarFormatoDialog)}.mainFlow";
        }



        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {        
            return await stepContext.PromptAsync($"{nameof(SolicitarFormatoDialog)}.tipoFormato",
                new PromptOptions 
                {
                    Prompt = MessageFactory.Text("¿Qué formato necesitas?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> {"Vacaciones","Permiso","Acceso","Constancia laboral"}),
                    Style = ListStyle.HeroCard
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> DescargarFormatoStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccesor.GetAsync(stepContext.Context, () => new UserProfile());

            stepContext.Values["tipoFormato"] = (string)stepContext.Context.Activity.Text.ToLower();
            if ((string)stepContext.Values["tipoFormato"]=="constancia laboral")
            {
                var IDTicket = userProfile.IDEmpleado + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString();
                var detallesTicket = "Solicitud de constancia laboral";
                try
                {
                    var bd = await ConexionDB.RegistroTicketDB(IDTicket, detallesTicket);
                    await stepContext.Context.SendActivityAsync($"Tu solicitud de constancia laboral ha sido registrada", cancellationToken: cancellationToken);
                    await stepContext.Context.SendActivityAsync($"El número de tu ticket de seguimiento es: {IDTicket}", cancellationToken: cancellationToken);
                }
                catch (Exception)
                {
                    await stepContext.Context.SendActivityAsync($"Hubo un error inesperado, por favor intentalo nuevamente", cancellationToken: cancellationToken);      
                }
                try
                {
                    await SendEmails.SendAsync(detallesTicket, detallesTicket + " Número de ticket: " + IDTicket);
                }
                catch (Exception)
                {
                    await stepContext.Context.SendActivityAsync($"No se pudo enviar email", cancellationToken: cancellationToken);
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync($"Aquí tienes, descarga tu formato de {stepContext.Values["tipoFormato"]}", cancellationToken: cancellationToken);
                await stepContext.Context.SendActivityAsync(AttatchmentCard.GetDocumento((string)stepContext.Values["tipoFormato"]), cancellationToken);
            }
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmarRegresoMenuStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var options = await stepContext.PromptAsync($"{nameof(UniformesDialog)}.confirmarRegreso",
                new PromptOptions
                {
                    Prompt = CreateSuggestedActions(stepContext)
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

        private Activity CreateSuggestedActions(WaterfallStepContext stepContext)
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
