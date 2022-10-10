using Shivers_Randomizer_x64.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;

namespace Shivers_Randomizer_x64.room_randomizer;
public class RoomRandomizer
{
    private Dictionary<int, Room> map = new();
    private readonly Random rng;
    private readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public RoomRandomizer(Random rng)
    {
        this.rng = rng;
    }

    public RoomTransition[] RandomizeMap()
    {
        bool validMap = false;
        while (!validMap)
        {
            try
            {
                map = JsonSerializer.Deserialize<Dictionary<int, Room>>(Resources.DefaultMap, options) ?? throw new Exception();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load default map.", ex);
            }

            if (BuildMap())
            {
                validMap = map.Values.Where(room => room.WalkToRoom != null).All(WalkRoom);
            }
        }

         return map.Values.SelectMany(room =>
            room.DefaultMoves.Select(defaultMove =>
            {
                int from = defaultMove.Key / 10 == 3401 ? 34010 : defaultMove.Key;
                int newTo = room.Moves.TryGetValue(defaultMove.Key, out Move? temp) ? temp.Id : 0;
                return new RoomTransition(from, defaultMove.Value.Id, newTo);
            })
            .Where(transition => transition.NewTo != 0 && transition.DefaultTo != transition.NewTo)
        ).ToArray();
    }

    private bool BuildMap()
    {
        List<Room> singleEntranceRooms = map.Values.Where(room =>
            room.AvailableIncomingEdges.Count == 1 || room.AvailableIncomingEdges.Count > 1 && !room.AvailableOutgoingEdges.Any()
        ).ToList();
        List<Room> availableRooms = map.Values.Where(room => room.AvailableIncomingEdges.Count != 1 && room.AvailableOutgoingEdges.Any()).ToList();

        while (availableRooms.Any())
        {
            if (singleEntranceRooms.Any())
            {
                Room singleRoom = singleEntranceRooms[rng.Next(singleEntranceRooms.Count)];
                Room availableRoom = availableRooms[rng.Next(availableRooms.Count)];
                ConnectRooms(availableRoom, singleRoom);
                ModifyRoomLists(availableRoom, availableRooms, singleEntranceRooms);

                if (!singleRoom.AvailableIncomingEdges.Any())
                {
                    singleEntranceRooms.Remove(singleRoom);

                    if (singleRoom.AvailableOutgoingEdges.Any())
                    {
                        availableRooms.Add(singleRoom);
                    }
                }
            }
            else
            {
                List<Room> sRooms = availableRooms.Where(room => room.AvailableIncomingEdges.Any()).ToList();
                Room singleRoom = sRooms[rng.Next(sRooms.Count)];
                List<Room> aRooms = availableRooms.Where(room => room != singleRoom).ToList();
                Room availableRoom = aRooms.Any() ? aRooms[rng.Next(aRooms.Count)] : availableRooms.First();
                ConnectRooms(availableRoom, singleRoom);
                ModifyRoomLists(availableRoom, availableRooms, singleEntranceRooms);
                ModifyRoomLists(singleRoom, availableRooms, singleEntranceRooms);
            }
        }

        if (singleEntranceRooms.Any() && singleEntranceRooms.Count == 2 && singleEntranceRooms.First() != singleEntranceRooms.Last())
        {
            Room? room1 = singleEntranceRooms.FirstOrDefault(room => room.AvailableOutgoingEdges.Any());
            if (room1 == null)
            {
                return false;
            }

            Room room2 = singleEntranceRooms.FirstOrDefault(room => room != room1) ?? singleEntranceRooms.First();
            ConnectRooms(room1, room2);
            singleEntranceRooms.Remove(room1);
            singleEntranceRooms.Remove(room2);
        }
        else if (singleEntranceRooms.Any())
        {
            return false;
        }

        return true;
    }

    private void ConnectRooms(Room availableRoom, Room singleRoom)
    {
        Edge aROutgoing = availableRoom.AvailableOutgoingEdges[rng.Next(availableRoom.AvailableOutgoingEdges.Count)];
        List<Edge> sRIncomingEdges = singleRoom.AvailableIncomingEdges.Where(edge => edge != new Edge(aROutgoing.First, aROutgoing.Second)).ToList();
        Edge sRIncoming = sRIncomingEdges[rng.Next(sRIncomingEdges.Count)];

        availableRoom.Moves[aROutgoing.First] = new Move(sRIncoming.First, singleRoom.Id);
        availableRoom.AvailableOutgoingEdges.Remove(aROutgoing);
        singleRoom.AvailableIncomingEdges.Remove(sRIncoming);

        if (singleRoom.WalkToRoom?.IncomingEdge?.First == sRIncoming.First)
        {
            singleRoom.WalkToRoom.RoomId = availableRoom.Id;
        }

        if (aROutgoing.Second.HasValue)
        {
            if (sRIncoming.Second.HasValue)
            {
                singleRoom.Moves[sRIncoming.Second.Value] = new Move(aROutgoing.Second.Value, availableRoom.Id);
                availableRoom.AvailableIncomingEdges.Remove(new Edge(aROutgoing.Second.Value, aROutgoing.First));
                singleRoom.AvailableOutgoingEdges.Remove(new Edge(sRIncoming.Second.Value, sRIncoming.First));
            }
            else
            {
                availableRoom.AvailableIncomingEdges.Remove(new Edge(aROutgoing.Second.Value, aROutgoing.First));
            }

            if (availableRoom.WalkToRoom?.IncomingEdge?.First == aROutgoing.Second)
            {
                availableRoom.WalkToRoom.RoomId = singleRoom.Id;
            }
        }
        else if (sRIncoming.Second.HasValue)
        {
            // TODO: Won't happen unless slide doesn't go to library again
            singleRoom.Moves[sRIncoming.Second.Value] = new Move(-1, -1);
            singleRoom.AvailableOutgoingEdges.Remove(new Edge(sRIncoming.Second.Value, sRIncoming.First));
        }
    }

    private static void ModifyRoomLists(Room availableRoom, List<Room> availableRooms, List<Room> singleEntranceRooms)
    {
        if (availableRoom.AvailableIncomingEdges.Count == 1 || availableRoom.AvailableIncomingEdges.Count > 1 && !availableRoom.AvailableOutgoingEdges.Any())
        {
            availableRooms.Remove(availableRoom);
            singleEntranceRooms.Add(availableRoom);
        }
        else if (!availableRoom.AvailableIncomingEdges.Any() && !availableRoom.AvailableOutgoingEdges.Any())
        {
            availableRooms.Remove(availableRoom);
        }
    }

    private bool WalkRoom(Room room)
    {
        Queue<Room> queue = new();
        room.Visited = true;
        List<Room> visitedRooms = new() { room };
        room.Moves.Where(move => move.Key != room.WalkToRoom?.IncomingEdge.Second).ToList().ForEach(move =>
        {
            Room nextRoom = map[move.Value.RoomId];
            queue.Enqueue(nextRoom);
        });

        while (queue.Any() && queue.Peek().Id != room.WalkToRoom?.RoomId)
        {
            ProcessNextRoom(queue, visitedRooms);
        }

        if (!queue.Any())
        {
            if (room.Id != 42)
            {
                return false;
            }
            else
            {
                // Skull door might have no skull dials behind it.
                visitedRooms.ForEach(room => room.Visited = false);
                visitedRooms.Clear();
                room.Moves.Where(move => move.Key == room.WalkToRoom?.IncomingEdge.Second).ToList().ForEach(move =>
                {
                    Room nextRoom = map[move.Value.RoomId];
                    queue.Enqueue(nextRoom);
                });

                while (queue.Any() && !queue.Peek().HasSkull)
                {
                    ProcessNextRoom(queue, visitedRooms);
                }

                if (queue.Any())
                {
                    return false;
                }

                visitedRooms.ForEach(room => room.Visited = false);
                return true;
            }
        }

        visitedRooms.ForEach(room => room.Visited = false);
        return true;
    }

    private void ProcessNextRoom(Queue<Room> queue, List<Room> visitedRooms)
    {
        Room roomToProcess = queue.Dequeue();
        if (!roomToProcess.Visited)
        {
            roomToProcess.Visited = true;
            visitedRooms.Add(roomToProcess);
            if (roomToProcess.DefaultMoves.Count == roomToProcess.Moves.Count) {
                roomToProcess.Moves.Where(move => move.Key != roomToProcess.WalkToRoom?.IncomingEdge.Second).ToList().ForEach(move =>
                {
                    Room nextRoom = map[move.Value.RoomId];
                    if (!nextRoom.Visited)
                    {
                        queue.Enqueue(nextRoom);
                    }
                });
            }
            else
            {
                IEnumerable<KeyValuePair<int, Move>> missingMoves = roomToProcess.DefaultMoves.Where(move => !roomToProcess.Moves.ContainsKey(move.Key));
                List<KeyValuePair<int, Move>> moves = roomToProcess.Moves.Concat(missingMoves).ToList();
                moves.ForEach(move =>
                {
                    Room nextRoom = map[move.Value.RoomId];
                    if (!nextRoom.Visited)
                    {
                        queue.Enqueue(nextRoom);
                    }
                });
            }
        }
    }
}
