using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Data;

namespace Simply.Reactive.Wpf.Xaml
{
    public sealed class ObservableBinding : DependencyObject, IDisposable
    {
        public static readonly DependencyProperty ObservableProperty = DependencyProperty.Register(
            "Observable", typeof(object), typeof(ObservableBinding), new UIPropertyMetadata(null, OnObservableChanged));

        private readonly object _lock = new object();

        private readonly Subject<ChangedEventArgs> _observables = new Subject<ChangedEventArgs>();
        private readonly BindingExpressionBase _observableBinding;
        private bool _isDisposed;

        public ObservableBinding(object dataContext, PropertyPath path, Action<ChangedEventArgs> subscriber)
        {
            _observables.ObserveOn(DispatcherScheduler.Current).Subscribe(subscriber);
            var binding = new Binding { Source = dataContext, Path = path, Mode = BindingMode.OneWay };
            _observableBinding = BindingOperations.SetBinding(this, ObservableProperty, binding);
        }

        private static void OnObservableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ObservableBinding)d;
            target.OnValueChanged(e);
        }

        private void OnValueChanged(DependencyPropertyChangedEventArgs e)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;
                _observables.OnNext(new ChangedEventArgs(e.OldValue, e.NewValue));
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                Dispatcher.Invoke(() => BindingOperations.ClearBinding(this, ObservableProperty));
                _observables.Dispose();
                _isDisposed = true;                 
            }
        }
    }
}