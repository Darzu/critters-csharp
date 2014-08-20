using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using SimpleCritters.Common;
using SimpleCritters.Environment;
using SimpleCritters.Helpers;

namespace SimpleCritters.Critters
{
    public interface ISensor
    {
        float Sense();

        ISensorTemplate BuildTemplate();
    }
    public interface ISensorTemplate
    {
        ISensor Create(Critter host);
    }

    public class LayerSensor : ISensor
    {
        readonly int xOffset;
        readonly int yOffset;
        readonly string layerName;
        readonly bool senseInvert;

        readonly Critter host;

        LayerSensor(Critter host, string layerName, int xOffset, int yOffset, bool senseInvert)
        {
            this.xOffset = xOffset;
            this.yOffset = yOffset;
            this.layerName = layerName;
            this.host = host;
            this.senseInvert = senseInvert;
        }

        public float Sense()
        {
            Layer layer = host.Habitat.Layers[layerName];

            Contract.Assert(layer != null);

            int ix = MathHelper.Wrap((int)host.XPos + xOffset, layer.Width);
            int iy = MathHelper.Wrap((int)host.YPos + yOffset, layer.Height);
            var signal = layer[ix, iy];

            if (senseInvert)
                return 1f - signal;
            else
                return signal;
        }

        public ISensorTemplate BuildTemplate()
        {
            return new Template(layerName, xOffset, yOffset, senseInvert);
        }

        public class Template : ISensorTemplate
        {
            public readonly int XOffset;
            public readonly int YOffset;
            public readonly string LayerName;
            public readonly bool SenseInvert;

            public Template(string layerName, int xOffset = 0, int yOffset = 0, bool senseInvert = false)
            {
                this.LayerName = layerName;
                this.XOffset = xOffset;
                this.YOffset = yOffset;
                this.SenseInvert = senseInvert;
            }

            public ISensor Create(Critter host)
            {
                return new LayerSensor(host, LayerName, XOffset, YOffset, SenseInvert);
            }
        }
    }
}
