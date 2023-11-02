//-----------------------------------------------------------------------------
// FILE:	    Helper.cs
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
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using k8s;
using k8s.Models;

using Neon.Common;
using Neon.IO;
using Neon.Tasks;

namespace KubernetesDebugger
{
    /// <summary>
    /// Implements useful utility methods.
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// Executes a command within a pod.
        /// </summary>
        /// <param name="k8s">Specifies the Kubernetes client.</param>
        /// <param name="pod">Specifies th target pod.</param>
        /// <param name="container">Specifies the name of the target container within the pod.</param>
        /// <param name="path">
        /// Specifies the fully qualified path to the executable within the pod
        /// as well as any arguments.
        /// </param>
        /// <param name="args">Optionally specifies command arguments.</param>
        /// <returns>An <see cref="ExecuteResponse"/>.</returns>
        public static async Task<ExecuteResponse> ExecAsync(IKubernetes k8s, V1Pod pod, string container, string path, params string[] args)
        {
            await SyncContext.Clear;
            Covenant.Requires<ArgumentNullException>(k8s != null, nameof(k8s));
            Covenant.Requires<ArgumentNullException>(pod != null, nameof(pod));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(container), nameof(container));
            Covenant.Requires<ArgumentNullException>(!string.IsNullOrEmpty(path), nameof(path));
            //Covenant.Requires<ArgumentException>(path.StartsWith("/"), nameof(path), "Command path must be fully qualified and start with a \"/\".");

            if (!pod.Spec.Containers.Any(c => c.Name == container))
            {
                throw new InvalidOperationException($"Container [{container}] does not exist in pod [{pod.Namespace()}/{pod.Name()}].");
            }

            var outputText = string.Empty;
            var errorText  = string.Empty;

            var action = new ExecAsyncCallback(
                async (Stream stdIn, Stream stdOut, Stream stdErr) =>
                {
                    using var outputStream = new MemoryStream();
                    using var errorStream  = new MemoryStream();

                    await stdOut.CopyToAsync(outputStream);
                    await stdErr.CopyToAsync(errorStream);

                    // We see a bunch of '\0' characters appended to these strings sometimes.
                    // I believe this may be due to Kubernetes sending zeros from the end of
                    // its send buffer or something.  We're going to remove these.

                    string StripZeros(string input)
                    {
                        var firstZeroPos = input.IndexOf('\0');

                        if (firstZeroPos < 0)
                        {
                            return input;
                        }
                        else
                        {
                            return input.Substring(0, firstZeroPos);
                        }
                    }

                    outputText = StripZeros(Encoding.UTF8.GetString(outputStream.GetBuffer()));
                    errorText  = StripZeros(Encoding.UTF8.GetString(errorStream.GetBuffer()));
                });

            var exitcode = await k8s.NamespacedPodExecAsync(
                name:              pod.Metadata.Name, 
                @namespace:        pod.Metadata.Namespace(), 
                container:         container, 
                command:           new string[] { path }, 
                tty:               false,
                action:            action,
                cancellationToken: default(CancellationToken));

            return new ExecuteResponse(exitcode, outputText, errorText);
        }
    }
}
