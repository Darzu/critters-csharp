using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleCritters.Critters;
using SimpleCritters.Neurons;

namespace SimpleCritters.Common
{
    public interface IMutatorSettingsProvider
    {
        T GetSettings<T>();
    }

    public class MutatorSettingsProvider : IMutatorSettingsProvider
    {
        Critter.Mutator.Settings critterMutatorSettings;
        NeuralNetwork.Mutator.Settings brainMutatorSettings;

        public MutatorSettingsProvider(Critter.Mutator.Settings critterMutatorSettings, NeuralNetwork.Mutator.Settings brainMutatorSettings)
        {
            this.critterMutatorSettings = critterMutatorSettings;
            this.brainMutatorSettings = brainMutatorSettings;
        }

        public T GetSettings<T>()
        {
            var settingsType = typeof(T);

            if (settingsType == typeof(Critter.Mutator.Settings))
            {
                return (T)(object)critterMutatorSettings;
            }
            else if (settingsType == typeof(NeuralNetwork.Mutator.Settings))
            {
                return (T)(object)brainMutatorSettings;
            }

            throw new NotImplementedException();
        }
    }
}
