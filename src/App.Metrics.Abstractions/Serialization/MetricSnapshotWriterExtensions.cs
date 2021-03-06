﻿// <copyright file="MetricSnapshotWriterExtensions.cs" company="App Metrics Contributors">
// Copyright (c) App Metrics Contributors. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using App.Metrics.Apdex;
using App.Metrics.Counter;
using App.Metrics.Histogram;
using App.Metrics.Meter;
using App.Metrics.Timer;

namespace App.Metrics.Serialization
{
    public static class MetricSnapshotWriterExtensions
    {
        public static void WriteApdex(
            this IMetricSnapshotWriter writer,
            string context,
            MetricValueSourceBase<ApdexValue> valueSource,
            DateTime timestamp)
        {
            if (valueSource == null || writer.MetricFields.Apdex.Count == 0)
            {
                return;
            }

            var data = new Dictionary<string, object>();
            valueSource.Value.AddApdexValues(data, writer.MetricFields.Apdex);
            WriteMetric(writer, context, valueSource, data, timestamp);
        }

        public static void WriteCounter(
            this IMetricSnapshotWriter writer,
            string context,
            MetricValueSourceBase<CounterValue> valueSource,
            CounterValueSource counterValueSource,
            DateTime timestamp)
        {
            if (counterValueSource == null || writer.MetricFields.Counter.Count == 0)
            {
                return;
            }

            if (counterValueSource.Value.Items.Any() && counterValueSource.ReportSetItems)
            {
                var itemSuffix = writer.MetricFields.Counter.ContainsKey(CounterFields.MetricSetItemSuffix)
                    ? writer.MetricFields.Counter[CounterFields.MetricSetItemSuffix]
                    : DefaultMetricFieldNames.DefaultMetricsSetItemSuffix;

                foreach (var item in counterValueSource.Value.Items.Distinct())
                {
                    var itemData = new Dictionary<string, object>();

                    if (writer.MetricFields.Counter.ContainsKey(CounterFields.Total))
                    {
                        itemData.Add(writer.MetricFields.Counter[CounterFields.Total], item.Count);
                    }

                    if (counterValueSource.ReportItemPercentages && writer.MetricFields.Counter.ContainsKey(CounterFields.SetItemPercent))
                    {
                        itemData.AddIfNotNanOrInfinity(writer.MetricFields.Counter[CounterFields.SetItemPercent], item.Percent);
                    }

                    if (itemData.Any())
                    {
                        WriteMetricWithSetItems(
                            writer,
                            context,
                            valueSource,
                            item.Tags,
                            itemData,
                            itemSuffix,
                            timestamp);
                    }
                }
            }

            if (writer.MetricFields.Counter.ContainsKey(CounterFields.Value))
            {
                var count = valueSource.ValueProvider.GetValue(resetMetric: counterValueSource.ResetOnReporting).Count;
                WriteMetricValue(writer, context, valueSource, writer.MetricFields.Counter[CounterFields.Value], count, timestamp);
            }
        }

        public static void WriteGauge(
            this IMetricSnapshotWriter writer,
            string context,
            MetricValueSourceBase<double> valueSource,
            DateTime timestamp)
        {
            if (valueSource == null || writer.MetricFields.Gauge.Count == 0)
            {
                return;
            }

            if (!double.IsNaN(valueSource.Value) && !double.IsInfinity(valueSource.Value) && writer.MetricFields.Gauge.ContainsKey(GaugeFields.Value))
            {
                WriteMetricValue(writer, context, valueSource, writer.MetricFields.Gauge[GaugeFields.Value], valueSource.Value, timestamp);
            }
        }

        public static void WriteHistogram(
            this IMetricSnapshotWriter writer,
            string context,
            MetricValueSourceBase<HistogramValue> valueSource,
            DateTime timestamp)
        {
            if (valueSource == null || writer.MetricFields.Histogram.Count == 0)
            {
                return;
            }

            var data = new Dictionary<string, object>();
            valueSource.Value.AddHistogramValues(data, writer.MetricFields.Histogram);
            WriteMetric(writer, context, valueSource, data, timestamp);
        }

        public static void WriteMeter(
            this IMetricSnapshotWriter writer,
            string context,
            MeterValueSource valueSource,
            DateTime timestamp)
        {
            if (valueSource == null || writer.MetricFields.Meter.Count == 0)
            {
                return;
            }

            var data = new Dictionary<string, object>();

            if (valueSource.Value.Items.Any() && valueSource.ReportSetItems)
            {
                var itemSuffix = writer.MetricFields.Meter.ContainsKey(MeterFields.MetricSetItemSuffix)
                    ? writer.MetricFields.Meter[MeterFields.MetricSetItemSuffix]
                    : DefaultMetricFieldNames.DefaultMetricsSetItemSuffix;

                foreach (var item in valueSource.Value.Items.Distinct())
                {
                    var setItemData = new Dictionary<string, object>();

                    item.AddMeterSetItemValues(setItemData, writer.MetricFields.Meter);

                    if (setItemData.Any())
                    {
                        WriteMetricWithSetItems(
                            writer,
                            context,
                            valueSource,
                            item.Tags,
                            setItemData,
                            itemSuffix,
                            timestamp);
                    }
                }
            }

            valueSource.Value.AddMeterValues(data, writer.MetricFields.Meter);

            WriteMetric(writer, context, valueSource, data, timestamp);
        }

        public static void WriteTimer(
            this IMetricSnapshotWriter writer,
            string context,
            MetricValueSourceBase<TimerValue> valueSource,
            DateTime timestamp)
        {
            if (valueSource == null)
            {
                return;
            }

            var data = new Dictionary<string, object>();

            if (writer.MetricFields.Meter.Count > 0)
            {
                valueSource.Value.Rate.AddMeterValues(data, writer.MetricFields.Meter);
            }

            if (writer.MetricFields.Histogram.Count > 0)
            {
                valueSource.Value.Histogram.AddHistogramValues(data, writer.MetricFields.Histogram);
            }

            if (data.Count > 0)
            {
                WriteMetric(writer, context, valueSource, data, timestamp);
            }
        }

        private static MetricTags ConcatIntrinsicMetricTags<T>(MetricValueSourceBase<T> valueSource)
        {
            var intrinsicTags = new MetricTags(AppMetricsConstants.Pack.MetricTagsUnitKey, valueSource.Unit.ToString());

            if (typeof(T) == typeof(TimerValue))
            {
                var timerValueSource = valueSource as MetricValueSourceBase<TimerValue>;

                var timerIntrinsicTags = new MetricTags(
                    new[] { AppMetricsConstants.Pack.MetricTagsUnitRateDurationKey, AppMetricsConstants.Pack.MetricTagsUnitRateKey },
                    new[] { timerValueSource?.Value.DurationUnit.Unit(), timerValueSource?.Value.Rate.RateUnit.Unit() });

                intrinsicTags = MetricTags.Concat(intrinsicTags, timerIntrinsicTags);
            }
            else if (typeof(T) == typeof(MeterValue))
            {
                var meterValueSource = valueSource as MetricValueSourceBase<MeterValue>;

                var meterIntrinsicTags = new MetricTags(AppMetricsConstants.Pack.MetricTagsUnitRateKey, meterValueSource?.Value.RateUnit.Unit());
                intrinsicTags = MetricTags.Concat(intrinsicTags, meterIntrinsicTags);
            }

            if (AppMetricsConstants.Pack.MetricValueSourceTypeMapping.ContainsKey(typeof(T)))
            {
                var metricTypeTag = new MetricTags(AppMetricsConstants.Pack.MetricTagsTypeKey, AppMetricsConstants.Pack.MetricValueSourceTypeMapping[typeof(T)]);
                intrinsicTags = MetricTags.Concat(metricTypeTag, intrinsicTags);
            }
            else
            {
                var metricTypeTag = new MetricTags(AppMetricsConstants.Pack.MetricTagsTypeKey, typeof(T).Name.ToLowerInvariant());
                intrinsicTags = MetricTags.Concat(metricTypeTag, intrinsicTags);
            }

            return intrinsicTags;
        }

        private static MetricTags ConcatMetricTags<T>(MetricValueSourceBase<T> valueSource, MetricTags setItemTags)
        {
            var tagsWithSetItems = MetricTags.Concat(valueSource.Tags, setItemTags);
            var intrinsicTags = ConcatIntrinsicMetricTags(valueSource);

            return MetricTags.Concat(tagsWithSetItems, intrinsicTags);
        }

        private static MetricTags ConcatMetricTags<T>(MetricValueSourceBase<T> valueSource)
        {
            var intrinsicTags = ConcatIntrinsicMetricTags(valueSource);

            return MetricTags.Concat(valueSource.Tags, intrinsicTags);
        }

        private static void WriteMetric<T>(
            IMetricSnapshotWriter writer,
            string context,
            MetricValueSourceBase<T> valueSource,
            IDictionary<string, object> data,
            DateTime timestamp)
        {
            var keys = data.Keys.ToList();
            var values = keys.Select(k => data[k]);
            var tags = ConcatMetricTags(valueSource);

            if (valueSource.IsMultidimensional)
            {
                writer.Write(
                    context,
                    valueSource.MultidimensionalName,
                    keys,
                    values,
                    tags,
                    timestamp);

                return;
            }

            writer.Write(context, valueSource.Name, keys, values, tags, timestamp);
        }

        private static void WriteMetricValue<T>(
            IMetricSnapshotWriter writer,
            string context,
            MetricValueSourceBase<T> valueSource,
            string field,
            object value,
            DateTime timestamp)
        {
            var tags = ConcatMetricTags(valueSource);

            if (valueSource.IsMultidimensional)
            {
                writer.Write(
                    context,
                    valueSource.MultidimensionalName,
                    field,
                    value,
                    tags,
                    timestamp);

                return;
            }

            writer.Write(context, valueSource.Name, field, value, tags, timestamp);
        }

        private static void WriteMetricWithSetItems<T>(
            IMetricSnapshotWriter writer,
            string context,
            MetricValueSourceBase<T> valueSource,
            MetricTags setItemTags,
            IDictionary<string, object> itemData,
            string metricSetItemSuffix,
            DateTime timestamp)
        {
            var keys = itemData.Keys.ToList();
            var values = keys.Select(k => itemData[k]);
            var tags = ConcatMetricTags(valueSource, setItemTags);

            if (valueSource.IsMultidimensional)
            {
                writer.Write(
                    context,
                    valueSource.MultidimensionalName + metricSetItemSuffix,
                    keys,
                    values,
                    tags,
                    timestamp);

                return;
            }

            writer.Write(context, valueSource.Name + metricSetItemSuffix, keys, values, tags, timestamp);
        }
    }
}