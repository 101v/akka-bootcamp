using System.Windows.Forms;
using Akka.Actor;

namespace ChartApp.Actors
{
    public class ButtonToggleActor : UntypedActor
    {
        #region Message types

        public class Toggle { }

        #endregion

        private readonly IActorRef _coordinatorActor;
        private readonly Button _button;
        private readonly CounterType _counterType;
        private bool _isToogleOn;

        public ButtonToggleActor(IActorRef coordinatorActor, Button button, CounterType counterType, bool isToogleOn = true)
        {
            _coordinatorActor = coordinatorActor;
            _button = button;
            _counterType = counterType;
            _isToogleOn = isToogleOn;
        }


        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case Toggle _ when _isToogleOn:
                    _coordinatorActor.Tell(new Unwatch(_counterType));
                    FlipToggle();
                    break;
                case Toggle _ when !_isToogleOn:
                    _coordinatorActor.Tell(new Watch(_counterType));
                    FlipToggle();
                    break;
                default:
                    Unhandled(message);
                    break;
            }
        }

        private void FlipToggle()
        {
            _isToogleOn = !_isToogleOn;
            _button.Text = $@"{_counterType.ToString().ToUpperInvariant()} ({(_isToogleOn ? "ON" : "OFF")})";
        }
    }
}