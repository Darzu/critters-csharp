using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleCritters.Environment;
using SimpleCritters.Critters;
using SimpleCritters.Helpers;

namespace SimpleCritters.Incubators
{
    public class Incubator
    {//ToDo(DZ): Finish incubator; use incubator in sim; serialie trials; guid?;
        /*
         * Gather settings
         * 
         */
        public struct Settings
        {
            public readonly int MaxThoughtsPerSecond;
        }

        readonly Settings settings;
        readonly IHabitatTemplate habitatTemplate;
        readonly IEnumerable<ICritterTemplate> critterTemplates;

        readonly IReadOnlyDictionary<Type, object> settingsCache;

        public Trial CurrentTrial { get; set; }

        public Incubator
        (
            Settings settings,
            IHabitatTemplate habitatTemplate,
            IEnumerable<ICritterTemplate> critterTemplates
        )
        {
            this.settings = settings;
            this.habitatTemplate = habitatTemplate;
            this.critterTemplates = critterTemplates;
        }

        public class Trial
        {
            public readonly Habitat Habitat;
            public readonly Critter[] Critters;
        }

        public class Builder
        {
            readonly IDictionary<Type, object> settingsCache;
        }

        //mutator
        //trial
        //tracker
        //judge
    }

}
