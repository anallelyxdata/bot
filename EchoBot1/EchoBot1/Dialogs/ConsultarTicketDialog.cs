using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EchoBot1.Dialogs
{
    public class ConsultarTicketDialog : ComponentDialog
    {
        #region Variables
        private readonly StateService _botStateService;
        #endregion
        public ConsultarTicketDialog(string dialogId, StateService stateService) : base(dialogId)
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
                FinalStepAsync
            };

            //Agregamos subdialogos
            AddDialog(new WaterfallDialog($"{nameof(ConsultarTicketDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(ConsultarTicketDialog)}.IDTicket", IDValidatorAsync));

            //Indicamos con cual subdialogo comenzar
            InitialDialogId = $"{nameof(ConsultarTicketDialog)}.mainFlow";
        }


        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
                return await stepContext.PromptAsync($"{nameof(ConsultarTicketDialog)}.IDTicket",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Por favor proporciona el número de tu ticket de seguimiento"),
                    RetryPrompt = MessageFactory.Text("Por favor proporciona un número de ticket válido"),
                }, cancellationToken);
        
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            //Indicamos que se ha finalizado el diálogo

            try
            {
                stepContext.Values["IDTicket"]= (string)stepContext.Result;
                var valid = Regex.Match((string)stepContext.Values["IDTicket"], @"\d+-\d+-\d+-\d+-\d+-\d+").Success;
                if (valid == true)
                {
                    var bd = await conexionBD((string)stepContext.Values["IDTicket"]);
                    await stepContext.Context.SendActivityAsync($"El estado de tu solicitud es: {bd.ToLower()}", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync("Demasiados intentos, ¿En qué te puedo ayudar?", cancellationToken: cancellationToken);
                    return await stepContext.EndDialogAsync(null, cancellationToken);
                }
            }
            catch (Exception)
            {
                await stepContext.Context.SendActivityAsync("Hubo un error por favor intentalo nuevamente", cancellationToken: cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private Task<bool> IDValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;
            if (promptContext.Recognized.Succeeded )
            {
                valid = Regex.Match(promptContext.Recognized.Value, @"\d+-\d+-\d+-\d+-\d+-\d+").Success;
                if (promptContext.AttemptCount >= 3)
                    valid = true;
            }
            return Task.FromResult(valid);
        }

        private async Task<string> conexionBD(string id)
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
                    var valor = "";
                    await conn.OpenAsync();
                    using (var command = conn.CreateCommand())
                    {
                        var campo = "estatus";
                        command.CommandText = $"SELECT {campo} FROM RegistroTickets WHERE idTicket=@id;";
                        command.Parameters.AddWithValue("@id", id);
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                valor = reader.GetString(0);
                            }
                        }
                    }
                    return valor;
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