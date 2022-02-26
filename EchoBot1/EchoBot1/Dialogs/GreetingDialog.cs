using EchoBot1.Models;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Globalization;

namespace EchoBot1.Dialogs
{
    public class GreetingDialog: ComponentDialog 
    {
        #region Variables
        private readonly StateService _botStateService;
        #endregion
        public GreetingDialog(string dialogId, StateService stateService): base(dialogId)
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
            AddDialog(new WaterfallDialog($"{nameof(GreetingDialog)}.mainFlow", waterfallSteps));
            AddDialog(new TextPrompt($"{nameof(GreetingDialog)}.IDEmpleado",IDValidatorAsync));

            //Indicamos con cual subdialogo comenzar
            InitialDialogId = $"{nameof(GreetingDialog)}.mainFlow";
        }


        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccesor.GetAsync(stepContext.Context, () => new UserProfile());
            //bool valid = Regex.Match(userProfile.IDEmpleado, @"^\d+$").Success;
            XmlDocument doc = new XmlDocument();
            string ruta = "http://187.174.187.177//WSPersonal/WsPersonal.asmx/TraePersonal?NumeroEmpleado=" + userProfile.IDEmpleado;
            //string ruta = "https://x-data.mx/prueba.xml";
            doc.Load(ruta);
            ////Obtener nombre del empleado
            //XmlNode nombre = doc.SelectSingleNode("ObjetoMF/Nombrecompleto");
            //string nombreempleado = nombre.InnerText;
            //string[] words = nombreempleado.Split(' ');
            ////Obtener la ultima palabra del nombre completo
            //string myLastWord = words[words.Length - 1];

          

            //Checar si existe node
            if (doc.SelectSingleNode("ObjetoMF/NumeroEmpleado") == null)
            {
                return await stepContext.PromptAsync($"{nameof(GreetingDialog)}.IDEmpleado",
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Por favor proporciona un número de empleado válido"),
                    RetryPrompt = MessageFactory.Text("Por favor proporciona un número de empleado válido"),
                }, cancellationToken);
            }
            else
            {
                //Obtener nombre del empleado
                XmlNode nombre = doc.SelectSingleNode("ObjetoMF/Nombrecompleto");
                string nombreempleado = nombre.InnerText;

                //Pasar a Title Case
                System.Globalization.TextInfo textInfo = new System.Globalization.CultureInfo("en-US", false).TextInfo;
                nombreempleado = textInfo.ToTitleCase(nombreempleado.ToLower());

                //string[] words = nombreempleado.Split(' ');
                //Obtener la ultima palabra del nombre completo
                //string myLastWord = words[words.Length - 1];

                await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Hola {0}", nombreempleado)), cancellationToken);
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
            //termina prueba

            //if (valid==false)
            //{

            //    return await stepContext.PromptAsync($"{nameof(GreetingDialog)}.IDEmpleado",
            //    new PromptOptions
            //    {
            //        Prompt = MessageFactory.Text("Por favor proporciona un número de empleado válido"),
            //        RetryPrompt = MessageFactory.Text("Por favor proporciona un número de empleado válido"),
            //    }, cancellationToken);

            //}
            //else
            //{
            //    await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Hola empleado {0}", userProfile.IDEmpleado)), cancellationToken);
            //    return await stepContext.EndDialogAsync(null, cancellationToken);
            //}
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _botStateService.UserProfileAccesor.GetAsync(stepContext.Context, () => new UserProfile());
            userProfile.IDEmpleado = (string)stepContext.Result;
            await _botStateService.UserProfileAccesor.SetAsync(stepContext.Context, userProfile);

            //Obtener nombre del empleado
            XmlDocument doc = new XmlDocument();
            string ruta = "http://187.174.187.177//WSPersonal/WsPersonal.asmx/TraePersonal?NumeroEmpleado=" + userProfile.IDEmpleado;
            //string ruta = "https://x-data.mx/prueba.xml";
            doc.Load(ruta);
            XmlNode nombre = doc.SelectSingleNode("ObjetoMF/Nombrecompleto");
            string nombreempleado = nombre.InnerText;
            
            //Pasar a Title Case
            System.Globalization.TextInfo textInfo = new System.Globalization.CultureInfo("en-US", false).TextInfo;
            nombreempleado = textInfo.ToTitleCase(nombreempleado.ToLower());
            string[] words = nombreempleado.Split(' ');
            //Obtener la ultima palabra del nombre completo
            string myLastWord = words[words.Length - 1];

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Hola {0}", myLastWord)), cancellationToken);
            //Indicamos que se ha finalizado el diálogo
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }

        private Task<bool> IDValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var valid = false;
            if (promptContext.Recognized.Succeeded)
            {
                valid = Regex.Match(promptContext.Recognized.Value, @"^\d+$").Success;
            }
            return Task.FromResult(valid);
        }
    }
}
