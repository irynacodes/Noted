using System;
namespace Noted.Seeding
{
	public class SongSeeding
	{
        private readonly string _projectRoot = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        private readonly string _relativePath = Path.Combine("LogicContext", "DB", "Storage", "Songs");
        private readonly string _directoryPath;

        public SongSeeding()
		{
            _directoryPath = Path.Combine(_projectRoot, _relativePath);
        }

        public string GetSerializedString(List<string> songs)
        {
            var serializedSongs = songs.Select((song, id) =>
            {
                var file = TagLib.File.Create(song);
                return SerializeSong(file, id);
            });
            
            return "[" + string.Join(",", serializedSongs) + "]";
        }

        public string SerializeSong(TagLib.File song, int id)
        {
            return $"{{\"Id\":{id},\"Name\":\"{song.Tag.Title}\",\"Artist\":\"{song.Tag.FirstPerformer}\"," +
                $"\"Album\":\"{song.Tag.Album}\",\"ReleaseYear\":{song.Tag.Year},\"LocationPath\":\"{song.Name}\"}}";
        }

        public List<string> ReadSongsFromDir()
        {
            return Directory.GetFiles(_directoryPath).ToList();
        }
    }
}

