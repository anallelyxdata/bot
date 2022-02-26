using EchoBot1.Helpers;
using EchoBot1.Bots;
using Microsoft.Bot.Schema;

using EchoBot1.Models;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EchoBot1.Bots
{
    public class OtraPreguntaDialog : ComponentDialog
    {
        #region Variables
        private readonly StateService _botStateService;
        private readonly BotServices _botService;

        #endregion
        public OtraPreguntaDialog(string dialogId, StateService stateService) : base(dialogId)
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
                GuardarStepAsync,
                ConfirmarRegresoMenuStepAsync,
                FinalStepAsync
            };

            //Agregamos subdialogos
            AddDialog(new WaterfallDialog($"{nameof(OtraPreguntaDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(OtraPreguntaDialog)}.otraPregunta"));
            AddDialog(new TextPrompt($"{nameof(OtraPreguntaDialog)}.confirmarRegreso"));

            //Indicamos con cual subdialogo comenzar
            InitialDialogId = $"{nameof(OtraPreguntaDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(OtraPreguntaDialog)}.otraPregunta",
              new PromptOptions
              {
                  Prompt = MessageFactory.Text("Cuéntanos, ¿Cuál es tu pregunta? La consideraremos en nuestra próxima actualización")
              }, cancellationToken);
        }

        private async Task<DialogTurnResult> GuardarStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            stepContext.Values["otraPregunta"] = (string)stepContext.Context.Activity.Text.ToLower();
            UserProfile userProfile = await _botStateService.UserProfileAccesor.GetAsync(stepContext.Context, () => new UserProfile());
            var IDPregunta = userProfile.IDEmpleado + "-" + DateTime.Now.Day.ToString() + "-" + DateTime.Now.Month.ToString() + "-" + DateTime.Now.Year.ToString() + "-" + DateTime.Now.Hour.ToString() + "-" + DateTime.Now.Minute.ToString();
            try
            {
                var bd = await ConexionDB.RegistrarPreguntaDB(IDPregunta, stepContext.Values["otraPregunta"].ToString());
                await stepContext.Context.SendActivityAsync("¡Gracias por tu aportación! Puedes recibir atención personalizada en el área de RH", cancellationToken: cancellationToken);
            }
            catch
            {

            }
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmarRegresoMenuStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            var options = await stepContext.PromptAsync($"{nameof(OtraPreguntaDialog)}.confirmarRegreso",
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
