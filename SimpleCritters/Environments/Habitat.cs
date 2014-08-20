using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleCritters.Common;
using SimpleCritters.Helpers;

namespace SimpleCritters.Environment
{
    public interface IResource
    {
    }
    public interface IResourceTemplate
    {
        IResource Create(Habitat host);
    }

    public interface IHabitatTemplate
    {
        Habitat Create();
    }

    public class Habitat
    {
        public readonly int Width;
        public readonly int Height;

        public readonly IReadOnlyDictionary<string, Layer> Layers;
        readonly IDictionary<string, Layer> layers;

        public Habitat(int width, int height, params KeyValuePair<string, IResourceTemplate>[] resourceTemplatesWithNames)
        {
            Width = width;
            Height = height;

            layers = new Dictionary<string, Layer>();
            Layers = layers.AsReadOnly();

            //Resolve and instantiate our resource templates
            foreach (var pair in resourceTemplatesWithNames)
            {
                //Layers
                var asLayerTemplate = pair.Value as ILayerTemplate;
                if (asLayerTemplate != null)
                {
                    layers[pair.Key] = asLayerTemplate.Create(this);
                }
            }
        }

        public class Template : IHabitatTemplate
        {
            readonly int width;
            readonly int height;
            readonly KeyValuePair<string, IResourceTemplate>[] resourceTemplatesWithNames;

            public Template(int width, int height, params KeyValuePair<string, IResourceTemplate>[] resourceTemplatesWithNames)
            {
                this.width = width;
                this.height = height;
                this.resourceTemplatesWithNames = resourceTemplatesWithNames;
            }

            public Habitat Create()
            {
                return new Habitat(width, height, resourceTemplatesWithNames);
            }
        }
    }
}
