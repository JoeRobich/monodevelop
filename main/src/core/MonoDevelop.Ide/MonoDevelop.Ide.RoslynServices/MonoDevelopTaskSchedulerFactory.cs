﻿//
// MonoDevelopTaskSchedulerFactory.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Roslyn.Utilities;
using System.Threading;
using Microsoft.VisualStudio.Threading;

namespace MonoDevelop.Ide.RoslynServices
{
	[ExportWorkspaceService(typeof(IWorkspaceTaskSchedulerFactory), ServiceLayer.Host), Shared]
	class MonoDevelopTaskSchedulerFactory : EditorTaskSchedulerFactory
	{
		readonly IThreadingContext _threadingContext;

		[ImportingConstructor]
		[Obsolete (MefConstruction.ImportingConstructorMessage, error: true)]
		public MonoDevelopTaskSchedulerFactory (IThreadingContext threadingContext, IAsynchronousOperationListenerProvider listenerProvider) : base (listenerProvider)
		{
			_threadingContext = threadingContext;
		}

		public override IWorkspaceTaskScheduler CreateEventingTaskQueue ()
		{
			// When we are creating the workspace, we might not actually have established what the UI thread is, since
			// we might be getting created via MEF. So we'll allow the queue to be created now, and once we actually need
			// to queue something we'll then start using the task queue from there.
			// In Visual Studio, we raise these events on the UI thread. At this point we should know
			// exactly which thread that is.
			return new MonoDevelopTaskScheduler (this);
		}

		class MonoDevelopTaskScheduler : IWorkspaceTaskScheduler
		{
			readonly Lazy<WorkspaceTaskQueue> _queue;
			readonly MonoDevelopTaskSchedulerFactory _factory;

			public MonoDevelopTaskScheduler (MonoDevelopTaskSchedulerFactory factory)
			{
				_factory = factory;
				_queue = new Lazy<WorkspaceTaskQueue> (CreateQueue);
			}

			WorkspaceTaskQueue CreateQueue ()
			{
				// At this point, we have to know what the UI thread is.
				Contract.ThrowIfFalse (_factory._threadingContext.HasMainThread);
				return new WorkspaceTaskQueue (_factory, new JoinableTaskFactoryTaskScheduler (_factory._threadingContext.JoinableTaskFactory));
			}

			public Task ScheduleTask (Action taskAction, string taskName, CancellationToken cancellationToken = default(CancellationToken))
			{
				return _queue.Value.ScheduleTask (taskAction, taskName, cancellationToken);
			}

			public Task<T> ScheduleTask<T> (Func<T> taskFunc, string taskName, CancellationToken cancellationToken = default(CancellationToken))
			{
				return _queue.Value.ScheduleTask (taskFunc, taskName, cancellationToken);
			}

			public Task ScheduleTask (Func<Task> taskFunc, string taskName, CancellationToken cancellationToken = default(CancellationToken))
			{
				return _queue.Value.ScheduleTask (taskFunc, taskName, cancellationToken);
			}

			public Task<T> ScheduleTask<T> (Func<Task<T>> taskFunc, string taskName, CancellationToken cancellationToken = default(CancellationToken))
			{
				return _queue.Value.ScheduleTask (taskFunc, taskName, cancellationToken);
			}
		}

		class JoinableTaskFactoryTaskScheduler : TaskScheduler
		{
			readonly JoinableTaskFactory _joinableTaskFactory;

			public JoinableTaskFactoryTaskScheduler (JoinableTaskFactory joinableTaskFactory)
			{
				_joinableTaskFactory = joinableTaskFactory;
			}

			public override int MaximumConcurrencyLevel => 1;

			protected override IEnumerable<Task> GetScheduledTasks () => null;

			protected override void QueueTask (Task task)
			{
				_joinableTaskFactory.RunAsync (async () => {
					await _joinableTaskFactory.SwitchToMainThreadAsync (alwaysYield: true);
					TryExecuteTask (task);
				});
			}

			protected override bool TryExecuteTaskInline (Task task, bool taskWasPreviouslyQueued)
			{
				if (_joinableTaskFactory.Context.IsOnMainThread) {
					return TryExecuteTask (task);
				}

				return false;
			}
		}
	}
}
