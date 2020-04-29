using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace FreeMarketOne.BlockChain.Helpers
{
    public class CoroutineManager
    {
        List<Stack<IEnumerator>> coroutines = new List<Stack<IEnumerator>>();

        public void RegisterCoroutine(IEnumerator coroutine)
        {
            var coroutineChain = new Stack<IEnumerator>();
            coroutineChain.Push(coroutine);
            coroutines.Add(coroutineChain);
        }

        public void Start()
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
    public interface ICoroutineCommand
    {
        IEnumerator<object> Execute();
    }

    public sealed class WaitUntil : IEnumerator
    {
        Func<bool> m_Predicate;

        public bool keepWaiting { 
            get { 
                return !m_Predicate(); 
            } 
        }

        public WaitUntil(Func<bool> predicate) {
            m_Predicate = predicate;
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
