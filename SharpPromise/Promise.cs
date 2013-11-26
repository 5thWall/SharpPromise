using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;

namespace SharpPromise
{
	public class Promise
	{
		public enum PromiseState { Pending, Resolved, Error };

		public Object Value { get; private set; }

		public Exception Error { get; private set; }

		public PromiseState State { get; private set; }

		private Collection<Promise> children = new Collection<Promise>();

		protected Func<Object, Object> resolve = new Func<Object, Object>(delegate {
			(obj) => { return obj; };
		});

		protected Func<Exception, Object> handleError = new Func<Exception, Object> (delegate {
			(ex) => { throw ex; };
		});

		protected Func<Object, Object> progress = new Func<Object, Object>(delegate {
			(obj) => { return obj; };
		});

		public Promise Then(Func<Object, Object> resolve, Func<Exception, Object> handleError = null, Func<Object,Object> progress = null)
		{
			var promise = new Promise();

			promise.resolve = resolve;
			if (handleError != null)
				promise.handleError = handleError;
			if (progress != null)
				promise.progress = progress;

			this.children.Add(promise);

			return promise;
		}

		#region Internals
		protected void Resolve(Object value)
		{
			if (State != PromiseState.Pending)
				return;

			State = PromiseState.Resolved;
			Value = value;

			try
			{
				var newVal = resolve(value);
				EachChild(c => c.Resolve (newVal));
			}

			catch (Exception ex)
			{
				EachChild(c => c.HandleError (ex));
			}
		}

		protected void HandleError(Exception ex)
		{
			if (State != PromiseState.Pending)
				return;

			State = PromiseState.Error;
			Error = ex;

			try
			{
				var value = handleError(ex);
				EachChild(c => c.Resolve(value));
			}

			catch (Exception newEx)
			{
				EachChild(c => c.HandleError(newEx));
			}
		}

		protected void Progress(T value)
		{
			if (State != PromiseState.Pending)
				return;

			try
			{
				var newValue = progress(value);
				EachChild(c => c.progress(newValue));
			}

			catch (Exception) { /* progress exceptions stop chain */ }
		}
		#endregion

		#region Helpers
		private void EachChild(Action<Promise> action)
		{
			foreach (var child in children) action(child);
		}
		#endregion

		public static Promise When(Action<Action<Object>, Action<Exception>, Action<Object>> resolver)
		{
			var promise = new Promise();

			resolver(promise.Resolve, promise.Error, promise.Progress);

			return promise;
		}

		public static Promise When(Action<Action<Object>, Action<Exception>> resolver)
		{
			var promise = new Promise();

			resolver(promise.Resolve, promise.Error);

			return promise;
		}

		public static Promise When(Action<Action<Object>> resolver)
		{
			var promise = new Promise();

			resolver(promise.Resolve);

			return promise;
		}

		public static Promise When(Object obj)
		{
			var promise = new Promise();

			promise.Resolve(obj);

			return promise;
		}
	}
}

