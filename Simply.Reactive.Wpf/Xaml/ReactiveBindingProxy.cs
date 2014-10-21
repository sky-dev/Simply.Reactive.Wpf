using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Simply.Reactive.Wpf.Monads;

namespace Simply.Reactive.Wpf.Xaml
{
    public class ReactiveBindingProxy : DependencyObject, IDisposable
    {
        private readonly DependencyObject _uiElementTarget;
        private readonly DependencyProperty _uiElementDependencyProperty;
        private readonly Binding _originalBindingInfo;

        private object _boundProperty;
        private IMaybe<ReactiveBindingExpressions.OnNext> _onNext = Maybe<ReactiveBindingExpressions.OnNext>.Nothing;
        private IMaybe<ReactiveBindingExpressions.Subscribe> _subscription = Maybe<ReactiveBindingExpressions.Subscribe>.Nothing;

        private object _previousValue;
        private DataContextBinding _dataContextBinding;
        private ObservableBinding _observableBinding;
        private UiElementBinding _uiElementBinding;

        public ReactiveBindingProxy(DependencyObject uiElementTarget, DependencyProperty uiElementDependencyProperty, Binding originalBindingInfo)
        {
            Console.WriteLine("UI Thread is '{0}'", Thread.CurrentThread.ManagedThreadId);
            _uiElementTarget = uiElementTarget;
            _uiElementDependencyProperty = uiElementDependencyProperty;
            _originalBindingInfo = originalBindingInfo;
            _previousValue = GetDefaultValue();

            _dataContextBinding = new DataContextBinding(uiElementTarget, uiElementDependencyProperty, CreateObservableBinding);
        }

        public object Value
        {
            get { return _uiElementBinding != null ? _uiElementBinding.Value : _previousValue; }
        }

        private void CreateObservableBinding(ChangedEventArgs dataContextChangedArgs)
        {
            Console.WriteLine("Path '{0}' on Thread '{1}'", _originalBindingInfo.Path.Path, Thread.CurrentThread.ManagedThreadId);
            if (_observableBinding != null)
            {
                _observableBinding.Dispose();
            }
            if (dataContextChangedArgs.NewValue != null)
            {
                _observableBinding = new ObservableBinding(dataContextChangedArgs.NewValue, _originalBindingInfo.Path, CreateUiElementBinding);
            }
        }

        private void CreateUiElementBinding(ChangedEventArgs observableChangedArgs)
        {
            if (_uiElementBinding != null)
            {
                _uiElementBinding.Dispose();
            }
            if (observableChangedArgs.NewValue != null)
            {
                SubscribeToObservable(observableChangedArgs.NewValue);

                _uiElementBinding = new UiElementBinding(_uiElementTarget, _uiElementDependencyProperty, _originalBindingInfo, _previousValue, OnValueChanged);
            }
        }

        private void SubscribeToObservable(object boundObservable)
        {
            _boundProperty = boundObservable;

            var uiDispatcher = DispatcherScheduler.Current;
            // Creating the expressions can be slow.  Do this on the background thread.
            Task.Factory.StartNew(() =>
                {
                    _onNext = ReactiveBindingExpressions.GetOnNext(boundObservable);
                    var subscription = ReactiveBindingExpressions.GetSubscribe(boundObservable);
                    if (subscription.HasValue)
                    {
                        subscription.Value(_boundProperty, uiDispatcher, OnSourcePropertyChanged);
                        _subscription = subscription;
                    }
                });
        }

        private void OnValueChanged(ChangedEventArgs valueChangedArgs)
        {
            var newValue = valueChangedArgs.NewValue;
            //if (e.NewValue == null)
            //    return;
            if (EqualityComparer<object>.Default.Equals(_previousValue, newValue))
                return;
            // Setting OnNext will in turn call the OnValueChanged.  There is no way to discern a UIElement trigger from a IObservable trigger.  Comparing previous should be ok.  Everything executes on the UI thread (IObservable subscription and the OnValueChanged call)
            OnNext(newValue);
            _previousValue = newValue;
        }

        private void OnSourcePropertyChanged(object value)
        {
            if (_uiElementBinding == null)
                return;
            if (!ReactiveBindingExpressions.IsObservable(_boundProperty))
                return;
            _uiElementBinding.Value = value;
        }

        private void OnNext(object value)
        {
            var onNext = _onNext.Value;
            if (onNext == null)
                return;
            //if (value != null ) // TODO - expression handles this and sets to default(T)
            onNext(_boundProperty, value);
        }

        private object GetDefaultValue()
        {
            return Utility.GetBindingDefaultValue(_uiElementDependencyProperty.PropertyType);
        }

        ~ReactiveBindingProxy()
        {
            // Confirm this is needed
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool isDisposing)
        {
            _onNext = Maybe<ReactiveBindingExpressions.OnNext>.Nothing;
            _subscription = Maybe<ReactiveBindingExpressions.Subscribe>.Nothing;
            if (_uiElementBinding != null)
            {
                _uiElementBinding.Dispose();
                _uiElementBinding = null;
            }
            if (_observableBinding != null)
            {
                _observableBinding.Dispose();
                _observableBinding = null;
            }
            if (_dataContextBinding != null)
            {
                _dataContextBinding.Dispose();
                _dataContextBinding = null;
            }

            //if (_subscription.HasValue)
            //    _subscription.Value.Dispose();
        }
    }
}
