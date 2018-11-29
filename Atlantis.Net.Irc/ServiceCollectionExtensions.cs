// -----------------------------------------------------------------------------
//  <copyright file="Program.cs" author="Zack Loveless">
//      Copyright (c) Zachary Loveless. All rights reserved.
//  </copyright>
// -----------------------------------------------------------------------------

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Atlantis.Net.Irc
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddIrcClient(this IServiceCollection services)
        {
            throw new NotImplementedException();
        }

        public static IServiceCollection AddIrcServer(this IServiceCollection services)
        {
            throw new NotImplementedException();
        }
    }
}
