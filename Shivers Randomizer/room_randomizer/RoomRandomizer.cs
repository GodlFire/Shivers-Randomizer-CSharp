using Shivers_Randomizer.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Shivers_Randomizer.room_randomizer;
public class RoomRandomizer
{
    private readonly App app;
    private readonly Random rng;
    private readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private Dictionary<RoomEnum, Room> map = new();

    public RoomRandomizer(App app, Random rng)
    {
        this.app = app;
        this.rng = rng;
    }

    public RoomTransition[] RandomizeMap()
    {
        bool validMap = false;
        while (!validMap)
        {
            try
            {
                map = JsonSerializer.Deserialize<Dictionary<RoomEnum, Room>>(Resources.DefaultMap, options) ?? throw new Exception();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load default map.", ex);
            }

            if (!app.settingsIncludeElevators)
            {
                map[RoomEnum.UNDERGROUND_ELEVATOR].AvailableOutgoingEdges.Clear();
                map[RoomEnum.UNDERGROUND_ELEVATOR].AvailableIncomingEdges.Clear();
                map[RoomEnum.LAKE_TUNNEL].AvailableOutgoingEdges.RemoveAt(1);
                map[RoomEnum.LAKE_TUNNEL].AvailableIncomingEdges.RemoveAt(1);
                map[RoomEnum.OFFICE].AvailableOutgoingEdges.RemoveAt(2);
                map[RoomEnum.OFFICE].AvailableIncomingEdges.RemoveAt(2);
                map[RoomEnum.OFFICE].AvailableOutgoingEdges.RemoveAt(0);
                map[RoomEnum.OFFICE].AvailableIncomingEdges.RemoveAt(0);

                map[RoomEnum.BEDROOM_ELEVATOR].AvailableOutgoingEdges.Clear();
                map[RoomEnum.BEDROOM_ELEVATOR].AvailableIncomingEdges.Clear();
                map[RoomEnum.BEDROOM_HALLWAY].AvailableOutgoingEdges.RemoveAt(0);
                map[RoomEnum.BEDROOM_HALLWAY].AvailableIncomingEdges.RemoveAt(0);

                map[RoomEnum.THREE_FLOOR_ELEVATOR].AvailableOutgoingEdges.Clear();
                map[RoomEnum.THREE_FLOOR_ELEVATOR].AvailableIncomingEdges.Clear();
                map[RoomEnum.MAINTENANCE_TUNNEL].AvailableOutgoingEdges.RemoveAt(1);
                map[RoomEnum.MAINTENANCE_TUNNEL].AvailableIncomingEdges.RemoveAt(1);
                map[RoomEnum.BACK_HALLWAYS].AvailableOutgoingEdges.RemoveRange(3, 2);
                map[RoomEnum.BACK_HALLWAYS].AvailableIncomingEdges.RemoveRange(3, 2);
            }

            if (app.settingsRedDoor) {
                map[RoomEnum.ANANSI].WalkToRoom = null;
            } else {
                map[RoomEnum.ANANSI].AvailableOutgoingEdges.RemoveAt(0);
            }

            if (BuildMap())
            {
                validMap = map.Values.Where(room => room.WalkToRoom != null).All(WalkRoom);
            }
        }

         return map.Values.SelectMany(room =>
            room.DefaultMoves.Select(defaultMove =>
            {
                int from = room.Id == RoomEnum.THREE_FLOOR_ELEVATOR ? 34010 : defaultMove.Key;
                int newTo = room.Moves.TryGetValue(defaultMove.Key, out Move? newMove) ? newMove.Id : 0;
                return new RoomTransition(from, defaultMove.Value.Id, newTo, newMove?.ElevatorFloor);
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

    private void ConnectRooms(Room outgoingRoom, Room incomingRoom)
    {
        Edge outgoingEdge = outgoingRoom.AvailableOutgoingEdges[rng.Next(outgoingRoom.AvailableOutgoingEdges.Count)];
        List<Edge> incomingEdges = incomingRoom.AvailableIncomingEdges.Where(edge => edge != new Edge(outgoingEdge.First, outgoingEdge.Second)).ToList();
        Edge incomingEdge = incomingEdges[rng.Next(incomingEdges.Count)];

        outgoingRoom.Moves[outgoingEdge.First] = new Move(incomingEdge.First, incomingRoom.Id);
        outgoingRoom.AvailableOutgoingEdges.Remove(outgoingEdge);
        incomingRoom.AvailableIncomingEdges.Remove(incomingEdge);

        if (incomingEdge.Second.HasValue)
        {
            if (incomingRoom.Id == RoomEnum.THREE_FLOOR_ELEVATOR)
            {
                outgoingRoom.Moves[outgoingEdge.First].ElevatorFloor = incomingEdge.Second.Value - 34010;
            }
            else if (incomingRoom.Id == RoomEnum.BEDROOM_ELEVATOR)
            {
                outgoingRoom.Moves[outgoingEdge.First].ElevatorFloor = incomingEdge.Second.Value == 38011 ? 1 : 2;
            }
        }

        if (incomingRoom.WalkToRoom?.IncomingEdge?.First == incomingEdge.First && !incomingRoom.WalkToRoom.RoomId.HasValue)
        {
            incomingRoom.WalkToRoom.RoomId = outgoingRoom.Id;
        }

        if (outgoingEdge.Second.HasValue)
        {
            if (incomingEdge.Second.HasValue)
            {
                incomingRoom.Moves[incomingEdge.Second.Value] = new Move(outgoingEdge.Second.Value, outgoingRoom.Id);
                outgoingRoom.AvailableIncomingEdges.Remove(new Edge(outgoingEdge.Second.Value, outgoingEdge.First));
                incomingRoom.AvailableOutgoingEdges.Remove(new Edge(incomingEdge.Second.Value, incomingEdge.First));

                if (outgoingRoom.Id == RoomEnum.THREE_FLOOR_ELEVATOR)
                {
                    incomingRoom.Moves[incomingEdge.Second.Value].ElevatorFloor = outgoingEdge.First - 34010;
                }
                else if (outgoingRoom.Id == RoomEnum.BEDROOM_ELEVATOR)
                {
                    incomingRoom.Moves[incomingEdge.Second.Value].ElevatorFloor = outgoingEdge.First == 38011 ? 1 : 2;
                }
            }
            else
            {
                outgoingRoom.AvailableIncomingEdges.Remove(new Edge(outgoingEdge.Second.Value, outgoingEdge.First));
            }

            if (outgoingRoom.WalkToRoom?.IncomingEdge?.First == outgoingEdge.Second && !outgoingRoom.WalkToRoom.RoomId.HasValue)
            {
                outgoingRoom.WalkToRoom.RoomId = incomingRoom.Id;
            }
        }
        else if (incomingEdge.Second.HasValue)
        {
            // TODO: Won't happen unless slide doesn't go to library again
            incomingRoom.Moves[incomingEdge.Second.Value] = new Move(-1, RoomEnum.INVALID);
            incomingRoom.AvailableOutgoingEdges.Remove(new Edge(incomingEdge.Second.Value, incomingEdge.First));
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
        room.Moves.Where(move => move.Key != room.WalkToRoom?.IncomingEdge?.Second).ToList().ForEach(move =>
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
            if (room.Id != RoomEnum.SKULL_DOOR)
            {
                return false;
            }
            else
            {
                // Skull door might have no skull dials behind it.
                visitedRooms.ForEach(room => room.Visited = false);
                visitedRooms.Clear();
                room.Moves.Where(move => move.Key == room.WalkToRoom?.IncomingEdge?.Second).ToList().ForEach(move =>
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
            if (roomToProcess.DefaultMoves.Count == roomToProcess.Moves.Count)
            {
                roomToProcess.Moves.Where(move => move.Key != roomToProcess.WalkToRoom?.IncomingEdge?.Second).ToList().ForEach(move =>
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
                List<KeyValuePair<int, Move>> moves = roomToProcess.Moves.Concat(missingMoves).Where(move => move.Key != roomToProcess.WalkToRoom?.IncomingEdge?.Second).ToList();
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
