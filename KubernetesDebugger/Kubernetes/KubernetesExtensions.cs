//-----------------------------------------------------------------------------
// FILE:	    KubernetesExtensions.cs
// CONTRIBUTOR: Jeff Lill
// COPYRIGHT:   Copyright (c) 2024 by neonFORGE, LLC.  All rights reserved.
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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using k8s;
using k8s.Models;

using Neon.Common;

namespace KubernetesDebugger
{
    // $todo(jefflill):
    //
    // Add these extensions to the [Neon.k8s] library after we consolidate the
    // NEONKUBE and OPERATORSDK Kubernetes classes into NEONSDK.

    /// <summary>
    /// Implements <see cref="Kubernetes"/> client related extensions.
    /// </summary>
    public static class KubernetesExtensions
    {
        /// <summary>
        /// Submits a PATCH request to the API server, throwing an <see cref="HttpException "/>
        /// for HTTP errors.
        /// </summary>
        /// <param name="client">Specifies the Kubernetes client.</param>
        /// <param name="uri">Specifies a <b>relative</b> URI.</param>
        /// <param name="body">Specifies the patch body.</param>
        /// <returns>The received <see cref="HttpResponseMessage"/>.</returns>
        /// <exception cref="HttpException">Thrown for HTTP errors.</exception>
        public static async Task<HttpResponseMessage> PatchSafeAsync(this Kubernetes client, Uri uri, HttpContent body)
        {
            Covenant.Requires<ArgumentNullException>(client != null, nameof(client));
            Covenant.Requires<ArgumentNullException>(uri != null, nameof(uri));
            Covenant.Requires<ArgumentException>(!uri.IsAbsoluteUri, nameof(uri), "URI cannot be absolute.");

            return await client.HttpClient.PatchSafeAsync(new Uri(client.BaseUri, uri), body);
        }

        /// <summary>
        /// Submits a PATCH request to the API server.
        /// </summary>
        /// <param name="client">Specifies the Kubernetes client.</param>
        /// <param name="uri">Specifies a <b>relative</b> URI.</param>
        /// <param name="body">Specifies the patch body.</param>
        /// <returns>The received <see cref="HttpResponseMessage"/>.</returns>
        public static async Task<HttpResponseMessage> PatchAsync(this Kubernetes client, Uri uri, HttpContent body)
        {
            Covenant.Requires<ArgumentNullException>(client != null, nameof(client));
            Covenant.Requires<ArgumentNullException>(uri != null, nameof(uri));
            Covenant.Requires<ArgumentException>(!uri.IsAbsoluteUri, nameof(uri), "URI cannot be absolute.");

            return await client.HttpClient.PatchAsync(new Uri(client.BaseUri, uri), body);
        }
    }
}
