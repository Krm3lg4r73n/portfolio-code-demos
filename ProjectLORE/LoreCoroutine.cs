using System.Collections;
using UnityEngine;

namespace Lore
{
    public enum ELoreCoroutineState
    {
        None,
        Poked,
        Returned,
        Finished,
        Cancelled
    }

    public class LoreCoroutine<T>
    {

        private bool m_Paused = false;

        public ELoreCoroutineState state { get { return m_State; } }
        private ELoreCoroutineState m_State = ELoreCoroutineState.None;

        public T value { get { return m_ReturnVal; } }
        private T m_ReturnVal = default(T);

        public System.Exception exception { get { return m_Exception; } }
        private System.Exception m_Exception = null;

        public bool isRunning
        {
            get
            {
                return m_State == ELoreCoroutineState.None ||
                        m_State == ELoreCoroutineState.Poked;
            }
        }

        public IEnumerator routineState;


        public LoreCoroutine(IEnumerator coroutine)
        {
            routineState = this.InternalRoutine(coroutine);
        }

        public void Cancel()
        {
            if (state == ELoreCoroutineState.Returned ||
                state == ELoreCoroutineState.Finished)
                return;
            m_State = ELoreCoroutineState.Cancelled;
        }

        public void Poke()
        {
            if (state == ELoreCoroutineState.Returned ||
                state == ELoreCoroutineState.Finished ||
                state == ELoreCoroutineState.Cancelled)
                return;
            m_State = ELoreCoroutineState.Poked;
        }


        private IEnumerator InternalRoutine(IEnumerator coroutine)
        {
            m_ReturnVal = default(T);
            m_Exception = null;
            m_State = ELoreCoroutineState.None;
            m_Paused = false;

            //wait one frame for (other/previous) cancelled routines to finish
            yield return null;

            if (m_State == ELoreCoroutineState.Cancelled)
            {
                //routine was cancelled before it could start
                m_Exception = new CoroutineCancelledException();
                yield break;
            }

            while (true)
            {
                //inspect yield
                object yielded = coroutine.Current;
                if (yielded != null)
                {
                    if (yielded is T)
                    {
                        m_ReturnVal = (T)yielded;
                        m_State = ELoreCoroutineState.Returned;
                        yield break;
                    }
                    else if (yielded is LoreYieldInstruction)
                    {
                        var yieldInstruction = (LoreYieldInstruction)yielded;
                        if (m_State == ELoreCoroutineState.Cancelled)
                        {
                            yieldInstruction.OnStateChange(ELoreCoroutineState.Cancelled);
                            yieldInstruction.Cleanup();
                        }
                        else if (m_State == ELoreCoroutineState.Poked)
                        {
                            m_State = ELoreCoroutineState.None;
                            yieldInstruction.OnStateChange(ELoreCoroutineState.Poked);
                        }

                        if (yieldInstruction.ResumeCoroutine())
                        {
                            m_Paused = false;
                            if (m_State != ELoreCoroutineState.Cancelled)
                                yieldInstruction.Cleanup();
                        }
                        else
                        {
                            m_Paused = true;
                        }
                    }
                }

                //move coroutine
                try
                {
                    if (!m_Paused && !coroutine.MoveNext())
                    {
                        m_State = ELoreCoroutineState.Finished;
                        yield break;
                    }
                }
                catch (System.Exception e)
                {
                    DebugHelper.LogError(e.Message);
                    m_Exception = e;
                    yield break;
                }

                //cancel routine
                if (m_State == ELoreCoroutineState.Cancelled)
                {
                    m_Exception = new CoroutineCancelledException();
                    yield break;
                }

                yield return coroutine.Current;
            }
        }
    }


    public abstract class LoreYieldInstruction
    {
        public bool wasFired
        {
            get { return _wasFired; }
            protected set { _wasFired = value; }
        }
        private bool _wasFired = false;

        public abstract bool ResumeCoroutine();
        public virtual void OnStateChange(ELoreCoroutineState state) { }
        public virtual void Cleanup() { }
    }

    public class YieldAnd : LoreYieldInstruction
    {
        private LoreYieldInstruction lhs;
        private LoreYieldInstruction rhs;

        public YieldAnd(LoreYieldInstruction _lhs, LoreYieldInstruction _rhs)
        {
            lhs = _lhs;
            rhs = _rhs;
        }

        public override bool ResumeCoroutine()
        {
            if (lhs.ResumeCoroutine() && rhs.ResumeCoroutine())
                this.wasFired = true;
            return this.wasFired;
        }

        public override void Cleanup()
        {
            lhs.Cleanup();
            rhs.Cleanup();
        }

        public override void OnStateChange(ELoreCoroutineState state)
        {
            lhs.OnStateChange(state);
            rhs.OnStateChange(state);
        }
    }

    public class YieldOr : LoreYieldInstruction
    {
        private LoreYieldInstruction lhs;
        private LoreYieldInstruction rhs;

        public YieldOr(LoreYieldInstruction _lhs, LoreYieldInstruction _rhs)
        {
            lhs = _lhs;
            rhs = _rhs;
        }

        public override bool ResumeCoroutine()
        {
            if (lhs.ResumeCoroutine() || rhs.ResumeCoroutine())
                this.wasFired = true;
            return this.wasFired;
        }

        public override void Cleanup()
        {
            lhs.Cleanup();
            rhs.Cleanup();
        }

        public override void OnStateChange(ELoreCoroutineState state)
        {
            lhs.OnStateChange(state);
            rhs.OnStateChange(state);
        }
    }

    public class YieldNot : LoreYieldInstruction
    {
        private LoreYieldInstruction lhs;

        public YieldNot(LoreYieldInstruction _lhs)
        {
            lhs = _lhs;
        }

        public override bool ResumeCoroutine()
        {
            if (!lhs.ResumeCoroutine())
                this.wasFired = true;
            return this.wasFired;
        }

        public override void Cleanup()
        {
            lhs.Cleanup();
        }

        public override void OnStateChange(ELoreCoroutineState state)
        {
            lhs.OnStateChange(state);
        }
    }

    public class WaitForTimer : LoreYieldInstruction
    {
        private float timeStarted;
        private float seconds;

        public WaitForTimer(float _seconds)
        {
            timeStarted = Time.time;
            seconds = _seconds;
        }

        public void Reset()
        {
            this.wasFired = true;
            timeStarted = Time.time;
        }

        public override bool ResumeCoroutine()
        {
            if ((Time.time - timeStarted) > seconds)
                this.wasFired = true;
            return this.wasFired;
        }
    }

    public class WaitForEvent<T> : LoreYieldInstruction where T : ILoreEvent
    {
        public T eventData;

        private EventManager m_EventManager;
        private bool m_Registered;


        public WaitForEvent(EventManager _evntManager)
        {
            m_EventManager = _evntManager;
            m_Registered = false;

            this.eventData = default(T);
        }

        private void OnT(ILoreEvent arg0)
        {
            this.wasFired = true;
            eventData = (T)arg0;
        }

        public void Reset()
        {
            this.wasFired = false;
            this.eventData = default(T);
        }

        public override void Cleanup()
        {
            if (!GameManager.isQuitting)
            {
                m_EventManager.UnregisterListener(typeof(T), OnT);
                m_Registered = false;
            }
        }

        public override bool ResumeCoroutine()
        {
            if (!m_Registered)
            {
                m_EventManager.RegisterListener(typeof(T), OnT);
                m_Registered = true;
            }

            return this.wasFired;
        }
    }

    public class WaitForStateChange : LoreYieldInstruction
    {
        public ELoreCoroutineState state { get; private set; }

        public override void OnStateChange(ELoreCoroutineState state)
        {
            this.state = state;
            this.wasFired = true;
        }

        public void Reset()
        {
            this.wasFired = false;
            this.state = ELoreCoroutineState.None;
        }

        public override bool ResumeCoroutine()
        {
            return this.wasFired;
        }
    }

    public class WaitForCSharpEvent : LoreYieldInstruction
    {
        public WaitForCSharpEvent()
        {
        }

        public void OnCSharpEvent()
        {
            this.wasFired = true;
        }

        public void Reset()
        {
            this.wasFired = false;
        }

        public override bool ResumeCoroutine()
        {
            return this.wasFired;
        }
    }

    public class WaitForCSharpEvent<T> : LoreYieldInstruction
    {
        public T arg0;

        public WaitForCSharpEvent()
        {
            this.arg0 = default(T);
        }

        public void OnCSharpEvent(T arg0)
        {
            this.wasFired = true;
            this.arg0 = arg0;
        }

        public void Reset()
        {
            this.wasFired = false;
            this.arg0 = default(T);
        }

        public override bool ResumeCoroutine()
        {
            return this.wasFired;
        }
    }

    public class WaitForCSharpEvent<T0, T1> : LoreYieldInstruction
    {
        public T0 arg0;
        public T1 arg1;

        public WaitForCSharpEvent()
        {
            this.arg0 = default(T0);
            this.arg1 = default(T1);
        }

        public void OnCSharpEvent(T0 arg0, T1 arg1)
        {
            this.wasFired = true;
            this.arg0 = arg0;
            this.arg1 = arg1;
        }

        public void Reset()
        {
            this.wasFired = false;
            this.arg0 = default(T0);
            this.arg1 = default(T1);
        }

        public override bool ResumeCoroutine()
        {
            return this.wasFired;
        }
    }

    public class WaitForCoroutine : LoreYieldInstruction
    {
        private IEnumerator m_Routine;

        public WaitForCoroutine(IEnumerator routine)
        {
            m_Routine = routine;
        }

        public override bool ResumeCoroutine()
        {
            wasFired = !m_Routine.MoveNext();
            return wasFired;
        }
    }


    public class CoroutineCancelledException : System.Exception
    {
        public CoroutineCancelledException() : base("Coroutine was cancelled") { }
    }
}
