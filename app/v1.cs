using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace RobloxClientTracker
{
    public partial class Form1 : Form
    {
        private RichTextBox logBox;
        private FileSystemWatcher watcher;
        private Dictionary<string, string> fileHashes = new();

        private string robloxPath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Roblox", "Versions");

        public Form1()
        {
            InitializeComponent();
            SetupUI();
            LoadInitialHashes();
            StartWatcher();
        }

        private void SetupUI()
        {
            Text = "Roblox Client Tracker";
            Width = 900;
            Height = 600;
            BackColor = Color.FromArgb(25, 25, 25);

            logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 10),
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White,
                ReadOnly = true
            };

            Controls.Add(logBox);
            Log("Monitoring started...", ChangeType.Note);
        }

        private void StartWatcher()
        {
            watcher = new FileSystemWatcher(robloxPath)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size
            };

            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Changed += OnChanged;

            watcher.EnableRaisingEvents = true;
        }

        private void LoadInitialHashes()
        {
            foreach (var file in Directory.GetFiles(robloxPath, "*.*", SearchOption.AllDirectories))
            {
                fileHashes[file] = ComputeHash(file);
            }
        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            Invoke(() =>
            {
                fileHashes[e.FullPath] = ComputeHash(e.FullPath);
                Log($"File added: {Path.GetFileName(e.FullPath)}", ChangeType.Added);
            });
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            Invoke(() =>
            {
                fileHashes.Remove(e.FullPath);
                Log($"File removed: {Path.GetFileName(e.FullPath)}", ChangeType.Removed);
            });
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            Invoke(() =>
            {
                if (!File.Exists(e.FullPath)) return;

                string newHash = ComputeHash(e.FullPath);

                if (!fileHashes.TryGetValue(e.FullPath, out string oldHash))
                {
                    fileHashes[e.FullPath] = newHash;
                    return;
                }

                if (oldHash != newHash)
                {
                    fileHashes[e.FullPath] = newHash;
                    Log($"File modified: {Path.GetFileName(e.FullPath)}", ChangeType.Modified);
                }
            });
        }

        private string ComputeHash(string file)
        {
            try
            {
                using var sha = SHA256.Create();
                using var stream = File.OpenRead(file);
                return Convert.ToHexString(sha.ComputeHash(stream));
            }
            catch
            {
                return string.Empty;
            }
        }

        private void Log(string message, ChangeType type)
        {
            Color color = type switch
            {
                ChangeType.Added => Color.LightGreen,
                ChangeType.Removed => Color.IndianRed,
                ChangeType.Modified => Color.Orange,
                ChangeType.Note => Color.LightGray,
                _ => Color.White
            };

            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionColor = Color.Gray;
            logBox.AppendText($"[{DateTime.Now:HH:mm:ss}] ");

            logBox.SelectionColor = color;
            logBox.AppendText(message + Environment.NewLine);

            logBox.SelectionColor = logBox.ForeColor;
            logBox.ScrollToCaret();
        }
    }

    public enum ChangeType
    {
        Added,
        Removed,
        Modified,
        Note
    }
}
