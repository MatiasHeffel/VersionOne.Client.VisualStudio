﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using VersionOne.VisualStudio.DataLayer;
using VersionOne.VisualStudio.VSPackage.Events;
using VersionOne.VisualStudio.VSPackage.Settings;

namespace VersionOne.VisualStudio.VSPackage {
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [ProvideToolWindow(typeof(ProjectsWindow))]
    [ProvideToolWindow(typeof(TaskWindow))]
    [Guid(GuidList.guidVersionOnTrackerPkgString)]
    public sealed class V1Tracker : Package {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public V1Tracker() {
            Configuration cfg = Configuration.Instance;
            ISettings settings = SettingsImpl.Instance;
            IDataLayer dataLayer = ApiDataLayer.Instance;
            IEventDispatcher eventDispatcher = EventDispatcher.Instance;
            
            try {
                //Setup DataLayer
                dataLayer.ApiVersion = cfg.APIVersion;
                AddProperties(cfg);

                dataLayer.CurrentProjectId = settings.SelectedProjectId;
                dataLayer.ShowAllTasks = !settings.ShowMyTasks;
                dataLayer.Connect(settings.ApplicationUrl, settings.Username, settings.Password, settings.IntegratedAuth);
                eventDispatcher.InvokeModelChanged(this, ModelChangedArgs.SettingsChanged);
            } catch (DataLayerException ex) {
                Debug.WriteLine("Error while loading V1Package: " + ex.Message);
                Debug.WriteLine("\t" + ex.StackTrace);
            }

            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        private static void AddProperties(Configuration cfg) {
            foreach (ColumnSetting column in cfg.AssetDetail.TaskColumns) {
                AddProperty(column, Entity.TaskPrefix);
            }
            foreach (ColumnSetting column in cfg.AssetDetail.StoryColumns) {
                AddProperty(column, Entity.StoryPrefix);
            }
            foreach (ColumnSetting column in cfg.AssetDetail.DefectColumns) {
                AddProperty(column, Entity.DefectPrefix);
            }
            foreach (ColumnSetting column in cfg.AssetDetail.TestColumns) {
                AddProperty(column, Entity.TestPrefix);
            }
            foreach (ColumnSetting column in cfg.GridSettings.Columns) {
                AddProperty(column, Entity.TaskPrefix);
                AddProperty(column, Entity.StoryPrefix);
                AddProperty(column, Entity.DefectPrefix);
                AddProperty(column, Entity.TestPrefix);
            }
            foreach (ColumnSetting column in cfg.ProjectTree.Columns) {
                AddProperty(column, Entity.ProjectPrefix);
            }
        }

        private static void AddProperty(ColumnSetting column, string prefix) {
            ApiDataLayer.Instance.AddProperty(column.Attribute, prefix, column.Type == "List" || column.Type == "Multi");
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow<TWindow>(object sender, EventArgs e) where TWindow : ToolWindowPane {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = FindToolWindow(typeof(TWindow), 0, true);
            
            if((null == window) || (null == window.Frame)) {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            
            var windowFrame = (IVsWindowFrame) window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }


        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize() {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if(null != mcs) {
                // Create the command for Tasks tool window
                var projectsCommandId = new CommandID(GuidList.guidVersionOneTrackerCmdSet, (int) PkgCmdIDList.CmdidVersionOneProjects);
                var menuItem = new MenuCommand(ShowToolWindow<ProjectsWindow>, projectsCommandId);
                mcs.AddCommand(menuItem);
                // Create the command for the tool window
                var tasksCommandId = new CommandID(GuidList.guidVersionOneTrackerCmdSet, (int)PkgCmdIDList.CmdidVersionOneTasks);
                var menuToolWin = new MenuCommand(ShowToolWindow<TaskWindow>, tasksCommandId);
                mcs.AddCommand(menuToolWin);
            }
        }

        #endregion

    }
}