using EchoBot1.Models;
using EchoBot1.Bots;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using MySql.Data.MySqlClient;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EchoBot1.Dialogs
{
    public class CategoriaDialog : ComponentDialog
    {
        #region Variables
        private readonly StateService _botStateService;
        private readonly BotServices _botService;

        #endregion
        public CategoriaDialog(string dialogId, StateService stateService) : base(dialogId)
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
                ConfirmarReposicionStepAsync,
                ConfirmarRegresoMenuStepAsync,
                FinalStepAsync,
            };

            //Agregamos subdialogos
            AddDialog(new WaterfallDialog($"{nameof(CategoriaDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(CategoriaDialog)}.tipoPregunta"));
            AddDialog(new TextPrompt($"{nameof(UniformesDialog)}.confirmarRegreso"));

            //Indicamos con cual subdialogo comenzar
            InitialDialogId = $"{nameof(CategoriaDialog)}.mainFlow";
        }


        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(CategoriaDialog)}.tipoPregunta",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("¿Qué duda tienes?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "¿Cuándo me corresponde cambiar de categoria?","¿Qué porcentaje tengo en mi evaluación?"}),
                    Style = ListStyle.HeroCard
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmarReposicionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccesor.GetAsync(stepContext.Context, () => new UserProfile());
            stepContext.Values["tipoPregunta"] = (string)stepContext.Context.Activity.Text.ToLower();
            switch ((string)stepContext.Values["tipoPregunta"])
            {
                case "¿cuándo me corresponde cambiar de categoria?":
                    await stepContext.Context.SendActivityAsync($"Tu cambio de categoría te corresponde el día: ", cancellationToken: cancellationToken);
                    break;
                case "¿qué porcentaje tengo en mi evaluación?":
                    await stepContext.Context.SendActivityAsync($"Tu evaluación tiene un resultado de: ", cancellationToken: cancellationToken);
                    break;
            }
            //var bd= await conexionBD();
            //await stepContext.Context.SendActivityAsync($"{userProfile.IDEmpleado} La respuesta a tu pregunta {stepContext.Values["tipoPregunta"]} es: {bd}", cancellationToken: cancellationToken);
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
        private async Task<string> conexionBD()
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = "107.180.47.15",
                Database = "InterproteccionTest3",
                UserID = "koj8s2gxf97o",
                Password = "X@data1234",
            };
            try
            {
                using (var conn = new MySqlConnection(builder.ConnectionString))
                {
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand())
                    {
                        var campo = "fechaDeNacimiento";
                        command.CommandText = $"SELECT {campo} FROM consentimientos_vida_datos WHERE noPolizaCertificado=@id;";
                        //command.Parameters.AddWithValue("@campo", "fechaDeNacimiento");
                        command.Parameters.AddWithValue("@id", "12000-4253-2-1");
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var valor = reader.GetInt32(0);
                            }
                        }
                    }
                    return "CORRECTO";
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "FALLIDA";
            }
        }
    }
}
