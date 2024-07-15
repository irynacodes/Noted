using Noted.LogicContext.DB;
using Noted.Exceptions;
using System.Text.Json.Serialization;

namespace Noted.LogicContext.Entities
{
	public class Song
	{
		private static int IdCounter { get; set; } = 0;
		private static SongDbContext? SongDbContext { get; set; }

		public int Id { get; }
		public string LocationPath { get; set; }
		public string Name { get; }
		public string Artist { get; }
		public string Album { get; }
		public int ReleaseYear { get; }

		[JsonConstructor]
		public Song(int Id, string Name, string Artist, string Album, int ReleaseYear,
					string LocationPath)
		{
			this.Id = Id;
			this.Name = Name;
			this.Artist = Artist;
			this.Album = Album;
			this.ReleaseYear = ReleaseYear;
			this.LocationPath = LocationPath;
		}

		public Song(string name, string artist, string album, int releaseYear,
					string locationPath, SongDbContext songDbContext)
		{
			if (songDbContext.SongExists(locationPath))
			{
				throw new EntityAlreadyExistsException<Song>(locationPath);
            }

			Id = IdCounter++;
			LocationPath = locationPath;
			Name = name;
			Artist = artist;
			Album = album;
			ReleaseYear = releaseYear;
			SongDbContext ??= songDbContext;
		}
	}
}