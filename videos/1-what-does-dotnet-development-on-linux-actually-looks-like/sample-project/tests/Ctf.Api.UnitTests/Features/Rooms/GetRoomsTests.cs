using Ctf.Api.Features.Rooms;
using Ctf.Api.Repositories.Rooms;

namespace Ctf.Api.UnitTests.Features.Rooms;

public sealed class GetRoomsTests
{
    private static (Mock<IRoomRepository>, GetRooms.Handler, GetRooms.Query) Init()
    {
        var r = new Mock<IRoomRepository>();

        var h = new GetRooms.Handler(r.Object);
        var q = new GetRooms.Query(
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
    public async Task Handle_Should_GetAccessibleRoomsAsync()
    {
        // Arrange
        var (r, h, q) = Init();
        r.Setup(r =>
                r.QueryAsync(
                    q.UserId,
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>(),
                    It.IsAny<int>(),
                    It.IsAny<int>()
                )
            )
            .ReturnsAsync(PagedList<RoomDto>.Empty());

        // Act
        var a = await h.Handle(q);

        // Assert
        a.IsSuccess.Should().BeTrue();
        r.Verify(
            r =>
                r.QueryAsync(
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
