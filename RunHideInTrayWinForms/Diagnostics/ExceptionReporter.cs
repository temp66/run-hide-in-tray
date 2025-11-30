extern alias RunHideInTrayCommon;

using RunHideInTrayCommon::RunHideInTrayCommon;

namespace RunHideInTrayWinForms.Diagnostics;

internal static class ExceptionReporter
{
    public static void ShowMessageBox(Exception ex)
    {
        MessageBox.Show(ex.ToString(), ApplicationInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
