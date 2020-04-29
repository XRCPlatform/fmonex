using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.BlockChain.Helpers
{
    internal class CoroutineManager
    {
        private List<Stack<IEnumerator>> coroutines = new List<Stack<IEnumerator>>();

        internal void RegisterCoroutine(IEnumerator coroutine)
        {
            var coroutineChain = new Stack<IEnumerator>();
            coroutineChain.Push(coroutine);
            coroutines.Add(coroutineChain);
        }

        internal void Start()
        {
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
    }
    internal interface ICoroutineCommand
    {
        IEnumerator<object> Execute();
    }

    internal sealed class WaitUntil : IEnumerator
    {
        private Func<bool> predicate;

        public bool keepWaiting { 
            get { 
                return !this.predicate(); 
            } 
        }

        internal WaitUntil(Func<bool> predicate) {
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
