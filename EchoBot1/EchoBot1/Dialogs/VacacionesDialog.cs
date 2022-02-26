using EchoBot1.Bots;
using EchoBot1.Models;
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
using System.Xml;


namespace EchoBot1.Dialogs
{
    public class VacacionesDialog : ComponentDialog
    {
        #region Variables
        private readonly StateService _botStateService;
        private readonly BotServices _botService;
        #endregion
        public VacacionesDialog(string dialogId, StateService stateService) : base(dialogId)
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
            AddDialog(new WaterfallDialog($"{nameof(VacacionesDialog)}.mainFlow", waterfallSteps));
            AddDialog(new ChoicePrompt($"{nameof(VacacionesDialog)}.tipoPregunta"));
            AddDialog(new TextPrompt($"{nameof(UniformesDialog)}.confirmarRegreso"));

            //Indicamos con cual subdialogo comenzar
            InitialDialogId = $"{nameof(VacacionesDialog)}.mainFlow";
        }


        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(VacacionesDialog)}.tipoPregunta",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("¿Qué duda tienes?"),
                    Choices = ChoiceFactory.ToChoices(new List<string> { "¿Cuántos días de vacaciones tengo actualmente?", "¿Cuántos días de vacaciones me corresponden el próximo año?" }),
                    Style = ListStyle.HeroCard
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> ConfirmarReposicionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccesor.GetAsync(stepContext.Context, () => new UserProfile());
            stepContext.Values["tipoPregunta"] = (string)stepContext.Context.Activity.Text.ToLower();

            //Leer xml
            XmlDocument doc = new XmlDocument();
            string ruta = "http://187.174.187.177//WSPersonal/WsPersonal.asmx/TraePersonal?NumeroEmpleado=" + userProfile.IDEmpleado;
            doc.Load(ruta);
            //Obtener fecha y hora
            XmlNode fechayhora = doc.SelectSingleNode("ObjetoMF/FechaIngreso");
            string fechayhoratxt = fechayhora.InnerText;
            //Extraer fecha
            string fecha = fechayhoratxt.Split(' ').FirstOrDefault();
            //Extraer año

            DateTime dt = Convert.ToDateTime(fecha);
            int year = dt.Year;
            int yearActual = DateTime.Now.Year;
            int yearProximo = yearActual + 1;

            //Dias de vacaciones actual
            int yearsLaborados = (yearActual - year);
            int diasVacacionesActual = 6 + (2 * (yearsLaborados - 1));
            int difCinco = yearsLaborados / 5;
            if (yearsLaborados > 4)
            {
                diasVacacionesActual = 12 + (difCinco * 2);
            }

            //Dias de vacaciones próximo año
            int yearsLaboradosProximo = (yearProximo - year);
            int diasVacacionesProximo = 6 + (2 * (yearsLaboradosProximo - 1));
            int difCincoProximo = yearsLaboradosProximo / 5;
            if (yearsLaboradosProximo > 4)
            {
                diasVacacionesProximo = 12 + (difCinco * 2);
            }


            switch ((string)stepContext.Values["tipoPregunta"])
            {
                case "¿cuántos días de vacaciones tengo actualmente?":
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Tienes {0} días de vacaciones sin utilizar", diasVacacionesActual)), cancellationToken);
                    //await stepContext.Context.SendActivityAsync($"Tienes n días de vacaciones sin utilizar", cancellationToken: cancellationToken);
                    break;
                case "¿cuántos días de vacaciones me corresponden el próximo año?":
                    //await stepContext.Context.SendActivityAsync($"El próximo año te corresponden n días de vacaciones", cancellationToken: cancellationToken);
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Tendrás {0} días de vacaciones sin utilizar", diasVacacionesProximo)), cancellationToken);
                    break;
            }
            //var bd= await conexionBD();
            //await stepContext.Context.SendActivityAsync($"{userProfile.IDEmpleado} La respuesta a tu pregunta {stepContext.Values["tipoPregunta"]} es: {bd}", cancellationToken: cancellationToken);
            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<string> conexionBD()
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = "107.180.47.15",
                Database = "InterproteccionTest3",
                UserID = "koj8s2gxf97o",
                Password = "Xd@ta1234",
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
