namespace Shivers_Randomizer.room_randomizer;

public record RoomTransition
(
    int From,
    int DefaultTo,
    int NewTo,
    int? ElevatorFloor
);
