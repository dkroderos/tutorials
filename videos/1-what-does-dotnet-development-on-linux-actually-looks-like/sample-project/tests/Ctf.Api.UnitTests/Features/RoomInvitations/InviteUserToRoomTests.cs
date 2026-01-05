using Ctf.Api.Features.RoomInvitations;
using Ctf.Api.Repositories.RoomIntivations;
using Ctf.Api.Repositories.RoomMembers;
using Ctf.Api.Repositories.Rooms;
using Ctf.Api.Repositories.Users;

namespace Ctf.Api.UnitTests.Features.RoomInvitations;

public sealed class InviteUserToRoomTests
{
    private static (
        Mock<IRoomInvitationRepository>,
        Mock<IUserRepository>,
        Mock<IRoomMemberRepository>,
        Mock<IValidator<InviteUserToRoom.Command>>,
        InviteUserToRoom.Handler,
        InviteUserToRoom.Command
    ) Init()
    {
        var rir = new Mock<IRoomInvitationRepository>();
        var ur = new Mock<IUserRepository>();
        var rmr = new Mock<IRoomMemberRepository>();
        var v = new Mock<IValidator<InviteUserToRoom.Command>>();

        var h = new InviteUserToRoom.Handler(rir.Object, ur.Object, rmr.Object, v.Object);
        var c = new InviteUserToRoom.Command(
            It.IsAny<Guid>(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            It.IsAny<RoomRole>()
        );

        return (rir, ur, rmr, v, h, c);
    }

    [Fact]
    public async Task Handle_Should_ReturnBadRequest_WhenCommandIsInvalidAsync()
    {
        // Arrange
        var (rir, ur, rmr, v, h, c) = Init();

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
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.InviterId), Times.Never);
        ur.Verify(r => r.ExistsAsync(c.InviteeId), Times.Never);
        rir.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Never);
        rmr.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Never);
        rir.Verify(r => r.CreateAsync(It.IsAny<CreateRoomInvitationDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenInviterIsNotMemberAsync()
    {
        // Arrange
        var (rir, ur, rmr, v, h, c) = Init();

        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.InviterId)).ReturnsAsync((RoomRole?)null);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("Room was not found.");
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.InviterId), Times.Once);
        ur.Verify(r => r.ExistsAsync(c.InviteeId), Times.Never);
        rir.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Never);
        rmr.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Never);
        rir.Verify(r => r.CreateAsync(It.IsAny<CreateRoomInvitationDto>()), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Editor)]
    [InlineData(RoomRole.Player)]
    public async Task Handle_Should_ReturnBadRequest_WhenInviterRoleIsLowerThanAdminRoleAsync(
        RoomRole inviterRole
    )
    {
        // Arrange
        var (rir, ur, rmr, v, h, c) = Init();

        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.InviterId)).ReturnsAsync(inviterRole);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("Not allowed to invite users in the specified room.");
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.InviterId), Times.Once);
        ur.Verify(r => r.ExistsAsync(c.InviteeId), Times.Never);
        rir.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Never);
        rmr.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Never);
        rir.Verify(r => r.CreateAsync(It.IsAny<CreateRoomInvitationDto>()), Times.Never);
    }

    [Theory]
    [InlineData(RoomRole.Owner, RoomRole.Owner)]
    [InlineData(RoomRole.Admin, RoomRole.Admin)]
    public async Task Handle_Should_ReturnBadRequest_WhenInivterRoleIsNotLowerThanTheRoleForTheInviteeAsync(
        RoomRole inviterRole,
        RoomRole inviteeRole
    )
    {
        // Arrange
        var (rir, ur, rmr, v, h, _) = Init();
        var c = new InviteUserToRoom.Command(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            inviteeRole
        );

        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.InviterId)).ReturnsAsync(inviterRole);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status403Forbidden);
        a.Error.Detail.Should().Be("You can only invite users with a lower role than yours.");
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.InviterId), Times.Once);
        ur.Verify(r => r.ExistsAsync(c.InviteeId), Times.Never);
        rir.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Never);
        rmr.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Never);
        rir.Verify(r => r.CreateAsync(It.IsAny<CreateRoomInvitationDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenInviteeDoesNotExistsAsync()
    {
        // Arrange
        var (rir, ur, rmr, v, h, c) = Init();

        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.InviterId)).ReturnsAsync((RoomRole)int.MaxValue);
        ur.Setup(r => r.ExistsAsync(c.InviteeId)).ReturnsAsync(false);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status404NotFound);
        a.Error.Detail.Should().Be("The user to invite was not found.");
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.InviterId), Times.Once);
        ur.Verify(r => r.ExistsAsync(c.InviteeId), Times.Once);
        rir.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Never);
        rmr.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Never);
        rir.Verify(r => r.CreateAsync(It.IsAny<CreateRoomInvitationDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenInviteeAlreadyInvitedAsync()
    {
        // Arrange
        var (rir, ur, rmr, v, h, c) = Init();

        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.InviterId)).ReturnsAsync((RoomRole)int.MaxValue);
        ur.Setup(r => r.ExistsAsync(c.InviteeId)).ReturnsAsync(true);
        rir.Setup(r => r.ExistsAsync(c.RoomId, c.InviteeId)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should().Be("The user is already a invited in the room.");
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.InviterId), Times.Once);
        ur.Verify(r => r.ExistsAsync(c.InviteeId), Times.Once);
        rir.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Once);
        rmr.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Never);
        rir.Verify(r => r.CreateAsync(It.IsAny<CreateRoomInvitationDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenInviteeAlreadyMemberAsync()
    {
        // Arrange
        var (rir, ur, rmr, v, h, c) = Init();

        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.InviterId)).ReturnsAsync((RoomRole)int.MaxValue);
        ur.Setup(r => r.ExistsAsync(c.InviteeId)).ReturnsAsync(true);
        rir.Setup(r => r.ExistsAsync(c.RoomId, c.InviteeId)).ReturnsAsync(false);
        rmr.Setup(r => r.ExistsAsync(c.RoomId, c.InviteeId)).ReturnsAsync(true);

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeFalse();
        a.Error.Code.Should().Be(StatusCodes.Status409Conflict);
        a.Error.Detail.Should().Be("The user is already a member of the room.");
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.InviterId), Times.Once);
        ur.Verify(r => r.ExistsAsync(c.InviteeId), Times.Once);
        rir.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Once);
        rmr.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Once);
        rir.Verify(r => r.CreateAsync(It.IsAny<CreateRoomInvitationDto>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_Create_WhenCommandIsValidAsync()
    {
        // Arrange
        var (rir, ur, rmr, v, h, c) = Init();

        v.Setup(v => v.ValidateAsync(c, default)).ReturnsAsync(new ValidationResult());
        rmr.Setup(r => r.GetRoleAsync(c.RoomId, c.InviterId)).ReturnsAsync((RoomRole)int.MaxValue);
        ur.Setup(r => r.ExistsAsync(c.InviteeId)).ReturnsAsync(true);
        rir.Setup(r => r.ExistsAsync(c.RoomId, c.InviteeId)).ReturnsAsync(false);
        rmr.Setup(r => r.ExistsAsync(c.RoomId, c.InviteeId)).ReturnsAsync(false);
        rir.Setup(r => r.CreateAsync(It.IsAny<CreateRoomInvitationDto>()));

        // Act
        var a = await h.Handle(c);

        // Assert
        a.IsSuccess.Should().BeTrue();
        rmr.Verify(r => r.GetRoleAsync(c.RoomId, c.InviterId), Times.Once);
        ur.Verify(r => r.ExistsAsync(c.InviteeId), Times.Once);
        rir.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Once);
        rmr.Verify(r => r.ExistsAsync(c.RoomId, c.InviteeId), Times.Once);
        rir.Verify(r => r.CreateAsync(It.IsAny<CreateRoomInvitationDto>()), Times.Once);
    }
}
