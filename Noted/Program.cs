using Noted.UserInterface;
using Noted.LogicContext.DB;

namespace Noted
{
    public class Program
    {
        public static void Main()
        {
            /* SEEDING */
            
            /*  UNCOMMENT THE FOLLOWING LINES BEFORE THE *FIRST* RUN OF THE APPLICATION
                COMMENT BACK AFTERWARD
            */
            
            /* var songDbContext = new SongDbContext();
            songDbContext.SaveSongs(); */
            
            /* SEEDING */

            var ui = new UserInteraction();
            ui.NotedSession();

        }
    }
}