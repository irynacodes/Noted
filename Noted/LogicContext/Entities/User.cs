using System.Text;
using System.Security.Cryptography;
using Noted.LogicContext.DB;
using Noted.Exceptions;
using System.Text.Json.Serialization;

namespace Noted.LogicContext.Entities
{
    public class User
	{
        public static UserDbContext? UserDbContext { get; set; } = new();
        private static InteractionDbContext? FriendsDbContext { get; } = new (new string[] { "LogicContext", "Db", "Storage", "Friends.json" });
        private static InteractionDbContext? BlockedDbContext { get; } = new (new string[] { "LogicContext", "Db", "Storage", "Blocked.json" });
        private static InteractionDbContext? LibraryDbContext { get; } = new (new string[] { "LogicContext", "Db", "Storage", "Library.json" });

        public int Id { get; }
		public byte[] HashedPassword { get; set; }
		public string Name { get; set; }
		public string Username { get; set; }
        public DateTime Birthday { get; set; }

        [JsonConstructor]
		public User(int Id, byte[] HashedPassword, string Name, string Username, DateTime Birthday)
		{
			this.Id = Id;
			this.HashedPassword = HashedPassword;
            this.Name = Name;
            this.Username = Username;
            this.Birthday = Birthday;
		}

        public User(string username, string password, string name, DateTime birthday,
                    UserDbContext userDbContext, PlaylistDbContext playlistDbContext)
        {
            if (userDbContext.UsernameExists(username))
            {
                throw new EntityAlreadyExistsException<User>(username);
            }

            Username = username;
            HashedPassword = new MD5CryptoServiceProvider().ComputeHash(ASCIIEncoding.ASCII.GetBytes(password));
            Name = name;
            Birthday = birthday;
            Id = userDbContext.GetIdToSet();

            FriendsDbContext?.SaveUserData(Id, new List<int>());
            BlockedDbContext?.SaveUserData(Id, new List<int>());
            LibraryDbContext?.SaveUserData(Id, new List<int>());
            playlistDbContext.SaveUserData(Id, new List<int>());
        }

        public void AddFriend(User user)
        {
            if (Username == user.Username)
            {
                Console.WriteLine("Love yourself, but not too much!");
                return;
            }
            
            if (!IsFriends(user))
            {
                var friends = FriendsDbContext.GetUserData(this);
                friends.Add(user.Id);
                FriendsDbContext.SaveUserData(Id, friends);
                Console.WriteLine($"User {user.Username} added to friends.");
            }
        }

        public void RemoveFriend(User? user)
        {
            if (IsFriends(user))
            {
                var friends = FriendsDbContext.GetUserData(this);
                friends.Remove(user.Id);
                FriendsDbContext.SaveUserData(Id, friends);
                Console.WriteLine($"User {user.Username} removed from friends.");
            }
        }

        public void Block(User user)
        {
            if (Username == user.Username)
            {
                Console.WriteLine("Can't escape from yourself!");
                return;
            }
            
            if (!IsBlocked(user))
            {
                var blocked = BlockedDbContext.GetUserData(this);
                blocked.Add(user.Id);
                BlockedDbContext.SaveUserData(Id, blocked);
                RemoveFriend(user);
                Console.WriteLine($"User {user.Username} blocked.");
            }
        }

        public void Unblock(User user)
        {
            if (IsBlocked(user))
            {
                var blocked = BlockedDbContext.GetUserData(this);
                blocked.Remove(user.Id);
                BlockedDbContext.SaveUserData(Id, blocked);
                Console.WriteLine($"User {user.Username} unblocked.");
            }
        }

        public void AddSong(Song song)
        {
            if (HasSong(song))
            {
                Console.WriteLine("Song is already in the library.");
                return;
            }
            var songs = LibraryDbContext.GetUserData(this);
            songs.Add(song.Id);
            LibraryDbContext.SaveUserData(Id, songs);
            Console.WriteLine($"{song.Name} successfully added.");
        }

        public void DeleteSong(Song song)
        {
            if (!HasSong(song))
            {
                Console.WriteLine("Song is not in the library.");
                return;
            }
            var songs = LibraryDbContext.GetUserData(this);
            songs.Remove(song.Id);
            LibraryDbContext.SaveUserData(Id, songs);
            Console.WriteLine($"{song.Name} successfully removed.");
        }

        public bool IsFriends(User user)
        {
            var friends = FriendsDbContext.GetUserData(this);
            return friends.Contains(user.Id);
        }

        public bool IsBlocked(User user)
        {
            var blocked = BlockedDbContext.GetUserData(this);
            return blocked.Contains(user.Id);
        }

        public bool HasSong(Song song)
        {
            var songs = LibraryDbContext.GetUserData(this);
            return songs.Contains(song.Id);
        }

        public bool HashCorresponds(byte[] hashedInput)
        {
            return HashedPassword.Length == hashedInput.Length && HashedPassword.SequenceEqual(hashedInput);
        }
    }
}

