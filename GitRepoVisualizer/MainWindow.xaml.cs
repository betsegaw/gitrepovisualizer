using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GitRepoVisualizer;
using System.Windows.Forms;

namespace GitCommenting
{
    /// <summary>
    /// Interaction logic for VisualizationWindow.xaml
    /// </summary>
    public partial class VisualizationWindow
    {
        public string RepoPath
        {
            get;
            set;
        }

        public string ApplicationPath
        {
            get;
            private set;
        }

        public VisualizationWindow()
        {
            InitializeComponent();
            this.RepoPath = null;
            this.SetAppFolder();
        }

        public VisualizationWindow(string repoPath)
        {
            InitializeComponent();
            this.RepoPath = repoPath;
        }

        private void WindowSizeChangedEvent(object sender, SizeChangedEventArgs e)
        {
            this.RefreshPage();
        }

        private async void RefreshPage()
        {
            if (RepoPath == null)
            {
                wb.NavigateToString(@"<html>
                                        <body>
                                        <font size='800%' color='6666ff'>Select a repo using the button at the top of this window</font>
                                        </body>
                                        </html>
                                        ");
                return;
            }
            else if (!LibGit2Sharp.Repository.IsValid(this.RepoPath))
            {
                wb.NavigateToString(@"<html>
                                        <body>
                                        <font size='800%' color='6666ff'>The folder you selected is not a valid git repo</font>
                                        </body>
                                        </html>
                                        ");
                return;
            }

            string page = File.ReadAllText(this.ApplicationPath + @"Visualization\" + "GraphCanvas.html").Replace("[WIDTH]", ((int)this.ActualWidth - 20).ToString()).Replace("[HEIGHT]", ((int)this.ActualHeight - 50).ToString());

            var repo = new LibGit2Sharp.Repository(this.RepoPath);

            StringBuilder nodes = new StringBuilder();
            StringBuilder edges = new StringBuilder();

            nodes.Append("[");
            edges.Append("[");

            var objs = repo.ObjectDatabase;
            HashSet<LibGit2Sharp.Commit> nodeSet = new HashSet<LibGit2Sharp.Commit>();

            foreach (var obj in objs)
            {
                if (obj.GetType() == typeof(LibGit2Sharp.Commit))
                {
                    var commit = (LibGit2Sharp.Commit)obj;

                    foreach (var parent in commit.Parents)
                    {
                        edges.Append(
                            "{from: '" +
                            commit.Sha +
                            "', to: '" +
                            parent.Sha +
                            "', style: 'arrow'},"
                            );
                        nodeSet.Add(parent);
                    }

                    nodeSet.Add(commit);
                }
            }

            foreach (var uniqueCommit in nodeSet)
            {
                nodes.Append(
                    "{id: '" +
                    uniqueCommit.Sha +
                    "', label: '" +
                    uniqueCommit.Sha.Substring(0, 6) +
                    "', title: '<p>" +
                    uniqueCommit.Author + "</p><p>" +
                    uniqueCommit.Committer.When.ToString() + "</p><p>" +
                    uniqueCommit.Message.Replace("'", "").Replace("\"", "").Replace("\n", "") +
                    "</p>'},"
                    );
            }


            foreach (var reference in repo.Branches)
            {
                nodes.Append(
                    "{id: '" +
                    reference.CanonicalName +
                    "', label: '" +
                    reference.CanonicalName + "', " +
                    "color: 'red'," +
                    "shape: 'square'" +
                    "},"
                    );

                edges.Append(
                    "{from: '" +
                    reference.CanonicalName +
                    "', to: '" +
                    reference.Tip.Sha +
                    "', style: 'arrow'},"
                    );
            }

            foreach (var tag in repo.Tags)
            {
                nodes.Append(
                    "{id: '" +
                    tag.CanonicalName +
                    "', label: '" +
                    tag.CanonicalName + "', " +
                    "color: 'red'," +
                    "shape: 'square'" +
                    "},"
                    );

                edges.Append(
                    "{from: '" +
                    tag.CanonicalName +
                    "', to: '" +
                    tag.Target.Sha +
                    "', style: 'arrow'},"
                    );
            }

            nodes.Remove(nodes.Length - 1, 1);
            edges.Remove(edges.Length - 1, 1);

            nodes.Append("]");
            edges.Append("]");

            page = page.Replace("[NODES]", nodes.ToString());
            page = page.Replace("[EDGES]", edges.ToString());

            page = page.Replace("[CURRENTDIRECTORY]", this.ApplicationPath + @"Visualization\");

            wb.NavigateToString(page);
        }

        private void SetAppFolder()
        {
            string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            this.ApplicationPath = System.IO.Path.GetDirectoryName(path) + @"\";
        }

        private void RefreshDiagram(object sender, RoutedEventArgs e)
        {
            this.RefreshPage();
        }

        private void SelectRepositoryButtonClickEvent(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.ShowDialog();

            if (dialog.SelectedPath != string.Empty)
            {
                this.RepoPath = dialog.SelectedPath;
                this.Title = "GitRepoVisualizer - " + dialog.SelectedPath;
                this.RefreshPage();
            }
        }
    }
}
