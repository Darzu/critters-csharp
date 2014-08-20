using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using SimpleCritters.Helpers;
using System.Diagnostics.Contracts;
using System.Diagnostics;
using SimpleCritters.Common;

namespace SimpleCritters.Neurons
{
    public interface INeuralNetwork : INeuron
    {
        void Think(int maxSignalsProcessed = 0);

        new INeuralNetworkTemplate BuildTemplate();

        INeuralNetworkTemplate BuildMutatingTemplate(IMutatorSettingsProvider mutatorSettingsProvider);
    }
    public interface INeuralNetworkTemplate : INeuronTemplate
    {
        new INeuralNetwork Create(int id, NeuronAction onOutput);
    }
    public class NeuralNetwork : INeuralNetwork
    {
        public struct Settings
        {
            public int InputCount;
            public int OutputCount;
            public int InitialConnectionsPerOutput;
        }

        public int InputCount { get { return settings.InputCount; } }
        public int OutputCount { get { return settings.OutputCount; } }
        public int Id { get; set; }

        readonly Settings settings;

        readonly IDictionary<NeuronIndex, NeuronIndex[]> neuronMap;
        /// <summary>
        /// Maps neuron oldOutput port to connected neuron oldInput ports
        /// </summary>
        public readonly IReadOnlyDictionary<NeuronIndex, NeuronIndex[]> NeuronMap;

        readonly IDictionary<int, INeuron> neurons;
        public readonly IReadOnlyDictionary<int, INeuron> Neurons;

        public Queue<NeuronSignal> signalQueue;

        public NeuralNetwork
        (   int id,
            NeuronAction onOutput,
            Settings settings,
            params IEnumerable<INeuronTemplate>[] neuronTemplates //ToDo (DZ) Get rid of nested array
        )
        {
            //Basics
            this.settings = settings;
            signalQueue = new Queue<NeuronSignal>();
            Output += onOutput;

            //Determine our neuron count
            var templates = neuronTemplates.Flatten().ToList();
            int neuronCount = templates.Count;

            //Instantiate the neuronTemplates
            neurons = new Dictionary<int, INeuron>();
            for (int i = 0, nextId = 0; i < neuronCount; i++, nextId++)
            {
                if (nextId == Id)
                    nextId++;

                INeuron neuron = neurons[nextId] = templates[i].Create(nextId, Input);
            }

            //Set internal accessible NeuronMap
            Neurons = neurons.AsReadOnly();

            //Make lists of all oldInput & oldOutput ports
            List<NeuronIndex> inputPorts = new List<NeuronIndex>(neurons.Count * 2/*just a guess*/);
            List<NeuronIndex> outputPorts = new List<NeuronIndex>(neurons.Count * 2);
            foreach (var i in neurons.Keys)
            {
                INeuron neuron = neurons[i];
                for (int j = 0; j < neuron.InputCount; j++)
                {
                    inputPorts.Add(NeuronIndex.Create(i, j));
                }
                for (int j = 0; j < neuron.OutputCount; j++)
                {
                    outputPorts.Add(NeuronIndex.Create(i, j));
                }
            }
            for (int j = 0; j < settings.OutputCount; j++)
            {
                inputPorts.Add(NeuronIndex.Create(id, j));
            }
            for (int j = 0; j < settings.InputCount; j++)
            {
                outputPorts.Add(NeuronIndex.Create(id, j));
            }

            //Wire up oldOutput ports to oldInput ports
            neuronMap = new Dictionary<NeuronIndex, NeuronIndex[]>();

            for (int i = 0; i < outputPorts.Count; i++)
            {
                NeuronIndex[] targetPorts = new NeuronIndex[settings.InitialConnectionsPerOutput];
                for (int j = 0; j < settings.InitialConnectionsPerOutput; j++)
                {
                    targetPorts[j] = inputPorts[MathHelper.Random.Next(inputPorts.Count)];
                }

                neuronMap[outputPorts[i]] = targetPorts;
            }

            //Set internal accessible NeuronMap
            NeuronMap = neuronMap.AsReadOnly();
        }
        NeuralNetwork
        (
            int id,
            NeuronAction onOutput,
            Settings settings,
            IDictionary<NeuronIndex, NeuronIndex[]> neuronMap,
            params IEnumerable<KeyValuePair<int, INeuronTemplate>>[] neuronTemplatesWithIds
        )
        {
            //Basics
            this.settings = settings;
            signalQueue = new Queue<NeuronSignal>();
            Output += onOutput;

            //Setup our neurons
            var neuronTemplatesWithIds_flattened = neuronTemplatesWithIds.Flatten();
            this.neurons = new Dictionary<int, INeuron>();
            foreach (var pair in neuronTemplatesWithIds_flattened)
            {
                neurons[pair.Key] = pair.Value.Create(pair.Key, Input);
            }
            Neurons = neurons.AsReadOnly();

            //Save our neuron map
            this.neuronMap = neuronMap;
            NeuronMap = neuronMap.AsReadOnly();
        }

        public event NeuronAction Output;
        public void Input(NeuronSignal signal)
        {
            signalQueue.Enqueue(signal);
        }

        void ProcessSignal(NeuronSignal signal)
        {
            //Get all connected neuron input ports
            NeuronIndex[] inputs;
            bool isMapped = neuronMap.TryGetValue(signal.Index, out inputs);
            if (!isMapped)
                return;

            //Split our output between the inputs
            signal.Signal /= inputs.Length;

            //Find & notify neurons of input
            foreach (var input in inputs)
            {
                var signalToInput = NeuronSignal.Create(signal.Signal, input);

                //Is the signal going to an output port on this neural network?
                if (input.Id == Id)
                {
                    Output(signalToInput);
                }
                else
                {
                    //Find our destination neuron
                    var neuron = neurons[input.Id];

                    //Notify neuron of signal
                    neuron.Input(signalToInput);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxSignalsProcessed">Leave 0 to process all</param>
        public void Think(int maxSignalsProcessed = 0)
        {
            int signalsProcessed = 0;

            while ((signalsProcessed < maxSignalsProcessed || maxSignalsProcessed == 0) &&
                   signalQueue.Count > 0)
            {
                var signal = signalQueue.Dequeue();
                ProcessSignal(signal);
                signalsProcessed++;
            }
        }

        public void Reset()
        {
            foreach (var neuron in neurons.Values)
            {
                neuron.Reset();
            }

            signalQueue.Clear();
        }

        INeuronTemplate INeuron.BuildTemplate()
        {
            return (this as INeuralNetwork).BuildTemplate();
        }
        public INeuralNetworkTemplate BuildTemplate()
        {
            var neuronTemplatesWithIds = new Dictionary<int, INeuronTemplate>();
            foreach (var pair in neurons)
            {
                neuronTemplatesWithIds.Add(pair.Key, pair.Value.BuildTemplate());
            }

            return new ExactTemplate
            (
                Id,
                settings,
                NeuronMap,
                neuronTemplatesWithIds.AsReadOnly()
            );
        }

        public INeuralNetworkTemplate BuildMutatingTemplate(IMutatorSettingsProvider mutatorSettingsProvider)
        {
            var mutatorSettings = mutatorSettingsProvider.GetSettings<Mutator.Settings>();

            var mutator = new Mutator(mutatorSettings);

            return new MutatingTemplate(this, mutator);
        }

        static class Helper
        {
            public static void ChangeId
            (
                int sourceId,
                int newId,
                IDictionary<NeuronIndex, NeuronIndex[]> sourceNeuronMap,
                IDictionary<int, INeuronTemplate> sourceNeuronTemplatesWithIds,
                out IDictionary<NeuronIndex, NeuronIndex[]> newNeuronMap,
                out IDictionary<int, INeuronTemplate> newNeuronTemplatesWithIds
            )
            {
                bool newIdIsDifferent = newId != sourceId;
                bool newIdConflicts = newIdIsDifferent && sourceNeuronTemplatesWithIds.ContainsKey(newId);

                //Find a new id for the conflicting neuron
                int newIdForConflictingId = -1;
                if (newIdConflicts)
                {
                    var neuronCount = sourceNeuronTemplatesWithIds.Count();
                    for (int i = 0; i < neuronCount + 1; i++)
                    {
                        if (i != newId && !sourceNeuronTemplatesWithIds.ContainsKey(i))
                        {
                            newIdForConflictingId = i;
                            break;
                        }
                    }
                }

                //Copy neuronMap
                newNeuronMap = new Dictionary<NeuronIndex, NeuronIndex[]>();
                foreach (var pair in sourceNeuronMap)
                {
                    NeuronIndex oldOutput = pair.Key;

                    //Copy output index
                    NeuronIndex newOutput = oldOutput;
                    if (newIdIsDifferent && newOutput.Id == sourceId)
                        newOutput.Id = newId;
                    else if (newIdConflicts && newOutput.Id == newId)
                        newOutput.Id = newIdForConflictingId;

                    NeuronIndex[] oldInputs = pair.Value;

                    //Copy input indices
                    NeuronIndex[] newInputs = new NeuronIndex[oldInputs.Length];
                    for (int i = 0; i < oldInputs.Length; i++)
                    {
                        NeuronIndex oldInput = oldInputs[i];

                        //Copy input index
                        NeuronIndex newInput = oldInput;
                        if (newIdIsDifferent && newInput.Id == sourceId)
                            newInput.Id = newId;
                        else if (newIdConflicts && newInput.Id == newId)
                            newInput.Id = newIdForConflictingId;

                        newInputs[i] = newInput;
                    }

                    //Save
                    newNeuronMap.Add(newOutput, newInputs);
                }

                //Copy neurons
                newNeuronTemplatesWithIds = new Dictionary<int, INeuronTemplate>();
                foreach (var pair in sourceNeuronTemplatesWithIds)
                {
                    int oldNeuronId = pair.Key;

                    //Copy neuron id
                    int newNeuronId = oldNeuronId;
                    if (newIdIsDifferent && newNeuronId == sourceId)
                        newNeuronId = newId;
                    else if (newIdConflicts && newNeuronId == newId)
                        newNeuronId = newIdForConflictingId;

                    //Save
                    newNeuronTemplatesWithIds.Add(newNeuronId, pair.Value);
                }
            }
        }

        class ExactTemplate : INeuralNetworkTemplate
        {
            readonly int oldId;
            readonly Settings settings;
            readonly IReadOnlyDictionary<NeuronIndex, NeuronIndex[]> neuronMap;
            readonly IReadOnlyDictionary<int, INeuronTemplate> neuronTemplatesWithIds;

            public ExactTemplate
            (
                int id,
                Settings settings,
                IReadOnlyDictionary<NeuronIndex, NeuronIndex[]> neuronMap,
                IReadOnlyDictionary<int, INeuronTemplate> neuronTemplatesWithIds
            )
            {
                this.oldId = id;
                this.settings = settings;
                this.neuronMap = neuronMap;
                this.neuronTemplatesWithIds = neuronTemplatesWithIds;
            }

            INeuron INeuronTemplate.Create(int id, NeuronAction onOutput)
            {
                return (this as INeuralNetworkTemplate).Create(id, onOutput);
            }
            public INeuralNetwork Create(int id, NeuronAction onOutput)
            {
                IDictionary<NeuronIndex, NeuronIndex[]> newNeuronMap;
                IDictionary<int, INeuronTemplate> newNeuronTemplatesWithIds;

                Helper.ChangeId(oldId, id, neuronMap, neuronTemplatesWithIds, out newNeuronMap, out newNeuronTemplatesWithIds);

                return new NeuralNetwork(id, onOutput, settings, newNeuronMap, newNeuronTemplatesWithIds);
            }

        }
        public class Template : INeuralNetworkTemplate
        {
            readonly Settings settings;
            readonly IEnumerable<INeuronTemplate> neuronTemplates;

            public Template(Settings settings, params IEnumerable<INeuronTemplate>[] neuronTemplates)
            {
                this.settings = settings;
                this.neuronTemplates = neuronTemplates.Flatten();
            }

            INeuron INeuronTemplate.Create(int id, NeuronAction onOutput)
            {
                return (this as INeuralNetworkTemplate).Create(id, onOutput);
            }
            public INeuralNetwork Create(int id, NeuronAction onOutput)
            {
                return new NeuralNetwork(id, onOutput, settings, neuronTemplates);
            }
        }

        public class MutatingTemplate : INeuralNetworkTemplate
        {
            readonly Mutator mutator;
            readonly NeuralNetwork sourceNeuralNetwork;

            public MutatingTemplate(NeuralNetwork sourceNeuralNetwork, Mutator mutator)
            {
                this.sourceNeuralNetwork = sourceNeuralNetwork;
                this.mutator = mutator;
            }

            INeuron INeuronTemplate.Create(int id, NeuronAction onOutput)
            {
                return (this as INeuralNetworkTemplate).Create(id, onOutput);
            }
            public INeuralNetwork Create(int id, NeuronAction onOutput)
            {
                return mutator.CreateMutant(sourceNeuralNetwork, id, onOutput);
            }
        }

        public class Mutator
        {
            public struct Settings
            {
                public double ChanceToReplaceConnection;
                public double ChanceToReplaceNeuron;
                public INeuronTemplate[] NeuronChoices;
            }

            readonly Settings settings;

            public Mutator(Settings settings)
            {
                this.settings = settings;
            }

            public INeuralNetwork CreateMutant(NeuralNetwork source, int newId, NeuronAction newOnOutput)
            {
                //Mutate neurons
                var mutantNeurons = new List<INeuron>();
                foreach (var neuron in source.Neurons.Values)
                {
                    //Mutate?
                    var randVal1 = MathHelper.Random.NextDouble();
                    if (randVal1 < settings.ChanceToReplaceNeuron)
                    {
                        //Make mutant
                        var randVal2 = MathHelper.Random.Next(0, settings.NeuronChoices.Length - 1);
                        var mutantTemplate = settings.NeuronChoices[randVal2];
                        mutantNeurons.Add(mutantTemplate.Create(neuron.Id, null));
                    }
                    else
                    {
                        mutantNeurons.Add(neuron.BuildTemplate().Create(neuron.Id, null));
                    }
                }

                //Mutate & validate connection
                var mutantNeuronMap = new Dictionary<NeuronIndex, NeuronIndex[]>();
                foreach (var mapPair in source.NeuronMap)
                {
                    NeuronIndex outputIndex = mapPair.Key;
                    INeuron outputNeuron = outputIndex.Id == source.Id ? source : source.Neurons[outputIndex.Id];

                    //Is the index valid?
                    if (outputIndex.Port >= outputNeuron.OutputCount)
                        continue;

                    IList<NeuronIndex> newInputs = new List<NeuronIndex>();
                    for (int i = 0; i < mapPair.Value.Length; i++)
                    {
                        NeuronIndex inputIndex = mapPair.Value[i];
                        INeuron inputNeuron = inputIndex.Id == source.Id ? source : source.Neurons[inputIndex.Id];

                        //Is the index valid?
                        if (inputIndex.Port >= inputNeuron.InputCount)
                            continue;

                        //Mutate?
                        var randVal1 = MathHelper.Random.NextDouble();
                        if (randVal1 < settings.ChanceToReplaceConnection / 2d)
                        {
                            //Remove connection
                            continue;
                        }
                        else if (randVal1 < settings.ChanceToReplaceConnection)
                        {
                            //Create new connection
                            int randVal2 = MathHelper.Random.Next(0, mutantNeurons.Count - 1);
                            int newInputId = mutantNeurons[randVal2].Id;
                            int randVal3 = MathHelper.Random.Next(0, mutantNeurons[newInputId].InputCount - 1);
                            int newInputPort = randVal3;
                            newInputs.Add(NeuronIndex.Create(newInputId, newInputPort));
                        }
                        else
                        {
                            newInputs.Add(inputIndex);
                        }
                    }

                    if (newInputs.Count > 0)
                        mutantNeuronMap.Add(outputIndex, newInputs.ToArray());
                }

                //Build neuron templates
                var mutantNeuronTemplatesWithIds = new Dictionary<int, INeuronTemplate>();
                foreach (var neuron in mutantNeurons)
                {
                    mutantNeuronTemplatesWithIds.Add(neuron.Id, neuron.BuildTemplate());
                }

                //Change the neural network id
                IDictionary<int, INeuronTemplate> reidentifiedMutantNeuronTemplatesWithIds;
                IDictionary<NeuronIndex, NeuronIndex[]> reidentifiedMutantNeuronMap;
                Helper.ChangeId
                (
                    source.Id,
                    newId,
                    mutantNeuronMap,
                    mutantNeuronTemplatesWithIds,
                    out reidentifiedMutantNeuronMap,
                    out reidentifiedMutantNeuronTemplatesWithIds
                );

                return new NeuralNetwork(newId, newOnOutput, source.settings, reidentifiedMutantNeuronMap, reidentifiedMutantNeuronTemplatesWithIds);
            }
        }
    }
}
