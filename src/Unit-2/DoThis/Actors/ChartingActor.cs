using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ChartingActor : ReceiveActor, IWithUnboundedStash
    {
        #region Messages

        public class InitializeChart
        {
            public InitializeChart(Dictionary<string, Series> initialSeries)
            {
                InitialSeries = initialSeries;
            }

            public Dictionary<string, Series> InitialSeries { get; private set; }
        }

        public class AddSeries
        {
            public Series Series { get; }

            public AddSeries(Series series)
            {
                Series = series;
            }
        }

        public class RemoveSeries
        {
            public Series Series { get; }

            public RemoveSeries(Series series)
            {
                Series = series;
            }
        }

        public class TogglePause { }

        #endregion

        private readonly Chart _chart;
        private readonly Button _pauseButton;
        private Dictionary<string, Series> _seriesIndex;
        private const int MaxPoints = 250;
        private int _xPosCounter = 0;

        public ChartingActor(Chart chart, Button pauseButton) : this(chart, new Dictionary<string, Series>(), pauseButton)
        {
        }

        public ChartingActor(Chart chart, Dictionary<string, Series> seriesIndex, Button pauseButton)
        {
            _chart = chart;
            _seriesIndex = seriesIndex;
            _pauseButton = pauseButton;

            Charting();
        }

        private void Charting()
        {
            Receive<InitializeChart>(msg => HandleInitialize(msg));
            Receive<AddSeries>(msg => HandleAddSeries(msg));
            Receive<RemoveSeries>(rs => HanleRemoveSeries(rs));
            Receive<Metric>(m => HandleMatrics(m));

            Receive<TogglePause>(pause =>
            {
                SetPauseButtonText(true);
                BecomeStacked(Paused);
            });
        }

        private void SetPauseButtonText(bool paused)
        {
            _pauseButton.Text = paused ? "RESUME >" : "PAUSED II";
        }

        private void Paused()
        {
            Receive<Metric>(metric => HandlePausedMatrics(metric));
            Receive<AddSeries>(series => Stash.Stash());
            Receive<RemoveSeries>(series => Stash.Stash());

            Receive<TogglePause>(pause =>
            {
                SetPauseButtonText(false);
                UnbecomeStacked();
                Stash.UnstashAll();
            });

        }


        #region Individual Message Type Handlers

        private void HandleInitialize(InitializeChart ic)
        {
            if (ic.InitialSeries != null)
            {
                //swap the two series out
                _seriesIndex = ic.InitialSeries;
            }

            //delete any existing series
            _chart.Series.Clear();

            var area = _chart.ChartAreas[0];
            area.AxisX.IntervalType = DateTimeIntervalType.Number;
            area.AxisY.IntervalType = DateTimeIntervalType.Number;

            SetChartBoundries();

            if (_seriesIndex.Any())
            {
                foreach (var series in _seriesIndex)
                {
                    series.Value.Name = series.Key;
                    _chart.Series.Add(series.Value);
                }
            }

            SetChartBoundries();
        }

        private void HandleAddSeries(AddSeries series)
        {
            if (!string.IsNullOrEmpty(series.Series.Name) && !_seriesIndex.ContainsKey(series.Series.Name))
            {
                _chart.Series.Add(series.Series);
                _seriesIndex.Add(series.Series.Name, series.Series);
                SetChartBoundries();
            }
        }

        private void HanleRemoveSeries(RemoveSeries series)
        {
            if (!string.IsNullOrEmpty(series.Series.Name) && _seriesIndex.ContainsKey(series.Series.Name))
            {
                var seriesToRemove = _seriesIndex[series.Series.Name];
                _seriesIndex.Remove(series.Series.Name);
                _chart.Series.Remove(seriesToRemove);
                SetChartBoundries();
            }
        }

        private void HandleMatrics(Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) && _seriesIndex.ContainsKey(metric.Series))
            {
                var series = _seriesIndex[metric.Series];
                series.Points.AddXY(_xPosCounter++, metric.CounterValue);
                while (series.Points.Count > MaxPoints) series.Points.RemoveAt(0);
                SetChartBoundries();
            }
        }

        private void HandlePausedMatrics(Metric metric)
        {
            if (!string.IsNullOrEmpty(metric.Series) && _seriesIndex.ContainsKey(metric.Series))
            {
                var series = _seriesIndex[metric.Series];
                series.Points.AddXY(_xPosCounter++, 0.0d);
                while (series.Points.Count > MaxPoints) series.Points.RemoveAt(0);
                SetChartBoundries();
            }
        }


        #endregion

        private void SetChartBoundries()
        {
            double maxAxisX, maxAxisY, minAxisX, minAxisY = 0.0d;

            var allPoints = _seriesIndex.Values.SelectMany(series => series.Points).ToList();
            var yValues = allPoints.SelectMany(point => point.YValues).ToList();

            maxAxisX = _xPosCounter;
            minAxisX = _xPosCounter - MaxPoints;
            maxAxisY = yValues.Count > 0 ? Math.Ceiling(yValues.Max()) : 1.0d;
            minAxisY = yValues.Count > 0 ? Math.Floor(yValues.Min()) : 0.0d;

            if (allPoints.Count > 2)
            {
                var area = _chart.ChartAreas[0];
                area.AxisX.Minimum = minAxisX;
                area.AxisX.Maximum = maxAxisX;
                area.AxisY.Minimum = minAxisY;
                area.AxisY.Maximum = maxAxisY;
            }
        }

        public IStash Stash { get; set; }
    }
}
