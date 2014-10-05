using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;

namespace Simply.Reactive.Wpf.Xaml
{
    internal static class ReactiveBindingExpressions
    {
        public static Func<object, DispatcherScheduler, Action<object>, IDisposable> CreateSubscribeMethod(object observable)
        {
            var observableType = observable.GetType();
            var observableParam = Expression.Parameter(typeof(object), "observable");
            var schedulerParam = Expression.Parameter(typeof(DispatcherScheduler), "scheduler");
            var onNextParam = Expression.Parameter(typeof(Action<Object>), "onNextWrapper");
            var subscribeMethodCall = CreateInnerSubscribeMethod(observableParam, schedulerParam, onNextParam, observableType);
            return Expression.Lambda<Func<object, DispatcherScheduler, Action<object>, IDisposable>>(subscribeMethodCall, observableParam, schedulerParam, onNextParam).Compile();
        }

        private static Expression CreateInnerSubscribeMethod(Expression observableParam, Expression schedulerParam, Expression onNextParam, Type observableType)
        {
            var observableTypeParameter = observableType
                .GetInterfaces()
                .First(i => i.Name.StartsWith("IObservable"))
                .GetGenericArguments()
                .First();            
            var convertToObservable = Expression.Convert(observableParam, observableType);
            var onNextDelegateParam = Expression.Parameter(observableTypeParameter);
            var onNextDelegate = Expression.Lambda(Expression.Invoke(onNextParam, Expression.Convert(onNextDelegateParam, typeof(object))), onNextDelegateParam);
            var callObserveOn = Expression.Call(typeof(DispatcherObservable), "ObserveOn", new[] { observableTypeParameter }, convertToObservable, schedulerParam);
            return Expression.Call(typeof(ObservableExtensions), "Subscribe", new[] { observableTypeParameter }, callObserveOn, onNextDelegate);
        }

        public static Action<object, object> CreateOnNextMethod(object observer)
        {
            var observerType = observer.GetType();
            var observerParam = Expression.Parameter(typeof(object), "observer");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var onNextMethodCall = CreateInnerOnNextMethod(observerParam, valueParam, observerType);
            return Expression.Lambda<Action<object, object>>(onNextMethodCall, observerParam, valueParam).Compile();
        }

        private static Expression CreateInnerOnNextMethod(Expression observerParam, Expression valueParam, Type observerType)
        {
            var convertToObserver = Expression.Convert(observerParam, observerType);
            var observerTypeParameter = observerType
                .GetInterfaces()
                .First(i => i.Name.StartsWith("IObserver"))
                .GetGenericArguments()
                .First();
            var valueParam2 = Expression.Convert(valueParam, observerTypeParameter);
            return Expression.Call(convertToObserver, "OnNext", new Type[0], valueParam2);
        }
    }
}
