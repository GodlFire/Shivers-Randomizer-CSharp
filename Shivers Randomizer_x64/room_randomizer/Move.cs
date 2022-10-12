namespace Shivers_Randomizer_x64.room_randomizer;

public record Move
(
    int Id,
    int RoomId
)
{
    public int? ElevatorFloor { get; set; }
};
