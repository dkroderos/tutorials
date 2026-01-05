using Ctf.Api.Features.Teams;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.UnitTests.Features.Teams;

public sealed class PlayAsSoloTests
{
    private static (
        Mock<IRoomMemberRepository>,
        Mock<IRoomRepository>,
        Mock<ITeamRepository>,
        Mock<IValidator<PlayAsSolo.Command>>,
        PlayAsSolo.Handler,
        PlayAsSolo.Command
    ) Init()
    {
        var rmr = new Mock<IRoomMemberRepository>();
        var rr = new Mock<IRoomRepository>();
        var tr = new Mock<ITeamRepository>();
        var v = new Mock<IValidator<PlayAsSolo.Command>>();

        var h = new PlayAsSolo.Handler(rmr.Object, rr.Object, tr.Object, v.Object);
        var c = new PlayAsSolo.Command(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>());

        return (rmr, rr, tr, v, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenCommandIsNotValidAsync()
    {
        // Arrange
        var (rmr, rr, tr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default))
            .ReturnsAsync(
                new ValidationResult(
                    [new ValidationFailure(It.IsAny<string>(), It.IsAny<string>())]
                )
            );

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Never);
        rr.Verify(r => r.AllowsPlayerCreatedTeamsAsync(c.RoomId), Times.Never);
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Never);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenUserIsNotInRoomAsync()
    {
        // Arrange
        var (rmr, rr, tr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Room was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        rr.Verify(r => r.AllowsPlayerCreatedTeamsAsync(c.RoomId), Times.Never);
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Never);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Editor)]
    [InlineData(RoomRole.Admin)]
    [InlineData(RoomRole.Owner)]
    public async Task Handle_Should_ReturnForbidden_WhenUserRoleIsNotPlayerAsync(RoomRole roomRole)
    {
        // Arrange
        var (rmr, rr, tr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync(roomRole);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should()
            .Be("The candidate must have a player role in order to be added in a team.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        rr.Verify(r => r.AllowsPlayerCreatedTeamsAsync(c.RoomId), Times.Never);
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Never);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenAllowsUserCreatedTeamsWasNotFoundAsync()
    {
        // Arrange
        var (rmr, rr, tr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.AllowsPlayerCreatedTeamsAsync(c.RoomId)).ReturnsAsync((bool?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Room was not found.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        rr.Verify(r => r.AllowsPlayerCreatedTeamsAsync(c.RoomId), Times.Once);
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Never);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenPlayerCreatedTeamIsNotAllowedAsync()
    {
        // Arrange
        var (rmr, rr, tr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.AllowsPlayerCreatedTeamsAsync(c.RoomId)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("Player created teams are not allowed in this room.");
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        rr.Verify(r => r.AllowsPlayerCreatedTeamsAsync(c.RoomId), Times.Once);
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Never);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenNameAlreadyExistsInRoomAsync()
    {
        // Arrange
        var (rmr, rr, tr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.AllowsPlayerCreatedTeamsAsync(c.RoomId)).ReturnsAsync(true);
        tr.Setup(r => r.NameInRoomExistsAsync(c.RoomId, c.Name)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should()
            .Be(
                "There's already a team in the room with this name. Please choose a different name."
            );
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        rr.Verify(r => r.AllowsPlayerCreatedTeamsAsync(c.RoomId), Times.Once);
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Once);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Create_WhenCommandIsValidAsync()
    {
        // Arrange
        var (rmr, rr, tr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync(RoomRole.Player);
        rr.Setup(r => r.AllowsPlayerCreatedTeamsAsync(c.RoomId)).ReturnsAsync(true);
        tr.Setup(r => r.NameInRoomExistsAsync(c.RoomId, c.Name)).ReturnsAsync(false);
        tr.Setup(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), c.UserId))
            .ReturnsAsync(It.IsAny<Guid>());

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        rr.Verify(r => r.AllowsPlayerCreatedTeamsAsync(c.RoomId), Times.Once);
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Once);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), c.UserId), Times.Once);
    }
}
