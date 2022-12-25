using System.Collections.Generic;

namespace Shivers_Randomizer.room_randomizer;

public record Room
{
    public string Name { get; init; } = "";

    public RoomEnum Id { get; init; } = RoomEnum.INVALID;

    public List<Edge> AvailableOutgoingEdges { get; init; } = new();

    public List<Edge> AvailableIncomingEdges { get; init; } = new();

    public Dictionary<int, Move> DefaultMoves { get; init; } = new();

    public Dictionary<int, Move> Moves { get; init; } = new();

    public WalkToRoom? WalkToRoom { get; set; }

    public bool HasSkull { get; set; } = false;

    public bool Visited { get; set; } = false;
}
