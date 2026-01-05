using Ctf.Api.Features.RoomInvitations;
using Ctf.Api.Repositories.RoomIntivations;

namespace Ctf.Api.UnitTests.Features.RoomInvitations;

public sealed class GetReceivedInvitesTests
{
    private static (
        Mock<IRoomInvitationRepository>,
        GetReceivedInvites.Handler,
        GetReceivedInvites.Query
    ) Init()
    {
        var r = new Mock<IRoomInvitationRepository>();

        var h = new GetReceivedInvites.Handler(r.Object);
        var q = new GetReceivedInvites.Query(
            It.IsAny<Guid>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<int>()
        );

        return (r, h, q);
    }

    [Fact]
    public async Task Handle_Should_GetReceivedInvitesAsync()
    {
        // Arrange
        var (r, h, q) = Init();
        r.Setup(r =>
                r.GetReceivedAsync(
                    q.UserId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                )
            )
            .ReturnsAsync(PagedList<ReceivedRoomInvitationDto>.Empty());

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeTrue();
        r.Verify(
            r =>
                r.GetReceivedAsync(
                    q.UserId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                ),
            Times.Once
        );
    }
}
