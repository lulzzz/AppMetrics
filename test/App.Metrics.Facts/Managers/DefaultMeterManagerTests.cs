﻿// Copyright (c) Allan Hardy. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Linq;
using App.Metrics.Core;
using App.Metrics.Core.Options;
using App.Metrics.Facts.Fixtures;
using App.Metrics.Internal;
using App.Metrics.Meter.Interfaces;
using FluentAssertions;
using Xunit;

namespace App.Metrics.Facts.Managers
{
    public class DefaultMeterManagerTests : IClassFixture<MetricCoreTestFixture>
    {
        private readonly MetricCoreTestFixture _fixture;
        private readonly IMeasureMeterMetrics _manager;

        public DefaultMeterManagerTests(MetricCoreTestFixture fixture)
        {
            _fixture = fixture;
            _manager = _fixture.Managers.Meter;
        }

        [Fact]
        public void can_mark()
        {
            var metricName = "test_mark_meter";
            var options = new MeterOptions { Name = metricName };

            _manager.Mark(options);

            var data = _fixture.Registry.GetData(new NoOpMetricsFilter());

            data.Contexts.Single().Meters.Count(x => x.Name == metricName).Should().Be(1);
        }

        [Fact]
        public void can_mark_by_amount()
        {
            var metricName = "test_mark_meter_by_amount";
            var options = new MeterOptions { Name = metricName };

            _manager.Mark(options, 2L);

            var data = _fixture.Registry.GetData(new NoOpMetricsFilter());

            data.Contexts.Single().Meters.Count(x => x.Name == metricName).Should().Be(1);
        }

        [Fact]
        public void can_mark_with_item()
        {
            var metricName = "test_mark_meter_with_item";
            var options = new MeterOptions { Name = metricName };

            _manager.Mark(options, "item1");

            var data = _fixture.Registry.GetData(new NoOpMetricsFilter());

            data.Contexts.Single().MeterValueFor(metricName).Items.Length.Should().Be(1);
        }

        [Fact]
        public void can_mark_with_item_by_amount()
        {
            var metricName = "test_mark_meter_with_item_by_amount";
            var options = new MeterOptions { Name = metricName };

            _manager.Mark(options, 5L, "item1");

            var data = _fixture.Registry.GetData(new NoOpMetricsFilter());

            data.Contexts.Single().MeterValueFor(metricName).Items.Length.Should().Be(1);
        }

        [Fact]
        public void can_mark_with_metric_item()
        {
            var metricName = "test_mark_meter_with_metric_item";
            var options = new MeterOptions { Name = metricName };

            _manager.Mark(options, item => { item.With("tagKey", "tagvalue"); });

            var data = _fixture.Registry.GetData(new NoOpMetricsFilter());

            data.Contexts.Single().MeterValueFor(metricName).Items.Length.Should().Be(1);
        }

        [Fact]
        public void can_mark_with_metric_item_by_amount()
        {
            var metricName = "test_mark_meter_with_metric_item_by_amount";
            var options = new MeterOptions { Name = metricName };

            _manager.Mark(options, 5L, item => { item.With("tagKey", "tagvalue"); });

            var data = _fixture.Registry.GetData(new NoOpMetricsFilter());

            data.Contexts.Single().MeterValueFor(metricName).Items.Length.Should().Be(1);
        }
    }
}