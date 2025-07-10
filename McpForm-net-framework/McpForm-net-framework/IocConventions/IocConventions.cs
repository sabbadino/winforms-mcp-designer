﻿using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;


namespace WinFormsApp1
{
    public static class IocConventions
    {
        public static void RegisterByConvention<T>(this IServiceCollection services)
        {
            services.RegisterByConvention(typeof(T).Assembly);
        }
        public static void RegisterByConvention(this IServiceCollection services, Type type)
        {
            services.RegisterByConvention(type.Assembly);
        }
        public static void RegisterByConvention(this IServiceCollection services, Assembly assembly)
        {
            var implementations = assembly.GetExportedTypes()
                .Where(t => t.IsClass && typeof(ISingletonScope).IsAssignableFrom(t)).ToList();
            foreach (var implementation in implementations)
            {
                var interfaces = implementation.GetInterfaces().Where(itf =>
                    itf != typeof(ISingletonScope)).ToList();
                foreach (var itf in interfaces)
                {
                    services.AddSingleton(itf, implementation);
                }
            }
            implementations = assembly.GetExportedTypes()
                .Where(t => t.IsClass && typeof(ITransientScope).IsAssignableFrom(t)).ToList();
            foreach (var implementation in implementations)
            {
                var interfaces = implementation.GetInterfaces().Where(itf =>
                    itf != typeof(ITransientScope)).ToList();
                foreach (var itf in interfaces)
                {
                    services.AddTransient(itf, implementation);
                }
            }
            implementations = assembly.GetExportedTypes()
                .Where(t => t.IsClass && typeof(IRequestScope).IsAssignableFrom(t)).ToList();
            foreach (var implementation in implementations)
            {
                var interfaces = implementation.GetInterfaces().Where(itf =>
                    itf != typeof(IRequestScope)).ToList();
                foreach (var itf in interfaces)
                {
                    services.AddScoped(itf, implementation);
                }
            }
        }
    }
}
