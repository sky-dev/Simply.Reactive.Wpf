using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;

namespace Simply.Reactive.Wpf.Tests
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ISubject<bool> _subject;
        private readonly IDisposable _disposable;
        private TestParentDataSourceViewModel _testParentDataSource;

        public MainWindowViewModel()
        {
            _subject = new BehaviorSubject<bool>(true);
            _disposable = System.Reactive.Linq.Observable.Return(new TestParentDataSourceViewModel(Observable))
                .Delay(TimeSpan.FromSeconds(1))
                .Subscribe(vm => TestParentDataSource = vm);
        }

        public IObservable<bool> Observable
        {
            get { return OncePerSecond().Select(GetTime).Select(AreSecondsOdd); }
        }
        public IObservable<string> ObservableDescription
        {
            get { return Observable.Select(GetObservableDescription); }
        }
        public ISubject<bool> Subject
        {
            get { return _subject; }
        }
        public IObservable<string> SubjectDescription
        {
            get { return Subject.Select(GetSubjectDescription); }
        }
        public object TestTestDataTemplateDataSource
        {
            get { return new TestDataTemplateViewModel(Observable); }
        }
        public TestParentDataSourceViewModel TestParentDataSource
        {
            get { return _testParentDataSource; }
            set { _testParentDataSource = value; OnPropertyChanged(); }
        }
        public IObservable<TestParentObservableDataSourceViewModel> TestParentObservableDataSource
        {
            get { return System.Reactive.Linq.Observable.Return(new TestParentObservableDataSourceViewModel(Observable)).Delay(TimeSpan.FromSeconds(1)); }
        }

        private static IObservable<long> OncePerSecond()
        {
            return System.Reactive.Linq.Observable.Interval(TimeSpan.FromSeconds(1));
        }

        private static DateTime GetTime(long time)
        {
            return DateTime.Now;
        }

        private static bool AreSecondsOdd(DateTime time)
        {
            return DateTime.Now.TimeOfDay.Seconds % 2 == 1;
        }

        private static string GetObservableDescription(bool value)
        {
            return string.Format("Observable value is '{0}'.", value);
        }

        private static string GetSubjectDescription(bool value)
        {
            return string.Format("Subject value is '{0}'.", value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
