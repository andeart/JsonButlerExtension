﻿using Andeart.JsonButler.CodeSerialization;
using Andeart.JsonButlerIde.Forms;
using Andeart.JsonButlerIde.Utilities;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Design;
using Microsoft.VisualStudio.Shell.Interop;
using Newtonsoft.Json;
using System;
using System.ComponentModel.Design;
using System.Windows.Forms;


namespace Andeart.JsonButlerIde.Commands
{

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class SerializeTypeCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x0100;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid ("21aaa842-876e-414f-9e9d-b81fb8d21749");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly JsonButlerIdePackage _package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static SerializeTypeCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializeTypeCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private SerializeTypeCommand (JsonButlerIdePackage package)
        {
            _package = package ?? throw new ArgumentNullException (nameof(package));

            if (!(ServiceProvider.GetService (typeof(IMenuCommandService)) is OleMenuCommandService commandService))
            {
                throw new ArgumentNullException (nameof(commandService));
            }

            CommandID menuCommandId = new CommandID (CommandSet, CommandId);
            MenuCommand menuItem = new MenuCommand (Execute, menuCommandId);
            commandService.AddCommand (menuItem);
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize (JsonButlerIdePackage package)
        {
            // Verify the current thread is the UI thread - the call to AddCommand in SerializeTypeCommand's constructor requires
            // the UI thread.
            ThreadHelper.ThrowIfNotOnUIThread ();

            Instance = new SerializeTypeCommand (package);
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="args">Event args.</param>
        private void Execute (object sender, EventArgs args)
        {
            ThreadHelper.ThrowIfNotOnUIThread ();

            ConfirmationWindow confirmationWindow = new ConfirmationWindow ();
            DialogResult buildConfirmation = confirmationWindow.ShowDialogWithMessage ("JsonButler needs to build solution to identify this type.\nProceed?");

            if (buildConfirmation != DialogResult.OK)
            {
                return;
            }

            _package.Dte?.Solution.SolutionBuild.Build (true);

            TextSelection textSelection = _package.Dte?.ActiveDocument.Selection as TextSelection;
            CodeElement codeElement = EditorUtilities.GetCodeElement (textSelection);
            AlertWindow alertWindow;
            if (codeElement == null)
            {
                alertWindow = new AlertWindow ();
                alertWindow.ShowDialogWithMessage ("Invalid code element selected for serialization.");
                return;
            }

            ITypeResolutionService resolutionService = GetResolutionService (codeElement.ProjectItem.ContainingProject);
            Type type = resolutionService.GetType (codeElement.FullName);

            ButlerSerializerSettings serializerSettings = new ButlerSerializerSettings (type?.Assembly);

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings ();
            SerializerContractResolver contractResolver = new SerializerContractResolver ();
            contractResolver.SerializationType = _package.SerializationType;
            jsonSerializerSettings.ContractResolver = contractResolver;
            jsonSerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            jsonSerializerSettings.Formatting = Formatting.Indented;

            serializerSettings.JsonSerializerSettings = jsonSerializerSettings;

            string serialized = ButlerSerializer.SerializeType (type, serializerSettings);
            Clipboard.SetText (serialized);

            _package.DoAlert ("Serialized JSON contents copied to clipboard.");
        }


        #region UTILS

        private ITypeResolutionService GetResolutionService (Project currentProject)
        {
            DynamicTypeService typeService = ServiceProvider.GetService (typeof(DynamicTypeService)) as DynamicTypeService;

            if (typeService == null)
            {
                throw new InvalidOperationException ("No dynamic type service registered.");
            }

            IVsSolution vsSolution = (IVsSolution) ServiceProvider.GetService (typeof(IVsSolution));
            vsSolution.GetProjectOfUniqueName (currentProject.UniqueName, out IVsHierarchy vsHierarchy);
            if (vsHierarchy == null)
            {
                throw new InvalidOperationException ("No active hierarchy is selected.");
            }

            return typeService.GetTypeResolutionService (vsHierarchy);
        }

        #endregion UTILS
    }

}