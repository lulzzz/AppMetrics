﻿// <copyright file="MetricsServiceExtensions.cs" company="App Metrics Contributors">
// Copyright (c) App Metrics Contributors. All rights reserved.
// </copyright>

using System;
using App.Metrics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MetricsSandbox
{
    public static class MetricsServiceExtensions
    {
        public static IServiceCollection ConfigureMetrics(this IServiceCollection services, HostBuilderContext hostContext, bool filterMetricValueTypes)
        {
            var metricsConfigSection = hostContext.Configuration.GetSection(nameof(MetricsOptions));
            // Metrics = AppMetrics.CreateDefaultBuilder() to use the default configuration
            var metrics = new MetricsBuilder()
                      .Configuration.Configure(options =>
                      {
                          options.AddServerTag();
                          options.AddAppTag();
                          options.AddEnvTag();
                      })
                      .Configuration.Configure(metricsConfigSection.AsEnumerable())
                      .OutputEnvInfo.AsPlainText()
                      .OutputMetrics.AsPlainText(
                              options =>
                              {
                                  if (filterMetricValueTypes)
                                  {
                                      options.FieldMapping.IncludeBasic();
                                  }
                              })
                      .SampleWith.ForwardDecaying(TimeSpan.FromMinutes(30))
                      .TimeWith.Clock(new TestClock())
                      .Report.Using<SimpleConsoleMetricsReporter>(TimeSpan.FromSeconds(2))
                      .Build();

            services.AddSingleton(metrics);

            return services;
        }
    }
}