using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MultiAgentPlatform.Application.Services;
using MultiAgentPlatform.Application.Wizard;
using MultiAgentPlatform.Domain.Entities;
using MultiAgentPlatform.Domain.Enums;
using MultiAgentPlatform.Infrastructure.Persistence;

namespace MultiAgentPlatform.Web.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly MainDbContext _dbContext;
    private readonly IWizardService _wizardService;
    private readonly IApplicationBuilderService _applicationBuilderService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        MainDbContext dbContext,
        IWizardService wizardService,
        IApplicationBuilderService applicationBuilderService,
        ILogger<ChatHub> logger)
    {
        _dbContext = dbContext;
        _wizardService = wizardService;
        _applicationBuilderService = applicationBuilderService;
        _logger = logger;
    }

    public async Task SendMessage(Guid conversationId, string message)
    {
        try
        {
            var userId = Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                throw new HubException("Unable to resolve the current user.");
            }

            var conversation = await _dbContext.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId, Context.ConnectionAborted);

            if (conversation is null || conversation.UserId != userId)
            {
                throw new HubException("Conversation not found.");
            }

            var state = DeserializeState(conversation.WizardStateJson);

            _dbContext.ChatMessages.Add(new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderType = SenderType.User,
                Content = message,
                CorrelationId = Guid.NewGuid().ToString("N")
            });

            var result = await _wizardService.ProcessUserMessageAsync(state, message, Context.ConnectionAborted);

            _dbContext.ChatMessages.Add(new ChatMessage
            {
                ConversationId = conversation.Id,
                SenderType = SenderType.Assistant,
                Content = result.AssistantMessage,
                CorrelationId = Guid.NewGuid().ToString("N")
            });

            conversation.WizardStateJson = JsonSerializer.Serialize(result.State);

            Guid? createdApplicationId = null;
            string? createdApplicationSlug = null;
            string? createdApplicationName = null;

            if (result.ReadyToBuild && result.State.Confirmed && conversation.GeneratedApplicationId is null)
            {
                var summary = _wizardService.BuildSummary(result.State);
                var createdApp = _applicationBuilderService.BuildFromSummary(summary, userId);

                _dbContext.GeneratedApplications.Add(createdApp);

                conversation.GeneratedApplicationId = createdApp.Id;

                _dbContext.DeploymentLogs.Add(new DeploymentLog
                {
                    GeneratedApplicationId = createdApp.Id,
                    Action = DeploymentAction.Created,
                    Details = $"Application '{createdApp.Name}' generated from wizard conversation {conversation.Id}."
                });

                createdApplicationId = createdApp.Id;
                createdApplicationSlug = createdApp.Slug;
                createdApplicationName = createdApp.Name;
            }

            await _dbContext.SaveChangesAsync(Context.ConnectionAborted);

            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                sender = "assistant",
                content = result.AssistantMessage,
                timestamp = DateTimeOffset.UtcNow
            });

            if (createdApplicationId is not null)
            {
                await Clients.Caller.SendAsync("ApplicationReady", new
                {
                    applicationId = createdApplicationId,
                    slug = createdApplicationSlug,
                    name = createdApplicationName
                });
            }
        }
        catch (HubException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chat message for conversation {ConversationId}.", conversationId);

            await Clients.Caller.SendAsync("ReceiveMessage", new
            {
                sender = "assistant",
                content = "Sorry, something went wrong processing that message. Please try again.",
                timestamp = DateTimeOffset.UtcNow
            });
        }
    }

    private static WizardState DeserializeState(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "{}")
        {
            return new WizardState();
        }

        try
        {
            return JsonSerializer.Deserialize<WizardState>(json) ?? new WizardState();
        }
        catch (JsonException)
        {
            return new WizardState();
        }
    }
}
