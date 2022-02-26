using EchoBot1.Dialogs;
using EchoBot1.Models;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace EchoBot1.Bots
{
    public class MainDialog : ComponentDialog 
    {
        #region Variables
        private readonly StateService _stateService;
        private readonly BotServices _botService;
        #endregion



        public MainDialog(StateService stateService, BotServices botServices): base(nameof(MainDialog))
        {
            _stateService = stateService ?? throw new System.ArgumentNullException(nameof(stateService));
            _botService = botServices ?? throw new ArgumentNullException(nameof(botServices));
            
            InitializeWaterfallDialog();
        }

        private void InitializeWaterfallDialog()
        {
            //Creamos pasos del waterfal
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                PreguntaStepAsync,
                FinalStepAsync
            };

            //Agregamos subdiálogos
            AddDialog(new GreetingDialog($"{nameof(MainDialog)}.greeting", _stateService));
            AddDialog(new ChoicePrompt($"{nameof(MainDialog)}.tipoSolicitud"));
            AddDialog(new ReposicionDialog($"{nameof(MainDialog)}.reposicionDialog", _stateService));
            AddDialog(new SolicitarFormatoDialog($"{nameof(MainDialog)}.solicitarFormatoDialog", _stateService));
            AddDialog(new BugTypeDialog($"{nameof(MainDialog)}.bugType", _stateService,_botService));
            AddDialog(new VacacionesDialog($"{nameof(MainDialog)}.preguntaVacaciones", _stateService));
            AddDialog(new UniformesDialog($"{nameof(MainDialog)}.preguntaUniformes", _stateService));
            AddDialog(new ConsultarTicketDialog($"{nameof(MainDialog)}.estadoTicket", _stateService));
            AddDialog(new CategoriaDialog($"{nameof(MainDialog)}.preguntaCategoria", _stateService));
            AddDialog(new TransporteDialog($"{nameof(MainDialog)}.preguntaTransporte", _stateService));
            AddDialog(new CambioTurnoDialog($"{nameof(MainDialog)}.cambioTurno", _stateService));
            AddDialog(new CambioCorreoDialog($"{nameof(MainDialog)}.cambioCorreo", _stateService));
            AddDialog(new OtraPreguntaDialog($"{nameof(MainDialog)}.otrasPreguntas", _stateService));

            AddDialog(new WaterfallDialog($"{nameof(MainDialog)}.mainFlow", waterfallSteps));

            //Indicamos subdialogo de comienzo
            InitialDialogId = $"{nameof(MainDialog)}.mainFlow";
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _stateService.UserProfileAccesor.GetAsync(stepContext.Context, () => new UserProfile());

            XmlDocument doc = new XmlDocument();
            string ruta = "http://187.174.187.177//WSPersonal/WsPersonal.asmx/TraePersonal?NumeroEmpleado=" + stepContext.Context.Activity.Text;
            ////string ruta = "http://187.174.187.177//WSPersonal/WsPersonal.asmx/TraePersonal?NumeroEmpleado=9177";
            //string ruta = "https://x-data.mx/prueba.xml";


            doc.Load(ruta);

            //Prueba de if
            //Si ese node no existe entonces hay error
            //if (doc.SelectSingleNode("ObjetoMF/NumeroEmpleado") == null)
            //{

            //    userProfile.IDEmpleado = stepContext.Context.Activity.Text;
            //    await _stateService.UserProfileAccesor.SetAsync(stepContext.Context, userProfile);
            //    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);
            //}
            //else
            //{
            //    XmlNode node = doc.SelectSingleNode("ObjetoMF/NumeroEmpleado");
            //    string num = node.InnerText;
            //    return await stepContext.NextAsync(stepContext, cancellationToken);

            //}


            //await stepContext.Context.SendActivityAsync($"{stepContext.Context.Activity.Text}", cancellationToken: cancellationToken);
            //if (num.Equals(stepContext.Context.Activity.Text))
            //{
            //    userProfile.IDEmpleado = stepContext.Context.Activity.Text;
            //    await _stateService.UserProfileAccesor.SetAsync(stepContext.Context, userProfile);
            //    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);
            //}
            //else
            //{
            //    return await stepContext.NextAsync(stepContext, cancellationToken);
            //}

            if (string.IsNullOrEmpty(userProfile.IDEmpleado))
            {
                userProfile.IDEmpleado = stepContext.Context.Activity.Text;
                await _stateService.UserProfileAccesor.SetAsync(stepContext.Context, userProfile);
                return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(stepContext, cancellationToken);
            }

        }

        private async Task<DialogTurnResult> PreguntaStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.PromptAsync($"{nameof(MainDialog)}.tipoSolicitud",
               new PromptOptions
               {
                   Prompt = MessageFactory.Text("¿Cómo puedo apoyarte?"),
                   Choices = ChoiceFactory.ToChoices(new List<string>
                   { 
                       "Necesito una reposición de tarjeta",
                       "Requiero un formato",
                       "Tengo dudas sobre mis vacaciones",
                        "¿Cuándo nos entregarán nuevos uniformes?",
                        "Tengo una consulta sobre mi categoría",
                        "Necesito un cambio en mi transporte",
                        "Requiero un cambio de turno",
                        "Necesito un cambio de correo electrónico",
                        //"Cual es el estado de mi ticket",
                        "Mi pregunta no aparece en el menú"
                   }),
                   Style = ListStyle.HeroCard
               }, cancellationToken);
        }
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _stateService.UserProfileAccesor.GetAsync(stepContext.Context, () => new UserProfile());

            stepContext.Values["tipoSolicitud"] = (string)stepContext.Context.Activity.Text;
            switch ((string)stepContext.Values["tipoSolicitud"])
            {
                
                case "Necesito una reposición de tarjeta":
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.reposicionDialog", null, cancellationToken);
                case "Requiero un formato":
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.solicitarFormatoDialog", null, cancellationToken);
                case "Tengo dudas sobre mis vacaciones":
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.preguntaVacaciones", null, cancellationToken);
                case "¿Cuándo nos entregarán nuevos uniformes?":
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.preguntaUniformes", null, cancellationToken);
                case "Cual es el estado de mi ticket":
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.estadoTicket", null, cancellationToken);
                case "Tengo una consulta sobre mi categoría":
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.preguntaCategoria", null, cancellationToken);
                case "Necesito un cambio en mi transporte":
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.preguntaTransporte", null, cancellationToken);
                case "Requiero un cambio de turno":
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.cambioTurno", null, cancellationToken);
                case "Necesito un cambio de correo electrónico":
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.cambioCorreo", null, cancellationToken);
                case "Mi pregunta no aparece en el menú":
                    return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.otrasPreguntas", null, cancellationToken);
                default:
                    await stepContext.Context.SendActivityAsync(MessageFactory.Text("Opción no válida"), cancellationToken);
                    break;
            }

            // UTILIZANDO PROCESAMIENTO DE LENGUAJE NATURAL
            //var recognizerResult = await _botService.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
            //var topIntent = recognizerResult.GetTopScoringIntent();
            //switch (topIntent.intent)
            //{
            //    case "PreguntarTipoFallaIntent":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.bugType", null, cancellationToken);
            //    case "ReposicionTarjetaIntent":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.reposicionDialog", null, cancellationToken);
            //    case "Saludo":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.greeting", null, cancellationToken);
            //    case "SolicitarFormatoIntent":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.solicitarFormatoDialog", null, cancellationToken);
            //    case "PreguntaVacacionesIntent":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.preguntaVacaciones", null, cancellationToken);
            //    case "UniformesIntent":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.preguntaUniformes", null, cancellationToken);
            //    case "EstadoTicketIntent":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.estadoTicket", null, cancellationToken);
            //    case "PreguntaCategoriaIntent":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.preguntaCategoria", null, cancellationToken);
            //    case "CambioTransporteIntent":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.preguntaTransporte", null, cancellationToken);
            //    case "CambioTurnoIntent":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.cambioTurno", null, cancellationToken);
            //    case "CambioCorreoIntent":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.cambioCorreo", null, cancellationToken);
            //    case "OtraPreguntaIntent":
            //        return await stepContext.BeginDialogAsync($"{nameof(MainDialog)}.otrasPreguntas", null, cancellationToken);
            //    default:
            //        await stepContext.Context.SendActivityAsync(MessageFactory.Text("Opción no válida"), cancellationToken);
            //        break;
            //}

            //return await stepContext.EndDialogAsync(null,cancellationToken);
            return await stepContext.ReplaceDialogAsync(nameof(MainDialog), cancellationToken);
        }

    }
}
