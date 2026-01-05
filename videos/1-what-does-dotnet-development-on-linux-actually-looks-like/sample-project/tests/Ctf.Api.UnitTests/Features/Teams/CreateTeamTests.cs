using Ctf.Api.Features.Teams;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Teams;

namespace Ctf.Api.UnitTests.Features.Teams;

public sealed class CreateTeamTests
{
    private static (
        Mock<IRoomMemberRepository>,
        Mock<ITeamRepository>,
        Mock<IValidator<CreateTeam.Command>>,
        CreateTeam.Handler,
        CreateTeam.Command
    ) Init()
    {
        var rmr = new Mock<IRoomMemberRepository>();
        var tr = new Mock<ITeamRepository>();
        var v = new Mock<IValidator<CreateTeam.Command>>();

        var h = new CreateTeam.Handler(rmr.Object, tr.Object, v.Object);
        var c = new CreateTeam.Command(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>());

        return (rmr, tr, v, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenCommandIsNotValidAsync()
    {
        // Arrange
        var (rmr, tr, v, h, c) = Init();
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
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Never);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenCreatorIsNotInRoomAsync()
    {
        // Arrange
        var (rmr, tr, v, h, c) = Init();
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
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Never);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Player)]
    [InlineData(RoomRole.Editor)]
    public async Task Handle_Should_ReturnForbidden_WhenCreatorRoleIsNotAdminAsync(
        RoomRole roomRole
    )
    {
        // Arrange
        var (rmr, tr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync(roomRole);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You must be an admin in this room to modify teams.");
        ;
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Never);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenNameAlreadyExistsInRoomAsync()
    {
        // Arrange
        var (rmr, tr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync((RoomRole)int.MaxValue);
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
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Once);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Create_WhenCommandIsValidAsync()
    {
        // Arrange
        var (rmr, tr, v, h, c) = Init();
        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.UserId)).ReturnsAsync((RoomRole)int.MaxValue);
        tr.Setup(r => r.NameInRoomExistsAsync(c.RoomId, c.Name)).ReturnsAsync(false);
        tr.Setup(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null))
            .ReturnsAsync(It.IsAny<Guid>());

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        v.Verify(v => v.ValidateAsync(c, default), Times.Once);
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.UserId), Times.Once);
        tr.Verify(r => r.NameInRoomExistsAsync(c.RoomId, c.Name), Times.Once);
        tr.Verify(r => r.CreateAsync(It.IsAny<CreateTeamDto>(), null), Times.Once);
    }
}
