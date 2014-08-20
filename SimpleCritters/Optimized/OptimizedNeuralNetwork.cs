using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleCritters.Neurons;

namespace SimpleCritters.Optimized
{
    public unsafe class OptimizedNeuralNetwork : INeuralNetwork
    {
        //Simple
        const int simple_inPorts = 1;
        const int simple_outPorts = 1;
        readonly int simplesCount;

        //And w/ 2 input ports
        const int and2_inPorts = 2;
        const int and2_outPorts = 1;
        readonly int and2sCount;

        //And w/ 3 input ports
        const int and3_inPorts = 3;
        const int and3_outPorts = 1;
        readonly int and3sCount;

        //Xor
        const int xor_inPorts = 2;
        const int xor_outPorts = 1;
        readonly int xorsCount;

        //Inverter
        const int inverter_inPorts = 1;
        const int inverter_outPorts = 1;
        readonly int invertersCount;

        //Diode+
        const int diodeP_inPorts = 1;
        const int diodeP_outPorts = 1;
        readonly int diodePsCount;

        //Diode-
        const int diodeN_inPorts = 1;
        const int diodeN_outPorts = 1;
        readonly int diodeNsCount;

        float[] state;
        NeuronIndex[][] outputs;
        Queue<NeuronSignal> signalQueue;

        OptimizedNeuralNetwork(NeuralNetwork source)
        {
            IList<SimpleNeuron> simples = new List<SimpleNeuron>();
            IList<AndNeuron> and2s = new List<AndNeuron>();
            IList<AndNeuron> and3s = new List<AndNeuron>();
            IList<XorNeuron> xors = new List<XorNeuron>();
            IList<InverterNeuron> inverters = new List<InverterNeuron>();
            IList<DiodeNeuron> diodePs = new List<DiodeNeuron>();
            IList<DiodeNeuron> diodeNs = new List<DiodeNeuron>();

            //Sort neurons
            foreach (var neuron in source.Neurons.Values)
            {
                var asSimple = neuron as SimpleNeuron;
                if (asSimple != null)
                    simples.Add(asSimple);

                var asAnd = neuron as AndNeuron;
                if (asAnd != null)
                {
                    if (asAnd.InputCount == 2)
                        and2s.Add(asAnd);
                    else if (asAnd.InputCount == 3)
                        and3s.Add(asAnd);
                    else
                        throw new NotImplementedException("Optimized nerual networks not implemented for AndNeurons with more than 3 input ports");
                }

                var asXor = neuron as XorNeuron;
                if (asXor != null)
                    xors.Add(asXor);

                var asInverter = neuron as InverterNeuron;
                if (asInverter != null)
                    inverters.Add(asInverter);

                var asDiode = neuron as DiodeNeuron;
                if (asDiode != null)
                {
                    if (asDiode.Sign)
                        diodePs.Add(asDiode);
                    else
                        diodeNs.Add(asDiode);
                }
            }

            throw new NotImplementedException();
        }

        public static OptimizedNeuralNetwork Create(NeuralNetwork source)
        {
            return new OptimizedNeuralNetwork(source);
        }

        void Think(int maxSignalsProcessed = 0)
        {
            throw new NotImplementedException();
        }
        INeuralNetworkTemplate BuildTemplate(int newId)
        {
            throw new NotImplementedException();
        }

        void INeuralNetwork.Think(int maxSignalsProcessed = 0)
        {
            throw new NotImplementedException();
        }

        public int InputCount
        {
            get { throw new NotImplementedException(); }
        }

        public int OutputCount
        {
            get { throw new NotImplementedException(); }
        }

        public int Id
        {
            get { throw new NotImplementedException(); }
        }

        public event NeuronAction Output;

        public void Input(NeuronSignal signal)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }


        public INeuronTemplate BuildTemplate()
        {
            throw new NotImplementedException();
        }


        INeuralNetworkTemplate INeuralNetwork.BuildTemplate()
        {
            throw new NotImplementedException();
        }


        public INeuralNetworkTemplate BuildMutatingTemplate(Common.IMutatorSettingsProvider mutatorSettingsProvider)
        {
            throw new NotImplementedException();
        }
    }
}
