using System;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;

namespace Simply.Reactive.Wpf.Tests
{
    public class TestDataTemplateViewModel : INotifyPropertyChanged
    {
        private readonly IObservable<bool> _observable;
        private bool _nonObservable = true;

        public TestDataTemplateViewModel(IObservable<bool> observable)
        {
            _observable = observable;
        }

        public IObservable<bool> Observable
        {
            get { return _observable; }
        }
        public IObservable<string> ObservableDescription
        {
            get { return Observable.Select(GetObservableDescription); }
        }
        public bool NonObservable
        {
            get { return _nonObservable; }
            set { _nonObservable = value; OnPropertyChanged(); OnPropertyChanged("NonObservableDescription"); }
        }
        public string NonObservableDescription
        {
            get { return GetNonObservableDescription(NonObservable); }
        }

        private static string GetObservableDescription(bool value)
        {
            return string.Format("Observable value is '{0}'.", value);
        }

        private static string GetNonObservableDescription(bool value)
        {
            return string.Format("NonObservable value is '{0}'.", value);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
