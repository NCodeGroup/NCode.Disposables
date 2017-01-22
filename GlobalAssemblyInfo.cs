#region Copyright Preamble
//
//    Copyright © 2017 NCode Group
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//
#endregion

using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

[assembly: AssemblyCompany("NCode Group")]
[assembly: AssemblyCopyright("Copyright © 2017 NCode Group")]
[assembly: AssemblyTrademark("")]

[assembly: AssemblyCulture("")]
[assembly: NeutralResourcesLanguage("en-us")]

[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

// http://semver.org/
// The actual version number is set during the TeamCity build, but in the code
// it MUST be "0.0.0" in order for the RegEx to find and replace the value.

[assembly: AssemblyVersion("0.0.0")]
[assembly: AssemblyFileVersion("0.0.0")]
[assembly: AssemblyInformationalVersion("0.0.0")]
