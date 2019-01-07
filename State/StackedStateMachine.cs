using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KS.Foundation
{
    public class StackedStateMachine<T> //where T : class
    {
        private Action<T, T> OnControllerStatusChanged;

        public StackedStateMachine(Action<T, T> ControllerStatusChangedAction)
        {
            OnControllerStatusChanged = ControllerStatusChangedAction;
            Enum.GetValues(typeof(T));
        }

        protected Stack<T> m_StatusStack = new Stack<T>(8);
        protected T m_ControllerStatus = default(T);
        public virtual T ControllerStatus
        {
            get
            {
                return m_ControllerStatus;
            }            
            set
            {
                T oldStatus = m_ControllerStatus;
                m_StatusStack.Push(value);
                m_ControllerStatus = value;
                OnControllerStatusChanged(oldStatus, value);
            }
        }

        public void ClearStatusStack()
        {
            m_StatusStack.Clear();
            ControllerStatus = default(T);
        }

        public virtual void RestoreControllerStatus()
        {
            if (m_StatusStack.Count > 0)
                m_StatusStack.Pop();

            if (m_StatusStack.Count > 0)
                ControllerStatus = m_StatusStack.Pop();
            else
                ControllerStatus = default(T);
        }
        
        private int iSuspendCounter = 0;
        public virtual void Suspend()
        {
            unchecked
            {
                iSuspendCounter++;
            }
        }

        public virtual void Resume(bool reset = false)
        {
            if (reset || iSuspendCounter < 0)
                iSuspendCounter = 0;
            else if (iSuspendCounter > 0)
                iSuspendCounter--;
        }

        public bool IsSuspended
        {
            get
            {
                return iSuspendCounter > 0;
            }
        }
    }
}
