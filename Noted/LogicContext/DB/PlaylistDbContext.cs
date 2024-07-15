using System.Text.Json;
using Noted.Helpers;
using Noted.LogicContext.Entities;

namespace Noted.LogicContext.DB
{
    public class PlaylistDbContext
    {
        private User? _user;
        
        private readonly string _projectRoot = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        private readonly string _relativePath = Path.Combine("LogicContext", "DB", "Storage", "Playlist.json");
        private readonly string _filePath;

        public PlaylistDbContext()
        {
            _filePath = Path.Combine(_projectRoot, _relativePath);
            FileHelper.CreateFile(_filePath);
        }

        public void SetUser(User user)
        {
            _user = user;
        }

        public void SaveUserData(int userId, List<int> songIds)
        {
            var playlist = ReadPlaylist();
            playlist[userId] = songIds;
            SavePlaylist(playlist);
        }

        public Dictionary<int, List<int>> ReadPlaylist()
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

        public void SaveSong(User user, Song song)
        {
            var playlist = ReadPlaylist();
            List<int>? usersEntry;
            if (!playlist.TryGetValue(user.Id, out usersEntry))
            {
                Console.WriteLine($"User {user.Username} has no entries in the playlist.");
                return;
            }
            usersEntry.Add(song.Id);
            playlist[user.Id] = usersEntry;
            SavePlaylist(playlist);
            if (_user?.Id == user.Id)
            {
                Console.WriteLine("Song successfully added to the playlist.");
            }
        }

        public bool SongInPlaylist(Song song)
        {
            var playlist = ReadPlaylist();
            var songs = playlist.Values.SelectMany(v => v);
            return songs.Contains(song.Id);
        }
        
        private void SavePlaylist(Dictionary<int, List<int>> playlist)
        {
            var jsonPlaylist = JsonSerializer.Serialize(playlist);
            using (StreamWriter outputFile = new(_filePath))
            {
                outputFile.WriteLine(jsonPlaylist);
            }
        }
    }
}

