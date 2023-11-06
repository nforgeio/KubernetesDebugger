//-----------------------------------------------------------------------------
// FILE:	    KubernetesDebuggerPackage.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2023 by neonFORGE, LLC.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

using Task = System.Threading.Tasks.Task;

namespace KubernetesDebugger
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(KubernetesDebuggerPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class KubernetesDebuggerPackage : AsyncPackage
    {
        //---------------------------------------------------------------------
        // Static members

        /// <summary>
        /// KubernetesDebuggerPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "19586d94-a8f1-4b90-b211-5623672d27f2";

        /// <summary>
        /// Returns our VSIX package instance.
        /// </summary>
        public static KubernetesDebuggerPackage Instance { get; private set; }

        private static IVsOutputWindowPane      _debugPane;
        private static readonly object          DebugSyncLock = new object();
        private static readonly Queue<string>   DebugLogQueue = new Queue<string>();

        /// <summary>
        /// Logs text to the Visual Studio Debug output panel.
        /// </summary>
        /// <param name="text">The output text.</param>
        public static void Log(string text)
        {
            //###############################
            // $debug(jefflill): DELETE THIS!

            using (var logFile = File.AppendText(@"C:\Temp\kubernetesdebugger.log"))
            {
                logFile.Write(text);
            }
            //###############################

            if (Instance == null || _debugPane == null)
            {
                return;     // Logging hasn't been initialized yet.
            }

            if (string.IsNullOrEmpty(text))
            {
                return;     // Nothing to log
            }

            // We're going to queue log messages in the current thread and 
            // then execute a fire-and-forget action on the UI thread to
            // write any queued log lines.  We'll use a lock to protect
            // the queue.
            // 
            // This pattern is nice because it ensures that the log lines
            // are written in the correct order while ensuring this all
            // happens on the UI thread in addition to not using a 
            // [Task.Run(...).Wait()] which would probably result in
            // background thread exhaustion.

            lock (DebugSyncLock)
            {
                DebugLogQueue.Enqueue(text);
            }

            _ = Instance.JoinableTaskFactory.RunAsync(
                async () =>
                {
                    await Task.Yield();     // Get off of the callers stack
                    await Instance.JoinableTaskFactory.SwitchToMainThreadAsync(Instance.DisposalToken);

                    lock (DebugSyncLock)
                    {
                        if (DebugLogQueue.Count == 0)
                        {
                            return;     // Nothing to do
                        }

                        _debugPane.Activate();

                        // Log any queued messages.

                        while (DebugLogQueue.Count > 0)
                        {
                            _ = _debugPane.OutputStringThreadSafe(DebugLogQueue.Dequeue());
                        }
                    }
                });
        }

        //---------------------------------------------------------------------
        // Instance members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.

            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // Basic initialization.

            Instance = this;

            // Initialize the log panel.

            var debugWindow     = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            var generalPaneGuid = VSConstants.GUID_OutWindowDebugPane;

            debugWindow?.GetPane(ref generalPaneGuid, out _debugPane);

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.

            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await AttachKubernetesCommand.InitializeAsync(this);
        }
    }
}
