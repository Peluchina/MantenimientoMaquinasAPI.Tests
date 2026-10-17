using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Moq;
using MantenimientoMaquinasAPI.Application.Agents;
using MantenimientoMaquinasAPI.Domain.Agregates;
using MantenimientoMaquinasAPI.Domain.Contracts;

namespace MantenimientoMaquinasAPI.Tests;

// Agrega aquí los using de los namespaces reales de:
// MachinesAgentTools, MachineRiskResult, IMachinesRespositoryWR y Machines.

public class MachinesAgentToolsTests
{
    private readonly Mock<IMachinesRespositoryWR> _repositoryMock;
    private readonly MachinesAgentTools _tools;

    public MachinesAgentToolsTests()
    {
        _repositoryMock = new Mock<IMachinesRespositoryWR>();
        _tools = new MachinesAgentTools(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetMachinesAtRiskAsync_TemperaturaNormal_NoIncluyeMaquina()
    {
        // Arrange
        var machines = new List<Machines>
        {
            new()
            {
                MachineId = 1,
                MachineName = "Excavadora",
                CurrentTemperature = 70m,
                MaxTemperature = 100m,
                MachineStatus = "OPERATIVA"
            }
        };

        _repositoryMock
            .Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(machines);

        // Act
        var result = await _tools.GetMachinesAtRiskAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetMachinesAtRiskAsync_TemperaturaIgualAlMaximo_IncluyeMaquina()
    {
        // Arrange
        var machines = new List<Machines>
        {
            new()
            {
                MachineId = 2,
                MachineName = "Perforadora",
                CurrentTemperature = 100m,
                MaxTemperature = 100m,
                MachineStatus = "OPERATIVA"
            }
        };

        _repositoryMock
            .Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(machines);

        // Act
        var result = await _tools.GetMachinesAtRiskAsync();

        // Assert
        var machine = Assert.Single(result);
        Assert.Equal(2, machine.MachineId);
        Assert.Contains("temperatura máxima", machine.Reason);
    }

    [Fact]
    public async Task GetMachinesAtRiskAsync_TemperaturaSuperiorAlMaximo_IncluyeMaquina()
    {
        // Arrange
        var machines = new List<Machines>
        {
            new()
            {
                MachineId = 3,
                MachineName = "Cargador",
                CurrentTemperature = 110m,
                MaxTemperature = 100m,
                MachineStatus = "OPERATIVA"
            }
        };

        _repositoryMock
            .Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(machines);

        // Act
        var result = await _tools.GetMachinesAtRiskAsync();

        // Assert
        var machine = Assert.Single(result);
        Assert.Equal(110m, machine.CurrentTemperature);
        Assert.Equal(100m, machine.MaxTemperature);
    }

    [Fact]
    public async Task GetMachinesAtRiskAsync_EstadoFalla_IncluyeMaquina()
    {
        // Arrange
        var machines = new List<Machines>
        {
            new()
            {
                MachineId = 4,
                MachineName = "Trituradora",
                CurrentTemperature = 60m,
                MaxTemperature = 100m,
                MachineStatus = "FALLA"
            }
        };

        _repositoryMock
            .Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(machines);

        // Act
        var result = await _tools.GetMachinesAtRiskAsync();

        // Assert
        var machine = Assert.Single(result);
        Assert.Equal("Trituradora", machine.MachineName);
        Assert.Equal("La máquina figura en estado FALLA.", machine.Reason);
    }

    [Fact]
    public async Task GetMachinesAtRiskAsync_SinMaquinasEnRiesgo_DevuelveListaVacia()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Machines>());

        // Act
        var result = await _tools.GetMachinesAtRiskAsync();

        // Assert
        Assert.Empty(result);
    }


    [Fact]
    public async Task GetMachinesAtRiskAsync_TemperaturaMaximaCero_NoIncluyePorTemperatura()
    {
        // Arrange: preparamos una máquina con temperatura máxima cero.
        var machines = new List<Machines>
    {
        new()
        {
            MachineId = 5,
            MachineName = "Excavadora de prueba",
            CurrentTemperature = 50m,
            MaxTemperature = 0m,
            MachineStatus = "OPERATIVA"
        }
    };

        _repositoryMock
            .Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(machines);

        // Act: ejecutamos el método real del agente.
        var result = await _tools.GetMachinesAtRiskAsync();

        // Assert: no debe considerarla en riesgo por temperatura.
        Assert.Empty(result);
    }


    [Fact]
    public async Task GetMachinesAtRiskAsync_EstadoFallaEnMinusculas_IncluyeMaquina()
    {
        // Arrange
        var machines = new List<Machines>
    {
        new()
        {
            MachineId = 6,
            MachineName = "Trituradora de prueba",
            CurrentTemperature = 50m,
            MaxTemperature = 100m,
            MachineStatus = "falla"
        }
    };

        _repositoryMock
            .Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(machines);

        // Act
        var result = await _tools.GetMachinesAtRiskAsync();

        // Assert
        var machine = Assert.Single(result);

        Assert.Equal(6, machine.MachineId);
        Assert.Equal(
            "La máquina figura en estado FALLA.",
            machine.Reason);
    }

    [Fact]
    public async Task GetMachinesAtRiskAsync_ConsultaRepositorioUnaSolaVez()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Machines>());

        // Act
        await _tools.GetMachinesAtRiskAsync();

        // Assert
        _repositoryMock.Verify(
            r => r.GetAll(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetMachinesAtRiskAsync_SiRepositorioFalla_PropagaLaExcepcion()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetAll(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "Error al consultar las máquinas."));

        // Act y Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _tools.GetMachinesAtRiskAsync());

        Assert.Equal(
            "Error al consultar las máquinas.",
            exception.Message);
    }


}
