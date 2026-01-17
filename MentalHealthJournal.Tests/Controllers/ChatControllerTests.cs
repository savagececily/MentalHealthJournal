using MentalHealthJournal.Models;
using MentalHealthJournal.Server.Controllers;
using MentalHealthJournal.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace MentalHealthJournal.Tests.Controllers
{
    /// <summary>
    /// Unit tests for ChatController
    /// Tests authentication, authorization, input validation, and error handling.
    /// </summary>
    public class ChatControllerTests
    {
        private readonly Mock<ILogger<ChatController>> _loggerMock;
        private readonly Mock<IChatService> _chatServiceMock;
        private readonly ChatController _controller;
        private const string TestUserId = "test-user-123";

        public ChatControllerTests()
        {
            _loggerMock = new Mock<ILogger<ChatController>>();
            _chatServiceMock = new Mock<IChatService>();

            _controller = new ChatController(
                _chatServiceMock.Object,
                _loggerMock.Object);

            // Setup authenticated user
            SetupAuthenticatedUser(TestUserId);
        }

        private void SetupAuthenticatedUser(string userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, "test@example.com")
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region SendMessage Tests

        [Fact]
        public async Task SendMessage_WithValidRequest_ReturnsOkWithResponse()
        {
            // Arrange
            var request = new ChatRequest
            {
                Message = "I've been feeling anxious lately"
            };

            var expectedResponse = new ChatResponse
            {
                SessionId = "session-123",
                Message = "I hear you're feeling anxious. Can you tell me more about what's been happening?",
                Timestamp = DateTime.UtcNow
            };

            _chatServiceMock.Setup(s => s.SendMessageAsync(TestUserId, request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal(expectedResponse.SessionId, response.SessionId);
            Assert.Equal(expectedResponse.Message, response.Message);
            _chatServiceMock.Verify(s => s.SendMessageAsync(TestUserId, request), Times.Once);
        }

        [Fact]
        public async Task SendMessage_WithExistingSession_ReturnsOkWithResponse()
        {
            // Arrange
            var request = new ChatRequest
            {
                Message = "Thank you for listening",
                SessionId = "session-123"
            };

            var expectedResponse = new ChatResponse
            {
                SessionId = "session-123",
                Message = "You're welcome. I'm here to support you.",
                Timestamp = DateTime.UtcNow
            };

            _chatServiceMock.Setup(s => s.SendMessageAsync(TestUserId, request))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ChatResponse>(okResult.Value);
            Assert.Equal(expectedResponse.SessionId, response.SessionId);
        }

        [Fact]
        public async Task SendMessage_WithEmptyMessage_ReturnsBadRequest()
        {
            // Arrange
            var request = new ChatRequest
            {
                Message = ""
            };

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Message cannot be empty", badRequestResult.Value);
            _chatServiceMock.Verify(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<ChatRequest>()), Times.Never);
        }

        [Fact]
        public async Task SendMessage_WithWhitespaceMessage_ReturnsBadRequest()
        {
            // Arrange
            var request = new ChatRequest
            {
                Message = "   "
            };

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Message cannot be empty", badRequestResult.Value);
        }

        [Fact]
        public async Task SendMessage_WhenServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            var request = new ChatRequest
            {
                Message = "Hello"
            };

            _chatServiceMock.Setup(s => s.SendMessageAsync(TestUserId, request))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while processing your message", statusCodeResult.Value);
        }

        #endregion

        #region GetSession Tests

        [Fact]
        public async Task GetSession_WithValidSessionId_ReturnsOkWithSession()
        {
            // Arrange
            var sessionId = "session-123";
            var expectedSession = new ChatSession
            {
                Id = sessionId,
                UserId = TestUserId,
                Title = "Test Conversation",
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "user", Content = "Hello", Timestamp = DateTime.UtcNow },
                    new ChatMessage { Role = "assistant", Content = "Hi! How can I help?", Timestamp = DateTime.UtcNow }
                },
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow,
                IsActive = true
            };

            _chatServiceMock.Setup(s => s.GetSessionAsync(TestUserId, sessionId))
                .ReturnsAsync(expectedSession);

            // Act
            var result = await _controller.GetSession(sessionId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var session = Assert.IsType<ChatSession>(okResult.Value);
            Assert.Equal(sessionId, session.Id);
            Assert.Equal(TestUserId, session.UserId);
            Assert.Equal(2, session.Messages.Count);
        }

        [Fact]
        public async Task GetSession_WithNonExistentSession_ReturnsNotFound()
        {
            // Arrange
            var sessionId = "non-existent-session";

            _chatServiceMock.Setup(s => s.GetSessionAsync(TestUserId, sessionId))
                .ReturnsAsync((ChatSession?)null);

            // Act
            var result = await _controller.GetSession(sessionId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Session not found", notFoundResult.Value);
        }

        [Fact]
        public async Task GetSession_WhenServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            var sessionId = "session-123";

            _chatServiceMock.Setup(s => s.GetSessionAsync(TestUserId, sessionId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetSession(sessionId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving the session", statusCodeResult.Value);
        }

        #endregion

        #region GetSessions Tests

        [Fact]
        public async Task GetSessions_WithExistingSessions_ReturnsOkWithSessionsList()
        {
            // Arrange
            var expectedSessions = new List<ChatSession>
            {
                new ChatSession
                {
                    Id = "session-1",
                    UserId = TestUserId,
                    Title = "Conversation 1",
                    LastMessageAt = DateTime.UtcNow,
                    IsActive = true
                },
                new ChatSession
                {
                    Id = "session-2",
                    UserId = TestUserId,
                    Title = "Conversation 2",
                    LastMessageAt = DateTime.UtcNow.AddHours(-1),
                    IsActive = true
                }
            };

            _chatServiceMock.Setup(s => s.GetUserSessionsAsync(TestUserId))
                .ReturnsAsync(expectedSessions);

            // Act
            var result = await _controller.GetSessions();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var sessions = Assert.IsType<List<ChatSession>>(okResult.Value);
            Assert.Equal(2, sessions.Count);
            Assert.All(sessions, s => Assert.Equal(TestUserId, s.UserId));
        }

        [Fact]
        public async Task GetSessions_WithNoSessions_ReturnsOkWithEmptyList()
        {
            // Arrange
            _chatServiceMock.Setup(s => s.GetUserSessionsAsync(TestUserId))
                .ReturnsAsync(new List<ChatSession>());

            // Act
            var result = await _controller.GetSessions();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var sessions = Assert.IsType<List<ChatSession>>(okResult.Value);
            Assert.Empty(sessions);
        }

        [Fact]
        public async Task GetSessions_WhenServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            _chatServiceMock.Setup(s => s.GetUserSessionsAsync(TestUserId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetSessions();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving sessions", statusCodeResult.Value);
        }

        #endregion

        #region DeleteSession Tests

        [Fact]
        public async Task DeleteSession_WithValidSessionId_ReturnsNoContent()
        {
            // Arrange
            var sessionId = "session-123";

            _chatServiceMock.Setup(s => s.DeleteSessionAsync(TestUserId, sessionId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.DeleteSession(sessionId);

            // Assert
            Assert.IsType<NoContentResult>(result);
            _chatServiceMock.Verify(s => s.DeleteSessionAsync(TestUserId, sessionId), Times.Once);
        }

        [Fact]
        public async Task DeleteSession_WhenServiceThrows_ReturnsInternalServerError()
        {
            // Arrange
            var sessionId = "session-123";

            _chatServiceMock.Setup(s => s.DeleteSessionAsync(TestUserId, sessionId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.DeleteSession(sessionId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while deleting the session", statusCodeResult.Value);
        }

        #endregion

        #region Authentication Tests

        [Fact]
        public async Task SendMessage_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var request = new ChatRequest
            {
                Message = "Hello"
            };

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetSession_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.GetSession("session-123");

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetSessions_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.GetSessions();

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task DeleteSession_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.DeleteSession("session-123");

            // Assert
            Assert.IsType<UnauthorizedResult>(result);
        }

        #endregion
    }
}
