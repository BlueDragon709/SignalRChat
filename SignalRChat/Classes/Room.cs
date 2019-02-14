using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRChat.Classes
{
    public class Room
    {
        public string RoomName { get; set; }
        public virtual ICollection<User> UsersInRoom { get; set; }
    }
}
