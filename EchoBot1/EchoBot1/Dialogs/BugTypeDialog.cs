using EchoBot1.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Text.RegularExpressions;
using EchoBot1.Helpers;

namespace EchoBot1.Dialogs
{
    public class BugTypeDialog : ComponentDialog 
    {
        #region Variables
        private readonly StateService _stateService;
        private readonly BotServices _botServices;
        #endregion

        public BugTypeDialog(string dialogId, StateService botStateService, BotServices botServices): base(dialogId)
        {
            _stateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _botServices = botServices ?? throw new System.ArgumentNullException(nameof(botServices));

            InitiallizeWaterfallDialog();
        }

        private void InitiallizeWaterfallDialog()
        {
            var waterfallSteps = new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync
            };

            AddDialog(new WaterfallDialog($"{nameof(BugTypeDialog)}.mainFlow", waterfallSteps));

            InitialDialogId = $"{nameof(BugTypeDialog)}.mainFlow";

        }


        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = await _botServices.Dispatch.RecognizeAsync(stepContext.Context, cancellationToken);
            var token = result.Entities.FindTokens("TipoFalla").First();
            Regex rgx = new Regex("[^a-zA-Z0-9 -]");
            var value = rgx.Replace(token.ToString(), "").Trim();

            if(Common.BugTypes.Any(s => s.Equals(value, StringComparison.OrdinalIgnoreCase)))
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("Yes {0} a bug type", value)), cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(String.Format("No {0} is not a bug type", value)), cancellationToken);
            }

            // PASAR AL SIGUIENTE PASO SIN ESPERAR RESPUESTA
            return await stepContext.NextAsync(null, cancellationToken);
        }        
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.EndDialogAsync(null, cancellationToken);
        }
    }
}
