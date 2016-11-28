using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell.Interop;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Reflection;
namespace SsrsReportDeploy
{

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidSsrsReportDeployPkgString)]
    public sealed class VsExtSsrsReportDeployPackage : Package
    {

        public static string testTarget = "http://XXXXX/TestEnvironment";

        public static string uatTarget = "http://XXXXX/UatEnvironment";

        public static string prodTarget = "http://XXXXX/ReportEnvironment";

        private const string tfsServer = @"http://XXXXX:8080/tfs";

        public VsExtSsrsReportDeployPackage()
        {
         
        }

        protected override void Initialize()
        {
            
            base.Initialize();


            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;

            if (null != mcs)
            {
                CommandID menuCommandItemID = new CommandID(GuidList.guidSsrsReportDeployCmdSetItem, (int)PkgCmdIDList.cmdidSsrsReportDeployItem);
                MenuCommand menuItemItem = new MenuCommand(TestDeploy, menuCommandItemID);
                mcs.AddCommand(menuItemItem);

                CommandID menuCommandCodeID = new CommandID(GuidList.guidSsrsReportDeployCmdSetItem, (int)PkgCmdIDList.cmdidSsrsReportDeployItem2);
                MenuCommand menuItemCode = new MenuCommand(UatDeploy, menuCommandCodeID);
                mcs.AddCommand(menuItemCode);


                CommandID menuCommandCodeID2 = new CommandID(GuidList.guidSsrsReportDeployCmdSetItem, (int)PkgCmdIDList.cmdidSsrsReportDeployItem3);
                MenuCommand menuItemCode2 = new MenuCommand(ProdDeploy, menuCommandCodeID2);
                mcs.AddCommand(menuItemCode2);

            }
        }

        private UIHierarchyItem GetSelectedUIHierarchy(UIHierarchy solutionExplorer)
        {
            object[] selection = solutionExplorer.SelectedItems as object[];

            if (selection != null && selection.Length == 1)
            {
                return selection[0] as UIHierarchyItem;
            }

            return null;
        }



        private List<UIHierarchyItem> GetSelectedUIHierarchies(UIHierarchy solutionExplorer)
        {
            object[] selection = solutionExplorer.SelectedItems as object[];

            if (selection != null)
            {
                return Array.ConvertAll(selection, item => (UIHierarchyItem)item).ToList<UIHierarchyItem>();
            }

            return null;
        }

        private void TestDeploy(object sender, EventArgs e)
        {
            deployToTarget("test");

        }

        private void UatDeploy(object sender, EventArgs e)
        {
            deployToTarget("uat");

        }

        private void ProdDeploy(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Report will be deployed to the prod environment , are you sure ?", "Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                deployToTarget("prod");
            }
        }

        private string getDeployTarget(string projeLokasyon, string env)
        {
            return (string)typeof(SsrsReportDeploy.VsExtSsrsReportDeployPackage).GetField(env + "Target", BindingFlags.Public | BindingFlags.Static).GetValue(false);
        }

        private bool deploy(string projeLokasyon, string deployTarget)
        {
            deployTarget = getDeployTarget(projeLokasyon, deployTarget);
            XDocument xmlFile = XDocument.Load(projeLokasyon);
            if (xmlFile.Descendants("Configurations").FirstOrDefault().Elements().Where(x => x.Element("Name").Value == "DebugLocal").Elements().Descendants().Where(x => x.Name == "TargetServerURL").FirstOrDefault().Value != deployTarget)
            {
                xmlFile.Descendants("Configurations").FirstOrDefault().Elements().Where(x => x.Element("Name").Value == "DebugLocal").Elements().Descendants().Where(x => x.Name == "TargetServerURL").FirstOrDefault().Value = deployTarget;
                CheckOutGetLatestFromTFS(projeLokasyon);
                xmlFile.Save(projeLokasyon);
                return true;
            }
            else return false;

        }

        private void deployToTarget(string deployTarget)
        {
            DTE2 dte = (DTE2)GetService(typeof(DTE));

            UIHierarchy solutionExplorer = dte.ToolWindows.SolutionExplorer;
            UIHierarchyItem item = GetSelectedUIHierarchy(solutionExplorer);
            List<UIHierarchyItem> items = GetSelectedUIHierarchies(solutionExplorer);
            List<ProjectItemDetail> projectItems = new List<ProjectItemDetail>();
            List<string> projects = new List<string>();
            string solutionName = Path.GetFileNameWithoutExtension(dte.Solution.FullName);

            try
            {
                if (items != null)
                {
                    foreach (UIHierarchyItem hi in items)
                    {
                        hi.Select(vsUISelectionType.vsUISelectionTypeSelect);
                        ProjectItem pi = hi.Object is ProjectItem ? (ProjectItem)hi.Object : null;
                        ProjectItemDetail projectItemDetail = new ProjectItemDetail();
                        projectItemDetail.ProjectName = pi.ContainingProject.Name;
                        projectItemDetail.ProjectUniqueName = pi.ContainingProject.UniqueName;
                        projectItemDetail.Name = pi.Name;
                        projectItemDetail.FilePath = pi.FileNames[0];
                        projectItems.Add(projectItemDetail);

                    }


                    foreach (ProjectItemDetail pi in projectItems)
                    {

                        if (pi != null && !projects.Contains(pi.ProjectName))
                        {
                            projects.Add(pi.ProjectName);
                            bool reloadProject = deploy(pi.ProjectUniqueName, deployTarget);

                            if (reloadProject)
                            {
                                dte.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Activate();
                                dte.ToolWindows.SolutionExplorer.GetItem(solutionName + @"\" + pi.ProjectName).Select(vsUISelectionType.vsUISelectionTypeSelect);

                                dte.ExecuteCommand("Project.UnloadProject");
                                dte.ExecuteCommand("Project.ReloadProject");
                            }
                        }
                    }

                    EnvDTE.vsUISelectionType type = EnvDTE.vsUISelectionType.vsUISelectionTypeSelect;
                    for (int i = 0; i < projectItems.Count; i++)
                    {

                        EnvDTE.UIHierarchyItem hierarchyItem = _findUIHierarchyItem(solutionExplorer.UIHierarchyItems, projectItems[i]);
                        if (hierarchyItem != null)
                        {
                            hierarchyItem.Select(type);
                            type = EnvDTE.vsUISelectionType.vsUISelectionTypeToggle;
                        }

                    }

                    Commands cmds;
                    Command cmd;

                    cmds = dte.Commands;
                    cmd = cmds.Item("ProjectandSolutionContextMenus.Item.Deploy", 1);
                    cmds.Raise("{878DCF4F-8C51-4180-A753-18FA0DC167CC}", 12348, null, null);


                }
            }
            catch (Exception)
            {
                throw;
            }

        }

        private static void CheckOutGetLatestFromTFS(string fileName)
        {
            using (TfsTeamProjectCollection pc = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(tfsServer)))
            {
                if (pc != null)
                {
                    WorkspaceInfo workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(fileName);
                    if (null != workspaceInfo)
                    {
                        Workspace workspace = workspaceInfo.GetWorkspace(pc);
                        workspace.PendEdit(fileName);
                    }
                }
            }
        }

        private UIHierarchyItem _findUIHierarchyItem(UIHierarchyItems items, ProjectItemDetail item)
        {
            if (!items.Expanded)
                items.Expanded = true;
            if (!items.Expanded)
            {
                UIHierarchyItem parent = ((UIHierarchyItem)items.Parent);
                parent.Select(vsUISelectionType.vsUISelectionTypeSelect);
                ((DTE2)GetService(typeof(DTE))).ToolWindows.SolutionExplorer.DoDefaultAction();
            }

            foreach (UIHierarchyItem child in items)
            {
                if (child.Object is Solution)
                {
                    var result = _findUIHierarchyItem(child.UIHierarchyItems, item);
                    if (result != null) return result;
                }
                else if (child.Object is Project)
                {

                    Project project = child.Object is Project ? (Project)child.Object : null;

                    if (project.Name == item.ProjectName)
                    {
                        var result = _findUIHierarchyItem(child.UIHierarchyItems, item);
                        if (result != null) return result;

                    }

                }
                else if (child.Object is ProjectItem)
                {
                    ProjectItem projectItem = child.Object is ProjectItem ? (ProjectItem)child.Object : null;

                    if (projectItem != null)
                    {
                        if (projectItem.Name == "Reports")
                        {
                            var result = _findUIHierarchyItem(child.UIHierarchyItems, item);
                            if (result != null) return result;
                        }
                        else if (projectItem.Name == item.Name)
                        {
                            return child;
                        }

                    }
                }
            }
            return null;
        }

    }

}



