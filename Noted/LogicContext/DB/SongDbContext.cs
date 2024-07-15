using System;
using System.Text.Json;
using Noted.Helpers;
using Noted.LogicContext.Entities;
using Noted.Seeding;

namespace Noted.LogicContext.DB
{
    public class SongDbContext
    {
        private readonly string _projectRoot = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        private readonly string _relativePath = Path.Combine("LogicContext", "DB", "Storage", "Songs.json");
        private readonly string _filePath;

        public SongDbContext()
        {
            _filePath = Path.Combine(_projectRoot, _relativePath);
            FileHelper.CreateFile(_filePath);
        }

        public void SaveSongs()
        {
            var seeding = new SongSeeding();
            var songs = seeding.ReadSongsFromDir();
            var jsonSongs = seeding.GetSerializedString(songs);
            using (StreamWriter outputFile = new(_filePath))
            {
                outputFile.WriteLine(jsonSongs);
            }
        }

        public List<Song> ReadSongs()
        {
            string? line;
            using (StreamReader inputFile = new StreamReader(_filePath))
            {
                line = inputFile.ReadLine();
            }

            if (line == null)
            {
                return new List<Song>();
            }

            return JsonSerializer.Deserialize<List<Song>>(line);
        }

        public Song? FindSongById(int id)
        {
            return ReadSongs().Find(song => song.Id == id);
        }

        public Song? FindSongByName(string name)
        {
            return ReadSongs().Find(song => song.Name.ToLower() == name.ToLower());
        }

        public bool SongExists(string locationPath)
        {
            return ReadSongs().Find(song => song.LocationPath == locationPath) is not null;
        }
    }
}

