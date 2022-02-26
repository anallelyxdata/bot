using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EchoBot1.Helpers;
using Microsoft.Extensions.Configuration;

namespace EchoBot1.Bots
{
    public class DialogBot<T> : ActivityHandler where T : Dialog
    {
        #region Variables
        protected readonly Dialog _dialog;
        protected readonly StateService _stateService;
        protected readonly ILogger _logger;

        //Variables para expirar sesión
        protected readonly int ExpireAfterSeconds;
        protected readonly int TimeoutSeconds;
        protected readonly IStatePropertyAccessor<DateTime> LastAccessedTimeProperty;
        protected readonly IStatePropertyAccessor<DialogState> DialogStateProperty;

        // Existing fields omitted...
        #endregion

        public DialogBot(StateService botStateService, T dialog, ILogger<DialogBot<T>> logger, IConfiguration configuration)
        {
            _stateService = botStateService ?? throw new System.ArgumentNullException(nameof(botStateService));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            TimeoutSeconds = configuration.GetValue<int>("ExpireAfterSeconds");
            DialogStateProperty = _stateService.ConversationState.CreateProperty<DialogState>(nameof(DialogState));
            LastAccessedTimeProperty = _stateService.ConversationState.CreateProperty<DateTime>(nameof(LastAccessedTimeProperty));
        }

        //Se llama cada que hay un cambio
        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Retrieve the property value, and compare it to the current time.
            var lastAccess = await LastAccessedTimeProperty.GetAsync(turnContext, () => DateTime.UtcNow, cancellationToken);
            if ((DateTime.UtcNow - lastAccess) >= TimeSpan.FromSeconds(TimeoutSeconds))
            {
                // Notify the user that the conversation is being restarted.
                await turnContext.SendActivityAsync("Hola de nuevo, comparteme tu número de empleado, por favor");

                // Clear state.
                await _stateService.ConversationState.ClearStateAsync(turnContext, cancellationToken);
                await _stateService.UserState.ClearStateAsync(turnContext, cancellationToken);
            }

            //Primero ocurre el metodo base y despues lo que sobreescribimos de la función
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Set LastAccessedTime to the current time.
            await LastAccessedTimeProperty.SetAsync(turnContext, DateTime.UtcNow, cancellationToken);

            //Guardamos cambios que hayan ocurrido en el turno
            await _stateService.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _stateService.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Running dialog with Message Activity.");

            //Corremos el dialogo con la Actividad de nuevo Mensaje
            await _dialog.Run(turnContext, _stateService.DialogStateAccesor, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var welcomeText = "Bienvenid@, comparteme tu número de empleado, por favor";
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text(welcomeText), cancellationToken);
                }
            }
        }
    }

    
}
