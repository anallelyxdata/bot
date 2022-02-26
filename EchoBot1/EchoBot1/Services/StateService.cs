using EchoBot1.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EchoBot1.Services
{
    public class StateService
    {
        #region Variables

        //STATE VARIABLES
        public ConversationState ConversationState { get; }
        public UserState UserState { get; }

        //IDs
        public static string ConversationDataId { get; } = $"{nameof(StateService)}.ConversationData";
        public static string UserProfileId { get; } = $"{nameof(StateService)}.UserProfile";

        public static string DialogStateId { get; } = $"{nameof(StateService)}.DialogState";

        //ACCESORS
        public IStatePropertyAccessor<ConversationData> ConversationDataAccesor { get; set; }
        public IStatePropertyAccessor<UserProfile> UserProfileAccesor { get; set; }
        public IStatePropertyAccessor<DialogState> DialogStateAccesor { get; set; }
        #endregion

        public StateService(UserState userState, ConversationState conversationState)
        {
            ConversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
            InitializeAccesors();
        }

        private void InitializeAccesors()
        {
            //Initialize Conversation State Accesors
            ConversationDataAccesor = ConversationState.CreateProperty<ConversationData>(ConversationDataId);

            // Inicializamos accesores del estado del dialogo
            DialogStateAccesor = ConversationState.CreateProperty<DialogState>(DialogStateId);
            //Initialize User State
            UserProfileAccesor = UserState.CreateProperty<UserProfile>(UserProfileId);
        }
    }
}
