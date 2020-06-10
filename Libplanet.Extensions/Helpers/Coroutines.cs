using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Libplanet.Extensions.Helpers
{
    public class CoroutineManager
    {
        private List<Stack<IEnumerator>> coroutines = new List<Stack<IEnumerator>>();

        public bool IsActive { get; set; }

        public void RegisterCoroutine(IEnumerator coroutine)
        {
            var coroutineChain = new Stack<IEnumerator>();
            coroutineChain.Push(coroutine);
            coroutines.Add(coroutineChain);
        }

        public void Start()
        {
            IsActive = true;
            int i = -1;
            while (true)
            {
                if (i < coroutines.Count - 1) i++; else i = 0;
                if (coroutines.Count == 0) break;

                if (!coroutines[i].Peek().MoveNext())
                {
                    coroutines[i].Pop();
                    if (coroutines[i].Count == 0)
                        coroutines.RemoveAt(i);
                    continue;
                }

                var command = coroutines[i].Peek().Current as ICoroutineCommand;
                if (command != null)
                {
                    coroutines[i].Push(command.Execute());
                }
            }
        }

        public void Stop()
        {
            IsActive = false;
            coroutines.Clear();
        }
    }
    public interface ICoroutineCommand
    {
        IEnumerator<object> Execute();
    }

    public sealed class WaitUntil : IEnumerator
    {
        private Func<bool> predicate;

        public bool keepWaiting { 
            get { 
                return !this.predicate(); 
            } 
        }

        public WaitUntil(Func<bool> predicate) {
            this.predicate = predicate;
        }

        public object Current
        {
            get
            {
                return null;
            }
        }

        public bool MoveNext() { return keepWaiting; }
        public void Reset() { }
    }
}
