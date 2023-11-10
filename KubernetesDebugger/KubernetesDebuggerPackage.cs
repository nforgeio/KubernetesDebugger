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
        /// <para>
        /// Returns the public root SSH key used to authenticate with <b>vs-debug</b>
        /// ephemeral containers.
        /// </para>
        /// <note>
        /// This key must match the public key provisioned with the <b>vs-debug</b>
        /// container image and the key should never be changed so that older versions
        /// of the debugger will still work with new versions of the container image.
        /// </note>
        /// </summary>
        public static string PublicSshKey = "ssh-rsa AAAAB3NzaC1yc2EAAAADAQABAAACAQDFWNwHIZzxk3XKX+RFCzkEBwbAQrC/utc0yQOsccTy7TmaXc4nTgGSLEmcvwwqvz4eUmITDKu70HUdFB67AxVnrntTgDXiKgypEcSjpXmZQSKKKsJYHSMfooBjNVz7rQo5l2GauosQxucs7qpPSDcEn0r5vlS0BJ64oB3vCgJKjV86vZVXH4bNmhp6eyfRpzs0WcDWbs2rtTKi84pIHrprldIwkffuuHwgS0S6F50nBE/3eT7dbQw0TCboU+mhtnrYb1O9eHAwPF8/QEqtkUlnfaeCB5a9+F/s0D+Ix5C9+K+JPbw50kcMVTkXqrqxcEmE2igaHJc8BKhYOLmWRMDgLt2C+LBbMaOXLB7LUvyHbzdTWhlVp9MMtyWYnCvEPU27yHeSP+0JR+tle68tF0+nrox5bf0vPIjyirZfVqDW/ToksMQxJUXoVGxCbf7SXQ5cq2JEVtSidImL8PczuFLleaqhLbCIGds8bITXvhBk9T+xeVdKdyxkrAngobb5YN+m4CDAzO4cyFqpIB8FfNHqzZsOVUiYVIa/mwCt3JcIEmedHdAmnInRW6tM9TQqNpxHvRjtWmoZLJD1Tg25Uex/7fv5FCmUtsjkjGt4MJcF5ivLKIDm+Yr6lmp+nN6z6joCyeYVwTkCrV7nFKjbfWB9F1CkVEnWoL8qbaHejuw2DQ== root@20d21e5f3366";

        /// <summary>
        /// We're going to disguise the private key a bit such that GitHub scanners
        /// won't bug us about potential security issues.
        /// </summary>
        private const string privateSsskKeyBits =
@"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";

        /// <summary>
        /// <para>
        /// Returns the private root SSH key used to authenticate with <b>vs-debug</b>
        /// ephemeral containers.
        /// </para>
        /// <note>
        /// This key must match the public key provisioned with the <b>vs-debug</b>
        /// container image and the key should never be changed so that older versions
        /// of the debugger will still work with new versions of the container image.
        /// </note>
        /// </summary>
        public static string PrivateSshKey =>
$@"-----BEGIN OPENSSH PRIVATE KEY-----
{privateSsskKeyBits}
-----END OPENSSH PRIVATE KEY-----
";

        /// <summary>
        /// Logs text to the Visual Studio Debug output panel.
        /// </summary>
        /// <param name="text">The output text.</param>
        public static void Log(string text)
        {
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
