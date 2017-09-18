using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    #region Message types

    public class Watch
    {
        public CounterType Counter { get; }

        public Watch(CounterType counter)
        {
            Counter = counter;
        }
    }

    public class Unwatch
    {
        public CounterType Counter { get; }

        public Unwatch(CounterType counter)
        {
            Counter = counter;
        }
    }

    #endregion
    public class PerformanceCounterCoordinator : ReceiveActor
    {
        private static readonly Dictionary<CounterType, Func<PerformanceCounter>> CounterGenerator =
            new Dictionary<CounterType, Func<PerformanceCounter>>()
            {
                {CounterType.Cpu, () => new PerformanceCounter("Processor", "% Processor Time", "_Total", true)},
                {CounterType.Memory, () => new PerformanceCounter("Memory", "% Committed Bytes In Use", true)},
                {CounterType.Disk, () => new PerformanceCounter("LogicalDisk", "% Disk Time", "_Total", true)}
            };

        private static readonly Dictionary<CounterType, Func<Series>> CounterSeries =
            new Dictionary<CounterType, Func<Series>>()
            {
                {
                    CounterType.Cpu, () => new Series(CounterType.Cpu.ToString())
                    {
                        ChartType = SeriesChartType.SplineArea,
                        Color = Color.DarkGreen
                    }
                },
                {
                    CounterType.Memory, () => new Series(CounterType.Memory.ToString())
                    {
                        ChartType = SeriesChartType.FastLine,
                        Color = Color.MediumBlue
                    }
                },
                {
                    CounterType.Disk, () => new Series(CounterType.Disk.ToString())
                    {
                        ChartType = SeriesChartType.SplineArea,
                        Color = Color.DarkRed
                    }
                }
            };

        private Dictionary<CounterType, IActorRef> _counterActors;
        private IActorRef _chartingActor;


        public PerformanceCounterCoordinator(IActorRef chartingActor) : this(chartingActor, new Dictionary<CounterType, IActorRef>())
        {
        }

        public PerformanceCounterCoordinator(IActorRef chartingActor, Dictionary<CounterType, IActorRef> counterActors)
        {
            this._chartingActor = chartingActor;
            this._counterActors = counterActors;

            Receive<Watch>(watch =>
            {
                if (!_counterActors.ContainsKey(watch.Counter))
                {
                    var counterActor = Context.ActorOf(Props.Create(() =>
                        new PerformanceCounterActor(watch.Counter.ToString(), CounterGenerator[watch.Counter])));
                    _counterActors[watch.Counter] = counterActor;
                }

                _chartingActor.Tell(new ChartingActor.AddSeries(CounterSeries[watch.Counter]()));

                _counterActors[watch.Counter].Tell(new SubscribeCounter(watch.Counter, _chartingActor));
            });

            Receive<Unwatch>(unwatch =>
            {
                if (!_counterActors.ContainsKey(unwatch.Counter))
                {
                    var counterActor = _counterActors[unwatch.Counter];
                }

                _counterActors[unwatch.Counter].Tell(new UnsubscribeCounter(unwatch.Counter, _chartingActor));
                _chartingActor.Tell(new ChartingActor.RemoveSeries(CounterSeries[unwatch.Counter]()));

            });
        }
    }
}