// -----------------------------------------------------------------------------
//  <copyright file="GlobalAssemblyInfo.cs" company="Zack Loveless">
//      Copyright (c) Zack Loveless.  All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

using System;
using System.Reflection;

#if DEBUG

[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("Zack Loveless")]
[assembly: AssemblyProduct("Atlantis Framework")]
[assembly: AssemblyCopyright("Copyright \u00a9 2014 Zack Loveless. All Rights Reserved.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: CLSCompliant(true)]
[assembly: AssemblyVersion("4.1.*")]
