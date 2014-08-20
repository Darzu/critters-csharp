using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleCritters.Common;
using SimpleCritters.Environment;
using SimpleCritters.Helpers;
using SimpleCritters.Neurons;

namespace SimpleCritters.Critters
{
    public interface ICritterTemplate
    {
        Critter Create(Habitat habitat);
    }

    public class Critter
    {
        public struct Settings
        {
            public int InitialConnectionsPerOutputPort;
            public float MaxSpeed;
            public float ImpetusDecayMultiplier;
        }

        public float XPos { get; set; }
        public float YPos { get; set; }
        public float XImpetus { get; set; }
        public float YImpetus { get; set; }

        public readonly Habitat Habitat;

        readonly Settings settings;

        readonly INeuralNetwork brain;
        readonly ISensor[] sensors;
        readonly IActuator[] actuators;

        public INeuralNetwork Brain
        {
            get
            {
                return brain;
            }
        }

        public Critter(Settings settings
                     , Habitat habitat
                     , INeuralNetworkTemplate brainTemplate
                     , IList<ISensorTemplate> sensorTemplates
                     , IList<IActuatorTemplate> actuatorTemplates)
        {
            //Save our settings
            this.settings = settings;

            //Link to our habitat
            Habitat = habitat;

            //Setup our sensors
            sensors = new ISensor[sensorTemplates.Count];
            for (int i = 0; i < sensors.Length; i++)
            {
                sensors[i] = sensorTemplates[i].Create(this);
            }

            //Setup our actuators
            actuators = new IActuator[actuatorTemplates.Count];
            for (int i = 0; i < actuators.Length; i++)
            {
                actuators[i] = actuatorTemplates[i].Create(this);
            }

            //Setup our brain
            brain = brainTemplate.Create(0, OnBrainOutput);
        }

        void OnBrainOutput(NeuronSignal signal)
        {
            actuators[signal.Port].Actuate(signal.Signal);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maxThoughtsToThink">Leave 0 to think until no more neurons are firing</param>
        public void Think(int maxThoughtsToThink = 0)
        {
            for (int i = 0; i < sensors.Length; i++)
            {
                float signal = sensors[i].Sense();
                if (signal > 0f)
                {
                    NeuronSignal sensorySignal = NeuronSignal.Create(signal, brain.Id, i);
                    brain.Input(sensorySignal);
                }
            }

            brain.Think(maxThoughtsToThink);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeSpan">The time period the criter is allowed to act for</param>
        public void Impel(TimeSpan timeSpan)
        {
            float distance = settings.MaxSpeed * (float)timeSpan.TotalSeconds;

            //X
            float xDistMoved = (Math.Abs(XImpetus) > distance         ?
                               (XImpetus >= 0 ? distance : -distance) :
                               (XImpetus));

            XPos += xDistMoved;
            XImpetus -= xDistMoved;

            XImpetus *= settings.ImpetusDecayMultiplier;

            //Y
            float yDistMoved = (Math.Abs(YImpetus) > distance ?
                               (YImpetus >= 0 ? distance : -distance) :
                               (YImpetus));

            YPos += yDistMoved;
            YImpetus -= yDistMoved;

            YImpetus *= settings.ImpetusDecayMultiplier;
        }

        public void Reset()
        {
            brain.Reset();
            XImpetus = 0f;
            YImpetus = 0f;
            XPos = 0f;
            YPos = 0f;
        }

        public Critter Copy(Habitat newHabitat)
        {
            //ToDo(DZ): What is the difference between this and BuildTemplate().Create(newHabitat) ?

            //Copy settings
            Settings newSettings = this.settings;

            //Copy brain
            INeuralNetworkTemplate newBrainTemplate = brain.BuildTemplate();

            //Copy sensors
            var newSensorTemplates = new List<ISensorTemplate>();
            for (int i = 0; i < sensors.Length; i++)
                newSensorTemplates.Add(sensors[i].BuildTemplate());

            //Copy actuators
            var newActuatorTemplates = new List<IActuatorTemplate>();
            for (int i = 0; i < actuators.Length; i++)
                newActuatorTemplates.Add(actuators[i].BuildTemplate());

            return new Critter(newSettings, newHabitat, newBrainTemplate, newSensorTemplates, newActuatorTemplates);
        }

        public ICritterTemplate BuildTemplate()
        {
            return new Template
            (
                settings,
                brain.BuildTemplate(),
                sensors.Select(s => s.BuildTemplate()),
                actuators.Select(a => a.BuildTemplate())
            );
        }

        public ICritterTemplate BuildMutatingTemplate(IMutatorSettingsProvider mutatorSettingsProvider)
        {
            var mutatorSettings = mutatorSettingsProvider.GetSettings<Mutator.Settings>();
            var mutatingBrainTemplate = brain.BuildMutatingTemplate(mutatorSettingsProvider);

            var mutator = new Mutator(mutatorSettings, mutatingBrainTemplate);

            return new MutatingTemplate(this, mutator);
        }

        public class Template : ICritterTemplate
        {
            readonly List<ISensorTemplate> sensorTemplates;
            readonly List<IActuatorTemplate> actuatorTemplates;
            readonly INeuralNetworkTemplate brainTemplate;
            readonly Settings Settings;

            public Template(Settings settings
                          , INeuralNetworkTemplate brainTemplate
                          , IEnumerable<ISensorTemplate> sensorTemplates
                          , IEnumerable<IActuatorTemplate> actuatorTemplates)
            {
                this.Settings = settings;
                this.sensorTemplates = sensorTemplates.ToList();
                this.actuatorTemplates = actuatorTemplates.ToList();
                this.brainTemplate = brainTemplate;
            }

            public Critter Create(Habitat habitat)
            {
                return new Critter(Settings, habitat, brainTemplate, sensorTemplates, actuatorTemplates);
            }
        }

        public class MutatingTemplate : ICritterTemplate
        {
            readonly Mutator mutator;
            readonly Critter sourceCritter;

            public MutatingTemplate(Critter sourceCritter, Mutator mutator)
            {
                this.sourceCritter = sourceCritter;
                this.mutator = mutator;
            }

            public Critter Create(Habitat habitat)
            {
                return mutator.CreateMutant(sourceCritter, habitat);
            }
        }

        public class Mutator
        {
            public struct Settings
            {
                public double ChanceToReplaceSensor;
                public ISensorTemplate[] SensorChoices;
                public double ChanceToReplaceActuator;
                public IActuatorTemplate[] ActuatorChoices;
            }

            readonly Settings settings;
            readonly INeuralNetworkTemplate mutantBrainTemplate;

            public Mutator(Settings settings, INeuralNetworkTemplate mutantBrainTemplate)
            {
                this.settings = settings;
                this.mutantBrainTemplate = mutantBrainTemplate;
            }

            public Critter CreateMutant(Critter source, Habitat newHabitat)
            {
                //Mutate sensors
                var mutantSensorTemplates = new List<ISensorTemplate>();
                for (int i = 0; i < source.sensors.Length; i++)
                {
                    var randVal1 = MathHelper.Random.NextDouble();
                    if (settings.ChanceToReplaceSensor < randVal1)
                    {
                        int newTemplateId = (int)(randVal1 * settings.SensorChoices.Length);
                        mutantSensorTemplates.Add(settings.SensorChoices[newTemplateId]);
                    }
                    else
                    {
                        mutantSensorTemplates.Add(source.sensors[i].BuildTemplate());
                    }
                }

                //Mutate actuators
                var mutantActuatorTemplates = new List<IActuatorTemplate>();
                for (int i = 0; i < source.actuators.Length; i++)
                {
                    var randVal1 = MathHelper.Random.NextDouble();
                    if (settings.ChanceToReplaceActuator < randVal1)
                    {
                        int newTemplateId = (int)(randVal1 * settings.ActuatorChoices.Length);
                        mutantActuatorTemplates.Add(settings.ActuatorChoices[newTemplateId]);
                    }
                    else
                    {
                        mutantActuatorTemplates.Add(source.actuators[i].BuildTemplate());
                    }
                }

                return new Critter(source.settings, newHabitat, mutantBrainTemplate, mutantSensorTemplates, mutantActuatorTemplates);
            }
            public IEnumerable<Critter> CreateMutants(Critter source, Habitat newHabitat, int count)
            {
                for (int i = 0; i < count; i++)
                    yield return CreateMutant(source, newHabitat);
            }
        }
    }
}
