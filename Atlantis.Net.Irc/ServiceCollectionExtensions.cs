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

    /* 
     * Some possible configurations for DI building.
     *
     * 1) IrcBuilder - configures both a client, server, or service
     * 2) IrcClientBuilder
     * 3) IrcServerBuilder
     * 4) IrcServiceBuilder
     *
     *//*

    public class IrcBuilder
    {
        public IrcConnection CreateClient(IrcConfiguration configuration)
        {
            return null;
        }

        public IrcConnection CreateServer(IrcConfiguration configuration)
        {
            return null;
        }
    }

    public class IrcClientBuilder
    {
        public IrcConnection Create(IrcConfiguration configuration)
        {
            return null;
        }
    }

    public class IrcServerBuilder
    {
        public IrcConnection Create(IrcConnection configuration)
        {
            return null;
        }
    }*/
}
