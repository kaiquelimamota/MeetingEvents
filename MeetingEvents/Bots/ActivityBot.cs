// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace MeetingEvents.Bots
{
    using AdaptiveCards;
    using MeetingEvents.Models;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Teams;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using System.IdentityModel.Tokens.Jwt;
    using Microsoft.Bot.Builder.Dialogs.Choices;

    public class ActivityBot : TeamsActivityHandler
    {
        private BotState _conversationState;
        private readonly string _baseUrl;
        private readonly DialogSet _dialogs;
        private readonly BotState _userState;
        private DialogContext _dialogContext;
        public ActivityBot(ConversationState conversationState, IConfiguration configuration)
        {
            _conversationState = conversationState;
            _baseUrl = configuration["BaseUrl"].EndsWith("/") ? configuration["BaseUrl"] : configuration["BaseUrl"] + "/";

            _dialogs = new DialogSet(_conversationState.CreateProperty<DialogState>("DialogState"));

            _dialogs.Add(new OAuthPrompt(
            nameof(OAuthPrompt),
            new OAuthPromptSettings
            {
                ConnectionName = "conn",
                Text = "Por favor, faça login para continuar.",
                Title = "Login",
                Timeout = 300000,
                ShowSignInLink = true
            }));
        }

        /// <summary>
        /// Activity Handler for Meeting Participant join event
        /// </summary>
        /// <param name="meeting"></param>
        /// <param name="turnContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task OnTeamsMeetingParticipantsJoinAsync(MeetingParticipantsEventDetails meeting, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Attachment(createAdaptiveCardInvokeResponseAsync(meeting.Members[0].User.Name, " has joined the meeting.")));
            return;
        }

        protected override Task OnSignInInvokeAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {

            var tes = turnContext as TokenResponse;
            var a = _dialogContext.FindDialog(nameof(OAuthPrompt));
            var token = turnContext.TurnState.Get<string>("token");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Activity Handler for Meeting Participant leave event
        /// </summary>
        /// <param name="meeting"></param>
        /// <param name="turnContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task OnTeamsMeetingParticipantsLeaveAsync(MeetingParticipantsEventDetails meeting, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(MessageFactory.Attachment(createAdaptiveCardInvokeResponseAsync(meeting.Members[0].User.Name, " left the meeting.")));
            return;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name = "turnContext" ></ param >
        /// < param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {

            _dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
            var results = await _dialogContext.ContinueDialogAsync(cancellationToken);

            if (results.Status == DialogTurnStatus.Empty)
            {
                await _dialogContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
            }
        }

        // Evento chamado quando o estado de verificação de login precisa ser validado
        protected override async Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {

            // Cria o contexto do diálogo
            _dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);

            // Continua o diálogo existente, se houver
            var result = await _dialogContext.ContinueDialogAsync(cancellationToken);

            // Verifica se o diálogo foi concluído
            if (result.Status == DialogTurnStatus.Complete)
            {
                // Verifica se o resultado contém um TokenResponse
                if (result.Result is TokenResponse tokenResponse)
                {
                    // Extrai o token JWT
                    var jwtToken = tokenResponse.Token;

                    // Valida o token JWT (opcional, dependendo da sua lógica de validação)
                    if (!string.IsNullOrEmpty(jwtToken))
                    {
                        // O login foi bem-sucedido, o token JWT foi recuperado
                        await turnContext.SendActivityAsync($"Login bem-sucedido! Token JWT: {jwtToken}", cancellationToken: cancellationToken);

                        // Aqui você pode validar o token JWT, por exemplo, verificando sua assinatura ou claims
                        // Se necessário, faça chamadas a APIs protegidas usando o token JWT
                    }
                    else
                    {
                        // O token JWT não foi recuperado, o login falhou
                        await turnContext.SendActivityAsync("Falha no login: Token JWT não encontrado.", cancellationToken: cancellationToken);
                    }
                }
                else
                {
                    // O resultado não contém um TokenResponse, o login falhou
                    await turnContext.SendActivityAsync("Falha no login: TokenResponse não encontrado.", cancellationToken: cancellationToken);
                }
            }
            else
            {
                // Se o diálogo não foi concluído, continue o fluxo normal
                await _dialogContext.ContinueDialogAsync(cancellationToken);
            }

        }

        protected override Task OnConversationUpdateActivityAsync(ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var addmenbers = turnContext.Activity.MembersAdded;
            if (addmenbers.LastOrDefault().Id == turnContext.Activity.Recipient.Id)
            {

            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Activity Handler for Meeting start event
        /// </summary>
        /// <param name="meeting"></param>
        /// <param name="turnContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task OnTeamsMeetingStartAsync(MeetingStartEventDetails meeting, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            // Save any state changes that might have occurred during the turn.
            var conversationStateAccessors = _conversationState.CreateProperty<MeetingData>(nameof(MeetingData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new MeetingData());
            conversationData.StartTime = meeting.StartTime;
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await turnContext.SendActivityAsync(MessageFactory.Attachment(GetAdaptiveCardForMeetingStart(meeting)));



            //var reply = MessageFactory.Attachment(new[] { GetTaskModuleHeroCardOptions() });
            //await turnContext.SendActivityAsync(reply, cancellationToken);

        }

        /// <summary>
        /// Activity Handler for Meeting end event.
        /// </summary>
        /// <param name="meeting"></param>
        /// <param name="turnContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task OnTeamsMeetingEndAsync(MeetingEndEventDetails meeting, ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var conversationStateAccessors = _conversationState.CreateProperty<MeetingData>(nameof(MeetingData));
            var conversationData = await conversationStateAccessors.GetAsync(turnContext, () => new MeetingData());

            await turnContext.SendActivityAsync(MessageFactory.Attachment(GetAdaptiveCardForMeetingEnd(meeting, conversationData)));

        }



        /// <summary>
        /// Sample Adaptive card for Meeting Start event.
        /// </summary>
        private Attachment GetAdaptiveCardForMeetingStart(MeetingStartEventDetails meeting)
        {
            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion("1.2"))
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = meeting.Title  + "- started",
                        Weight = AdaptiveTextWeight.Bolder,
                        Spacing = AdaptiveSpacing.Medium,
                    },
                    new AdaptiveColumnSet
                    {
                        Columns = new List<AdaptiveColumn>
                        {
                            new AdaptiveColumn
                            {
                                Width = AdaptiveColumnWidth.Auto,
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Text = "Start Time : ",
                                        Wrap = true,
                                    },
                                },
                            },
                            new AdaptiveColumn
                            {
                                Width = AdaptiveColumnWidth.Auto,
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Text = Convert.ToString(meeting.StartTime.ToLocalTime()),
                                        Wrap = true,
                                    },
                                },
                            },
                        },
                    },
                },
                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveOpenUrlAction
                    {
                        Title = "Join meeting",
                        Url = meeting.JoinUrl,
                    },
                },
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
            };
        }

        /// <summary>
        /// Sample Adaptive card for Meeting End event.
        /// </summary>
        private Attachment GetAdaptiveCardForMeetingEnd(MeetingEndEventDetails meeting, MeetingData conversationData)
        {

            TimeSpan meetingDuration = meeting.EndTime - conversationData.StartTime;
            var meetingDurationText = meetingDuration.Minutes < 1 ?
                  Convert.ToInt32(meetingDuration.Seconds) + "s"
                : Convert.ToInt32(meetingDuration.Minutes) + "min " + Convert.ToInt32(meetingDuration.Seconds) + "s";

            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion("1.2"))
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = meeting.Title  + "- ended",
                        Weight = AdaptiveTextWeight.Bolder,
                        Spacing = AdaptiveSpacing.Medium,
                    },
                     new AdaptiveColumnSet
                    {
                        Columns = new List<AdaptiveColumn>
                        {
                            new AdaptiveColumn
                            {
                                Width = AdaptiveColumnWidth.Auto,
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Text = "MeetingId: ",
                                        Wrap = true,
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = "Titulo: ",
                                        Wrap = true,
                                    },
                                },
                            },
                            new AdaptiveColumn
                            {
                                Width = AdaptiveColumnWidth.Auto,
                                Items = new List<AdaptiveElement>
                                {
                                    new AdaptiveTextBlock
                                    {
                                        Text = meeting.Id,
                                        Wrap = true,
                                    },
                                    new AdaptiveTextBlock
                                    {
                                        Text = meeting.Title,
                                        Wrap = true,
                                    },
                                },
                            },
                        },
                    },
                }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
            };
        }

        private static Attachment GetTaskModuleHeroCardOptions()
        {
            // Create a Hero Card with TaskModuleActions for each Dialogs (referred as task modules in TeamsJS v1.x)
            return new HeroCard()
            {
                Title = "Click para identificar o cliente.",
                Buttons = new[] { TaskModuleUIConstants.AdaptiveCard }
                            .Select(cardType => new TaskModuleAction(cardType.ButtonTitle, new CardTaskFetchValue<string>() { Data = cardType.Id }))
                            .ToList<CardAction>(),
            }.ToAttachment();
        }

        private Activity CreateTaskModuleResponse()
        {

            var taskInfo = new TaskModuleTaskInfo();

            taskInfo.Card = CreateAdaptiveCardAttachment();
            SetTaskInfo(taskInfo, TaskModuleUIConstants.AdaptiveCard);

            var taskModuleResponse = new TaskModuleResponse
            {
                Task = new TaskModuleContinueResponse
                {
                    Value = taskInfo
                }
            };

            return new Activity
            {
                Type = ActivityTypes.Invoke,
                Value = new InvokeResponse
                {
                    Status = 200,
                    Body = taskModuleResponse
                }
            };
        }

        private static Attachment GetTaskModuleAdaptiveCardOptions()
        {
            // Create an Adaptive Card with an AdaptiveSubmitAction for each Dialogs (referred as task modules in TeamsJS v1.x)
            var card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 2))
            {
                Body = new List<AdaptiveElement>()
                    {
                        new AdaptiveTextBlock(){ Text="Dialogs (referred as task modules in TeamsJS v1.x) Invocation from Adaptive Card", Weight=AdaptiveTextWeight.Bolder, Size=AdaptiveTextSize.Large}
                    },
                Actions = new[] { TaskModuleUIConstants.AdaptiveCard }
                            .Select(cardType => new AdaptiveSubmitAction() { Title = cardType.ButtonTitle, Data = new AdaptiveCardTaskFetchValue<string>() { Data = cardType.Id } })
                            .ToList<AdaptiveAction>(),
            };

            return new Attachment() { ContentType = AdaptiveCard.ContentType, Content = card };
        }

        protected override Task<TaskModuleResponse> OnTeamsTaskModuleFetchAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            var asJobject = JObject.FromObject(taskModuleRequest.Data);
            var value = asJobject.ToObject<CardTaskFetchValue<string>>()?.Data;

            var taskInfo = new TaskModuleTaskInfo();
            switch (value)
            {
                case TaskModuleIds.CustomForm:
                    taskInfo.Url = taskInfo.FallbackUrl = _baseUrl + TaskModuleIds.CustomForm;
                    SetTaskInfo(taskInfo, TaskModuleUIConstants.CustomForm);
                    break;
                case TaskModuleIds.AdaptiveCard:
                    taskInfo.Card = CreateAdaptiveCardAttachment();
                    SetTaskInfo(taskInfo, TaskModuleUIConstants.AdaptiveCard);
                    break;
                default:
                    break;
            }

            return Task.FromResult(taskInfo.ToTaskModuleResponse());
        }

        protected override async Task<TaskModuleResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext<IInvokeActivity> turnContext, TaskModuleRequest taskModuleRequest, CancellationToken cancellationToken)
        {
            var reply = MessageFactory.Text("OnTeamsTaskModuleSubmitAsync Value: " + JsonConvert.SerializeObject(taskModuleRequest));

            var test = turnContext.TurnState.Keys;

            

            //if (results.Status == DialogTurnStatus.Empty)
            //{
            //    await dialogContext.BeginDialogAsync("OAuthPrompt", null, cancellationToken);
            //}
            ////["Microsoft.Bot.Connector.Authentication.UserTokenClient"];
            //var tokenResponse = turnContext.Activity.Value as TokenResponse;
            //if (tokenResponse != null && !string.IsNullOrEmpty(tokenResponse.Token))
            //{
            //    var jwtToken = tokenResponse.Token;
            //    System.Diagnostics.Debug.WriteLine($"JWT Token: {jwtToken}");

            //    // Agora você pode usar o JWT para fazer chamadas autenticadas
            //    // Exemplo: Chamar a API Graph para obter informações do usuário
            //    //var userInfo = await CallGraphAPI(jwtToken);
            //    //await turnContext.SendActivityAsync(MessageFactory.Text($"Olá, {userInfo.DisplayName}!"), cancellationToken);
            //}


            //await turnContext.SendActivityAsync(reply, cancellationToken);

            return TaskModuleResponseFactory.CreateResponse("Thanks!");
        }

        
        private static void SetTaskInfo(TaskModuleTaskInfo taskInfo, UISettings uIConstants)
        {
            taskInfo.Height = uIConstants.Height;
            taskInfo.Width = uIConstants.Width;
            taskInfo.Title = uIConstants.Title.ToString();
        }

        private static Attachment CreateAdaptiveCardAttachment()
        {
            // combine path for cross platform support
            string[] paths = { ".", "Resources", "adaptiveCard.json" };
            var adaptiveCardJson = File.ReadAllText(Path.Combine(paths));

            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
        }

        /// <summary>
        /// Sample Adaptive card for Meeting participant events.
        /// </summary>
        private Attachment createAdaptiveCardInvokeResponseAsync(string userName, string action)
        {
            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion("1.4"))
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveRichTextBlock
                    {
                        Inlines = new List<AdaptiveInline>
                        {
                            new AdaptiveTextRun
                            {
                                Text = userName,
                                Weight = AdaptiveTextWeight.Bolder,
                                Size = AdaptiveTextSize.Default,
                            },
                            new AdaptiveTextRun
                            {
                                Text = action,
                                Weight = AdaptiveTextWeight.Default,
                                Size = AdaptiveTextSize.Default,
                            }
                        },
                    Spacing = AdaptiveSpacing.Medium,
                    }
                }
            };

            return new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card,
            };
        }
        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Prompt the user to sign in
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse?.Token != null)
            {
                // Pull in the data from the Microsoft Graph.
                //var client = new SimpleGraphClient(tokenResponse.Token);
                //var me = await client.GetMeAsync();
                //var title = !string.IsNullOrEmpty(me.JobTitle) ?
                //            me.JobTitle : "Unknown";

                //await stepContext.Context.SendActivityAsync($"You're logged in as {me.DisplayName} ({me.UserPrincipalName}); you job title is: {title}");

                return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Would you like to view your token?") }, cancellationToken);
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Login was not successful please try again."), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayTokenPhase1Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("Thank you."), cancellationToken);

            var result = (bool)stepContext.Result;
            if (result)
            {
                // Call the prompt again because we need the token. The reasons for this are:
                // 1. If the user is already logged in we do not need to store the token locally in the bot and worry
                // about refreshing it. We can always just call the prompt again to get the token.
                // 2. We never know how long it will take a user to respond. By the time the
                // user responds the token may have expired. The user would then be prompted to login again.
                //
                // There is no reason to store the token locally in the bot because we can always just call
                // the OAuth prompt to get the token or get a new token if needed.
                return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), cancellationToken: cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayTokenPhase2Async(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse != null)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Here is your token {tokenResponse.Token}"), cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }



    }
}