using MantenimientoMaquinasAPI.Domain.Contracts;
using Microsoft.Extensions.AI;
using Moq;
using Xunit;

namespace MantenimientoMaquinasAPI.Application.Agents;

public class AgentServiceTests
{
    private readonly Mock<IChatClient> _chatClientMock;
    private readonly Mock<IMachinesRespositoryWR> _repositoryMock;
    private readonly AgentService _agentService;

    public AgentServiceTests()
    {
        _chatClientMock = new Mock<IChatClient>();
        _repositoryMock = new Mock<IMachinesRespositoryWR>();

        var machinesTools = new MachinesAgentTools(
            _repositoryMock.Object);

        _agentService = new AgentService(
            _chatClientMock.Object,
            machinesTools);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AskAsync_PreguntaVacia_LanzaExcepcion(
        string message)
    {
        // Act
        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => _agentService.AskAsync(message));

        // Assert
        Assert.Equal("message", exception.ParamName);

        _chatClientMock.Verify(
            c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AskAsync_PreguntaValida_DevuelveRespuesta()
    {
        // Arrange
        const string expectedAnswer =
            "La excavadora necesita revisión por temperatura.";

        _chatClientMock
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new ChatResponse(
                    new ChatMessage(
                        ChatRole.Assistant,
                        expectedAnswer)));

        // Act
        var result = await _agentService.AskAsync(
            "¿Qué máquinas están en riesgo?");

        // Assert
        Assert.Equal(expectedAnswer, result);

        _chatClientMock.Verify(
            c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AskAsync_ClienteIAFalla_PropagaExcepcion()
    {
        // Arrange
        _chatClientMock
            .Setup(c => c.GetResponseAsync(
                It.IsAny<IEnumerable<ChatMessage>>(),
                It.IsAny<ChatOptions>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(
                new InvalidOperationException(
                    "Error simulado del cliente de IA."));

        // Act y Assert
        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _agentService.AskAsync(
                    "¿Qué máquinas están en riesgo?"));

        Assert.Equal(
            "Error simulado del cliente de IA.",
            exception.Message);
    }
}

