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
                        icon = new(iconFileStream, SystemInformation.SmallIconSize);
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
                        icon = Icon.ExtractIcon(exec[0], 0, true);
                    }
                    catch (IOException)
                    {
                    }
                    icon ??= SystemIcons.GetStockIcon(StockIconId.Application, StockIconOptions.SmallIcon);
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
