namespace Shivers_Randomizer.room_randomizer;

public record WalkToRoom
{
    public Edge? IncomingEdge { get; init; }
    public RoomEnum? RoomId { get; set; }
};
