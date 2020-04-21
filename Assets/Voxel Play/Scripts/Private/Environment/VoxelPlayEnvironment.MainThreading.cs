using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace VoxelPlay {
				
	public partial class VoxelPlayEnvironment {

		Thread mainThread;
		object lockObject = new object();
		readonly List<Action> actions = new List<Action>();

		public void ExecuteInMainThread(Action action) {
			if (Thread.CurrentThread == mainThread) {
				action();
			}
			else {
				if (actions == null) {
					return;
				}
				lock (lockObject) {
					actions.Add(action);
				}
			}
		}

		void InitMainThreading() {
			mainThread = Thread.CurrentThread;
		}

		void ProcessThreadMessages() {
			lock (lockObject) {
				int count = actions.Count;
				for (int k = 0; k < count; k++) {
					actions[k]();
				}
				actions.Clear();
			}
		}

		public void DebugLogInMainThread(string message) {
			ExecuteInMainThread(delegate() {
					Debug.Log(message);
				});
		}

	}

}