using EchoBot1.Models;
using EchoBot1.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EchoBot1.Bots
{
    public class GreetingBot: ActivityHandler
    {
        #region Variables
        private readonly StateService _stateService;
        #endregion

        public GreetingBot(StateService stateService)
        {
            _stateService = stateService ?? throw new System.ArgumentNullException(nameof(stateService));
        }

        private async Task GetName(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            UserProfile userProfile = await _stateService.UserProfileAccesor.GetAsync(turnContext, () => new UserProfile());
            ConversationData conversationData = await _stateService.ConversationDataAccesor.GetAsync(turnContext, () => new ConversationData());
            if (!string.IsNullOrEmpty(userProfile.Name))
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(String.Format("Hi {0}. How can I help you today?", userProfile.Name)), cancellationToken);
            }
            else
            {
                if (conversationData.PromptedUserForName)
                {
                    // Se asigna el nmombre de lo que escribió el usuario
                    userProfile.Name = turnContext.Activity.Text?.Trim();

                    //Ya se sabe el nombre del usuario
                    await turnContext.SendActivityAsync(MessageFactory.Text(String.Format("Thanks {0}. How can I help you today?", userProfile.Name)), cancellationToken);

                    //
                    conversationData.PromptedUserForName = false;
                }
                else
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"What is your name?"), cancellationToken);

                    conversationData.PromptedUserForName = true;
                }

                // Guardar cambios en el estado
                await _stateService.UserProfileAccesor.SetAsync(turnContext, userProfile);
                await _stateService.ConversationDataAccesor.SetAsync(turnContext, conversationData);

                await _stateService.UserState.SaveChangesAsync(turnContext);
                await _stateService.ConversationState.SaveChangesAsync(turnContext);
            }
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken) 
        {
            await GetName(turnContext, cancellationToken);
        } 

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach(var member in membersAdded) 
            {
                if(member.Id != turnContext.Activity.Recipient.Id)
                {
                    await GetName(turnContext, cancellationToken);
                }
            }
        }
    }
}
