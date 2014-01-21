using System.Collections.Generic;
using System.Threading;

namespace FileLog
{
    /// <summary>
    /// Queue for a efficient enqueueing and dequeueing between different threads
    /// </summary>
    /// <typeparam name="T">type of ojbect in queue</typeparam>
    class BlockingQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();

        public void Enqueue(T item)
        {
            lock (_queue)
            {
                _queue.Enqueue(item);
                if (_queue.Count == 1)
                {
                    // wake up any blocked dequeue
                    Monitor.PulseAll(_queue);
                }
            }
        }
        //blocks when queue is empty
        public T Dequeue()
        {
            lock (_queue)
            {
                while (_queue.Count == 0)
                {
                    Monitor.Wait(_queue);
                }
                T item = _queue.Dequeue();
                return item;
            }
        }

        public void Clear()
        {
            lock (_queue)
            {
                _queue.Clear();
                Monitor.PulseAll(_queue);
            }
        }
    }
}
