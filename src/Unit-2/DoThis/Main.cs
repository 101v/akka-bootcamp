﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using Akka.Actor;
using Akka.Util.Internal;
using ChartApp.Actors;

namespace ChartApp
{
    public partial class Main : Form
    {
        private IActorRef _chartActor;
        private IActorRef _coordinatorActor;
        private IDictionary<CounterType, IActorRef> _toggleActors = new Dictionary<CounterType, IActorRef>();

        private readonly AtomicCounter _seriesCounter = new AtomicCounter(1);

        public Main()
        {
            InitializeComponent();
        }

        #region Initialization


        private void Main_Load(object sender, EventArgs e)
        {
            _chartActor = Program.ChartActors.ActorOf(Props.Create(() => new ChartingActor(sysChart, btnPause)), "charting");
            _chartActor.Tell(new ChartingActor.InitializeChart(null));

            _coordinatorActor =
                Program.ChartActors.ActorOf(Props.Create(() => new PerformanceCounterCoordinator(_chartActor)),
                    "counters");

            _toggleActors[CounterType.Cpu] = Program.ChartActors
                .ActorOf(Props.Create(() => new ButtonToggleActor(_coordinatorActor, btnCPU, CounterType.Cpu, false))
                    .WithDispatcher("akka.actor.synchronized-dispatcher"));

            _toggleActors[CounterType.Memory] = Program.ChartActors
                .ActorOf(Props.Create(() => new ButtonToggleActor(_coordinatorActor, btnMemory, CounterType.Memory, false))
                    .WithDispatcher("akka.actor.synchronized-dispatcher"));

            _toggleActors[CounterType.Disk] = Program.ChartActors
                .ActorOf(Props.Create(() => new ButtonToggleActor(_coordinatorActor, btnDisk, CounterType.Disk, false))
                    .WithDispatcher("akka.actor.synchronized-dispatcher"));

        }

        private void Main_FormClosing(object sender, FormClosingEventArgs e)
        {
            //shut down the charting actor
            _chartActor.Tell(PoisonPill.Instance);

            //shut down the ActorSystem
            Program.ChartActors.Terminate();
        }

        #endregion

        private void btnCPU_Click(object sender, EventArgs e)
        {
            _toggleActors[CounterType.Cpu].Tell(new ButtonToggleActor.Toggle());
        }

        private void btnMemory_Click(object sender, EventArgs e)
        {
            _toggleActors[CounterType.Memory].Tell(new ButtonToggleActor.Toggle());
        }

        private void btnDisk_Click(object sender, EventArgs e)
        {
            _toggleActors[CounterType.Disk].Tell(new ButtonToggleActor.Toggle());
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            _chartActor.Tell(new ChartingActor.TogglePause());
        }
    }
}
