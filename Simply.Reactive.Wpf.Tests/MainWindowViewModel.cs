using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Simply.Reactive.Wpf.Tests
{
    public class MainWindowViewModel
    {
        private bool _isWorking;
        private readonly ISubject<bool> _isWorking2;

        public MainWindowViewModel()
        {
            _isWorking2= new BehaviorSubject<bool>(true);
        }

        public IObservable<bool> IsWorking
        {
            get { return Observable.Interval(TimeSpan.FromSeconds(1)).Select(_ =>
                {
//                    _isWorking = !_isWorking;
//                    _isWorking2.OnNext(_isWorking);
                    return DateTime.Now.TimeOfDay.Seconds % 2 == 1;
                }); }
        }

        public ISubject<bool> IsWorking2
        {
            get { return _isWorking2; }
        }

        public IObservable<string> IsWorkingDescription
        {
            get { return IsWorking.Select(v => 
                string.Format("IsWorking is {0}", v)); }
        }

        public IObservable<string> IsWorking2Description
        {
            get { return IsWorking2.Select(v => 
                string.Format("IsWorking2 is {0}", v)); }
        }
    }
}
