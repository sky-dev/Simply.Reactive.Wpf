using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Simply.Reactive.Wpf.Monads;

namespace Simply.Reactive.Wpf.Xaml
{
    internal static class ReactiveBindingExpressions
    {
        public delegate IDisposable Subscribe(object observable, DispatcherScheduler dispatcherScheduler, Action<object> onNext);
        public delegate void OnNext(object observer, object value);

        private static readonly ConcurrentDictionary<Type, Subscribe> SubscribeCache = new ConcurrentDictionary<Type, Subscribe>();
        private static readonly ConcurrentDictionary<Type, OnNext> OnNextCache = new ConcurrentDictionary<Type, OnNext>();

        public static IMaybe<Subscribe> GetSubscribe(object boundProperty)
        {
            return IsObservable(boundProperty) ? GetSubscribeMethod(boundProperty).ToMaybe() : Maybe<Subscribe>.Nothing;
        }

        public static bool IsObservable(object boundProperty)
        {
            return boundProperty != null && boundProperty.GetType().GetInterfaces().Any(i => i.Name.StartsWith("IObservable"));
        }

        public static Subscribe GetSubscribeMethod(object observable)
        {
            Subscribe subscribe;
            var key = observable.GetType();
            if (!SubscribeCache.TryGetValue(key, out subscribe))
            {
                subscribe = CreateSubscribeMethod(observable);
                SubscribeCache[key] = subscribe;
            }
            return subscribe;
        }

        public static Subscribe CreateSubscribeMethod(object observable)
        {
            var observableType = observable.GetType();
            var observableParam = Expression.Parameter(typeof(object), "observable");
            var schedulerParam = Expression.Parameter(typeof(DispatcherScheduler), "scheduler");
            var onNextParam = Expression.Parameter(typeof(Action<Object>), "onNextWrapper");
            var subscribeMethodCall = CreateInnerSubscribeMethod(observableParam, schedulerParam, onNextParam, observableType);
            return Expression.Lambda<Subscribe>(subscribeMethodCall, observableParam, schedulerParam, onNextParam).Compile();
        }

        private static Expression CreateInnerSubscribeMethod(Expression observableParam, Expression schedulerParam, Expression onNextParam, Type observableType)
        {
            var observableTypeParameter = GetObservableTypeParameter(observableType);
            var convertToObservable = Expression.Convert(observableParam, observableType);
            var onNextDelegateParam = Expression.Parameter(observableTypeParameter);
            var onNextDelegate = Expression.Lambda(Expression.Invoke(onNextParam, Expression.Convert(onNextDelegateParam, typeof(object))), onNextDelegateParam);
            var callObserveOn = Expression.Call(typeof(DispatcherObservable), "ObserveOn", new[] { observableTypeParameter }, convertToObservable, schedulerParam);
            return Expression.Call(typeof(ObservableExtensions), "Subscribe", new[] { observableTypeParameter }, callObserveOn, onNextDelegate);
        }

        public static Type GetObservableTypeParameter(Type observableType)
        {
            return observableType
                .GetInterfaces()
                .First(i => i.Name.StartsWith("IObservable"))
                .GetGenericArguments()
                .First();  
        }

        public static IMaybe<OnNext> GetOnNext(object boundProperty)
        {
            return IsObserver(boundProperty) ? GetOnNextMethod(boundProperty).ToMaybe() : Maybe<OnNext>.Nothing;
        }

        public static bool IsObserver(object boundProperty)
        {
            return boundProperty != null && boundProperty.GetType().GetInterfaces().Any(i => i.Name.StartsWith("IObserver"));
        }

        public static OnNext GetOnNextMethod(object observer)
        {
            OnNext onNext;
            var key = observer.GetType();
            if (!OnNextCache.TryGetValue(key, out onNext))
            {
                onNext = CreateOnNextMethod(observer);
                OnNextCache[key] = onNext;
            }
            return onNext;
        }

        public static OnNext CreateOnNextMethod(object observer)
        {
            var observerType = observer.GetType();
            var observerParam = Expression.Parameter(typeof(object), "observer");
            var valueParam = Expression.Parameter(typeof(object), "value");
            var onNextMethodCall = CreateInnerOnNextMethod(observerParam, valueParam, observerType);
            return Expression.Lambda<OnNext>(onNextMethodCall, observerParam, valueParam).Compile();
        }

        private static Expression CreateInnerOnNextMethod(Expression observerParam, Expression valueParam, Type observerType)
        {
            var convertToObserver = Expression.Convert(observerParam, observerType);
            var observerTypeParameter = GetObserverTypeParameter(observerType);
            var valueParam2 = Expression.Convert(valueParam, observerTypeParameter);
            return Expression.Call(convertToObserver, "OnNext", new Type[0], valueParam2);
        }

        public static Type GetObserverTypeParameter(Type observerType)
        {
            return observerType
                .GetInterfaces()
                .First(i => i.Name.StartsWith("IObserver"))
                .GetGenericArguments()
                .First();
        }
    }
}
