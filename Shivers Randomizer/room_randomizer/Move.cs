namespace Shivers_Randomizer.room_randomizer;

public record Move
(
    int Id,
    RoomEnum RoomId
)
{
    public int? ElevatorFloor { get; set; }
};
