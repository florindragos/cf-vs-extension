﻿namespace CloudFoundry.VisualStudio.MSBuild
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using EnvDTE;
    using Microsoft.Build.Evaluation;
    using Microsoft.VisualStudio.Shell;

    public class MSBuildProcess
    {
        public Dictionary<string, string> MSBuildProperties { get; set; }

        public void Publish(EnvDTE.Project projectInfo)
        {
            this.Publish(projectInfo, Microsoft.Build.Framework.LoggerVerbosity.Normal);
        }

        public void Publish(EnvDTE.Project projectInfo, Microsoft.Build.Framework.LoggerVerbosity verbosity)
        {
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                DTE dte = (DTE)CloudFoundry_VisualStudioPackage.GetGlobalService(typeof(DTE));
                var window = dte.Windows.Item(EnvDTE.Constants.vsWindowKindOutput);
                var output = (OutputWindow)window.Object;

                OutputWindowPane pane = null;

                pane = output.OutputWindowPanes.Cast<OutputWindowPane>().FirstOrDefault(x => x.Name == "Publish");

                if (pane == null)
                {
                    pane = output.OutputWindowPanes.Add("Publish");
                }

                pane.Activate();

                var engine = ProjectCollection.GlobalProjectCollection;

                ErrorListProvider errorList = CloudFoundry_VisualStudioPackage.GetErrorListPane();

                if (errorList == null)
                {
                    throw new InvalidOperationException("Could not retrieve error list provider");
                }

                errorList.Tasks.Clear();

                MSBuildLogger customLogger = new MSBuildLogger(pane, errorList);

                customLogger.Verbosity = verbosity;

                engine.RegisterLogger(customLogger);

                foreach (KeyValuePair<string, string> parameter in MSBuildProperties)
                {
                    engine.SetGlobalProperty(parameter.Key, parameter.Value);
                }

                pane.OutputString("Starting build...");

                Microsoft.Build.Evaluation.Project websiteProject = engine.LoadProject(MSBuildProperties["PublishProfile"]);
                websiteProject.Build("WebCloudFoundryPublish");

                if (errorList.Tasks.Count > 0)
                {
                    errorList.Show();
                }

                engine.UnregisterAllLoggers();
            });
        }
    }
}
