using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Windows;
using System.Windows.Data;

namespace Simply.Reactive.Wpf.Xaml
{
    public sealed class UiElementBinding : DependencyObject, IDisposable
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(object), typeof(UiElementBinding), new UIPropertyMetadata(null, OnValueChanged));

        private readonly object _lock = new object();
        private readonly DependencyObject _uiElementTarget;
        private readonly DependencyProperty _uiElementDependencyProperty;
        private readonly Subject<ChangedEventArgs> _values = new Subject<ChangedEventArgs>();
        private readonly BindingExpressionBase _observableBinding;
        private bool _isDisposed;

        public UiElementBinding(DependencyObject uiElementTarget, DependencyProperty uiElementDependencyProperty, Binding originalBinding, object initialValue, Action<ChangedEventArgs> subscriber)
        {
            _uiElementTarget = uiElementTarget;
            _uiElementDependencyProperty = uiElementDependencyProperty;
            Value = initialValue;

            _values.ObserveOn(DispatcherScheduler.Current).Subscribe(subscriber);

            var binding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Value"),
                Converter = originalBinding.Converter,
                ConverterCulture = originalBinding.ConverterCulture,
                ConverterParameter = originalBinding.ConverterParameter,
                ValidatesOnDataErrors = originalBinding.ValidatesOnDataErrors,
                ValidatesOnExceptions = originalBinding.ValidatesOnExceptions,
                UpdateSourceTrigger = originalBinding.UpdateSourceTrigger,
                StringFormat = originalBinding.StringFormat,
                Mode = BindingMode.TwoWay
            };
            _observableBinding = BindingOperations.SetBinding(uiElementTarget, uiElementDependencyProperty, binding);
        }

        public object Value
        {
            get { return GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (UiElementBinding)d;
            target.OnValueChanged(e);
        }

        private void OnValueChanged(DependencyPropertyChangedEventArgs e)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;
                _values.OnNext(new ChangedEventArgs(e.OldValue, e.NewValue));
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (Thread.CurrentThread.ManagedThreadId == Dispatcher.Thread.ManagedThreadId)
                {
                    BindingOperations.ClearBinding(_uiElementTarget, _uiElementDependencyProperty);
                }

                _values.Dispose();
                _isDisposed = true;
            }
        }
    }
}