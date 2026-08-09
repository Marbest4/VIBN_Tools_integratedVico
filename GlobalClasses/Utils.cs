using Microsoft.Win32;

namespace VIBN_Tools.GlobalClasses
{

    /// <summary>
    /// Provides methods for opening dialogs.
    /// </summary>
    public class SystemDialog
    {
        /// <summary>
        /// Opens a file dialog to select a file.
        /// </summary>
        /// <param name="filter">The filter string to use in the file dialog.</param>
        /// <returns>The selected file path, or an empty string if no file was selected.</returns>
        public static string OpenSelectFileDialog(string filter)
        {
            OpenFileDialog openFileDialog = new()
            {
                Filter = filter,
                FilterIndex = 2,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }
            return string.Empty;
        }

        /// <summary>
        /// Opens a file dialog to save a file.
        /// </summary>
        /// <param name="filter">The filter string to use in the file dialog.</param>
        /// <param name="path">The initial path for the file dialog.</param>
        /// <param name="fileNameprefix">The prefix to add to the file name (optional).</param>
        /// <returns>The selected file path, or an empty string if no file was selected.</returns>
        public static string OpenSaveFileDialog(string filter, string path, string fileNameprefix = "")
        {
            SaveFileDialog saveFileDialog = new()
            {
                Filter = filter,
                FilterIndex = 1,
                RestoreDirectory = true,

                FileName = string.Join("_", fileNameprefix, path)
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                return saveFileDialog.FileName;
            }
            return string.Empty;
        }
    }
}
