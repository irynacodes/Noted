using System.Text.Json;
using Noted.LogicContext.Entities;
using Noted.Helpers;

namespace Noted.LogicContext.DB
{
	public class UserDbContext
	{
        private readonly string _projectRoot = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        private readonly string _relativePath = Path.Combine("LogicContext", "DB", "Storage", "Users.json");
        private readonly string _filePath;

        public UserDbContext()
		{
            _filePath = Path.Combine(_projectRoot, _relativePath);
            FileHelper.CreateFile(_filePath);
        }

        public void SaveUser(User user)
        {
            var users = ReadUsers();
            users.Add(user);
            var jsonUsers = JsonSerializer.Serialize(users);
            using (StreamWriter outputFile = new(_filePath))
            {
                outputFile.WriteLine(jsonUsers);
            }
        }

        public void RemoveUser(User user)
        {
            var users = ReadUsers();
            users.Remove(user);
            var jsonUsers = JsonSerializer.Serialize(users);
            using (StreamWriter outputFile = new(_filePath))
            {
                outputFile.WriteLine(jsonUsers);
            }
        }

        public void EditUser(User newUser, string oldUsername)
        {
            var users = ReadUsers();
            User? oldUser = null;
            foreach (var user in users)
            {
                if (user.Username == oldUsername)
                {
                    oldUser = user;
                }
            }
            users.Remove(oldUser);
            users.Add(newUser);
            var jsonUsers = JsonSerializer.Serialize(users);
            using (StreamWriter outputFile = new(_filePath))
            {
                outputFile.WriteLine(jsonUsers);
            }
        }

        public void SaveUsers(List<User> users)
        {
            var existingUsers = ReadUsers();
            existingUsers = existingUsers.Concat(users).ToList();

            var jsonUsers = JsonSerializer.Serialize(existingUsers);
            using (StreamWriter outputFile = new(_filePath))
            {
                outputFile.WriteLine(jsonUsers);
            }
        }

        public List<User> ReadUsers()
        {
            string? line;
            using (StreamReader inputFile = new StreamReader(_filePath))
            {
                line = inputFile.ReadLine();
            }

            if (line == null)
            {
                return new List<User>();
            }

            return JsonSerializer.Deserialize<List<User>>(line);
        }

        public User? FindUserByUsername(string username)
        {
            return ReadUsers().Find(user => user.Username.ToLower() == username.ToLower());
        }

        public User? FindUserById(int id)
        {
            return ReadUsers().Find(user => user.Id == id);
        }

        public bool UsernameExists(string username)
        {
            return ReadUsers().Find(user => user.Username.ToLower() == username.ToLower()) is not null;
        }

        public int GetIdToSet()
        {
            var users = ReadUsers();
            if (users.Count == 0)
            {
                return 0;
            }

            return users.OrderByDescending(user => user.Id).First().Id + 1;
        }
    }
}

