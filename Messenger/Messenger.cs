using System;
using System.Collections.Generic;

namespace Messenger
{
	public enum MessengerMode
	{
		REQUIRE_LISTENER,
		NO_LISTENER
	}

	public delegate void Callback();
	public delegate void Callback<T>(T arg1);
	public delegate void Callback<T, U>(T arg1, U arg2);

	static internal class MessengerInternal
	{
		//--------------------------------------------------------------------------
		#region Fields

		static public Dictionary<String, Delegate> _eventTable = new Dictionary<String, Delegate>();
		static public readonly MessengerMode DEFAULT_MODE = MessengerMode.REQUIRE_LISTENER;

		#endregion

		//--------------------------------------------------------------------------
		#region Class methods

		/// <summary>
		/// Adds event key, as <see cref="String"/>, to <see cref="_eventTable"/> if not already present.
		/// Adds <see cref="Delegate"/> as value to key, if delegate signatures match.
		/// </summary>
		/// <param name="eventType">Event key to be verified and added to <see cref="_eventTable"/></param>
		/// <param name="listener"><see cref="Delegate"/> to verified and added to <see cref="_eventTable"/></param>
		static public void OnListenerAdding(String eventType, Delegate listener)
		{
			if (!_eventTable.ContainsKey(eventType))
			{
				_eventTable.Add(eventType, null);
			}

			Delegate del = _eventTable[eventType];
			if (del != null && del.GetType() != listener.GetType())
			{
				throw new ListenerException(string.Format("Attempting to add listener with inconsistent signature for event type {0}. Current listeners have type {1} and listener being added has type {2}", eventType, del.GetType().Name, listener.GetType().Name));
			}
		}

		/// <summary>
		/// Checks if key exists within <see cref="_eventTable"/>.
		/// Removes <see cref="Delegate"/> value if exists and matches signature.
		/// </summary>
		/// <param name="eventType">Event key to be checked</param>
		/// <param name="listener"><see cref="Delegate"/> value to be verified and removed from <see cref="_eventTable"/></param>
		static public void OnListenerRemoving(String eventType, Delegate listener)
		{
			if (_eventTable.ContainsKey(eventType))
			{
				Delegate del = _eventTable[eventType];

				if (del == null)
				{
					throw new ListenerException(string.Format("Attempting to remove listener with for event type {0} but current listener is null.", eventType));
				}
				else if (del.GetType() != listener.GetType())
				{
					throw new ListenerException(string.Format("Attempting to remove listener with inconsistent signature for event type {0}. Current listeners have type {1} and listener being removed has type {2}", eventType, del.GetType().Name, listener.GetType().Name));
				}
			}
			else
			{
				throw new ListenerException(string.Format("Attempting to remove listener for type {0} but Messenger doesn't know about this event type.", eventType));
			}
		}

		/// <summary>
		/// Removes key from <see cref="_eventTable"/> if not other <see cref="Delegate"/> value exists.
		/// </summary>
		/// <param name="eventType">Event key to check</param>
		static public void OnListenerRemoved(String eventType)
		{
			if (_eventTable[eventType] == null)
			{
				_eventTable.Remove(eventType);
			}
		}

		/// <summary>
		/// Verifies that event key exists if mode is set to <see cref="MessengerMode.REQUIRE_LISTENER"/>.
		/// </summary>
		/// <param name="eventType">Event key of current broadcast</param>
		/// <param name="mode"><see cref="MessengerMode"/> of current broadcast</param>
		static public void OnBroadcasting(String eventType, MessengerMode mode)
		{
			if (mode == MessengerMode.REQUIRE_LISTENER && !_eventTable.ContainsKey(eventType))
			{
				throw new BroadcastException(string.Format("Broadcasting message {0} but no listener found.", eventType));
			}
		}

		/// <summary>
		/// Throws a <see cref="BroadcastException"/> if event key has listeners with differing signatures.
		/// </summary>
		/// <param name="eventType">Event key with differing listener signatures</param>
		/// <returns><see cref="BroadcastException"/></returns>
		static public BroadcastException CreateBroadcastSignatureException(String eventType)
		{
			return new BroadcastException(string.Format("Broadcasting message {0} but listeners have a different signature than the broadcaster.", eventType));
		}

		#endregion

		//--------------------------------------------------------------------------
		#region Exception definitions

		public class ListenerException : Exception
		{
			public ListenerException(string message) : base(message) { }
		}

		public class BroadcastException : Exception
		{
			public BroadcastException(string message) : base(message) { }
		}

		#endregion
	}

	static public class Messenger
	{
		//--------------------------------------------------------------------------
		#region Fields

		private static Dictionary<String, Delegate> _eventTable = MessengerInternal._eventTable;

		#endregion

		//--------------------------------------------------------------------------
		#region Class methods

		/// <summary>
		/// Adds <see cref="Callback"/> to event key.
		/// </summary>
		/// <param name="eventType">Event key which <see cref="Callback"/> will be added to</param>
		/// <param name="handler"><see cref="Callback"/> to be called when broadcast received</param>
		public static void AddListener(String eventType, Callback handler)
		{
			MessengerInternal.OnListenerAdding(eventType, handler);
			_eventTable[eventType] = (Callback)_eventTable[eventType] + handler;
		}

		/// <summary>
		/// Removes <see cref="Callback"/> from event key.
		/// </summary>
		/// <param name="eventType">Event key to remove <see cref="Callback"/> from</param>
		/// <param name="handler"><see cref="Callback"/> which is to be removed</param>
		public static void RemoveListener(String eventType, Callback handler)
		{
			MessengerInternal.OnListenerRemoving(eventType, handler);
			_eventTable[eventType] = (Callback)_eventTable[eventType] - handler;
			MessengerInternal.OnListenerRemoved(eventType);
		}

		/// <summary>
		/// Broadcasts a message using the event key.
		/// Will send with via the specified <see cref="MessengerMode"/>.
		/// </summary>
		/// <param name="eventType">Event key to broadcast</param>
		/// <param name="mode">Mode in which to broadcast message</param>
		public static void Broadcast(String eventType, MessengerMode mode)
		{
			MessengerInternal.OnBroadcasting(eventType, mode);
			Delegate del;
			if (_eventTable.TryGetValue(eventType, out del))
			{
				Callback callback = del as Callback;
				if (callback != null)
				{
					callback();
				}
				else
				{
					throw MessengerInternal.CreateBroadcastSignatureException(eventType);
				}
			}
		}

		/// <summary>
		/// Broadcasts a message using the event key.
		/// Will send via the <see cref="MessengerInternal.DEFAULT_MODE"/>
		/// </summary>
		/// <param name="eventType">Event key to broadcast</param>
		public static void Broadcast(String eventType)
		{
			Broadcast(eventType, MessengerInternal.DEFAULT_MODE);
		}

		#endregion
	}

	public static class Messenger<T>
	{
		//--------------------------------------------------------------------------
		#region Fields

		private static Dictionary<String, Delegate> _eventTable = MessengerInternal._eventTable;

		#endregion

		//--------------------------------------------------------------------------
		#region Class methods

		/// <summary>
		/// Adds <see cref="Callback"/> to event key.
		/// </summary>
		/// <param name="eventType">Event key which <see cref="Callback"/> will be added to</param>
		/// <param name="handler"><see cref="Callback"/> to be called when broadcast received</param>
		public static void AddListener(String eventType, Callback<T> handler)
		{
			MessengerInternal.OnListenerAdding(eventType, handler);
			_eventTable[eventType] = (Callback<T>)_eventTable[eventType] + handler;
		}

		/// <summary>
		/// Removes <see cref="Callback"/> from event key.
		/// </summary>
		/// <param name="eventType">Event key to remove <see cref="Callback"/> from</param>
		/// <param name="handler"><see cref="Callback"/> which is to be removed</param>
		public static void RemoveListener(String eventType, Callback<T> handler)
		{
			MessengerInternal.OnListenerRemoving(eventType, handler);
			_eventTable[eventType] = (Callback<T>)_eventTable[eventType] - handler;
			MessengerInternal.OnListenerRemoved(eventType);
		}

		/// <summary>
		/// Broadcasts a message using the event key.
		/// Will send with via the specified <see cref="MessengerMode"/>.
		/// </summary>
		/// <param name="eventType">Event key to broadcast</param>
		/// <param name="arg1">Argument to be sent with broadcast</param>
		/// <param name="mode">Mode in which to broadcast message</param>
		public static void Broadcast(String eventType, T arg1, MessengerMode mode)
		{
			MessengerInternal.OnBroadcasting(eventType, mode);
			Delegate del;
			if (_eventTable.TryGetValue(eventType, out del))
			{
				Callback<T> callback = del as Callback<T>;
				if (callback != null)
				{
					callback(arg1);
				}
				else
				{
					throw MessengerInternal.CreateBroadcastSignatureException(eventType);
				}
			}
		}

		/// <summary>
		/// Broadcasts a message using the event key.
		/// Will send via the <see cref="MessengerInternal.DEFAULT_MODE"/>
		/// </summary>
		/// <param name="eventType">Event key to broadcast</param>
		/// <param name="arg1">Argument to be sent with broadcast</param>
		public static void Broadcast(String eventType, T arg1)
		{
			Broadcast(eventType, arg1, MessengerInternal.DEFAULT_MODE);
		}

		#endregion
	}

	public static class Messenger<T, U>
	{
		//--------------------------------------------------------------------------
		#region Fields

		private static Dictionary<String, Delegate> _eventTable = MessengerInternal._eventTable;

		#endregion

		//--------------------------------------------------------------------------
		#region Class methods

		/// <summary>
		/// Adds <see cref="Callback"/> to event key.
		/// </summary>
		/// <param name="eventType">Event key which <see cref="Callback"/> will be added to</param>
		/// <param name="handler"><see cref="Callback"/> to be called when broadcast received</param>
		public static void AddListener(String eventType, Callback<T, U> handler)
		{
			MessengerInternal.OnListenerAdding(eventType, handler);
			_eventTable[eventType] = (Callback<T, U>)_eventTable[eventType] + handler;
		}

		/// <summary>
		/// Removes <see cref="Callback"/> from event key.
		/// </summary>
		/// <param name="eventType">Event key to remove <see cref="Callback"/> from</param>
		/// <param name="handler"><see cref="Callback"/> which is to be removed</param>
		public static void RemoveListener(String eventType, Callback<T, U> handler)
		{
			MessengerInternal.OnListenerRemoving(eventType, handler);
			_eventTable[eventType] = (Callback<T, U>)_eventTable[eventType] - handler;
			MessengerInternal.OnListenerRemoved(eventType);
		}

		/// <summary>
		/// Broadcasts a message using the event key.
		/// Will send with via the specified <see cref="MessengerMode"/>.
		/// </summary>
		/// <param name="eventType">Event key to broadcast</param>
		/// <param name="arg1">First argument to be sent with broadcast</param>
		/// <param name="arg2">Second argument to be sent with broadcast</param>
		/// <param name="mode">Mode in which to broadcast message</param>
		public static void Broadcast(String eventType, T arg1, U arg2, MessengerMode mode)
		{
			MessengerInternal.OnBroadcasting(eventType, mode);
			Delegate del;
			if (_eventTable.TryGetValue(eventType, out del))
			{
				Callback<T, U> callback = del as Callback<T, U>;
				if (callback != null)
				{
					callback(arg1, arg2);
				}
				else
				{
					throw MessengerInternal.CreateBroadcastSignatureException(eventType);
				}
			}
		}

		/// <summary>
		/// Broadcasts a message using the event key.
		/// Will send via the <see cref="MessengerInternal.DEFAULT_MODE"/>
		/// </summary>
		/// <param name="eventType">Event key to broadcast</param>
		/// <param name="arg1">First argument to be sent with broadcast</param>
		/// <param name="arg2">Second argument to be sent with broadcast</param>
		public static void Broadcast(String eventType, T arg1, U arg2)
		{
			Broadcast(eventType, arg1, arg2, MessengerInternal.DEFAULT_MODE);
		}

		#endregion
	}
}
