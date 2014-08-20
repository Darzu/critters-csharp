using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using SimpleCritters.Common;

namespace SimpleCritters.Neurons
{
    public delegate void NeuronAction(NeuronSignal signal);
    public interface INeuron
    {
        int InputCount { get; }
        int OutputCount { get; }
        int Id { get; }

        void Input(NeuronSignal signal);

        INeuronTemplate BuildTemplate();

        void Reset();
    }
    public abstract class NeuronBase : INeuron
    {
        public int InputCount { get; protected set; }
        public int OutputCount { get; protected set; }
        public int Id { get; protected set; }

        protected NeuronBase(int id, NeuronAction onOutput, int inputCount, int outputCount)
        {
            Id = id;
            InputCount = inputCount;
            OutputCount = outputCount;
            Output = onOutput;
        }

        protected NeuronAction Output;
        public abstract void Input(NeuronSignal signal);

        public abstract INeuronTemplate BuildTemplate();

        public abstract void Reset();
    }
    public interface INeuronTemplate
    {
        INeuron Create(int id, NeuronAction onOutput);
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct NeuronIndex
    {
        [FieldOffset(0)]
        public ulong Index;

        [FieldOffset(0)]
        public int Id;

        [FieldOffset(4)]
        public int Port;

        public static NeuronIndex Create(int id = 0, int port = 0)
        {
            return new NeuronIndex { Id = id, Port = port };
        }

        public override int GetHashCode()
        {
            return Id ^ Port;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct NeuronSignal
    {
        [FieldOffset(0)]
        public float Signal;

        [FieldOffset(4)]
        public NeuronIndex Index;

        [FieldOffset(4)]
        public int Id;

        [FieldOffset(8)]
        public int Port;

        public static NeuronSignal Create(float signal = 0f, int id = 0, int port = 0)
        {
            return new NeuronSignal { Signal = signal, Id = id, Port = port };
        }
        public static NeuronSignal Create(float signal, NeuronIndex index)
        {
            return new NeuronSignal { Signal = signal, Index = index };
        }
    }
}
