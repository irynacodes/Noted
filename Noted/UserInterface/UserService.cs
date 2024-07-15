using System.Text;
using Noted.LogicContext.DB;
using Noted.LogicContext.Entities;
using System.Security.Cryptography;
using Noted.Exceptions;
using System.Globalization;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Noted.UserInterface
{
	public class UserService
    {
        private delegate User? FuncUser(User user);
        private delegate void FuncSong(Song? song, User? user);
        private readonly Dictionary<string, FuncUser> _profileOps;
        private readonly Dictionary<string, FuncSong> _libraryOps;
        
        private readonly UserDbContext _userDbContext;
        private readonly UserInteraction _userInteraction;
        private readonly SongDbContext _songDbContext;
        private readonly PlaylistDbContext _playlistDbContext;
        
        private User? _user;
        private static InteractionDbContext? FriendsDbContext { get; set; }
        private static InteractionDbContext? BlockedDbContext { get; set; }
        private static InteractionDbContext? LibraryDbContext { get; set; }

        public UserService(UserInteraction userInteraction)
		{
			_userDbContext = new();
            _songDbContext = new();
            _playlistDbContext = new();
            _userInteraction = userInteraction;
            _profileOps = new Dictionary<string, FuncUser>
            {   { "friends", ViewFriends },
                { "blocked", ViewBlocked },
                { "edit", EditProfile },
                { "back", GoBackUser }
            };
            _libraryOps = new Dictionary<string, FuncSong>
            {
                { "view", ViewSong },
                { "play", PlaySong },
                { "back", GoBackSong },
                { "shuffle", ShuffleLibrary }
            };
            FriendsDbContext = new InteractionDbContext(new string[] { "LogicContext", "Db", "Storage", "Friends.json" });
            BlockedDbContext = new InteractionDbContext(new string[] { "LogicContext", "Db", "Storage", "Blocked.json" });
            LibraryDbContext = new InteractionDbContext(new string[] { "LogicContext", "Db", "Storage", "Library.json" });
        }

        public User? ViewProfile(User? user)
        {
            PrintProfile(user);
            string? req = null;
            while (req is null || req != "back")
            {
                do
                {
                    req = _userInteraction.GetInfoFromUser(">> ");
                    if (req is null)
                    {
                        Console.WriteLine("Failed to read the request. Try again.");
                        return null;
                    }
                } while (!_profileOps.Keys.Contains(req.ToLower()));

                _profileOps[req.ToLower()](user);
                PrintProfile(user);
            }

            return user;
        }

        public User? EditProfile(User user)
        {
            string? req = null;
            var supported = new string[] { "edit", "back" };
            while (req is null || req != "back")
            {
                do
                {
                    PrintEditProfile(user);
                    req = _userInteraction.GetInfoFromUser(">> ");
                    if (req is null)
                    {
                        Console.WriteLine("Failed to read the request. Try again.");
                        return null;
                    }
                } while (!supported.Contains(req.ToLower()));

                if (req.ToLower() == "edit")
                {
                    var supportedEdit = new string[] { "name", "username", "birthday" };
                    string[] ops;
                    string? op;
                    do
                    {
                        op = _userInteraction.GetInfoFromUser(">> edit >> name/username/birthday? + NEW\n");
                        ops = op.Split(' ');
                        if (ops.Length < 2)
                        {
                            Console.WriteLine("Invalid request format.");
                        }
                    } while (!supportedEdit.Contains(ops[0]));
                    var par = op.Substring(op.IndexOf(' ') + 1);
                    switch (ops[0])
                    {
                        case "name":
                            user.Name = par;
                            _userDbContext.EditUser(user, user.Username);
                            break;
                        case "username":
                            var oldUsername = user.Username;
                            user.Username = par;
                            _userDbContext.EditUser(user, oldUsername);
                            break;
                        case "birthday":
                            DateTime birthday;
                            if (!DateTime.TryParse(par, new CultureInfo("fr-FR"), DateTimeStyles.None, out birthday))
                            {
                                Console.WriteLine("The date is invalid.");
                                break;
                            }
                            user.Birthday = birthday;
                            _userDbContext.EditUser(user, user.Username);
                            break;
                    }
                }
            }
            return user;
        }

        public User? ViewSuggestions(User user)
        {
            var friends = FriendsDbContext.GetData()[user.Id];
            var library = LibraryDbContext.GetData();
            var userSongs = library[user.Id];
            var songs = new List<int>();

            foreach (var friend in friends)
            {
                var friendSongs = library[friend];
                songs.AddRange(friendSongs.Where(song => !userSongs.Contains(song)));
            }

            if (songs.Count() > 10)
            {
                var rnd = new Random();
                var i = rnd.Next(songs.Count());
                // formula to randomly choose 10 songs out of the suggested and stay within the array boundaries
                var lower = i < 4 ? 0 : ((i + 5 > songs.Count()) ? i - 10 - (songs.Count() - i) : i - 5);
                songs = songs.Skip(lower).Take(10).ToList();
            }

            string? req;
            string? op = null;
            string songName;

            var supported = new string[] { "view", "play", "back" };

            do
            {
                PrintSuggestions(songs);
                req = _userInteraction.GetInfoFromUser(">> ");
                if (req is null)
                {
                    Console.WriteLine("Failed to read the request. Try again.");
                    return null;
                }

                var ops = req.Split(' ');
                if (ops[0] == "back")
                {
                    break;
                }

                else if (supported.Contains(ops[0]))
                {
                    op = ops[0];
                    songName = req.Substring(req.IndexOf(' ') + 1);
                    var song = _songDbContext.FindSongByName(songName);

                    if (op == "view")
                    {
                        ViewSong(song, user);
                    }
                    else if (op == "play")
                    {
                        PlaySong(song);
                    }
                }
            } while (true);

            return user;
        }

        public User ViewLibrary(User user)
        {
            string? req;
            string? op = null;
            string songName = "";
            while (op is null || op != "back")
            {
                do
                {
                    PrintLibrary(user);
                    req = _userInteraction.GetInfoFromUser(">> ");
                    if (req is null)
                    {
                        Console.WriteLine("Failed to read the request. Try again.");
                        continue;
                    }

                    op = req.Split(' ')[0];
                    songName = req.Substring(req.IndexOf(' ') + 1);
                } while (!_libraryOps.Keys.Contains(op.ToLower()));

                var song = _songDbContext.FindSongByName(songName);
                if (song is null && op != "shuffle")
                {
                    Console.WriteLine("Song does not exist.");
                    continue;
                }

                _libraryOps[op.ToLower()](song, user);
            }

            return user;
        }

        public User? ViewPlaylist(User? user = null)
        {
            var playlist = _playlistDbContext.ReadPlaylist();
            var songs = playlist.Values.SelectMany(s => s);
            songs = songs.Order();
            var songsPaged = songs.Chunk(10);
            var supported = new string[] { "next", "prev", "play", "view", "shuffle" };
            var i = 1;
            var pageN = 0;
            var pageCount = songsPaged.Count();

            string? req;
            do
            {
                var page = pageN < songsPaged.Count() ? songsPaged.ElementAt(pageN) : new int[] {};
                PrintPlaylistPage(playlist, page, i);
                req = _userInteraction.GetInfoFromUser(">> ");
                if (req is null)
                {
                    Console.WriteLine("Failed to read the request. Try again.");
                    return null;
                }

                var ops = req.Split(' ');
                if (ops[0] == "back")
                {
                    break;
                }
                if (!supported.Contains(ops[0]))
                {
                    Console.WriteLine("Invalid request format. Try again.");
                    continue;
                }

                if (ops[0] == "next")
                {
                    if (pageN < pageCount - 1)
                    {
                        i += page.Count();
                        pageN++;
                    }
                    continue;
                }

                if (ops[0] == "prev")
                {
                    if (pageN != 0)
                    {
                        pageN--;
                        i -= songsPaged.ElementAt(pageN).Count();
                    }
                    continue;
                }

                if (ops[0] == "shuffle")
                {
                    if (songs.Count() < 1)
                    {
                        continue;
                    }
                    Random rnd = new Random();
                    var k = rnd.Next(songs.Count());
                    var randomSongId = songs.ElementAt(k);
                    var randomSong = _songDbContext.FindSongById(randomSongId);
                    PlaySong(randomSong);
                    continue;
                }

                var songName = req.Substring(req.IndexOf(' ') + 1);
                var song = _songDbContext.FindSongByName(songName);
                if (song is null)
                {
                    Console.WriteLine("Song does not exist.");
                    continue;
                }

                if (ops[0] == "play")
                {
                    PlaySong(song);
                }
                else if (ops[0] == "view")
                {
                    ViewSong(song, user);
                    PrintPlaylistPage(playlist, page, i - 10);
                }
            } while (true);

            return user;
        }

        public void ViewSong(Song? song = null, User? user = null)
        {
            string? op = null;

            var supported = new string[] { "add", "delete", "play", "playlist" };
            do
            {
                PrintSong(song);
                op = _userInteraction.GetInfoFromUser(">> ");
                if (op is null)
                {
                    Console.WriteLine("Failed to read the request. Try again.");
                    return;
                }

                if (op == "back")
                {
                    break;
                }

                if (supported.Contains(op))
                {
                    if (op == "add")
                    {
                        user.AddSong(song);
                    }
                    else if (op == "delete")
                    {
                        user.DeleteSong(song);
                    }
                    else if (op == "playlist")
                    {
                        SaveSongToPlaylist(user, song);
                    }
                    else
                    {
                        PlaySong(song);
                    }
                }
                else
                {
                    Console.WriteLine("Request not recognized. Try again.");
                }
            } while (true);
        }

        public void PlaySong(Song? song = null, User? _ = null)
        {
            Process? proc;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                proc = Process.Start("afplay", song.LocationPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                proc = Process.Start("aplay", song.LocationPath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                proc = Process.Start("fmedia", song.LocationPath);
            }
            else
            {
                Console.WriteLine("Song cannot be played on this device.");
                return;
            }

            Console.WriteLine("Press any key to stop.");
            Console.ReadKey();
            proc.Kill();
            proc.Dispose();
        }

        public void ShuffleLibrary(Song? song = null, User? user = null)
        {
            var songIds = LibraryDbContext.GetUserData(user);
            if (songIds.Count == 0)
            {
                return;
            }
            Random rnd = new Random();
            var i = rnd.Next(songIds.Count());
            var randomSongId = songIds[i];
            var randomSong = _songDbContext.FindSongById(randomSongId);
            if (randomSong is null)
            {
                Console.WriteLine("Technical problems. Try again.");
                return;
            }
            PlaySong(randomSong);
        }

        public async void AddToPlaylistBackground(User user)
		{
			var library = LibraryDbContext.GetData()[user.Id];
            if (library.Count() == 0)
            {
                return;
            }

            Random rnd = new Random();

            while (true)
            {
                var i = rnd.Next(library.Count());
                var songId = library[i];
                var song = _songDbContext.FindSongById(songId);
                SaveSongToPlaylist(user, song);
                await Task.Delay(20000);
            }
		}

        public async void SaveSongToPlaylist(User user, Song song)
        {
            if (_playlistDbContext.SongInPlaylist(song))
            {
                if (user.Id == _user.Id)
                {
                    Console.WriteLine("Song is already in the playlist.");
                }
                return;
            }

            var playlist = _playlistDbContext.ReadPlaylist();
            var songCount = playlist.Values.SelectMany(p => p).Count();
            var mySongCount = playlist[user.Id].Count();
            var otherSongCount = songCount - mySongCount;

            if (mySongCount > otherSongCount + 1 && user.Id == _user.Id)
            {
                Console.WriteLine("You've gone over the limit of songs you can add. Wait a little and it might get added.");
            }
            while (mySongCount > otherSongCount + 1)
            {
                await Task.Delay(7000);
                playlist = _playlistDbContext.ReadPlaylist();
                otherSongCount = playlist.Values.SelectMany(p => p).Count() - mySongCount;
            }
            _playlistDbContext.SaveSong(user, song);
        }

        public User? ViewFriends(User user)
        {
            string? req;
            string? op;
            string username;

            var supported = new string[] { "delete", "block", "view" };

            do
            {
                PrintFriends(user);
                req = _userInteraction.GetInfoFromUser(">> ");
                if (req is null)
                {
                    Console.WriteLine("Failed to read the request. Try again.");
                    return null;
                }

                var ops = req.Split(' ');
                if (ops[0] == "back")
                {
                    break;
                }

                else if (supported.Contains(ops[0]))
                {
                    op = ops[0];
                    username = req.Substring(req.IndexOf(' ') + 1);
                    var friend = _userDbContext.FindUserByUsername(username);

                    if (friend is null)
                    {
                        Console.WriteLine("User does not exist.");
                        continue;
                    }

                    if (op == "delete")
                    {
                        user.RemoveFriend(friend);
                    }
                    else if (op == "block")
                    {
                        user.Block(friend);
                    }
                    else
                    {
                        ViewLibrary(friend);
                    }
                }
            } while (true);

            return user;
        }

        public User? ViewBlocked(User user)
        {
            string? req;
            do {
                PrintBlocked(user);
                req = _userInteraction.GetInfoFromUser(">> ");
                if (req is null)
                {
                    Console.WriteLine("Failed to read the request. Try again.");
                    return null;
                }

                var ops = req.Split(' ');
                if ( ops[0] == "back")
                {
                    break;
                }
                else if (ops[0] == "unblock")
                {
                    var username = req.Substring(req.IndexOf(' ') + 1);
                    var toUnblock = _userDbContext.FindUserByUsername(username);

                    if (toUnblock is null)
                    {
                        Console.WriteLine("User does not exist.");
                        continue;
                    }
                    user.Unblock(toUnblock);
                }
            } while (true);

            return user;
        }

        public User? ViewDatabase(User? user = null)
        {
            var songs = _songDbContext.ReadSongs();
            var songsPaged = songs.Chunk(10);
            var supported = new string[] { "next", "prev", "play", "view", "shuffle" };
            var i = 1;
            var pageN = 0;
            var pageCount = songsPaged.Count();

            string? req;
            do
            {
                var page = songsPaged.ElementAt(pageN);
                PrintSongPage(page, i);
                req = _userInteraction.GetInfoFromUser(">> ");
                if (req is null)
                {
                    Console.WriteLine("Failed to read the request. Try again.");
                    return null;
                }

                var ops = req.Split(' ');
                if (ops[0] == "back")
                {
                    break;
                }
                if (!supported.Contains(ops[0]))
                {
                    Console.WriteLine("Invalid request format. Try again.");
                    continue;
                }

                if (ops[0] == "next")
                {
                    if (pageN != pageCount - 1)
                    {
                        i += page.Count();
                        pageN++;
                    }
                    continue;
                }

                if (ops[0] == "prev")
                {
                    if (pageN != 0)
                    {
                        pageN--;
                        i -= songsPaged.ElementAt(pageN).Count();
                    }
                    continue;
                }

                if (ops[0] == "shuffle")
                {
                    if (songs.Count() == 0)
                    {
                        continue;
                    }
                    Random rnd = new Random();
                    var k = rnd.Next(songs.Count());
                    var randomSong = songs[k];
                    if (randomSong is null)
                    {
                        Console.WriteLine("Technical problems. Try again.");
                        continue;
                    }
                    PlaySong(randomSong);
                    continue;
                }

                var songName = req.Substring(req.IndexOf(' ') + 1);
                var song = _songDbContext.FindSongByName(songName);
                if (song is null)
                {
                    Console.WriteLine("Song does not exist.");
                    continue;
                }

                if (ops[0] == "play")
                {
                    PlaySong(song);
                }
                else if (ops[0] == "view")
                {
                    ViewSong(song, user);
                    PrintSongPage(page, i - 10);
                }
            } while (true);

            return null;
        }

        public User? ViewUsers(User? user)
        {
            var users = _userDbContext.ReadUsers();
            var usersPaged = users.Chunk(10);
            var supported = new string[] { "next", "prev", "add", "block", "view" };
            var i = 1;
            var pageN = 0;
            var pageCount = usersPaged.Count();

            string? req;
            do
            {
                var page = usersPaged.ElementAt(pageN);
                PrintUserPage(page, i);
                req = _userInteraction.GetInfoFromUser(">> ");
                if (req is null)
                {
                    Console.WriteLine("Failed to read the request. Try again.");
                    return null;
                }

                var ops = req.Split(' ');
                if (ops[0] == "back")
                {
                    break;
                }
                if (!supported.Contains(ops[0]))
                {
                    Console.WriteLine("Invalid request format. Try again.");
                    continue;
                }

                if (ops[0] == "next")
                {
                    if (pageN != pageCount - 1)
                    {
                        i += page.Count();
                        pageN++;
                    }
                    continue;
                }

                if (ops[0] == "prev")
                {
                    if (pageN != 0)
                    {
                        pageN--;
                        i -= usersPaged.ElementAt(pageN).Count();
                    }
                    continue;
                }

                var username = req.Substring(req.IndexOf(' ') + 1);
                var user1 = _userDbContext.FindUserByUsername(username);
                if (user1 is null)
                {
                    Console.WriteLine("User does not exist.");
                    continue;
                }

                if (ops[0] == "add")
                {
                    user?.AddFriend(user1);
                }
                else if (ops[0] == "block")
                {
                    user?.Block(user1);
                }
            } while (true);

            return user;
        }

		public User? SignIn(User? _ = null)
		{
			var tries = 3;

            var username = _userInteraction.GetInfoFromUser("Your username: ");

            var user = _userDbContext.FindUserByUsername(username);
            if (user is null)
            {
                Console.WriteLine("User does not exist.");
                return null;
            }

            while (tries > 0)
			{
                var password = _userInteraction.GetInfoFromUser("Your password: ");

                if (!user.HashCorresponds(new MD5CryptoServiceProvider().ComputeHash(ASCIIEncoding.ASCII.GetBytes(password))))
                {
                    Console.WriteLine("Password does not match. Try again.");
                    tries--;
                }
                else
                {
                    _playlistDbContext.SetUser(user);
                    _user = user;
                    return user;
                }
            }

            Console.WriteLine("Incorrect password. Redirecting to the main page...");
            return null;
        }

        public User? SignUp(User? _ = null)
        {
            var username = _userInteraction.GetInfoFromUser("Your username: ");

            if (_userDbContext.UsernameExists(username))
            {
                Console.WriteLine($"User {username} already exists.");
            }

            var password = _userInteraction.GetInfoFromUser("Your password: ");

            var tries = 3;
            while (tries > 0)
            {
                var passwordAgain = _userInteraction.GetInfoFromUser("Repeat your password: ");

                if (password != passwordAgain)
                {
                    Console.WriteLine("The passwords don't match. Try again");
                    tries--;
                }
                else
                {
                    var name = _userInteraction.GetInfoFromUser("What should we call you?\n" + ">> ");
                    var birthday_S = _userInteraction.GetInfoFromUser("Enter your date of birth in format dd.mm.yyyy\n" + ">> ");
                    DateTime birthday;
                    if (!DateTime.TryParse(birthday_S, new CultureInfo("fr-FR"), DateTimeStyles.None, out birthday))
                    {
                        Console.WriteLine("The date is invalid. You can set it up later.");
                    }
                    try
                    {
                        var user = new User(username, password, name, birthday.Date, _userDbContext, _playlistDbContext);
                        _userDbContext.SaveUser(user);
                        _playlistDbContext.SetUser(user);
                        _user = user;
                        return user;
                    }
                    catch (EntityAlreadyExistsException<User> e)
                    {
                        Console.WriteLine(e.ToString());
                        return null;
                    }
                }
            }

            return null;
        }

        public User? GoBackUser(User? user) { return null; }
        public void GoBackSong(Song song, User? user = null) {}

        public void PrintProfile(User user)
        {
            var birthday = user.Birthday.ToString("dd/MM/yyyy");
            Console.WriteLine("\n============================== PROFILE ==============================\n");
            Console.WriteLine($"    NAME: {user.Name}");
            Console.WriteLine($"    USERNAME: {user.Username}");
            Console.WriteLine($"    BORN: {birthday}\n");
            Console.WriteLine("\n                                  - \"friends\" to view friends");
            Console.WriteLine("                                  - \"blocked\" to view blocked");
            Console.WriteLine("                                  - \"edit\" to edit profile info");
            Console.WriteLine("                                  - \"back\" to return to main page");
            Console.WriteLine("=====================================================================\n");
        }

        public void PrintEditProfile(User user)
        {
            var birthday = user.Birthday.ToString("dd/MM/yyyy");
            Console.WriteLine("\n============================== PROFILE ==============================\n");
            Console.WriteLine($"    NAME: {user.Name}");
            Console.WriteLine($"    USERNAME: {user.Username}");
            Console.WriteLine($"    BORN: {birthday}\n");
            Console.WriteLine("\n                                  - \"edit\" to edit profile info");
            Console.WriteLine("                                  - \"back\" to return to main page");
            Console.WriteLine("=====================================================================\n");
        }

        public void PrintLibrary(User user)
        {
            var songIds = LibraryDbContext.GetUserData(user);
            Console.WriteLine($"\n=========================== {user.Username}'s LIBRARY ==========================\n");
            var i = 0;
            Song? song;
            foreach (var id in songIds)
            {
                i++;
                song = _songDbContext.FindSongById(id);
                if (song is null)
                {
                    continue;
                }
                Console.WriteLine($"{i}. \"{song.Name}\" by {song.Artist} from {song.Album}");
            }
            Console.WriteLine("\n                   - \"shuffle\" to play random song from the library");
            Console.WriteLine("                   - \"view song_name\" to view song info");
            Console.WriteLine("                   - \"play song_name\" to play the song");
            Console.WriteLine("                   - \"back\" to return to main page");
            Console.WriteLine("=====================================================================\n");
        }

        public void PrintFriends(User user)
        {
            var friendIds = FriendsDbContext.GetUserData(user);
            Console.WriteLine("\n============================== FRIENDS ==============================\n");
            foreach (var id in friendIds)
            {
                var friend = _userDbContext.FindUserById(id);
                if (friend is null)
                {
                    continue;
                }
                Console.WriteLine($"   - {friend.Username}");
            }
            Console.WriteLine("\n                   - \"delete username\" to delete user from friends");
            Console.WriteLine("                   - \"block username\" to block user");
            Console.WriteLine("                   - \"view username\" to view user's library");
            Console.WriteLine("                   - \"back\" to return to main page");
            Console.WriteLine("=====================================================================\n");
        }

        public void PrintBlocked(User user)
        {
            var blockedIds = BlockedDbContext.GetUserData(user);
            Console.WriteLine("\n============================== BLOCKED ==============================\n");
            foreach (var id in blockedIds)
            {
                var blocked = _userDbContext.FindUserById(id);
                if (blocked is null)
                {
                    continue;
                }
                Console.WriteLine($"   - {blocked.Username}");
            }
            Console.WriteLine("\n                   - \"unblock username\" to unblock user");
            Console.WriteLine("                   - \"back\" to return to main page");
            Console.WriteLine("=====================================================================\n");
        }

        public void PrintSong(Song song)
        {
            Console.WriteLine($"\n============================== {song.Name} ==============================");
            Console.WriteLine($"                               {song.Artist}                                ");
            Console.WriteLine($"                               {song.Album}                                \n\n");
            Console.WriteLine("                                         - \"add\" to add the song");
            Console.WriteLine("                                         - \"playlist\" to add song to the playlist");
            Console.WriteLine("                                         - \"delete\" to delete the song");
            Console.WriteLine("                                         - \"play\" to play the song");
            Console.WriteLine("                                         - \"back\" to return to main page");
        }

        public void PrintSongPage(Song[] page, int i)
        {
            Console.WriteLine("\n============================== SONGS ==============================\n");
            foreach (var song in page)
                {
                    Console.WriteLine($"{i}. \"{song.Name}\" by {song.Artist} from {song.Album}");
                    i++;
                }
            Console.WriteLine("\n                   - \"next\" to view next page");
            Console.WriteLine("                   - \"prev\" to view previous page");
            Console.WriteLine("                   - \"shuffle\" to play random song from the database");
            Console.WriteLine("                   - \"play song_name\" to play the song");
            Console.WriteLine("                   - \"view song_name\" to view song info");
            Console.WriteLine("                   - \"back\" to return to main page");
    }

        public void PrintUserPage(User[] page, int i)
        {
            Console.WriteLine("\n============================== USERS ==============================\n");
            foreach (var user in page)
                {
                    Console.WriteLine($"{i}. {user.Username}");
                    i++;
                }
            Console.WriteLine("\n                   - \"next\" to view next page");
            Console.WriteLine("                   - \"prev\" to view previous page");
            Console.WriteLine("                   - \"add username\" to add user to friends");
            Console.WriteLine("                   - \"block username\" to block user");
            Console.WriteLine("                   - \"back\" to return to main page");
        }

        public void PrintSuggestions(List<int> songIds)
        {
            Console.WriteLine("\n============================== YOU MIGHT LIKE ==============================\n");
            var i = 1;
            foreach (var songId in songIds)
            {
                var song = _songDbContext.FindSongById(songId);
                Console.WriteLine($"{i}. \"{song.Name}\" by {song.Artist} from {song.Album}");
                i++;
            }
            Console.WriteLine("\n                                  - \"play song_name\" to play the song");
            Console.WriteLine("                                  - \"view song_name\" to view song info");
            Console.WriteLine("                                  - \"back\" to return to main page");
        }

        public void PrintPlaylistPage(Dictionary<int, List<int>> playlist, int[] page, int i)
        {
            Console.WriteLine("\n============================== PLAYLIST ==============================\n");
            foreach (var songId in page)
            {
                var song = _songDbContext.FindSongById(songId);
                var userEntry = playlist.Values.Where(value => value.Contains(songId)).First();
                if (song is null)
                {
                    continue;
                }
                var userId = playlist.Where(entry => entry.Value.Equals(userEntry)).First().Key;
                var user = _userDbContext.FindUserById(userId);
                Console.WriteLine($"{i}. \"{song.Name}\" by {song.Artist} - ADDED BY {user.Username}");
                i++;
            }
            Console.WriteLine("\n                   - \"next\" to view next page");
            Console.WriteLine("                   - \"prev\" to view previous page");
            Console.WriteLine("                   - \"shuffle\" to play random song from the database");
            Console.WriteLine("                   - \"play song_name\" to play the song");
            Console.WriteLine("                   - \"view song_name\" to view song info");
            Console.WriteLine("                   - \"back\" to return to main page");
        }
	}
}
