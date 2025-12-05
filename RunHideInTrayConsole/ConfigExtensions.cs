using RunHideInTrayCommon;
using RunHideInTrayConsole.Diagnostics;
using String.Quoting;

namespace RunHideInTrayConsole;

internal static class ConfigExtensions
{
    extension(Config)
    {
        public static Config Create(bool elevate, int hideOnStartTimeout, string? title, FileInfo? iconFileInfo, string[] exec)
        {
            string exec_ = CommandLineQuoting.Quoted(exec);

            title ??= $"{ApplicationInfo.Name} - {exec_}";

            Size iconSize = SystemInformation.SmallIconSize;
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
                        icon = new(iconFileStream, iconSize);
                    }
                    catch (ArgumentException ex)
                    {
                        ExceptionReporter.ToConsole("Failed to load icon from file", ex);
                        icon = null;
                    }

                if (icon is null)
                {
                    try
                    {
                        icon = Icon.ExtractIcon(exec[0], 0, iconSize.Width);
                    }
                    catch (IOException)
                    {
                    }
                    icon ??= SystemIcons.GetStockIcon(StockIconId.Application, iconSize.Width);
                }

                return new()
                {
                    Elevate = elevate,
                    HideOnStartTimeout = hideOnStartTimeout,
                    Title = title,
                    Icon = icon,
                    Exec = exec_,
                };
            }
        }
    }
}
