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
    public class TransporteDialog: ComponentDialog
    {
        #region Variables
        private readonly StateService _botStateService;
        private readonly BotServices _botService;

        #endregion
        public TransporteDialog(string dialogId, StateService stateService) : base(dialogId)
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
                GeneracionTicketStepAsync,
                ConfirmarRegresoMenuStepAsync,
                FinalStepAsync,
            };

            //Agregamos subdialogos
            AddDialog(new WaterfallDialog($"{nameof(TransporteDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(TransporteDialog)}.tipoPregunta"));
            AddDialog(new TextPrompt($"{nameof(TransporteDialog)}.confirmarReposicion"));
            AddDialog(new TextPrompt($"{nameof(UniformesDialog)}.confirmarRegreso"));

            //Indicamos con cual subdialogo comenzar
            InitialDialogId = $"{nameof(TransporteDialog)}.mainFlow";
        }


        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(TransporteDialog)}.tipoPregunta",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("¿Qué cambio necesitas?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "Modificar mi ruta", "Implementar una parada" }),
                    Style = ListStyle.HeroCard
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmarReposicionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            stepContext.Values["tipoPregunta"] = (string)stepContext.Context.Activity.Text.ToLower();

            var options = await stepContext.PromptAsync($"{nameof(TransporteDialog)}.confirmarReposicion",
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
                var detallesReposicion = "Transporte "+(string)stepContext.Values["tipoPregunta"];
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
                    await SendEmails.SendAsync(detallesReposicion, detallesReposicion + " Número de ticket: " + IDTicket);
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
            var texto = "";
            switch (stepContext.Values["tipoPregunta"])
            {
                case "modificar mi ruta":
                    texto = "modificar tu ruta";
                    break;
                case "implementar una parada":
                    texto = "implementar una parada";
                    break;
                default:
                    texto = "otro cambio";
                    break;
            }
            var reply = MessageFactory.Text($"¿Confirmas tu solicitud para {texto}?");

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

        private async Task<string> conexionBD(String idTicket, String tipo)
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = "107.180.47.15",
                Database = "interproteccion-test",
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
                        command.CommandText = @"INSERT INTO RegistroTickets (idTicket, tipo, estatus) VALUES (@idTicket, @tipo, @estatus);";
                        command.Parameters.AddWithValue("@idTicket", idTicket);
                        command.Parameters.AddWithValue("@tipo", tipo);
                        command.Parameters.AddWithValue("@estatus", "En revisión");

                        int rowCount = await command.ExecuteNonQueryAsync();
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
