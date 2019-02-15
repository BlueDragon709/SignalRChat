using System;
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
            ConnectedUsers.Remove(currentUser);
            await Clients.Caller.SendAsync("update", "You have connected to room: ", name);
            await Clients.Group(name).SendAsync("update-people", JsonConvert.SerializeObject(UsersInRoom));
        }

        public async Task JoinRoom(string RoomName)
        {
            User currentUser = ConnectedUsers.Single(r => r.ID == Context.ConnectionId);
            Room currentRoom = AllRooms.Single(r => r.RoomName == RoomName);
            currentRoom.UsersInRoom.Add(currentUser);
            await Groups.AddToGroupAsync(currentRoom.RoomName, currentUser.ID);
            ConnectedUsers.Remove(currentUser);
            await Clients.Caller.SendAsync("update", "You have connected to room: ", currentRoom.RoomName);
            await Clients.OthersInGroup(currentRoom.RoomName).SendAsync("update", currentUser.Name + " has connected to the room", currentRoom.RoomName);
            await Clients.Group(currentRoom.RoomName).SendAsync("update-people", JsonConvert.SerializeObject(currentRoom.UsersInRoom));
        }

        public async Task PeopleTyping(bool check)
        {
            User currentUser = ConnectedUsers.Single(r => r.ID == Context.ConnectionId);
            if (check)
            {
                await Clients.Others.SendAsync("typing", currentUser.Name, "is typing...");
            }
            else
            {
                await Clients.Others.SendAsync("not-typing", "");
            }
        }

        public async Task Send(string msg) {
            User currentUser = ConnectedUsers.Single(r => r.ID == Context.ConnectionId);
            await Clients.All.SendAsync("chat", currentUser.Name, msg);
            await Clients.All.SendAsync("not-typing", "");
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            User itemToRemove = ConnectedUsers.Single(r => r.ID == Context.ConnectionId);
            ConnectedUsers.Remove(itemToRemove);

            Clients.Others.SendAsync("update", itemToRemove.Name + " has left the server.");
            Clients.All.SendAsync("update-people", JsonConvert.SerializeObject(ConnectedUsers));
            return base.OnDisconnectedAsync(exception);
        }
    }
}
