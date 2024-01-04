//-----------------------------------------------------------------------------
// FILE:	    ProcessInfo.cs
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

namespace KubernetesDebugger
{
    /// <summary>
    /// Holds required information about a process.
    /// </summary>
    public struct ProcessInfo
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="pid">Specifies the process ID.</param>
        /// <param name="name">Specifies the process (exe) name.</param>
        /// <param name="command">Specifies the process command line.</param>
        public ProcessInfo(int pid, string name, string command)
        {
            this.Pid     = pid;
            this.Name    = name;
            this.Command = command;
        }

        /// <summary>
        /// Returns the process ID.
        /// </summary>
        public int Pid { get; private set; }

        /// <summary>
        /// Returns the process (exe) name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Returns the command line for the process.
        /// </summary>
        public string Command { get; private set; }
    }
}