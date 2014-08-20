using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Runtime.InteropServices;

namespace SimpleCritters.Neurons
{
    public class SimpleNeuron : NeuronBase
    {
        float state = 0f;

        public float State { get { return state; } }

        public SimpleNeuron(int id, NeuronAction onOutput) : base(id, onOutput, 1, 1) { }

        public override void Input(NeuronSignal signal)
        {
            state += signal.Signal;

            if (state > 1f)
            {
                if (Output != null)
                    Output(NeuronSignal.Create(state, Id));

                state = 0f;
            }
        }

        public override INeuronTemplate BuildTemplate()
        {
            return new Template();
        }

        public override void Reset()
        {
            state = 0f;
        }

        public class Template : INeuronTemplate
        {
            public INeuron Create(int id, NeuronAction onOutput)
            {
                return new SimpleNeuron(id, onOutput);
            }
        }
    }
    public class AndNeuron : NeuronBase
    {
        float[] state;

        public IEnumerable<float> State { get { return state.ToArray(); } }

        public AndNeuron(int id, NeuronAction onOutput, int inputCount)
            : base(id, onOutput, inputCount, 1) 
        {
            state = new float[inputCount];
        }

        public override void Input(NeuronSignal signal)
        {
            state[signal.Port] += signal.Signal;

            float outputSignal = 0f;
            for (int i = 0; i < state.Length; i++)
            {
                if ((state[i] > 0f != signal.Signal > 0f) ||
                    (state[i] < 1f && state[i] > -1f))
                {
                    return;
                }
                else
                {
                    outputSignal += state[i];
                }
            }

            if (Output != null)
                Output(NeuronSignal.Create(outputSignal, Id));

            state = new float[state.Length];
        }

        public override INeuronTemplate BuildTemplate()
        {
            return new Template(state.Length);
        }

        public override void Reset()
        {
            state = new float[InputCount];
        }

        public class Template : INeuronTemplate
        {
            public readonly int InputCount;

            public Template(int inputCount)
            {
                this.InputCount = inputCount;
            }

            public INeuron Create(int id, NeuronAction onOutput)
            {
                return new AndNeuron(id, onOutput, InputCount);
            }
        }
    }
    public class XorNeuron : NeuronBase
    {
        float state0 = 0f;
        float state1 = 0f;

        public IEnumerable<float> State { get { return new[] {state0, state1} ; } }

        public XorNeuron(int id, NeuronAction onOutput) : base(id, onOutput, 2, 1) { }

        public override void Input(NeuronSignal signal)
        {
            if (signal.Port == 0)
                state0 += signal.Signal;
            else
                state1 += signal.Signal;

            if ((state0 < -1f && 1f < state1) ||
                (state1 < -1f && 1f < state0))
            {
                if (Output != null)
                    Output(NeuronSignal.Create(state0 + -state1, Id));

                state0 = state1 = 0f;
            }
        }

        public override INeuronTemplate BuildTemplate()
        {
            return new Template();
        }

        public override void Reset()
        {
            state0 = state1 = 0f;
        }

        public class Template : INeuronTemplate
        {
            public INeuron Create(int id, NeuronAction onOutput)
            {
                return new XorNeuron(id, onOutput);
            }
        }
    }
    public class InverterNeuron : NeuronBase
    {
        float state;
        
        public float State { get { return state; } }

        public InverterNeuron(int id, NeuronAction onOutput) : base(id, onOutput, 1, 1) { }

        public override void Input(NeuronSignal signal)
        {
            state += signal.Signal;

            if (state < -1f || 1f < state)
            {
                if (Output != null)
                    Output(NeuronSignal.Create(-state, Id));

                state = 0f;
            }
        }

        public override INeuronTemplate BuildTemplate()
        {
            return new Template();
        }

        public override void Reset()
        {
            state = 0f;
        }

        public class Template : INeuronTemplate
        {
            public INeuron Create(int id, NeuronAction onOutput)
            {
                return new InverterNeuron(id, onOutput);
            }
        }
    }
    public class DiodeNeuron : NeuronBase
    {
        public readonly bool Sign;

        float state;
        public float State { get { return state; } }

        public DiodeNeuron(int id, NeuronAction onOutput, bool sign)
            : base(id, onOutput, 1, 1)
        {
            Sign = sign;
        }

        public override void Input(NeuronSignal signal)
        {
            if (signal.Signal > 0f != Sign)
                return;

            state += signal.Signal;

            if (state < -1f || 1f < state)
            {
                if (Output != null)
                    Output(NeuronSignal.Create(state, Id));

                state = 0f;
            }
        }

        public override INeuronTemplate BuildTemplate()
        {
            return new Template(Sign);
        }

        public override void Reset()
        {
            state = 0f;
        }

        public class Template : INeuronTemplate
        {
            public readonly bool Sign;

            public Template(bool sign)
            {
                this.Sign = sign;
            }

            public INeuron Create(int id, NeuronAction onOutput)
            {
                return new DiodeNeuron(id, onOutput, Sign);
            }
        }
    }
}
