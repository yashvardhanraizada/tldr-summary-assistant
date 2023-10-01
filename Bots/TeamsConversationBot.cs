// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using AdaptiveCards.Templating;
using Newtonsoft.Json;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Graph;
using Azure.Identity;
using JsonSerializer = System.Text.Json.JsonSerializer;
using static LLMClient;
using System.Net.Http;
using Azure;
using System.Text.RegularExpressions;
using System.Text.Json;
using Microsoft.Rest;
using System.Globalization;
using System.Net;
using JsonException = Newtonsoft.Json.JsonException;
using Microsoft.Bot.Connector.Teams;
using Microsoft.Graph.TermStore;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class TeamsConversationBot : TeamsActivityHandler
    {
        private string _appId;
        private string _appPassword;

        private bool isMsitEnabled = false;

        private string _clientId = "d8c817ec-09e7-4289-8384-74af116f302c";
        private string _clientSecret = "~uF8Q~sKMikylp1zXhUksayR2Oc4veG5u8Yl2bgQ";

        private string msitGroupId = "58743514-6113-420c-9b86-64c1f6359b8d";
        private string emeaGroupId = "fc39987b-3f36-4779-8b3f-f4b7121931e3";

        public string _graphToken = "eyJ0eXAiOiJKV1QiLCJub25jZSI6IkNVNnJNTTVmR19XMHdBR1RjTk9qWnNxM2lLSUtBMXU0bmxjWW5yd1p5VFkiLCJhbGciOiJSUzI1NiIsIng1dCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyIsImtpZCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC83MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDcvIiwiaWF0IjoxNjc2ODA0MjkxLCJuYmYiOjE2NzY4MDQyOTEsImV4cCI6MTY3NjgwOTk2NiwiYWNjdCI6MCwiYWNyIjoiMSIsImFjcnMiOlsidXJuOnVzZXI6cmVnaXN0ZXJzZWN1cml0eWluZm8iXSwiYWlvIjoiQVZRQXEvOFRBQUFBRFVuaG03emZSd2llM1hVZnU4LzFoS0h1L1VpTUZ3MHlQZ1lHQXNCN2pqY1JZcDlBem9oRGFvOVNsdzczb3gxZzc0Vmcrdmd4YXppMWJUdnNlaTFlc0ZTVGU5MXpMUlFoZWF1SGY1TUw3SFU9IiwiYW1yIjpbInJzYSIsIm1mYSJdLCJhcHBfZGlzcGxheW5hbWUiOiJHcmFwaCBFeHBsb3JlciIsImFwcGlkIjoiZGU4YmM4YjUtZDlmOS00OGIxLWE4YWQtYjc0OGRhNzI1MDY0IiwiYXBwaWRhY3IiOiIwIiwiY29udHJvbHMiOlsiYXBwX3JlcyJdLCJjb250cm9sc19hdWRzIjpbImRlOGJjOGI1LWQ5ZjktNDhiMS1hOGFkLWI3NDhkYTcyNTA2NCIsIjAwMDAwMDAzLTAwMDAtMDAwMC1jMDAwLTAwMDAwMDAwMDAwMCIsIjAwMDAwMDAzLTAwMDAtMGZmMS1jZTAwLTAwMDAwMDAwMDAwMCJdLCJkZXZpY2VpZCI6Ijk0MzI4ZmI1LTIwY2MtNDQzYS05OWViLTViZTQxOGQ3Y2YyOSIsImZhbWlseV9uYW1lIjoiUmFpemFkYSIsImdpdmVuX25hbWUiOiJZYXNoIFZhcmRoYW4iLCJpZHR5cCI6InVzZXIiLCJpcGFkZHIiOiI0OS40My4yNDIuMTkyIiwibmFtZSI6Illhc2ggVmFyZGhhbiBSYWl6YWRhIiwib2lkIjoiNDc2OWIyNDMtOWQyNi00ZTExLWFmOTUtZjIxMGQyOTA4OGU4Iiwib25wcmVtX3NpZCI6IlMtMS01LTIxLTIxMjc1MjExODQtMTYwNDAxMjkyMC0xODg3OTI3NTI3LTU3NTI1Mzc3IiwicGxhdGYiOiIzIiwicHVpZCI6IjEwMDMyMDAxRjk4QjYxMkEiLCJyaCI6IjAuQVJvQXY0ajVjdkdHcjBHUnF5MTgwQkhiUndNQUFBQUFBQUFBd0FBQUFBQUFBQUFhQU1ZLiIsInNjcCI6IkNhbGVuZGFycy5SZWFkV3JpdGUgQ29udGFjdHMuUmVhZFdyaXRlIERldmljZU1hbmFnZW1lbnRBcHBzLlJlYWRXcml0ZS5BbGwgRGV2aWNlTWFuYWdlbWVudENvbmZpZ3VyYXRpb24uUmVhZC5BbGwgRGV2aWNlTWFuYWdlbWVudENvbmZpZ3VyYXRpb24uUmVhZFdyaXRlLkFsbCBEZXZpY2VNYW5hZ2VtZW50TWFuYWdlZERldmljZXMuUHJpdmlsZWdlZE9wZXJhdGlvbnMuQWxsIERldmljZU1hbmFnZW1lbnRNYW5hZ2VkRGV2aWNlcy5SZWFkLkFsbCBEZXZpY2VNYW5hZ2VtZW50TWFuYWdlZERldmljZXMuUmVhZFdyaXRlLkFsbCBEZXZpY2VNYW5hZ2VtZW50UkJBQy5SZWFkLkFsbCBEZXZpY2VNYW5hZ2VtZW50UkJBQy5SZWFkV3JpdGUuQWxsIERldmljZU1hbmFnZW1lbnRTZXJ2aWNlQ29uZmlnLlJlYWQuQWxsIERldmljZU1hbmFnZW1lbnRTZXJ2aWNlQ29uZmlnLlJlYWRXcml0ZS5BbGwgRGlyZWN0b3J5LkFjY2Vzc0FzVXNlci5BbGwgRGlyZWN0b3J5LlJlYWRXcml0ZS5BbGwgRmlsZXMuUmVhZFdyaXRlLkFsbCBHcm91cC5SZWFkV3JpdGUuQWxsIElkZW50aXR5Umlza0V2ZW50LlJlYWQuQWxsIE1haWwuUmVhZFdyaXRlIE1haWxib3hTZXR0aW5ncy5SZWFkV3JpdGUgTm90ZXMuUmVhZFdyaXRlLkFsbCBvcGVuaWQgUGVvcGxlLlJlYWQgUHJlc2VuY2UuUmVhZCBQcmVzZW5jZS5SZWFkLkFsbCBwcm9maWxlIFJlcG9ydHMuUmVhZC5BbGwgU2l0ZXMuUmVhZFdyaXRlLkFsbCBUYXNrcy5SZWFkV3JpdGUgVXNlci5SZWFkIFVzZXIuUmVhZEJhc2ljLkFsbCBVc2VyLlJlYWRXcml0ZSBVc2VyLlJlYWRXcml0ZS5BbGwgZW1haWwiLCJzaWduaW5fc3RhdGUiOlsiZHZjX21uZ2QiLCJkdmNfY21wIl0sInN1YiI6ImZOTFA1eWV1WVZlM0NqMjQ4WVBMbFRTV0U0MlJXOHpteFZmdUFjcnNlQ2MiLCJ0ZW5hbnRfcmVnaW9uX3Njb3BlIjoiV1ciLCJ0aWQiOiI3MmY5ODhiZi04NmYxLTQxYWYtOTFhYi0yZDdjZDAxMWRiNDciLCJ1bmlxdWVfbmFtZSI6InlyYWl6YWRhQG1pY3Jvc29mdC5jb20iLCJ1cG4iOiJ5cmFpemFkYUBtaWNyb3NvZnQuY29tIiwidXRpIjoiSUhvd2NWN0J4RS1QUExjdXlSRS1BQSIsInZlciI6IjEuMCIsIndpZHMiOlsiYjc5ZmJmNGQtM2VmOS00Njg5LTgxNDMtNzZiMTk0ZTg1NTA5Il0sInhtc19zdCI6eyJzdWIiOiJNeWhFQTY0VHJiVWNiRXpyTkxQSE1LS1ZSZU9IUWlxLVlsbnpEbk1BM0lJIn0sInhtc190Y2R0IjoxMjg5MjQxNTQ3fQ.SeqvqFFIEMQFMxLbHiP3lw2onISTslmI7jKz3RnDGxKv6r9-c7udlCORZ8jZeyERrQa-iQt_lGzkbCn5pKu4hsNkmCUHkA75TUB_imMXuOmNfBb413bpbXyUVWfRueDHVkK-P6zkhDwNysgGvYWWnr9Ej2woEZLy_WLNGySdBCtrC1Z_z3YN8mbjZExyLqPggmdyLVoQOWMQ03o1HbkQUFovEmbuHfisIOZKxVFV5YP6IOLvSyxLqVHWgi8lMWREuvrFHGRscXHLo1IbJKNWJfKJ0n9aLrItarJ4mtsg3KN4DExZol2YBJx5PwC9-CySCr0iZQENHOY-RQnU2fYuZA";
        public string _llmToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsImtpZCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyJ9.eyJhdWQiOiI2OGRmNjZhNC1jYWQ5LTRiZmQtODcyYi1jNmRkZGUwMGQ2YjIiLCJpc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNzJmOTg4YmYtODZmMS00MWFmLTkxYWItMmQ3Y2QwMTFkYjQ3L3YyLjAiLCJpYXQiOjE2Nzc2ODQ5OTAsIm5iZiI6MTY3NzY4NDk5MCwiZXhwIjoxNjc3NjkwMTg5LCJhaW8iOiJBVlFBcS84VEFBQUFocjMrY3hncnEzV1BvRCtJTnNwOHcrTUdxcWxSek9zOVpkOHVvbGFQR0ZXZ0xmRFFtaFRJUSswL2hiQ0RQY0F1WEkvaEZtRWV3Slk1eVZxYnhQWDF3c0hBamlnNHFaT2NjVlFITDM5YzBPOD0iLCJhenAiOiI2OGRmNjZhNC1jYWQ5LTRiZmQtODcyYi1jNmRkZGUwMGQ2YjIiLCJhenBhY3IiOiIwIiwiZW1haWwiOiJ5cmFpemFkYUBtaWNyb3NvZnQuY29tIiwibmFtZSI6Illhc2ggVmFyZGhhbiBSYWl6YWRhIiwib2lkIjoiNDc2OWIyNDMtOWQyNi00ZTExLWFmOTUtZjIxMGQyOTA4OGU4IiwicHJlZmVycmVkX3VzZXJuYW1lIjoieXJhaXphZGFAbWljcm9zb2Z0LmNvbSIsInJoIjoiMC5BUm9BdjRqNWN2R0dyMEdScXkxODBCSGJSNlJtMzJqWnl2MUxoeXZHM2Q0QTFySWFBTVkuIiwic2NwIjoiYWNjZXNzIiwic3ViIjoiRXRodDY0MHBlQnBHcnNmaWx0VHlOUWFlNUxuOHNGcGJ0SGZXNG5wWEZCQSIsInRpZCI6IjcyZjk4OGJmLTg2ZjEtNDFhZi05MWFiLTJkN2NkMDExZGI0NyIsInV0aSI6IjVCUHpvQkNJaWstTmZNcFZPS1FxQUEiLCJ2ZXIiOiIyLjAiLCJ2ZXJpZmllZF9wcmltYXJ5X2VtYWlsIjpbInlyYWl6YWRhQG1pY3Jvc29mdC5jb20iXX0.APOc_cyLAgLFAOBRUZnSrg6HZc6XqT8HqNlpYJFYbLgN9h-wLqGkeoYgKvKwDvoBI-KV-3yUJQS-AME9QOLxo0YRj1Fqzc2-pJ-K4oM3EoPwOklQ0hHsdvWM_AtAU18BY5XI35GysBbodT35NY_K2oUQpADuP62h9BbESNCZX7LHOBFXrbAJpWqYe6k1xmF56BBXHlJ78oeUCt_V3zAy9V8L3TSCfwuyulXeeYJDP9A1Pujl0XYDtTeh68lPW3xcki2xgDdbAwbXN0Vo2EE1FK_-gZn5zLny5-4rKr4ncAK7KApA2_G5ZrN_iLzp_YqzioIaZFGeCqyGKP97cqmbrw";

        public TeamsConversationBot(IConfiguration config)
        {
            _appId = config["MicrosoftAppId"];
            _appPassword = config["MicrosoftAppPassword"];
        }

        private readonly string _adaptiveCardTemplate = Path.Combine(".", "Resources", "UserMentionCardTemplate.json");

        private readonly string _immersiveReaderCardTemplate = Path.Combine(".", "Resources", "ImmersiveReaderCard.json");

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            turnContext.Activity.RemoveRecipientMention();
            var text = turnContext.Activity.Text.Trim().ToLower();

            if (text.Contains("mention me"))
                await MentionAdaptiveCardActivityAsync(turnContext, cancellationToken);
            else if (text.Contains("mention"))
                await MentionActivityAsync(turnContext, cancellationToken);
            else if (text.Contains("who"))
                await GetSingleMemberAsync(turnContext, cancellationToken);
            else if (text.Contains("update"))
                await CardActivityAsync(turnContext, true, cancellationToken);
            else if (text.Contains("message"))
                await MessageAllMembersAsync(turnContext, cancellationToken);
            else if (text.Contains("immersivereader"))
                await SendImmersiveReaderCardAsync(turnContext, cancellationToken);
            else if (text.Contains("delete"))
                await DeleteCardActivityAsync(turnContext, cancellationToken);
            else if (text.Contains("summarizetext"))
                await GetTextSummaryAsync(turnContext, cancellationToken);
            else if (text.Contains("summarizepost"))
                await GetPostSummaryAsync(turnContext, cancellationToken);
            else if (text.Contains("summarizereplies"))
                await GetReplySummaryAsync(turnContext, cancellationToken);
            else
                await CardActivityAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnTeamsMembersAddedAsync(IList<TeamsChannelAccount> membersAdded, Bot.Schema.Teams.TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var teamMember in membersAdded)
            {
                if(teamMember.Id != turnContext.Activity.Recipient.Id && turnContext.Activity.Conversation.ConversationType != "personal")
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Welcome to the team {teamMember.GivenName} {teamMember.Surname}."), cancellationToken);
                }
            }
        }

        protected override async Task OnInstallationUpdateActivityAsync(ITurnContext<IInstallationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            if(turnContext.Activity.Conversation.ConversationType == "channel")
            {
                await turnContext.SendActivityAsync($"Welcome to Microsoft Teams SummaryAssistant Bot.");
            }
            else
            {
                await turnContext.SendActivityAsync("Welcome to Microsoft Teams SummaryAssistant Bot. Testing YAML");
            }
        }

        private async Task CardActivityAsync(ITurnContext<IMessageActivity> turnContext, bool update, CancellationToken cancellationToken)
        {

            var card = new HeroCard
            {
                Buttons = new List<CardAction>
                        {
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Message all members",
                                Text = "MessageAllMembers"
                            },
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Who am I?",
                                Text = "whoami"
                            },
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Send Immersive Reader Card",
                                Text = "ImmersiveReader"
                            },
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Find me in Adaptive Card",
                                Text = "mention me"
                            },
                            new CardAction
                            {
                                Type = ActionTypes.MessageBack,
                                Title = "Delete card",
                                Text = "Delete"
                            }
                        }
            };


            if (update)
            {
                await SendUpdatedCard(turnContext, card, cancellationToken);
            }
            else
            {
                await SendWelcomeCard(turnContext, card, cancellationToken);
            }

        }

        private static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)(\W|\n|\r)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }

        private string ParseChannelPostToString(string post)
        {
            ChannelPost responseObject = JsonConvert.DeserializeObject<ChannelPost>(post);
            string channelPostUser = "";
            string channelPostText = "";

            if (responseObject.From.User != null)
            {
                channelPostUser = responseObject.From.User.DisplayName.ToString();
            }

            channelPostText = HtmlToPlainText(responseObject.Body.Content.ToString());
            channelPostText = channelPostText.Replace("TLDR SummarizePost", "").Trim();
            channelPostText = channelPostText.Replace("TLDR SummarizeText", "").Trim();
            channelPostText = channelPostText.Replace("TLDR SummarizeReplies", "").Trim();

            if ((channelPostText != "") && (channelPostUser != ""))
            {
                channelPostText = channelPostUser + " said " + channelPostText;
            }
            
            return channelPostText;
        }

        private string ParseChannelRepliesToString(string replies)
        {
            /*var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };*/

            //int replyCount = responseObject.Count;
            //return responseObject.Value[0].Body.Content;

            ChannelReplies responseObject = JsonConvert.DeserializeObject<ChannelReplies>(replies);
            
            string replyBatchText = "";
            int replyCount = responseObject.Value.Count();

            for (int i = replyCount - 1; i >= 0; i--)
            {
                if (responseObject.Value[i].From.User != null)
                {
                    string bodyText = HtmlToPlainText(responseObject.Value[i].Body.Content.ToString());
                    bodyText = bodyText.Replace("TLDR SummarizeReplies", "").Trim();
                    bodyText = bodyText.Replace("TLDR SummarizePost", "").Trim();
                    bodyText = bodyText.Replace("TLDR SummarizeText", "").Trim();
                    string userText = responseObject.Value[i].From.User.DisplayName.ToString();

                    if(bodyText == "")
                    {
                        continue;
                    }

                    replyBatchText += userText + " said " + bodyText + ", ";
                }
            }

            return replyBatchText;
        }

        private string ParseLLMResponseToString(string response)
        {
            LLMResponse responseObject = JsonConvert.DeserializeObject<LLMResponse>(response);
            return responseObject.Choices[0].Text;
        }

        private async Task GetPostSummaryAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            string conversationType = turnContext.Activity.Conversation.ConversationType;

            string summaryType = "";
            string summarizedText = "";

            if (conversationType == "channel")
            {
                //string ENDPOINT = "https://graph.microsoft.com/beta/teams/8f066dd5-4b6e-4cc8-b759-c304a521b3f0/channels/19:SfgdDVF-eEgvY18wQPNWMVrRGQrJG4cfWdfY48q8eOI1@thread.tacv2/messages/";
                //ENDPOINT += postId;
                //var channelPostData = await GetChannelPostDataAsync(ENDPOINT);

                //var graphClient = GetGraphServiceClient(_tenantId, _clientId, _clientSecret);
                //var graphResponseData = await graphClient.Teams[groupId].Channels[channelId].Messages[postId].Request().GetResponseAsync();
                //var channelPostData = graphResponseData.Content.ReadAsStringAsync();

                string messageId = turnContext.Activity.Conversation.Id;
                string channelId = turnContext.Activity.ChannelData["teamsChannelId"];
                string teamId = turnContext.Activity.ChannelData["teamsTeamId"];
                string groupId = isMsitEnabled ? msitGroupId : emeaGroupId;

                char[] separator = { '=' };
                Int32 count = 2;
                string[] strList = messageId.Split(separator, count);

                string postId = strList[1];

                var channelPostData = await GetGraphDataAsync(groupId, channelId, postId, isMsitEnabled, "channelPost");

                string channelPostText = ParseChannelPostToString(channelPostData);
                channelPostText = channelPostText.Trim();

                if (channelPostText == "")
                {
                    summarizedText = "Nothing relevant to summarize.";
                }
                else
                {
                    string preProcessText = "Summarize the following text concisely in not more than 100 words: " + channelPostText;
                    summarizedText = await GetSummarizedText(preProcessText);
                }

                summaryType = "Post Summary";
            }
            else
            {
                summaryType = "Error!";
                summarizedText = "This command is only supported inside a channel scope.";
            }

            var card = new HeroCard { };
            card.Title = summaryType;
            card.Text = summarizedText;

            var activity = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(activity, cancellationToken);
        }

        private async Task GetReplySummaryAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            string conversationType = turnContext.Activity.Conversation.ConversationType;

            string summaryType = "";
            string summarizedText = "";

            if (conversationType == "channel")
            {
                //var graphClient = GetGraphServiceClient(tenantId, clientId, clientSecret);
                //var graphClient1 = GetGraphServiceClient1(tenantId, clientId, clientSecret);
                //var replies = await graphClient1.Teams["8f066dd5-4b6e-4cc8-b759-c304a521b3f0"].Channels["19:SfgdDVF-eEgvY18wQPNWMVrRGQrJG4cfWdfY48q8eOI1@thread.tacv2"].Messages["1676488519847"].Replies.Request().GetAsync();

                //var chatInfo = await graphClient.Chats["19:e7fcd799-14f8-4e8b-bb19-8cdda6663a96_d8c817ec-09e7-4289-8384-74af116f302c@unq.gbl.spaces"].Messages.Request().GetAsync();
                //var channelInfo = await graphClient.Teams["8f066dd5-4b6e-4cc8-b759-c304a521b3f0"].Channels["19:SfgdDVF-eEgvY18wQPNWMVrRGQrJG4cfWdfY48q8eOI1@thread.tacv2"].Messages.Request().GetAsync();
                //var user = await graphClient.Users[userId].Request().GetAsync();

                //channelText = "Message Id: " + messageId + replies.ToString();

                //More reply processing.
                //Extracting User and Reply Info.
                //Like a series of "User: Reply" pairs separated by ";" etc.

                //string ENDPOINT = "https://graph.microsoft.com/beta/teams/8f066dd5-4b6e-4cc8-b759-c304a521b3f0/channels/19:SfgdDVF-eEgvY18wQPNWMVrRGQrJG4cfWdfY48q8eOI1@thread.tacv2/messages/";
                //ENDPOINT += postId + "/replies";
                //var channelReplyData = await GetChannelReplyDataAsync(ENDPOINT);

                //var graphClient = GetGraphServiceClient(_tenantId, _clientId, _clientSecret);
                //var graphResponseData = await graphClient.Teams[groupId].Channels[channelId].Messages[postId].Replies.Request().GetResponseAsync();
                //var channelReplyData = graphResponseData.Content.ReadAsStringAsync().Result;

                string messageId = turnContext.Activity.Conversation.Id;
                string channelId = turnContext.Activity.ChannelData["teamsChannelId"];
                string teamId = turnContext.Activity.ChannelData["teamsTeamId"];
                string groupId = isMsitEnabled ? msitGroupId : emeaGroupId;

                char[] separator = { '=' };
                Int32 count = 2;
                string[] strList = messageId.Split(separator, count);

                string postId = strList[1];

                var channelReplyData = await GetGraphDataAsync(groupId, channelId, postId, isMsitEnabled, "channelReplies");

                string channelReplyText = ParseChannelRepliesToString(channelReplyData);
                channelReplyText = channelReplyText.Trim();

                if (channelReplyText == "")
                {
                    summarizedText = "Nothing relevant to summarize.";
                }
                else
                {
                    string preProcessText = "Summarize the following conversation concisely in not more than 100 words: " + channelReplyText;
                    summarizedText = await GetSummarizedText(preProcessText);
                }

                summaryType = "Discussion Summary";
            }
            else
            {
                summaryType = "Error!";
                summarizedText = "This command is only supported inside a channel scope.";
            }

            var card = new HeroCard { };
            card.Title = summaryType;
            card.Text = summarizedText;

            var activity = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(activity, cancellationToken);
        }

        //Old Code in Comments. Saved for Reference.
        private async Task GetTextSummaryAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //string channelText = "";
            //string chatText = "";

            //string tenantId = "a00b3f8c-9a42-4269-82c6-147f8a7fcbef";
            //string clientId = "d8c817ec-09e7-4289-8384-74af116f302c";
            //string clientSecret = "~uF8Q~sKMikylp1zXhUksayR2Oc4veG5u8Yl2bgQ";
            //string conversationType = turnContext.Activity.Conversation.ConversationType;

            //var graphClient = GetGraphServiceClient(tenantId, clientId, clientSecret);
            //var chatInfo = await graphClient.Chats["19:e7fcd799-14f8-4e8b-bb19-8cdda6663a96_d8c817ec-09e7-4289-8384-74af116f302c@unq.gbl.spaces"].Messages.Request().GetAsync();

            //chatText = "This is your complete text summary. Hope you find it useful. " + summarizedText;
            //card.Text = conversationType == "channel" ? channelText : chatText;

            string activityText = turnContext.Activity.Text.Replace("SummarizeText", "").Trim();

            string summaryType = "";
            string summarizedText = "";

            if (activityText == "")
            {
                summarizedText = "Nothing relevant to summarize.";
            }
            else
            {
                string preProcessText = "Summarize the following text concisely in not more than 100 words: " + activityText;
                summarizedText = await GetSummarizedText(preProcessText);
            }

            summaryType = "Text Summary";

            var card = new HeroCard { };
            card.Title = summaryType;
            card.Text = summarizedText;

            var activity = MessageFactory.Attachment(card.ToAttachment());
            await turnContext.SendActivityAsync(activity, cancellationToken);
        }

        private async Task<string> GetSummarizedText(string text)
        {
            string requestData = JsonSerializer.Serialize(new ModelPrompt
            {
                Prompt = text,
                MaxTokens = 500,
                Temperature = 0.6,
                TopP = 1,
                N = 1,
                Stream = false,
                LogProbs = null,
                Stop = ""
            });

            LLMClient llmClient = new LLMClient();
            var response = await llmClient.SendRequest("text-davinci-003", requestData, _llmToken);

            //Parse the responseText well and present it neatly.

            var summaryText = ParseLLMResponseToString(response);

            return summaryText;
        }

        private GraphServiceClient GetGraphServiceClient()
        {
            // The client credentials flow requires that you request the
            // /.default scope, and preconfigure your permissions on the
            // app registration in Azure. An administrator must grant consent
            // to those permissions beforehand.

            var scopes = new[] { "https://graph.microsoft.com/.default" };

            // Multi-tenant apps can use "common",
            // single-tenant apps must use the tenant ID from the Azure portal
            var tenantId = "a00b3f8c-9a42-4269-82c6-147f8a7fcbef";

            // Values from app registration
            var clientId = _clientId;
            var clientSecret = _clientSecret;

            // using Azure.Identity;
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, options);

            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            return graphClient;
        }

        private async Task<string> GetGraphDataAsync(string groupId, string channelId, string postId, bool isMsitEnables, string dataType)
        {
            string graphResponseData = "";

            if (isMsitEnabled)
            {
                string ENDPOINT = "https://graph.microsoft.com/beta/teams/";
                ENDPOINT += groupId + "/channels/" + channelId + "/messages/" + postId;

                if (dataType == "channelReplies")
                {
                    ENDPOINT += "/replies";
                    graphResponseData = await GetChannelReplyDataAsync(ENDPOINT);
                }
                else if (dataType == "channelPost")
                {
                    graphResponseData = await GetChannelPostDataAsync(ENDPOINT);
                }
            }
            else
            {
                var graphClient = GetGraphServiceClient();

                if (dataType == "channelReplies")
                {
                    var clientResponseData = await graphClient.Teams[groupId].Channels[channelId].Messages[postId].Replies.Request().GetResponseAsync();
                    graphResponseData = clientResponseData.Content.ReadAsStringAsync().Result;
                }
                else if (dataType == "channelPost")
                {
                    var clientResponseData = await graphClient.Teams[groupId].Channels[channelId].Messages[postId].Request().GetResponseAsync();
                    graphResponseData = clientResponseData.Content.ReadAsStringAsync().Result;
                }                
            }

            return graphResponseData;
        }

        private async Task<string> GetChannelReplyDataAsync(string ENDPOINT)
        {
            var token = _graphToken;
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, ENDPOINT);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var httpResponse = await httpClient.SendAsync(request);

            return await httpResponse.Content.ReadAsStringAsync();
        }

        private async Task<string> GetChannelPostDataAsync(string ENDPOINT)
        {
            var token = _graphToken;
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, ENDPOINT);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var httpResponse = await httpClient.SendAsync(request);

            return await httpResponse.Content.ReadAsStringAsync();
        }

        /*private GraphServiceClient GetGraphServiceClient1(string tenantId, string clientId, string clientSecret)
        {
            var scopes = new[] { "User.Read" };

            // For authorization code flow, the user signs into the Microsoft
            // identity platform, and the browser is redirected back to your app
            // with an authorization code in the query parameters
            var authorizationCode = "eyJ0eXAiOiJKV1QiLCJub25jZSI6IlNYbU9zQl9JYllsVWhWSG9jM2x0LTNZM0xVYjhOdGlhUXVRYkw2TFc4bEUiLCJhbGciOiJSUzI1NiIsIng1dCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyIsImtpZCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyJ9.eyJhdWQiOiIwMDAwMDAwMy0wMDAwLTAwMDAtYzAwMC0wMDAwMDAwMDAwMDAiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hMDBiM2Y4Yy05YTQyLTQyNjktODJjNi0xNDdmOGE3ZmNiZWYvIiwiaWF0IjoxNjc2NDk4NTQxLCJuYmYiOjE2NzY0OTg1NDEsImV4cCI6MTY3NjUwMzQyMiwiYWNjdCI6MCwiYWNyIjoiMSIsImFpbyI6IkFUUUF5LzhUQUFBQW41M0tOVE5CTXc2bjdYNVM5SWI2Z21NZUZreURIWS9ZakxTeGNPdUsxYjlIcEI1R3AyNzNKY1FDTEx2TVBJTVIiLCJhbXIiOlsicHdkIl0sImFwcF9kaXNwbGF5bmFtZSI6IkdyYXBoIEV4cGxvcmVyIiwiYXBwaWQiOiJkZThiYzhiNS1kOWY5LTQ4YjEtYThhZC1iNzQ4ZGE3MjUwNjQiLCJhcHBpZGFjciI6IjAiLCJmYW1pbHlfbmFtZSI6IkFkbWluaXN0cmF0b3IiLCJnaXZlbl9uYW1lIjoiTU9EIiwiaWR0eXAiOiJ1c2VyIiwiaXBhZGRyIjoiNDkuNDMuMjQwLjIwMSIsIm5hbWUiOiJNT0QgQWRtaW5pc3RyYXRvciIsIm9pZCI6ImU3ZmNkNzk5LTE0ZjgtNGU4Yi1iYjE5LThjZGRhNjY2M2E5NiIsInBsYXRmIjoiMyIsInB1aWQiOiIxMDAzMjAwMENBNzc3MkEwIiwicmgiOiIwLkFWd0FqRDhMb0VLYWFVS0N4aFJfaW5fTDd3TUFBQUFBQUFBQXdBQUFBQUFBQUFCY0FNUS4iLCJzY3AiOiJvcGVuaWQgcHJvZmlsZSBVc2VyLlJlYWQgZW1haWwgQ2hhbm5lbE1lc3NhZ2UuUmVhZC5BbGwiLCJzdWIiOiJXV2NYQnpIZTU1STVPaHV6MFVlbHV2bGJsNWxwaDYzeTJsenZzOU00TmtFIiwidGVuYW50X3JlZ2lvbl9zY29wZSI6IkVVIiwidGlkIjoiYTAwYjNmOGMtOWE0Mi00MjY5LTgyYzYtMTQ3ZjhhN2ZjYmVmIiwidW5pcXVlX25hbWUiOiJhZG1pbkBNMzY1eDM3MDkxMS5vbm1pY3Jvc29mdC5jb20iLCJ1cG4iOiJhZG1pbkBNMzY1eDM3MDkxMS5vbm1pY3Jvc29mdC5jb20iLCJ1dGkiOiJIQUEyeWIzNjJrMmtYb1VpaU5SQUFBIiwidmVyIjoiMS4wIiwid2lkcyI6WyI2MmU5MDM5NC02OWY1LTQyMzctOTE5MC0wMTIxNzcxNDVlMTAiLCI2OTA5MTI0Ni0yMGU4LTRhNTYtYWE0ZC0wNjYwNzViMmE3YTgiLCJiNzlmYmY0ZC0zZWY5LTQ2ODktODE0My03NmIxOTRlODU1MDkiXSwieG1zX3N0Ijp7InN1YiI6IklxeHB6c3hzMDlDZklJRjlHcnBDXzlpaENTUkpnTXg2UjhTVndjVlJCczgifSwieG1zX3RjZHQiOjE1OTI4OTk3OTgsInhtc190ZGJyIjoiRVUifQ.Lnl9psebB46k2_Tb_mMlcAdZAkRdwdLkjE57dmicHj0ZHHy46oeUgkNzBD_6EyOECKjc2bXA14daKhNiGFxkyo8zLIiO7RCGd0MSmSkpec9czEPBvpScx9vX6m9BkMeWhXS109xLB7kLfZsQT3vGRcc1FTUQ4-8hvEuJYl8WL-55IvnTXmNHy83udb6wspY7dIgJ3Y336p0Em887RPhhNjc9gO7guMcH-xoMejW1iJMFmYwdl2tkRLLRw_rI5zO6h0nT0vKFwAKv9DwrUR753joC6gABm3YKdjzWBuciHn5aLBEKpI9rRtFaAYAJhwfb2DjOcTeTrrv5Nfic-2Pxqw";

            // using Azure.Identity;
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // https://learn.microsoft.com/dotnet/api/azure.identity.authorizationcodecredential
            var authCodeCredential = new AuthorizationCodeCredential(
                tenantId, clientId, clientSecret, authorizationCode, options);

            var graphClient = new GraphServiceClient(authCodeCredential, scopes);

            return graphClient;
        }*/

        private async Task GetSingleMemberAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var member = new TeamsChannelAccount();

            try
            {
                member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken);
            }
            catch (ErrorResponseException e)
            {
                if (e.Body.Error.Code.Equals("MemberNotFoundInConversation", StringComparison.OrdinalIgnoreCase))
                {
                    await turnContext.SendActivityAsync("Member not found.");
                    return;
                }
                else
                {
                    throw e;
                }
            }

            var message = MessageFactory.Text($"You are: {member.Name}.");
            var res = await turnContext.SendActivityAsync(message);

        }

        private async Task DeleteCardActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.DeleteActivityAsync(turnContext.Activity.ReplyToId, cancellationToken);
        }

        private async Task MessageAllMembersAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var teamsChannelId = turnContext.Activity.TeamsGetChannelId();
            var serviceUrl = turnContext.Activity.ServiceUrl;
            var credentials = new MicrosoftAppCredentials(_appId, _appPassword);
            ConversationReference conversationReference = null;

            var members = await GetPagedMembers(turnContext, cancellationToken);

            foreach (var teamMember in members)
            {
                var proactiveMessage = MessageFactory.Text($"Hello {teamMember.GivenName} {teamMember.Surname}. I'm a Teams conversation bot.");

                var conversationParameters = new ConversationParameters
                {
                    IsGroup = false,
                    Bot = turnContext.Activity.Recipient,
                    Members = new ChannelAccount[] { teamMember },
                    TenantId = turnContext.Activity.Conversation.TenantId,
                };

                await ((CloudAdapter)turnContext.Adapter).CreateConversationAsync(
                    credentials.MicrosoftAppId,
                    teamsChannelId,
                    serviceUrl,
                    credentials.OAuthScope,
                    conversationParameters,
                    async (t1, c1) =>
                    {
                        conversationReference = t1.Activity.GetConversationReference();
                        await ((CloudAdapter)turnContext.Adapter).ContinueConversationAsync(
                            _appId,
                            conversationReference,
                            async (t2, c2) =>
                            {
                                await t2.SendActivityAsync(proactiveMessage, c2);
                            },
                            cancellationToken);
                    },
                    cancellationToken);
            }

            await turnContext.SendActivityAsync(MessageFactory.Text("All messages have been sent."), cancellationToken);
        }

        private static async Task<List<TeamsChannelAccount>> GetPagedMembers(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var members = new List<TeamsChannelAccount>();
            string continuationToken = null;

            do
            {
                var currentPage = await TeamsInfo.GetPagedMembersAsync(turnContext, 100, continuationToken, cancellationToken);
                continuationToken = currentPage.ContinuationToken;
                members = members.Concat(currentPage.Members).ToList();
            }
            while (continuationToken != null);

            return members;
        }

        private static async Task SendWelcomeCard(ITurnContext<IMessageActivity> turnContext, HeroCard card, CancellationToken cancellationToken)
        {
            var initialValue = new JObject { { "count", 0 } };
            card.Title = "Welcome!";
            card.Buttons.Add(new CardAction
            {
                Type = ActionTypes.MessageBack,
                Title = "Update Card",
                Text = "UpdateCardAction",
                Value = initialValue
            });

            var activity = MessageFactory.Attachment(card.ToAttachment());

            await turnContext.SendActivityAsync(activity, cancellationToken);
        }

        private static async Task SendUpdatedCard(ITurnContext<IMessageActivity> turnContext, HeroCard card, CancellationToken cancellationToken)
        {
            card.Title = "I've been updated";

            var data = turnContext.Activity.Value as JObject;
            data = JObject.FromObject(data);
            data["count"] = data["count"].Value<int>() + 1;
            card.Text = $"Update count - {data["count"].Value<int>()}";

            card.Buttons.Add(new CardAction
            {
                Type = ActionTypes.MessageBack,
                Title = "Update Card",
                Text = "UpdateCardAction",
                Value = data
            });

            var activity = MessageFactory.Attachment(card.ToAttachment());
            activity.Id = turnContext.Activity.ReplyToId;

            await turnContext.UpdateActivityAsync(activity, cancellationToken);
        }

        private async Task MentionAdaptiveCardActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var member = new TeamsChannelAccount();

            try
            {
                member = await TeamsInfo.GetMemberAsync(turnContext, turnContext.Activity.From.Id, cancellationToken);
            }
            catch (ErrorResponseException e)
            {
                if (e.Body.Error.Code.Equals("MemberNotFoundInConversation", StringComparison.OrdinalIgnoreCase))
                {
                    await turnContext.SendActivityAsync("Member not found.");
                    return;
                }
                else
                {
                    throw e;
                }
            }

            var templateJSON = System.IO.File.ReadAllText(_adaptiveCardTemplate);
            AdaptiveCardTemplate template = new AdaptiveCardTemplate(templateJSON);
            var memberData = new
            {
                userName = member.Name,
                userUPN = member.UserPrincipalName,
                userAAD = member.AadObjectId
            };
            string cardJSON = template.Expand(memberData);
            var adaptiveCardAttachment = new Bot.Schema.Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJSON),
            };
            await turnContext.SendActivityAsync(MessageFactory.Attachment(adaptiveCardAttachment), cancellationToken);
        }

        private async Task SendImmersiveReaderCardAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var cardJSON = System.IO.File.ReadAllText(_immersiveReaderCardTemplate);
            var adaptiveCardAttachment = new Bot.Schema.Attachment
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(cardJSON),
            };

            await turnContext.SendActivityAsync(MessageFactory.Attachment(adaptiveCardAttachment), cancellationToken);
        }

        private async Task MentionActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            var mention = new Mention
            {
                Mentioned = turnContext.Activity.From,
                Text = $"<at>{XmlConvert.EncodeName(turnContext.Activity.From.Name)}</at>",
            };

            var replyActivity = MessageFactory.Text($"Hello {mention.Text}.");
            replyActivity.Entities = new List<Bot.Schema.Entity> { mention };

            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }


        //-----Subscribe to Conversation Events in Bot integration
        protected override async Task OnTeamsChannelCreatedAsync(ChannelInfo channelInfo, Bot.Schema.Teams.TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var heroCard = new HeroCard(text: $"{channelInfo.Name} is the Channel created");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken);
        }

        protected override async Task OnTeamsChannelRenamedAsync(ChannelInfo channelInfo, Bot.Schema.Teams.TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var heroCard = new HeroCard(text: $"{channelInfo.Name} is the new Channel name");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken);
        }

        protected override async Task OnTeamsChannelDeletedAsync(ChannelInfo channelInfo, Bot.Schema.Teams.TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var heroCard = new HeroCard(text: $"{channelInfo.Name} is the Channel deleted");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken);
        }

        protected override async Task OnTeamsMembersRemovedAsync(IList<TeamsChannelAccount> membersRemoved, Bot.Schema.Teams.TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (TeamsChannelAccount member in membersRemoved)
            {
                if (member.Id == turnContext.Activity.Recipient.Id)
                {
                    // The bot was removed
                    // You should clear any cached data you have for this team
                }
                else
                {
                    var heroCard = new HeroCard(text: $"{member.Name} was removed from {teamInfo.Name}");
                    await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken);
                }
            }
        }

        protected override async Task OnTeamsTeamRenamedAsync(Bot.Schema.Teams.TeamInfo teamInfo, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var heroCard = new HeroCard(text: $"{teamInfo.Name} is the new Team name");
            await turnContext.SendActivityAsync(MessageFactory.Attachment(heroCard.ToAttachment()), cancellationToken);
        }
        protected override async Task OnReactionsAddedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var reaction in messageReactions)
            {
                var newReaction = $"You reacted with '{reaction.Type}' to the following message: '{turnContext.Activity.ReplyToId}'";
                var replyActivity = MessageFactory.Text(newReaction);
                await turnContext.SendActivityAsync(replyActivity, cancellationToken);
            }
        }

        protected override async Task OnReactionsRemovedAsync(IList<MessageReaction> messageReactions, ITurnContext<IMessageReactionActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var reaction in messageReactions)
            {
                var newReaction = $"You removed the reaction '{reaction.Type}' from the following message: '{turnContext.Activity.ReplyToId}'";
                var replyActivity = MessageFactory.Text(newReaction);
                await turnContext.SendActivityAsync(replyActivity, cancellationToken);
            }
        }

        // This method is invoked when message sent by user is updated in chat.
        protected override async Task OnTeamsMessageEditAsync(ITurnContext<IMessageUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var replyActivity = MessageFactory.Text("Message is updated");
            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }

        // This method is invoked when message sent by user is undeleted in chat.
        protected override async Task OnTeamsMessageUndeleteAsync(ITurnContext<IMessageUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            var replyActivity = MessageFactory.Text("Message is undeleted");
            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }

        // This method is invoked when message sent by user is soft deleted in chat.
        protected override async Task OnTeamsMessageSoftDeleteAsync(ITurnContext<IMessageDeleteActivity> turnContext, CancellationToken cancellationToken)
        {
            var replyActivity = MessageFactory.Text("Message is soft deleted");
            await turnContext.SendActivityAsync(replyActivity, cancellationToken);
        }
    }
}
