﻿using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Windows.Forms;
using Andeart.JsonButler.CodeSerialization;
using Andeart.JsonButlerIde.Utilities;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Design;
using Microsoft.VisualStudio.Shell.Interop;



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
        public static readonly Guid CommandSet = new Guid ("dd6fa331-344e-4e45-a8e7-6c6ceb15dcd0");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

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
        /// <param name="commandService">Command service to add command to, not null.</param>
        private SerializeTypeCommand (Package package)
        {
            _package = package ?? throw new ArgumentNullException (nameof(package));

            OleMenuCommandService commandService = ServiceProvider.GetService (typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
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
        public static void Initialize (Package package)
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
            TextSelection textSelection = GetCurrentTextElement ();
            CodeElement codeElement = EditorUtilities.GetCodeElement (textSelection);
            if (codeElement == null)
            {
                return;
            }

            ITypeResolutionService resolutionService = GetResolutionService (codeElement.ProjectItem.ContainingProject);
            Type type = resolutionService.GetType (codeElement.FullName);
            string serialized = ButlerSerializer.SerializeType (type);
            Clipboard.SetText (serialized);
            Console.WriteLine ($"JsonButler: Serialized text from {type.FullName} copied.");
        }


        #region UTILS

        /// <summary>
        /// Returns the text-selection under the text-caret, or where the right-click context menu was invoked.
        /// </summary>
        private TextSelection GetCurrentTextElement ()
        {
            JsonButlerPackage mainPackage = _package as JsonButlerPackage;
            return mainPackage?.Dte.ActiveDocument.Selection as TextSelection;
        }

        private ITypeResolutionService GetResolutionService (Project currentProject)
        {
            DynamicTypeService typeService = ServiceProvider.GetService (typeof(DynamicTypeService)) as DynamicTypeService;
            Debug.Assert (typeService != null, "No dynamic type service registered.");

            IVsSolution vsSolution = (IVsSolution) ServiceProvider.GetService (typeof(IVsSolution));
            vsSolution.GetProjectOfUniqueName (currentProject.UniqueName, out IVsHierarchy vsHierarchy);
            Debug.Assert (vsHierarchy != null, "No active hierarchy is selected.");

            return typeService.GetTypeResolutionService (vsHierarchy);
        }

        #endregion UTILS
    }

}