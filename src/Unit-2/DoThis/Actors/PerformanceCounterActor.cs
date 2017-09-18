using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class PerformanceCounterActor : UntypedActor
    {
        private readonly string _seriesName;
        private readonly Func<PerformanceCounter> _peformanceCounterGenerator;
        private PerformanceCounter _counter;

        private readonly HashSet<IActorRef> _subscriptions;
        private readonly ICancelable _cancelPublishing;

        public PerformanceCounterActor(string seriesName, Func<PerformanceCounter> peformanceCounterGenerator)
        {
            _seriesName = seriesName;
            _peformanceCounterGenerator = peformanceCounterGenerator;
            _subscriptions = new HashSet<IActorRef>();
            _cancelPublishing = new Cancelable(Context.System.Scheduler);

        }

        protected override void PreStart()
        {
            _counter = _peformanceCounterGenerator();
            Context.System.Scheduler.ScheduleTellRepeatedly(
                TimeSpan.FromMilliseconds(250),
                TimeSpan.FromMilliseconds(250),
                Self,
                new GatherMetrics(),
                Self,
                _cancelPublishing);
        }

        protected override void PostStop()
        {
            try
            {
                _cancelPublishing.Cancel();
                _counter.Dispose();
            }
            catch
            {
            }
            finally
            {
                base.PostStop();
            }
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case GatherMetrics _:
                    var metric = new Metric(_seriesName, _counter.NextValue());
                    foreach (var actorRef in _subscriptions)
                    {
                        actorRef.Tell(metric);
                    }
                    break;
                case SubscribeCounter sc:
                    _subscriptions.Add(sc.Subscriber);
                    break;
                case UnsubscribeCounter uc:
                    _subscriptions.Remove(uc.Subscriber);
                    break;
            }
        }
    }
}