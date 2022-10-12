namespace Shivers_Randomizer_x64.room_randomizer;

public record WalkToRoom
{
    public Edge? IncomingEdge { get; init; }
    public int? RoomId { get; set; }
};
