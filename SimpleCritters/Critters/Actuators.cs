using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SimpleCritters.Common;

namespace SimpleCritters.Critters
{
    public interface IActuator
    {
        void Actuate(float signal);

        IActuatorTemplate BuildTemplate();
    }
    public interface IActuatorTemplate
    {
        IActuator Create(Critter host);
    }

    public class MoveActuator : IActuator
    {
        readonly float xMultiplier;
        readonly float yMultiplier;

        readonly Critter host;

        MoveActuator(Critter host, float xMultiplier, float yMultiplier)
        {
            this.host = host;
            this.xMultiplier = xMultiplier;
            this.yMultiplier = yMultiplier;
        }

        public void Actuate(float signal)
        {
            host.XImpetus += signal * xMultiplier;
            host.YImpetus += signal * yMultiplier;
        }

        public IActuatorTemplate BuildTemplate()
        {
            return new Template(xMultiplier, yMultiplier);
        }

        public class Template : IActuatorTemplate
        {
            public readonly float XMultiplier;
            public readonly float YMultiplier;

            public Template(float xMultiplier, float yMultiplier)
            {
                this.XMultiplier = xMultiplier;
                this.YMultiplier = yMultiplier;
            }

            public IActuator Create(Critter host)
            {
                return new MoveActuator(host, XMultiplier, YMultiplier);
            }
        }
    }
}
