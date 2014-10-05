using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Windows;
using System.Windows.Data;
using Simply.Reactive.Wpf.Monads;

namespace Simply.Reactive.Wpf.Xaml
{
    public class ReactiveBindingProxy : DependencyObject, IDisposable
    {
        public static readonly DependencyProperty ObservableProperty = DependencyProperty.Register(
            "Observable", typeof(object), typeof(ReactiveBindingProxy), new UIPropertyMetadata(null, OnObservableChanged));

        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(object), typeof(ReactiveBindingProxy), new UIPropertyMetadata(null, OnValueChanged));

        private readonly DependencyObject _uiElementTarget;
        private readonly DependencyProperty _uiElementDependencyProperty;
        private readonly Binding _originalBindingInfo;

        private object _boundProperty;
        private BindingExpressionBase _bindingExpression;
        private Action<object, object> _onNext;
        private Maybe<IDisposable> _subscription = Maybe<IDisposable>.Nothing;

        private object _previousValue;

        public ReactiveBindingProxy(DependencyObject uiElementTarget, DependencyProperty uiElementDependencyProperty, Binding originalBindingInfo)
        {
            _uiElementTarget = uiElementTarget;
            _uiElementDependencyProperty = uiElementDependencyProperty;
            _originalBindingInfo = originalBindingInfo;
        }

        public object Value
        {
            get { return GetValue(ValueProperty); }
            private set { SetValue(ValueProperty, value); }
        }

        public BindingExpressionBase BindTo(object viewModelDataSource, PropertyPath viewModelPath)
        {
            var bindingToViewModelObservable = new Binding { Source = viewModelDataSource, Path = viewModelPath, Mode = BindingMode.OneWay };
            return BindingOperations.SetBinding(this, ObservableProperty, bindingToViewModelObservable);
        }

        private static void OnObservableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ReactiveBindingProxy)d;
            if (e.OldValue != null)
            {
                target.RemoveUiElementBinding(e.OldValue);
            }
            if (e.NewValue != null)
            {
                target.AddUiElementBinding(e.NewValue);
            }
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var target = (ReactiveBindingProxy)d;
            if (e.NewValue == null)
                return;
            if (EqualityComparer<object>.Default.Equals(target._previousValue, e.NewValue))
                return;
            // Setting OnNext will in turn call the OnValueChanged.  There is no way to discern a UIElement trigger from a IObservable trigger.  Comparing previous should be ok.  Everything executes on the UI thread (IObservable subscription and the OnValueChanged call)
            target.OnNext(e.NewValue);
            target._previousValue = e.NewValue;
        }
  
        private void RemoveUiElementBinding(object o)
        {
            ClearOnNext();
            UnbindUiElementFromValueDp();
            MaybeUnsubscribe();
        }

        private void AddUiElementBinding(object boundProperty)
        {
            _boundProperty = boundProperty;
            _onNext = MaybeGetOnNext(boundProperty);
            _bindingExpression = BindUiElementToValueDp();
            _subscription = MaybeSubscribe(boundProperty, OnSourcePropertyChanged);
        }

        private static Action<object, object> MaybeGetOnNext(object boundProperty)
        {
            return IsObserver(boundProperty) ? ReactiveBindingExpressions.CreateOnNextMethod(boundProperty) : null;
        }

        private void ClearOnNext()
        {
            _onNext = null;
        }

        private BindingExpressionBase BindUiElementToValueDp()
        {
            var binding = new Binding
            {
                Source = this,
                Path = new PropertyPath("Value"),
                Converter = _originalBindingInfo.Converter,
                ConverterCulture = _originalBindingInfo.ConverterCulture,
                ConverterParameter = _originalBindingInfo.ConverterParameter,
                ValidatesOnDataErrors = _originalBindingInfo.ValidatesOnDataErrors,
                ValidatesOnExceptions = _originalBindingInfo.ValidatesOnExceptions,
                //Mode = GetBindingMode(boundProperty)
                Mode = BindingMode.TwoWay
                //                Mode = observer == null ? BindingMode.OneWay : BindingMode.TwoWay
            };
            return BindingOperations.SetBinding(_uiElementTarget, _uiElementDependencyProperty, binding);
        }

        private void UnbindUiElementFromValueDp()
        {
            if (_bindingExpression != null)
            {
                BindingOperations.ClearBinding(_uiElementTarget, _uiElementDependencyProperty);
            }
            _bindingExpression = null;
        }

        private static Maybe<IDisposable> MaybeSubscribe(object boundProperty, Action<object> subscription)
        {
            var subscribe = MaybeGetSubscribe(boundProperty);
            return subscribe != null ? subscribe(boundProperty, DispatcherScheduler.Current, subscription).ToMaybe() : Maybe<IDisposable>.Nothing;
        }

        private static Func<object, DispatcherScheduler, Action<object>, IDisposable> MaybeGetSubscribe(object boundProperty)
        {
            return IsObservable(boundProperty) ? ReactiveBindingExpressions.CreateSubscribeMethod(boundProperty) : null;
        }

        private void MaybeUnsubscribe()
        {
            if (_subscription.HasValue)
            {
                _subscription.Value.Dispose();
            }
            _subscription = null; 
        }

        private static bool IsObserver(object boundProperty)
        {
            return boundProperty != null && boundProperty.GetType().GetInterfaces().Any(i => i.Name.StartsWith("IObserver"));
        }

        private static bool IsObservable(object boundProperty)
        {
            return boundProperty != null && boundProperty.GetType().GetInterfaces().Any(i => i.Name.StartsWith("IObservable"));
        }

        private void OnSourcePropertyChanged(object value)
        {
            if (!IsObservable(_boundProperty))
                return;
            Value = value;
        }

        private void OnNext(object value)
        {
            if (_onNext == null)
                return;
            //if (value != null ) // TODO - expression handles this and sets to default(T)
            _onNext(_boundProperty, value);
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
            if (_subscription.HasValue)
                _subscription.Value.Dispose();
        }
    }
}
