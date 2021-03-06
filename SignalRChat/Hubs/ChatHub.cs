﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SignalRChat.Classes;
using Newtonsoft.Json;

namespace SignalRChat.Hubs
{
    
    public class ChatHub : Hub
    {
        static List<User> ConnectedUsers = new List<User>();
        static List<User> UsersInRoom;
        static List<Room> AllRooms = new List<Room>();


        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public async Task SendMessage(string user, string message)
        {
            await Clients.All.SendAsync("ReceiveMessage", user, message);
        }

        public async Task Join(string name)
        {

            var id = Context.ConnectionId;
            ConnectedUsers.Add(new User {ID = id, Name = name});
            await Clients.Caller.SendAsync("update-rooms", JsonConvert.SerializeObject(AllRooms));
            // Logic for non room implementation
            //await Clients.Caller.SendAsync("update", "You have connected to the server.");
            //await Clients.Others.SendAsync("update", name + " has joined the server.");
            //await Clients.All.SendAsync("update-people", JsonConvert.SerializeObject(ConnectedUsers));
        }

        public async Task CreateRoom(string name)
        {
            User currentUser = ConnectedUsers.Single(r => r.ID == Context.ConnectionId);
            UsersInRoom = new List<User>();
            UsersInRoom.Add(currentUser);
            AllRooms.Add(new Room { RoomName = name, UsersInRoom = UsersInRoom});
            await Groups.AddToGroupAsync(currentUser.ID, name);
            currentUser.Room = name;
            await Clients.Caller.SendAsync("update-roomName", name);
            await Clients.Caller.SendAsync("update", "You have connected to room: " + name);
            await Clients.Group(name).SendAsync("update-people", JsonConvert.SerializeObject(UsersInRoom));
        }

        public async Task UpdateRooms()
        {
            await Clients.Caller.SendAsync("update-rooms", JsonConvert.SerializeObject(AllRooms));
        }

        public async Task JoinRoom(string RoomName)
        {
            User currentUser = ConnectedUsers.Single(r => r.ID == Context.ConnectionId);
            Room currentRoom = AllRooms.Single(r => r.RoomName == RoomName);
            await Groups.AddToGroupAsync(currentUser.ID, currentRoom.RoomName);
            currentRoom.UsersInRoom.Add(currentUser);
            currentUser.Room = currentRoom.RoomName;
            await Clients.Caller.SendAsync("update-roomName", currentRoom.RoomName);
            await Clients.Caller.SendAsync("update", "You have connected to room: " + currentRoom.RoomName);
            await Clients.OthersInGroup(currentRoom.RoomName).SendAsync("update", currentUser.Name + " has connected to the room");
            await Clients.Group(currentRoom.RoomName).SendAsync("update-people", JsonConvert.SerializeObject(currentRoom.UsersInRoom));
        }

        public async Task PeopleTyping(bool check)
        {
            User currentUser = ConnectedUsers.Single(r => r.ID == Context.ConnectionId);
            if (check)
            {
                await Clients.OthersInGroup(currentUser.Room).SendAsync("typing", currentUser.Name, "is typing...");
            }
            else
            {
                await Clients.OthersInGroup(currentUser.Room).SendAsync("not-typing", "");
            }
        }

        public async Task Send(string msg) {
            User currentUser = ConnectedUsers.Single(r => r.ID == Context.ConnectionId);
            await Clients.Group(currentUser.Room).SendAsync("chat", currentUser.Name, msg);
            await Clients.Group(currentUser.Room).SendAsync("not-typing", "");
        }

        public async Task LeaveRoom(string room)
        {
            User currentUser = ConnectedUsers.Single(r => r.ID == Context.ConnectionId);
            Room RoomToLeave = AllRooms.Single(r => r.RoomName == room);
            RoomToLeave.UsersInRoom.Remove(currentUser);
            if (RoomToLeave.UsersInRoom.Count() == 0)
            {
                AllRooms.Remove(RoomToLeave);
            }
            else
            {
                await Clients.OthersInGroup(RoomToLeave.RoomName).SendAsync("update", currentUser.Name + " has left the room.");
                await Groups.RemoveFromGroupAsync(currentUser.ID, room);
                await Clients.Group(RoomToLeave.RoomName).SendAsync("update-people", JsonConvert.SerializeObject(RoomToLeave.UsersInRoom));
            }
            currentUser.Room = null;
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            User itemToRemove = ConnectedUsers.Single(r => r.ID == Context.ConnectionId);
            if (itemToRemove.Room != null)
            {
                Room RoomToRemoveUser = AllRooms.Single(r => r.RoomName == itemToRemove.Room);
                RoomToRemoveUser.UsersInRoom.Remove(itemToRemove);
                Clients.OthersInGroup(RoomToRemoveUser.RoomName).SendAsync("update", itemToRemove.Name + " has left the server.");
                Clients.Group(RoomToRemoveUser.RoomName).SendAsync("update-people", JsonConvert.SerializeObject(RoomToRemoveUser.UsersInRoom));
            }

            ConnectedUsers.Remove(itemToRemove);

            //Clients.Others.SendAsync("update", itemToRemove.Name + " has left the server.");
            //Clients.All.SendAsync("update-people", JsonConvert.SerializeObject(ConnectedUsers));
            return base.OnDisconnectedAsync(exception);
        }
    }
}
