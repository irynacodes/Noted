using System.Text.Json;
using Noted.Helpers;
using Noted.LogicContext.Entities;

namespace Noted.LogicContext.DB
{
	public class InteractionDbContext
	{
        private readonly string _projectRoot = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        private readonly string _filePath;

        public InteractionDbContext(string[] paths)
        {
            var relativePath = Path.Combine(paths);
            _filePath = Path.Combine(_projectRoot, relativePath);
            FileHelper.CreateFile(_filePath);
        }

        public Dictionary<int, List<int>> GetData()
        {
            string? line;
            using (StreamReader inputFile = new StreamReader(_filePath))
            {
                line = inputFile.ReadLine();
            }

            if (line == null)
            {
                return new Dictionary<int, List<int>>();
            }

            return JsonSerializer.Deserialize<Dictionary<int, List<int>>>(line);
        }

        public List<int> GetUserData(User user)
        {
            return GetData().GetValueOrDefault(user.Id, new List<int>());
        }

        public void SaveUserData(int id, List<int> newData)
        {
            var data = GetData();
            data[id] = newData;

            var jsonData = JsonSerializer.Serialize(data);
            using (StreamWriter outputFile = new(_filePath))
            {
                outputFile.WriteLine(jsonData);
            }
        }
    }
}

