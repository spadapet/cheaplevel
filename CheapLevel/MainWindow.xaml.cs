using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace CheapLevel
{
    public partial class MainWindow : Window, System.Windows.Forms.IWin32Window
    {
        private bool _manuallyChoseDest;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChooseFile()
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.DefaultExt = ".set";
            dialog.FileName = FileTextBox.Text;
            dialog.Filter = "Level and Set Files|*.lev;*.set|All Files|*.*";
            dialog.Title = "Choose a level or set file";

            bool? result = dialog.ShowDialog(this);
            if (result.HasValue && result.Value)
            {
                FileTextBox.Text = dialog.FileName;

                if (!_manuallyChoseDest)
                {
                    string dir = Path.GetDirectoryName(dialog.FileName);
                    string file = Path.GetFileNameWithoutExtension(dialog.FileName);

                    DestTextBox.Text = Path.Combine(dir, file);
                }
            }
        }

        private void ChooseDest()
        {
            using (System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.SelectedPath = DestTextBox.Text;
                dialog.Description = "Choose a destination for extracted files";
                if (dialog.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
                {
                    DestTextBox.Text = dialog.SelectedPath;
                    _manuallyChoseDest = true;
                }
            }
        }

        private void OnChooseFile(object sender, RoutedEventArgs args)
        {
            ChooseFile();
        }

        private void OnChooseDest(object sender, RoutedEventArgs args)
        {
            ChooseDest();
        }

        private void OnOk(object sender, RoutedEventArgs args)
        {
            string file = FileTextBox.Text;
            string dest = DestTextBox.Text;

            if (!File.Exists(file))
            {
                MessageBox.Show(this, "File doesn't exist: " + file);
                return;
            }

            if (!Directory.Exists(dest))
            {
                try
                {
                    DirectoryInfo info = Directory.CreateDirectory(dest);
                    if (info == null)
                    {
                        throw new IOException();
                    }
                }
                catch
                {
                    MessageBox.Show(this, "Directory can't be created: " + dest);
                    return;
                }
            }

            Extract(file, dest);
        }

        private void OnCancel(object sender, RoutedEventArgs args)
        {
            Close();
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            if (!args.Handled && args.Key == Key.Escape)
            {
                args.Handled = true;
                Close();
            }
        }

        IntPtr System.Windows.Forms.IWin32Window.Handle
        {
            get
            {
                return new WindowInteropHelper(this).Handle;
            }
        }

        private void Extract(string file, string dest)
        {
            try
            {
                if (Path.GetExtension(file).Equals(".set", StringComparison.OrdinalIgnoreCase))
                {
                    using (CheapLevel.LevelSet levelSet = CheapLevel.LevelSet.Create(file))
                    {
                        levelSet.Save(dest);
                    }
                }
                else
                {
                    using (CheapLevel.Level level = CheapLevel.Level.Create(file))
                    {
                        level.Save(dest, 0);
                    }
                }
            }
            catch (Exception exception)
            {
                string message = string.Format("Invalid file: {0}\r\nError message: {1}", file, exception.Message);
                MessageBox.Show(this, message);
                return;
            }

            Process.Start(dest);
            Close();
        }
    }
}
