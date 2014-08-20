using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using SimpleCritters;
using SimpleCritters.Environment;
using SimpleCritters.Critters;
using SimpleCritters.Neurons;
using SimpleCritters.Helpers;
using System.Xml.Serialization;
using System.IO;
using SimpleCritters.Common;

namespace Simulator
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Simulator : Microsoft.Xna.Framework.Game
    {
        const int envSize = 128;
        const int offboardWidth = 64;
        const int drawScale = 6;
        const string layer1Name = "layer1";
        const string layer2Name = "layer2";
        const int critterCount = 20;
        const bool pauseOnEndOfRun = false;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        static Texture2D whiteDot;

        List<Critter> critters;
        Habitat habitat;

        SpriteFont hudFont;

        int fps;
        int frameCount;
        double drawTime;
        double updateTime;
        double drawTimeElapsed;
        double updateTimeElapsed;
        TimeSpan frameCountStart = TimeSpan.Zero;

        KeyboardState previousKeyboardState = default(KeyboardState);
        
        //Runs
        static readonly TimeSpan maxRuntime = TimeSpan.FromSeconds(2.5);
        TimeSpan currentRuntime = new TimeSpan();

        //Stats
        List<float> stats = new List<float>();

        //Paused
        bool paused = false;

        bool skipToEnd = false;

        public Simulator()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.SynchronizeWithVerticalRetrace = false;
            Content.RootDirectory = "Content";
            graphics.PreferredBackBufferHeight = envSize * drawScale;
            graphics.PreferredBackBufferWidth = (envSize + offboardWidth) * drawScale;
            graphics.ApplyChanges();
        }

        protected override void Initialize()
        {
            base.Initialize(); 

            ResetWorld();

            //string folder = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments);
            //string filename = Path.Combine(folder, "MyCritter.xml");
            //XmlSerializer serializer = new XmlSerializer(typeof(Critter.Template));
            //TextWriter tw = new StreamWriter(filename);
            //serializer.Serialize(tw, template);
            //tw.Dispose();
            //TextReader tr = new StreamReader(filename);
            //var oldOutput = serializer.Deserialize(tr) as Critter.Template;
        }
        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            whiteDot = Content.Load<Texture2D>("WhiteDot");
            hudFont = Content.Load<SpriteFont>("CourierNew");
        }
        protected override void UnloadContent()
        {
        }
        protected override void Update(GameTime gameTime)
        {
            var startUpdateTime = DateTime.Now;

            {// Check keyboard input
                //ToDo Move keyboard stuff to a nice isolated area
                KeyboardState keyboardState = Keyboard.GetState();

                // Allows the game to exit
                if (keyboardState.IsKeyDown(Keys.Escape))
                    this.Exit();

                //ToDo Create an abstraction for "onRelease" events
                if (previousKeyboardState.IsKeyDown(Keys.Space) && keyboardState.IsKeyUp(Keys.Space)) //Space is released
                    paused = !paused; //Toggle pause

                previousKeyboardState = keyboardState;
            }

            if (!paused)
            {
                //Update critters
                foreach (var critter in critters)
                {
                    critter.Think(10000);
                    critter.Impel(gameTime.ElapsedGameTime);

                    //World wrap
                    critter.XPos = SimpleCritters.Helpers.MathHelper.Wrap(critter.XPos, envSize);
                    critter.YPos = SimpleCritters.Helpers.MathHelper.Wrap(critter.YPos, envSize);
                }

                //Collect stats
                for (int i = 0; i < critterCount; i++)
                {
                    var critter = critters[i];
                    var layer = habitat.Layers[layer1Name];
                    int ix = SimpleCritters.Helpers.MathHelper.Wrap((int)critter.XPos, layer.Width);
                    int iy = SimpleCritters.Helpers.MathHelper.Wrap((int)critter.YPos, layer.Height);
                    float signal = layer[ix, iy];
                    stats[i] += signal;
                }

                //Watch the run
                currentRuntime += gameTime.ElapsedGameTime;
                if (skipToEnd || currentRuntime > maxRuntime)
                {
                    if (pauseOnEndOfRun && !skipToEnd)
                    {
                        paused = true;
                        skipToEnd = true;
                    }
                    else
                    {
                        skipToEnd = false;
                        OnEndOfRun();
                    }
                }
            }

            //Update frame counts
            frameCount++;
            updateTimeElapsed += (DateTime.Now - startUpdateTime).TotalMilliseconds;
            var timeSinceLastReset = (gameTime.TotalGameTime - frameCountStart);
            if (timeSinceLastReset.TotalSeconds >= 1d)
            {
                frameCountStart = gameTime.TotalGameTime;
                fps = (int)(frameCount / timeSinceLastReset.TotalSeconds);
                frameCount = 0;
                drawTime = drawTimeElapsed;
                updateTime = updateTimeElapsed;
                updateTimeElapsed = 0d;
                drawTimeElapsed = 0d;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            var startDrawTime = DateTime.Now;
            GraphicsDevice.Clear(Color.White);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            {
                //Draw layers
                foreach (var layerAndName in habitat.Layers)
                {
                    var name = layerAndName.Key;
                    var layer = layerAndName.Value;

                    switch (name)
                    {
                        case layer1Name:
                            DrawLayer(layer, spriteBatch, Color.Black, scale: drawScale, alphaScaler: 1f);
                            break;
                        case layer2Name:
                            //DrawLayer(layer, spriteBatch, Color.DarkGreen, scale: drawScale);
                            break;
                        default:
                            break;
                    }
                }

                //Draw critters
                foreach (var critter in critters)
                    DrawCritter(critter, spriteBatch, Color.Red, drawScale);

                //Draw brain
                DrawBrain(critters.First(), spriteBatch, new Rectangle(envSize * drawScale/*push to the offboard*/, 0, offboardWidth * drawScale, envSize * drawScale));

                //Draw hud
                DrawHud(spriteBatch, Color.DarkOrange);
            }
            spriteBatch.End();

            drawTimeElapsed += (DateTime.Now - startDrawTime).TotalMilliseconds;

            base.Draw(gameTime);
        }

        private void DrawHud(SpriteBatch sb, Color color)
        {
            const int lineOffset = 20;

            sb.DrawString(hudFont, "FPS: " + fps, new Vector2(0, 0 * lineOffset), color);
            sb.DrawString(hudFont, "Drawtime: " + (drawTime).ToString("F") + "ms", new Vector2(0, 1 * lineOffset), color);
            sb.DrawString(hudFont, "Updatetime: " + (updateTime).ToString("F") + "ms", new Vector2(0, 2 * lineOffset), color);

            if (paused)
                sb.DrawString(hudFont, "<PAUSED> (space to continue)", new Vector2(0, 3 * lineOffset), color);
        }
        private static void DrawCritter(Critter critter, SpriteBatch sb, Color color, int scale = 1)
        {
            Rectangle destination = new Rectangle((int)(critter.XPos * scale),
                                                  (int)(critter.YPos * scale),
                                                  scale, scale);

            sb.Draw(whiteDot, destination, color);
        }
        private static void DrawLayer(Layer layer, SpriteBatch sb, Color color, int scale = 1, Point? offset = null, float alphaScaler = 0.5f)
        {
            if (offset == null)
                offset = Point.Zero;

            for (int x = 0; x < layer.Width; x++)
            {
                for (int y = 0; y < layer.Height; y++)
                {
                    float shade = layer[x, y];
                    if (shade == 0f)
                        continue;

                    Rectangle destination = new Rectangle(x * scale + offset.Value.X,
                                                          y * scale + offset.Value.Y,
                                                          scale, scale);

                    int alpha = (int)(255f * alphaScaler * shade);
                    color = new Color(color.R, color.G, color.B, alpha);
                    sb.Draw(whiteDot, destination, color);
                }
            }
        }
        
        private static void DrawBrain(Critter critter, SpriteBatch sb, Rectangle destination)
        {//ToDo Draw sensors and actuators
            var width = destination.Width;
            var height = destination.Height;

            var brain = critter.Brain as NeuralNetwork;

            var neurons = brain.Neurons;

            var neuronCount = neurons.Count;

            int horizontalDivisions = 1;
            int neuronWidth;
            while(true)
            {
                neuronWidth = width / horizontalDivisions;

                var verticalDivisions = (int)Math.Ceiling((double)neuronCount / (double)horizontalDivisions);

                if (verticalDivisions * neuronWidth < height)
                    break;

                horizontalDivisions++;
            }

            int neuronIndex = 0;
            foreach (var neuronAndId in neurons)
            {
                var xIndex = neuronIndex % horizontalDivisions;
                var yIndex = neuronIndex / horizontalDivisions;

                var neuronRectangle = new Rectangle(destination.X + xIndex * neuronWidth, destination.Y + yIndex * neuronWidth, neuronWidth, neuronWidth);

                var neuron = neuronAndId.Value;

                DrawNeuron(neuron, sb, neuronRectangle);

                neuronIndex++;
            }
        }
        private static void DrawNeuron(INeuron neuron, SpriteBatch sb, Rectangle destination)
        {
            var width = destination.Width;

            var asSimple = neuron as SimpleNeuron;
            if (asSimple != null)
            {
                var state = asSimple.State;

                var barWidth = (int) (Math.Min(Math.Abs(state), 1f) * width);
                var bar = new Rectangle(destination.X, destination.Y, barWidth, destination.Height);

                sb.Draw(whiteDot, destination, Color.White);
                sb.Draw(whiteDot, bar, Color.Black);
            }

            //ToDo
            var asAnd = neuron as AndNeuron;
            var asXor = neuron as XorNeuron;
            var asInverter = neuron as InverterNeuron;
            var asDiode = neuron as DiodeNeuron;
        }

        private void OnEndOfRun()
        {
            //Judge critters
            var rankedCritters = critters.OrderByDescending(c => stats[critters.IndexOf(c)]);
            
            //Cull, clone, and mutate critters
            var survivors = rankedCritters.Take(critters.Count / 2);
            var actuatorTemplates = new IEnumerable<IActuatorTemplate>[]
                {
                     new MoveActuator.Template(1f, 0f).Copy(3)
                   , new MoveActuator.Template(0f, 1f).Copy(3)
                   , new MoveActuator.Template(-1f, 0f).Copy(3)
                   , new MoveActuator.Template(0f, -1f).Copy(3)
                }.Flatten().ToArray();
            var sensorTemplates = new IEnumerable<ISensorTemplate>[]
                {
                     new LayerSensor.Template(layer1Name, 0, 0).Copy(1)
                   , new LayerSensor.Template(layer1Name, 1, 0).Copy(1)
                   , new LayerSensor.Template(layer1Name, 0, 1).Copy(1)
                   , new LayerSensor.Template(layer1Name, -1, 0).Copy(1)
                   , new LayerSensor.Template(layer1Name, 0, -1).Copy(1)
                   , new LayerSensor.Template(layer1Name, 0, 0, true).Copy(1)
                   , new LayerSensor.Template(layer1Name, 1, 0, true).Copy(1)
                   , new LayerSensor.Template(layer1Name, 0, 1, true).Copy(1)
                   , new LayerSensor.Template(layer1Name, -1, 0, true).Copy(1)
                   , new LayerSensor.Template(layer1Name, 0, -1, true).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 0, 0).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 1, 0).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 0, 1).Copy(1)
                   //, new LayerSensor.Template(layer2Name, -1, 0).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 0, -1).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 0, 0, true).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 1, 0, true).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 0, 1, true).Copy(1)
                   //, new LayerSensor.Template(layer2Name, -1, 0, true).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 0, -1, true).Copy(1)
                }.Flatten().ToArray();
            var critterMutatorSettings = new Critter.Mutator.Settings
            {
                ActuatorChoices = actuatorTemplates,
                SensorChoices = sensorTemplates,
                ChanceToReplaceActuator = 0.0d, //0.1d,
                ChanceToReplaceSensor = 0.0d, //0.1d,
            };
            var neuronTemplates = new IEnumerable<INeuronTemplate>[]
            {
                // new AndNeuron.Template(2).Copy(5)
                //, new AndNeuron.Template(3).Copy(5)
                //, new XorNeuron.Template().Copy(5)
                //, new InverterNeuron.Template().Copy(3)
                //, new DiodeNeuron.Template(true).Copy(3)
            }.Flatten().ToArray();
            var brainMutatorSettings = new NeuralNetwork.Mutator.Settings
            {
                NeuronChoices = neuronTemplates,
                ChanceToReplaceConnection = 0.1d,
                ChanceToReplaceNeuron = 0.0d
            };
            var mutatorSettings = new MutatorSettingsProvider(critterMutatorSettings, brainMutatorSettings);

            var resourceTemplates = new Dictionary<string, IResourceTemplate>();
            resourceTemplates[layer1Name] = new Layer.BlobFillTemplate(brushCount: 3);
            resourceTemplates[layer2Name] = new Layer.BlobFillTemplate(brushCount: 3);
            var newHabitat = new Habitat(envSize, envSize, resourceTemplates.ToArray());

            var newCritters = new List<Critter>();
            foreach (var survivor in survivors)
            {
                newCritters.Add(survivor.BuildMutatingTemplate(mutatorSettings).Create(newHabitat));
                newCritters.Add(survivor.BuildTemplate().Create(newHabitat));
            }

            foreach (var critter in newCritters)
            {
                critter.XPos = envSize / 2;
                critter.YPos = envSize / 2;
            }

            critters.Clear();
            critters.AddRange(newCritters);
            habitat = newHabitat;
            stats.Clear();
            stats.AddRange((0f).Copy(critterCount));

            currentRuntime = new TimeSpan();
        }
        private void ResetWorld()
        {
            var resourceTemplates = new Dictionary<string, IResourceTemplate>();
            resourceTemplates[layer1Name] = new Layer.BlobFillTemplate(brushCount: 3);
            resourceTemplates[layer2Name] = new Layer.BlobFillTemplate(brushCount: 3);
            habitat = new Habitat(envSize, envSize, resourceTemplates.ToArray());

            critters = new List<Critter>();
            Critter.Settings settings = new Critter.Settings()
            {
                ImpetusDecayMultiplier = .0f,
                MaxSpeed = 30f,
                InitialConnectionsPerOutputPort = 3
            };
            for (int i = 0; i < critterCount; i++)
            {
                var sensorTemplates = new IEnumerable<ISensorTemplate>[]
                {
                     new LayerSensor.Template(layer1Name, 0, 0).Copy(3)
                   , new LayerSensor.Template(layer1Name, 1, 0).Copy(3)
                   , new LayerSensor.Template(layer1Name, 0, 1).Copy(3)
                   , new LayerSensor.Template(layer1Name, -1, 0).Copy(3)
                   , new LayerSensor.Template(layer1Name, 0, -1).Copy(3)
                   , new LayerSensor.Template(layer1Name, 0, 0, true).Copy(3)
                   , new LayerSensor.Template(layer1Name, 1, 0, true).Copy(3)
                   , new LayerSensor.Template(layer1Name, 0, 1, true).Copy(3)
                   , new LayerSensor.Template(layer1Name, -1, 0, true).Copy(3)
                   , new LayerSensor.Template(layer1Name, 0, -1, true).Copy(3)
                   //, new LayerSensor.Template(layer2Name, 0, 0).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 1, 0).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 0, 1).Copy(1)
                   //, new LayerSensor.Template(layer2Name, -1, 0).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 0, -1).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 0, 0, true).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 1, 0, true).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 0, 1, true).Copy(1)
                   //, new LayerSensor.Template(layer2Name, -1, 0, true).Copy(1)
                   //, new LayerSensor.Template(layer2Name, 0, -1, true).Copy(1)
                }.Flatten();
                var actuatorTemplates = new IEnumerable<IActuatorTemplate>[]
                {
                     new MoveActuator.Template(1f, 0f).Copy(5)
                   , new MoveActuator.Template(0f, 1f).Copy(5)
                   , new MoveActuator.Template(-1f, 0f).Copy(5)
                   , new MoveActuator.Template(0f, -1f).Copy(5)
                }.Flatten();
                var brainSettings = new NeuralNetwork.Settings//ToDo(DZ): This is hard to understand
                {
                    InputCount = sensorTemplates.Count(),
                    OutputCount = actuatorTemplates.Count(),
                    InitialConnectionsPerOutput = 3
                };
                var brainTemplate = new NeuralNetwork.Template
                (
                    brainSettings
                    //Neurons
                    , new SimpleNeuron.Template().Copy(25)
                   //, new AndNeuron.Template(2).Copy(25)
                   //, new AndNeuron.Template(3).Copy(25)
                   //, new XorNeuron.Template().Copy(5)
                   //, new InverterNeuron.Template().Copy(3)
                   //, new DiodeNeuron.Template(true).Copy(3)
                );

                var critterTemplate = new Critter.Template(settings, brainTemplate, sensorTemplates, actuatorTemplates);

                var critter = critterTemplate.Create(habitat);

                critter.XPos = envSize / 2;
                critter.YPos = envSize / 2;

                critters.Add(critter);
            }

            stats.Clear();
            stats.AddRange((0f).Copy(critterCount));
        }
    }
}
