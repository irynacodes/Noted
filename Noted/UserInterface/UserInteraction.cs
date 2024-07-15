using Noted.LogicContext.Entities;
using Noted.LogicContext.DB;

namespace Noted.UserInterface
{
	public class UserInteraction
	{
        private delegate User? CustomFunc(User? optional);
        private User? _user;
		private UserService _service;
		private UserDbContext _userDbContext;
		private bool _sessionEnded;
		private readonly Dictionary<string, CustomFunc> _ops;

		public UserInteraction()
		{
			_service = new(this);
			_userDbContext = new();
            _ops = new Dictionary<string, CustomFunc>
			{	{ "sign in", _service.SignIn },
				{ "sign up", _service.SignUp },
				{ "profile", _service.ViewProfile },
                { "library", _service.ViewLibrary },
				{ "songs", _service.ViewDatabase },
				{ "users", _service.ViewUsers },
				{ "exit", Exit },
                { "playlist", _service.ViewPlaylist },
                { "suggestions", _service.ViewSuggestions },

            };
        }

		public void NotedSession()
		{
			PrintWelcomeMessage();
			string? req;
			while (_user is null)
			{
				var supported = new string[] { "sign in", "sign up" };
				do
				{
					req = GetInfoFromUser("\"sign in\" if you already have an account, \"sign up\" if you'd like to create one.\n>> ");
					if (req is null)
					{
						Console.WriteLine("Failed to read the request. Try again.");
						return;
					}
				} while (!supported.Contains(req.ToLower()));

				var user = _ops[req.ToLower()](_user);
				if (user is null)
				{
					Console.WriteLine("Failed to authenticate. Try again.");
					continue;
				}

				_user = user;
			}

			StartBackgroundAdding(_user);

			while (!_sessionEnded)
			{
				string? op;
				do
				{
					op = PrintManual(_user);
				} while (!_ops.Keys.Contains(op?.ToLower()));

				try
				{
					_ops[op.ToLower()](_user);
				}
				catch (Exception)
				{
					Console.WriteLine("Some technical issues occured. Please go on.");
				}
			}
        }

		public async void StartBackgroundAdding(User user)
		{
			var users = _userDbContext.ReadUsers();
			var otherUsers = users.Where(u => u.Id != user.Id);
			foreach (var u in otherUsers)
			{
				await Task.Run(() => _service.AddToPlaylistBackground(u));
				await Task.Delay(1234);
			}
		}

		public string? GetInfoFromUser(string question)
		{
			Console.Write(question);
			return Console.ReadLine()?.Trim();
		}

		public string? PrintManual(User user)
		{
			var manual = $"\nHey, {user.Name}! What brings you here?\n\n" +
				$"- \"profile\" to view your personal profile\n" +
				$"- \"library\" to view your music library\n" +
                $"- \"songs\" to view all available songs\n" +
				$"- \"users\" to view all platform users\n" +
                $"- \"playlist\" to view your mutual playlist with other Noted enthusiasts\n" +
				$"- \"suggestions\" to view songs Noted thinks you'll like\n" +
				$"- \"exit\" to exit the application\n\n";

			return GetInfoFromUser(manual + ">> ");
        }

		public void PrintWelcomeMessage()
		{
			Console.WriteLine("\nWelcome to Noted, your favourite minimalistic music streaming platform.\n");
		}

		public User? Exit(User? user = null)
		{
			_sessionEnded = true;
			 return null;
		}
	}
}

