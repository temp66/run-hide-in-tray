using RunHideInTrayCommon;
using RunHideInTrayConsole.Diagnostics;
using String.Quoting;

using System.Drawing;

namespace RunHideInTrayConsole;

internal static class ConfigExtensions
{
    extension(Config)
    {
        public static Config Create(int hideOnStartTimeout, string? title, FileInfo? iconFileInfo, string[] exec)
        {
            string exec_ = CommandLineQuoting.Quoted(exec);
            title ??= $"{ApplicationInfo.Name} - {exec_}";
            FileStream? iconFileStream;
            try
            {
                iconFileStream = iconFileInfo?.OpenRead();
            }
            catch (SystemException ex)
            {
                ExceptionReporter.ToConsole("Failed to open icon file", ex);
                iconFileStream = null;
            }
            using (iconFileStream)
            {
                Icon? icon;
                if (iconFileStream is null)
                    icon = null;
                else
                    try
                    {
                        icon = new(iconFileStream, Config.IconLargestSize, Config.IconLargestSize);
                    }
                    catch (ArgumentException ex)
                    {
                        ExceptionReporter.ToConsole("Failed to load icon from file", ex);
                        icon = null;
                    }
                icon ??= SystemIcons.GetStockIcon(StockIconId.Application);
                return new()
                {
                    HideOnStartTimeout = hideOnStartTimeout,
                    Title = title,
                    Icon = icon,
                    Exec = exec_,
                };
            }
        }
    }
}
