using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shivers_Randomizer_x64.room_randomizer;

public record Room
{
    public string Name { get; init; } = "";

    public int Id { get; init; } = -1;

    public List<Edge> AvailableOutgoingEdges { get; init; } = new();

    public List<Edge> AvailableIncomingEdges { get; init; } = new();

    public Dictionary<int, Move> DefaultMoves { get; init; } = new();

    public Dictionary<int, Move> Moves { get; init; } = new();

    public WalkToRoom? WalkToRoom { get; set; }

    public bool HasSkull { get; set; } = false;

    public bool Visited { get; set; } = false;
}
