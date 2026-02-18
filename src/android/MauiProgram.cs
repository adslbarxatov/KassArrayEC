using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

#if DEBUG
	using Microsoft.Extensions.Logging;
#endif

namespace RD_AAOW
	{
	public static class MauiProgram
		{
		public static MauiApp CreateMauiApp ()
			{
			var builder = MauiApp.CreateBuilder ();
			builder.UseMauiApp<App> ();

#if DEBUG
			builder.Logging.AddDebug ();
#endif

			return builder.Build ();
			}
		}
	}
