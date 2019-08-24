using System.Threading;
using System.Threading.Tasks;

namespace Copren.Async
{
    public abstract class AsyncTask
    {
        public bool IsRunning { get; }
        protected CancellationToken CancellationToken => _cancellationTokenSource.Token;
        private TaskStates State { get; set; } = TaskStates.None;
        private Thread _thread;
        private TaskCompletionSource<object> _taskCompletionSource;
        private CancellationTokenSource _cancellationTokenSource;

        protected AsyncTask()
        {
        }

        public Task StartAsync(CancellationToken ct = default)
        {
            lock (this)
            {
                if (State == TaskStates.Running) return _taskCompletionSource.Task;
                if (State == TaskStates.Stopped) return Task.CompletedTask;

                _taskCompletionSource = new TaskCompletionSource<object>();
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(ct);

                _thread = new Thread(InternalRun);
                _thread.Start();

                return _taskCompletionSource.Task;
            }
        }

        private void InternalRun()
        {
            State = TaskStates.Running;
            Run();
            State = TaskStates.Stopped;
            _taskCompletionSource.SetResult(null);
        }

        protected abstract void Run();

        public Task StopAsync()
        {
            lock (this)
            {
                if (State == TaskStates.None) return Task.CompletedTask;
                if (State == TaskStates.Stopped) return Task.CompletedTask;

                _cancellationTokenSource.Cancel();

                return _taskCompletionSource.Task;
            }
        }

        private enum TaskStates
        {
            None,
            Running,
            Stopped
        }
    }
}