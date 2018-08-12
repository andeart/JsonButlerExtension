﻿using System;
using System.ComponentModel.Design;
using System.Windows.Forms;
using Andeart.CaseConversion;
using Andeart.JsonButlerIde.Forms;
using Andeart.JsonButlerIde.Utilities;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;



namespace Andeart.JsonButlerIde.Commands
{

    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class ConvertPascalCaseCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 4132;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = new Guid ("21aaa842-876e-414f-9e9d-b81fb8d21749");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package _package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static ConvertPascalCaseCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => _package;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertPascalCaseCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private ConvertPascalCaseCommand (Package package)
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
        public static void Initialize (Package package)
        {
            // Verify the current thread is the UI thread - the call to AddCommand in ConvertPascalCaseCommand's constructor requires
            // the UI thread.
            ThreadHelper.ThrowIfNotOnUIThread ();

            Instance = new ConvertPascalCaseCommand (package);
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

            object service = ServiceProvider.GetService (typeof(SVsTextManager));
            IVsTextManager2 textManager = service as IVsTextManager2;
            string highlightedText = EditorUtilities.GetHighlightedText (textManager);

            string convertedText = highlightedText.ToPascalCase ();
            Clipboard.SetText (convertedText);
            AlertWindow alertWindow = new AlertWindow ();
            alertWindow.ShowDialogWithMessage ("Converted text copied to clipboard.");
        }
    }

}